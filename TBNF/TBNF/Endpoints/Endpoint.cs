/*
 * MIT License
 * 
 * Copyright (c) 2021 Renondedju
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace TBNF
{
    using System;
    using System.Threading;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;
    using System.Net.NetworkInformation;
    
    using SystemMessages;

    /// <summary>
    ///     Network endpoint
    ///     This class allows for communication over the network via Tcp
    /// </summary>
    public abstract class Endpoint : IDisposable
    {
        #region Constructors

        protected Endpoint(MessageHandler handler)
        {
            m_last_message_activity = DateTime.Now;

            MessageHandler     = handler;
            MessageQueue       = new ConcurrentQueue<Message>();
            MessageQueueLatch  = new CountdownLatch();
            GlobalCancellation = new CancellationTokenSource();
        }

        #endregion

        #region Members

        private DateTime m_last_message_activity;
        
        protected PhysicalAddress   MacAddress;
        protected EndpointTcpSocket CurrentSocket;

        protected readonly ConcurrentQueue<Message> MessageQueue;
        protected readonly MessageHandler           MessageHandler;
        protected readonly CountdownLatch           MessageQueueLatch;
        protected readonly CancellationTokenSource  GlobalCancellation;

        #endregion

        #region Properties

        /// <summary>
        ///     Relative amount of time after which if no message is received or sent,
        ///     the client will perform a connection test to see if we are still connected and, if needed, attempt a reconnection
        ///
        ///     Default is 15 seconds. Lower values will feel more reactive,
        ///     but will force the client to send more data over the network.
        /// </summary>
        public TimeSpan InactivityCheckInterval = TimeSpan.FromSeconds(15);
        
        /// <summary>
        ///     Maximum amount of time the client is gonna wait for a (re)connection
        ///     If the timeout expires, the reconnection process will be started from the beginning again
        ///
        ///     Default is 10 seconds.
        /// </summary>
        public TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        ///     Unique identifier of the client on the network
        /// </summary>
        public byte NetworkIdentifier { get; protected set; }
        
        #endregion

        #region Exposed Methods

        /// <summary>
        ///     Forces a disconnection for testing purposes
        /// </summary>
        public void ForceDisconnection()
        {
            CurrentSocket.Client.Close();
        }
        
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources
        /// </summary>
        public void Dispose()
        {
            GlobalCancellation?.Cancel();

            CurrentSocket     ?.Dispose();
            GlobalCancellation?.Dispose();
        }

        /// <summary>
        ///     Queues a messages for upload to the distant endpoint
        /// </summary>
        /// <param name="message">Message to queue</param>
        public void EnqueueMessage(Message message)
        {
            MessageQueue.Enqueue(message);
            MessageQueueLatch.Increment();
        }

        /// <summary>
        ///     This is called to attempt a new connection using a new client
        ///     If the connection succeeded with that new client, the old one will be cleaned up and be replaced by this one
        /// </summary>
        /// <param name="new_client">New client to connect to</param>
        /// <param name="cancellation_token">Cancellation token</param>
        protected async Task HandleEndConnection(TcpClient new_client, CancellationToken cancellation_token)
        {
            if (!new_client.Connected || !await HandleHandshake(new_client, cancellation_token))
            {
                OnConnectionFailure?.Invoke(this);
                return;
            }
            
            m_last_message_activity = DateTime.Now;

            OnConnectionSuccess?.Invoke(this);
            SwitchTcpClient(new_client);
        }
        
        /// <summary>
        ///     Handles a connection handshake
        /// </summary>
        /// <param name="client">Client to handshake with</param>
        /// <param name="cancellation_token">Cancellation token (timeout)</param>
        /// <returns>True if the handshake succeeded, false otherwise</returns>
        protected abstract Task<bool> HandleHandshake(TcpClient client, CancellationToken cancellation_token);
        
        #endregion

        #region Methods

        /// <summary>
        ///     Switches the current tcp client and cleans up the previous one
        /// </summary>
        /// <param name="new_client">New client to switch to</param>
        private void SwitchTcpClient(TcpClient new_client)
        {
            // Switching sockets
            CurrentSocket?.Dispose();
            CurrentSocket = new EndpointTcpSocket(new_client, GlobalCancellation.Token);

            // Starting sending and receiving tasks in the background
            // Those tasks should automatically get cleaned up when switching to a new client
            Task send = SendingRoutine  (new_client, CurrentSocket.CancellationSource.Token);
            Task read = ReceivingRoutine(new_client, CurrentSocket.CancellationSource.Token);
        }
        
        /// <summary>
        ///     Continuously attempts to send to a tcp client until it gets closed or until the task gets cancelled
        ///     If needed, this routine performs inactivity checks if needed
        /// </summary>
        /// <param name="tcp_client">Tcp Client</param>
        /// <param name="cancellation_token">Cancellation token</param>
        private async Task SendingRoutine(TcpClient tcp_client, CancellationToken cancellation_token)
        {
            // While a cancellation has not been requested
            while (!cancellation_token.IsCancellationRequested && tcp_client.Connected)
            {
                try
                {
                    // Time left before we have to do a new inactivity check
                    TimeSpan timeout = InactivityCheckInterval - (DateTime.Now - m_last_message_activity);

                    // Waiting for a message to send or a timeout
                    if (await MessageQueueLatch.WaitHandle.WaitOneAsync(timeout, cancellation_token))
                    {
                        // A new message has been enqueued, we need to try to send it without dequeuing it for now
                        // in case of a failure
                        MessageQueue.TryPeek(out Message message);

                        // If sending the message failed, that means the underlying client got killed and we need to
                        // swap it so we are just gonna exit the loop
                        if (!await SendMessage(tcp_client, message, cancellation_token))
                            continue;

                        // If the message has successfully been sent, we need to dequeue it and update our signal
                        MessageQueue.TryDequeue(out message);
                        MessageQueueLatch.Decrement();
                    }

                    // If the previous piece of code timed out, that means that we need to check if any message has been received in
                    // the meanwhile, and if not, we need to perform a connectivity check to 
                    else if (InactivityCheckInterval < DateTime.Now - m_last_message_activity)
                        await SendMessage(tcp_client, new InactivityCheckMessage(), cancellation_token);
                }

                catch (TaskCanceledException) { } // Ignoring cancellations 
                catch (Exception e)
                {
                    Console.WriteLine($"Endpoint exception {e}");
                }
            }
            
            OnDisconnection?.Invoke(this);
        }
        
        /// <summary>
        ///     Continuously attempts to read from a tcp client until it gets closed or until the task gets cancelled
        /// </summary>
        /// <param name="tcp_client">Tcp client</param>
        /// <param name="cancellation_token">Cancellation token</param>
        private async Task ReceivingRoutine(TcpClient tcp_client, CancellationToken cancellation_token)
        {
            while (!cancellation_token.IsCancellationRequested && tcp_client.Connected)
                MessageHandler.HandleMessage(this, await ReadMessage(tcp_client, cancellation_token));
        }

        /// <summary>
        ///     Sends a message to that endpoint
        /// </summary>
        /// <remarks>This method is asynchronous</remarks>
        /// <param name="tcp_client">Tcp client to send to</param>
        /// <param name="message">Message instance to send</param>
        /// <param name="cancellation_token">Cancellation token</param>
        /// <returns>True if the message has been sent, false otherwise</returns>
        private async Task<bool> SendMessage(TcpClient tcp_client, Message message, CancellationToken cancellation_token)
        {
            if (!await tcp_client.SendMessage(message, cancellation_token))
                return false;
            
            m_last_message_activity = DateTime.Now;
            OnRawMessageSent?.Invoke(this, message);

            return true;
        }

        /// <summary>
        ///     Reads a message from that endpoint
        ///     This method will only return if a message has been received
        /// </summary>
        /// <remarks>This method is asynchronous</remarks>
        /// <param name="tcp_client">Tcp client to read from</param>
        /// <param name="cancellation_token">Cancellation token</param>
        private async Task<Message> ReadMessage(TcpClient tcp_client, CancellationToken cancellation_token)
        {
            // If a cancellation has been requested, the returned data will be null and thus the built message will be null too
            Message message = await tcp_client.ReadMessage(cancellation_token);

            if (message == null)
                return null;
            
            m_last_message_activity = DateTime.Now;
            OnRawMessageReceived?.Invoke(this, message);

            return message;
        }

        #endregion
        
        #region Events

        /// <summary>
        ///     Called when a raw message is received
        /// </summary>
        public event Action<Endpoint, Message> OnRawMessageReceived;
        
        /// <summary>
        ///     Called when a raw message is sent
        /// </summary>
        public event Action<Endpoint, Message> OnRawMessageSent;
        
        /// <summary>
        ///     Called when a connection attempts fails
        /// </summary>
        public event Action<Endpoint> OnConnectionFailure;
        
        /// <summary>
        ///     Called when a connection attempts succeeds
        /// </summary>
        public event Action<Endpoint> OnConnectionSuccess;
        
        /// <summary>
        ///     Called when the client disconnects
        /// </summary>
        public event Action<Endpoint> OnDisconnection;

        #endregion
    }
}

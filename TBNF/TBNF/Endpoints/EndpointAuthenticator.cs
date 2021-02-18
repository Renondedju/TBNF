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
    using System.Net;
    using System.Threading;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Net.NetworkInformation;
    
    using SystemMessages;

    public class EndpointAuthenticator : IDisposable
    {
        #region Constructors

        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="handler">Message handler instance</param>
        /// <param name="listening_port">Port to listen to</param>
        public EndpointAuthenticator(MessageHandler handler, int listening_port)
        {
            m_message_handler = handler;
            m_tcp_socket      = TcpListener.Create(listening_port);
            m_end_token       = new CancellationTokenSource();
            m_clients         = new Dictionary<PhysicalAddress, RemoteEndpoint>();
        }

        #endregion

        #region Members

        private readonly Dictionary<PhysicalAddress, RemoteEndpoint> m_clients;
        private readonly CancellationTokenSource                     m_end_token;
        private readonly MessageHandler                              m_message_handler;
        private readonly TcpListener                                 m_tcp_socket;

        protected int ListenedPort => ((IPEndPoint)m_tcp_socket.LocalEndpoint).Port;
        
        public IEnumerable<Endpoint> Clients      => m_clients.Values;
        public int                   ClientsCount => m_clients.Count;

        /// <summary>
        ///     Relative amount of time after which if no message is received or sent,
        ///     the host will perform a connection test to see if we are still connected and, if needed, attempt a reconnection
        ///
        ///     Default is 20 seconds. Lower values will feel more reactive,
        ///     but will force the client to send more data over the network.
        /// </summary>
        public TimeSpan InactivityCheckInterval = TimeSpan.FromSeconds(10);
        
        /// <summary>
        ///     Maximum amount of time the client is gonna wait for a (re)connection
        ///     If the timeout expires, the reconnection process will be started from the beginning again
        ///
        ///     Default is 15 seconds.
        /// </summary>
        public TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(15);
        
        #endregion

        #region Exposed Methods

        /// <summary>
        ///     Starts the endpoint
        ///     Use the <see cref="EndpointAuthenticator.Dispose"/> method to stop the endpoint
        /// </summary>
        public virtual void Start()
        {
            m_tcp_socket.Start();

            // Starting the host main loop an another thread
            Task _ = Task.Run(HostLoop);
        }
        
        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            
            m_tcp_socket.Stop();
            
            foreach (Endpoint client in Clients)
                client.Dispose();
            
            m_end_token.Cancel();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
        
        #region Methods

        /// <summary>
        ///     Main host loop
        ///     Allows the host to wait for new incoming connections
        /// </summary>
        private async Task HostLoop()
        {
            while (!m_end_token.IsCancellationRequested)
                await HandleConnectionRequest();
        }

        /// <summary>
        ///     Attempts to communicate with the passed socket to identify it using its mac address
        /// </summary>
        /// <param name="incoming_socket">Socket to identify</param>
        /// <param name="timeout">Timeout time span</param>
        /// <param name="cancellation_token">Cancellation token</param>
        /// <returns>Identified mac address, or null if the process failed/timed out</returns>
        private static async Task<PhysicalAddress> IdentifySocket(TcpClient incoming_socket, TimeSpan timeout, CancellationToken cancellation_token)
        {
            // Creating the cancellation token
            using (CancellationTokenSource cancellation_source = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token))
            {
                cancellation_source.CancelAfter(timeout);

                // Reading the first incoming message
                IdentificationMessage identification_message = await incoming_socket.ReadMessage(cancellation_source.Token) as IdentificationMessage;

                // Checking if the message is valid and retrieving the mac address of the user
                // (A timeout would cause the ReadMessage method to return null)
                return identification_message?.MacAddress;
            }
        }

        /// <summary>
        ///     Handles any incoming connection requests
        /// </summary>
        private async Task HandleConnectionRequest()
        {
            TcpClient       socket  = await m_tcp_socket.AcceptTcpClientAsync();
            PhysicalAddress address = await IdentifySocket(socket, TimeSpan.FromSeconds(20), m_end_token.Token);

            // If the identification failed, closing the socket and stopping the process here            
            if (address == null)
            {
                socket.Close();
                return;
            }
            
            // Checking if this is a connection or reconnection attempt
            if (m_clients.ContainsKey(address))
                await m_clients[address].Reconnect(socket);
                
            else
            {
                // Connecting the new client
                RemoteEndpoint endpoint = new RemoteEndpoint(socket, address, (byte) m_clients.Count, m_message_handler)
                {
                    InactivityCheckInterval = InactivityCheckInterval,
                    ConnectionTimeout       = ConnectionTimeout
                };
                
                m_clients[address] = endpoint;
                OnNewClientRegistered?.Invoke(this, endpoint);
            }
        }

        #endregion

        #region Events

        public event Action<EndpointAuthenticator, Endpoint> OnNewClientRegistered;

        #endregion
    }
}

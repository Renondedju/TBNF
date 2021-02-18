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
    using System.Linq;
    using System.Threading;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System.Net.NetworkInformation;
    
    using SystemMessages;

    public sealed class ClientEndpoint : Endpoint
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="handler">Message handler instance</param>
        /// <param name="host_ip_address">Distant IP address to connect to</param>
        /// <param name="host_port">Distant port to connect to</param>
        /// <param name="additional_identifier">Additional identifier, used to allow multiple clients on the same machine</param>
        public ClientEndpoint(MessageHandler handler, IPAddress host_ip_address, int host_port, ushort additional_identifier = 0) : base(handler)
        {
            // Looking for every available mac address on this machine
            IEnumerable<PhysicalAddress> addresses = from   net_interface in NetworkInterface.GetAllNetworkInterfaces()
                                                     where  net_interface.NetworkInterfaceType != NetworkInterfaceType.Loopback
                                                     select net_interface.GetPhysicalAddress();

            byte[] destination      = new byte[8];
            byte[] address_bytes    = addresses.First().GetAddressBytes();
            byte[] additional_bytes = BitConverter.GetBytes(additional_identifier);

            Buffer.BlockCopy(address_bytes   , 0, destination, 0, 6);
            Buffer.BlockCopy(additional_bytes, 0, destination, 6, 2);
            
            m_host_address = host_ip_address;
            m_host_port    = host_port;
            MacAddress     = new PhysicalAddress(destination);

            // Automatic reconnection logic
            OnDisconnection     += async _ => await RequestConnection(ConnectionTimeout);
            OnConnectionFailure += async _ => await RequestConnection(ConnectionTimeout);
        }

        #region Members

        private readonly IPAddress m_host_address;
        private readonly int       m_host_port;

        #endregion

        #region Exposed Methods

        /// <summary>
        ///     Starts the client and begins an initial connection
        ///     Use the <see cref=".Dispose"/> method to stop the client
        /// </summary>
        public void Start()
        {
            Task _ = RequestConnection(ConnectionTimeout);
        }
        
        /// <summary>
        ///     Handles a connection handshake
        /// </summary>
        /// <param name="client">Client to handshake with</param>
        /// <param name="cancellation_token">Cancellation token (timeout)</param>
        /// <returns>True if the handshake succeeded, false otherwise</returns>
        protected override async Task<bool> HandleHandshake(TcpClient client, CancellationToken cancellation_token)
        {
            // Requesting a login by sending our connection information
            await client.SendMessage(new IdentificationMessage {MacAddress = MacAddress}, cancellation_token);

            // Waiting a validation from the server, if the returned message is null
            // that means that the server declined the login.
            if (!(await client.ReadMessage(cancellation_token) is LoginConfirmationMessage confirmation))
                return false;

            NetworkIdentifier = confirmation.Data.NetworkIdentifier;

            return true;
        }

        /// <summary>
        ///     Attempts to connect the client to a distant host
        /// </summary>
        /// <remarks>This method is asynchronous</remarks>
        public async Task RequestConnection(TimeSpan timeout)
        {
            if (GlobalCancellation.IsCancellationRequested)
                return;

            // Creating the timeout cancellation source
            CancellationTokenSource timeout_cancellation = CancellationTokenSource.CreateLinkedTokenSource(GlobalCancellation.Token);
            timeout_cancellation.CancelAfter(timeout);

            // Connecting to the server
            TcpClient new_client = new TcpClient();

            Task timeout_task = Task.Delay(timeout);
            Task connect_task = new_client.ConnectAsync(m_host_address, m_host_port);

            await Task.WhenAny(timeout_task, connect_task);

            // If we didn't timed out
            if (connect_task.IsCompleted)
                await HandleEndConnection(new_client, timeout_cancellation.Token);
        }

        #endregion
    }
}

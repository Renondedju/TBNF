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
    using System.Text;
    using System.Net.Sockets;

    public class DiscoverableEndpointAuthenticator : EndpointAuthenticator
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="handler">Message handler instance</param>
        /// <param name="listening_port">Port to listen to</param>
        /// <param name="endpoint_info">Endpoint info</param>
        public DiscoverableEndpointAuthenticator(MessageHandler handler, int listening_port, DiscoverableEndpointInfo endpoint_info) : base(handler, listening_port)
        {
            EndpointInfo       = endpoint_info;
            m_data_stream      = new byte[1024];
            m_discovery_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        #region Members

        public  readonly DiscoverableEndpointInfo EndpointInfo;
        private readonly byte[]                   m_data_stream;
        private readonly Socket                   m_discovery_socket;

        #endregion

        #region Exposed Metods

        /// <summary>
        ///     Starts the endpoint
        ///     Use the <see cref=".Dispose"/> method to stop the endpoint
        /// </summary>
        public override void Start()
        {
            base.Start();
            
            m_discovery_socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            m_discovery_socket.Bind(new IPEndPoint(IPAddress.Any, DiscoveryInfo.DiscoveryPort));

            IPEndPoint clients   = new IPEndPoint(IPAddress.Any, 0);
            EndPoint   ep_sender = clients;
            
            // Starting to listen for discovery broadcasts
            m_discovery_socket.BeginReceiveFrom(m_data_stream, 0, m_data_stream.Length, SocketFlags.None, ref ep_sender, TryAnswerDiscoveryBroadcast, ep_sender);  
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            m_discovery_socket.Close();
            m_discovery_socket.Dispose();
        }

        #endregion
        
        #region Methods

        /// <summary>
        ///     Finds and returns the local, sharable, IP address of the device
        /// </summary>
        /// <returns>IP address, or null if the operation failed</returns>
        private static IPAddress GetIpAddress()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 0);

            if (socket.LocalEndPoint is IPEndPoint endpoint)
                return endpoint.Address;

            return null;
        }
        
        /// <summary>
        ///     This method is automatically called when the discovery sockets receives data
        ///     If the data received is a discovery broadcast, the method will answer it
        ///
        ///     Any other type of data are ignored
        /// </summary>
        private void TryAnswerDiscoveryBroadcast(IAsyncResult async_result)
        {
            IPEndPoint clients   = new IPEndPoint(IPAddress.Any, 0);
            EndPoint   ep_sender = clients;

            // Receive all data. Sets epSender to the address of the caller
            m_discovery_socket.EndReceiveFrom(async_result, ref ep_sender);
            
            // Get the message received
            string message = Encoding.UTF8.GetString(m_data_stream);
            if (message.StartsWith(DiscoveryInfo.BroadcastHeader, StringComparison.CurrentCultureIgnoreCase))
            {
                byte[] data = EndpointInfo.Serialize(GetIpAddress(), ListenedPort);
                
                // Send the response message to the client who was searching
                m_discovery_socket.BeginSendTo(data, 0, data.Length, SocketFlags.None, ep_sender, delegate (IAsyncResult result)
                {
                    m_discovery_socket.EndSend(result);
                }, ep_sender);
            }
            
            // Listen for more connections again...
            m_discovery_socket.BeginReceiveFrom(m_data_stream, 0, m_data_stream.Length, SocketFlags.None, ref ep_sender, TryAnswerDiscoveryBroadcast, ep_sender);
        }

        #endregion
    }
}

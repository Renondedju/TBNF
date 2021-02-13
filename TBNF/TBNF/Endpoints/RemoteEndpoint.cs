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
    using System.Threading;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using System.Net.NetworkInformation;
    
    using SystemMessages;

    /// <summary>
    ///     Remote client endpoint variant
    ///     This class only exists in the scope of a <see cref="EndpointAuthenticator"/> and is used
    ///     to manage and interact with a <see cref="ClientEndpoint"/>
    ///
    ///     Unlike the <see cref="ClientEndpoint"/> this endpoint type is passive, meaning that it won't automatically try to reconnect
    /// </summary>
    internal sealed class RemoteEndpoint : Endpoint
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="client">Original TCP client</param>
        /// <param name="mac_address">Mac address of the distant client</param>
        /// <param name="network_identifier">Unique identifier of the distant client</param>
        /// <param name="handler">Message handler to be used by the endpoint</param>
        public RemoteEndpoint(TcpClient client, PhysicalAddress mac_address, byte network_identifier, MessageHandler handler) : base(handler)
        {
            NetworkIdentifier = network_identifier;
            MacAddress        = mac_address;

            Task _ = HandleConnectionAttempt(client);
        }

        #region Exposed Methods

        /// <summary>
        ///     Handles a connection handshake
        /// </summary>
        /// <param name="client">Client to handshake with</param>
        /// <param name="cancellation_token">Cancellation token (timeout)</param>
        /// <returns>True if the handshake succeeded, false otherwise</returns>
        protected override async Task<bool> HandleHandshake(TcpClient client, CancellationToken cancellation_token)
        {
            // The passed socket has already been identified, we just need to finalize
            // the connection before listening for the incoming messages
            return await client.SendMessage(new LoginConfirmationMessage {Data = {NetworkIdentifier = NetworkIdentifier}}, cancellation_token);
        }
        
        /// <summary>
        ///     Attempts to reconnect a disconnected client
        /// </summary>
        /// <param name="client">New socket to transition to</param>
        public async Task Reconnect(TcpClient client) => await HandleConnectionAttempt(client);

        #endregion

        #region Methods

        /// <summary>
        ///     Attempts to end a connection process within the <see cref="Endpoint.ConnectionTimeout"/> timeout 
        /// </summary>
        /// <param name="client">Client to finalize the connection process with</param>
        private async Task HandleConnectionAttempt(TcpClient client)
        {
            // Creating the timeout cancellation source
            CancellationTokenSource timeout_cancellation = CancellationTokenSource.CreateLinkedTokenSource(GlobalCancellation.Token);
            timeout_cancellation.CancelAfter(ConnectionTimeout);

            await HandleEndConnection(client, timeout_cancellation.Token);
        }

        #endregion
    }
}

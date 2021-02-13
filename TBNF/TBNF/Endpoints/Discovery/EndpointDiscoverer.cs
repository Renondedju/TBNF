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
    using System.Threading.Tasks;
    using System.Collections.Generic;
    
    /// <summary>
    ///     Helper class allowing to easley find and list discoverable endpoints
    /// </summary>
    public static class EndpointDiscoverer
    {
        #region Exposed Methods

        /// <summary>
        ///     Lists all the the endpoints available for discovery
        /// </summary>
        /// <param name="game_identifier">Unique game identifier, null will return any game session</param>
        /// <returns>List of all the discovered endpoints</returns>
        public static async Task<List<Tuple<DiscoverableEndpointInfo, IPEndPoint>>> FindEndpoints(string game_identifier)
        {
            // This client is never closed for now 
            UdpClient udp_client = new UdpClient {
                EnableBroadcast = true, 
                DontFragment    = true
            };
            
            udp_client.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            
            // Sending a broadcast
            byte[] data = Encoding.UTF8.GetBytes(DiscoveryInfo.BroadcastHeader);
            await udp_client.SendAsync(data, data.Length, IPAddress.Broadcast.ToString(), DiscoveryInfo.DiscoveryPort);

            // Waiting for every endpoint to answer our call within our timeout
            return CollectAnswers(udp_client, game_identifier, TimeSpan.FromSeconds(1));
        }
        
        #endregion

        #region Methods

        /// <summary>
        ///     Loops on the passed udp client during the given timeout to collect any endpoint discovery answer and parse them
        /// </summary>
        /// <param name="udp_client">Udp client to listen on</param>
        /// <param name="game_identifier">Unique game identifier, null will return any game session</param>
        /// <param name="timeout">Timeout</param>
        /// <returns>List of all the endpoints that sent a valid answer during the passed timeout</returns>
        private static List<Tuple<DiscoverableEndpointInfo, IPEndPoint>> CollectAnswers(UdpClient udp_client, string game_identifier, TimeSpan timeout)
        {
            List<Tuple<DiscoverableEndpointInfo, IPEndPoint>> discovered_endpoints = new List<Tuple<DiscoverableEndpointInfo, IPEndPoint>>();
            
            IPEndPoint remote_endpoint = null;
            DateTime   scan_start      = DateTime.Now;

            do
            {
                // Recomputing the timeout delay
                udp_client.Client.ReceiveTimeout = (int) (timeout.TotalMilliseconds - (DateTime.Now - scan_start).TotalMilliseconds);

                try
                {
                    // If an endpoint answered our call, we need to parse it here, and add it to the list
                    Tuple<DiscoverableEndpointInfo, IPEndPoint> endpoint = TryParseMessage(udp_client.Receive(ref remote_endpoint));
                    if (endpoint != null && (endpoint.Item1.GameIdentifier == game_identifier || game_identifier == null))
                        discovered_endpoints.Add(endpoint);
                }
                catch (SocketException socket_exception)
                {
                    // Catching timeouts
                    if (socket_exception.ErrorCode != (int) SocketError.TimedOut)
                        throw;

                    break;
                }

            } while (udp_client.Client.ReceiveTimeout > 0);

            return discovered_endpoints;
        }
        
        /// <summary>
        ///     Attempts to parse incoming data as a discovery answer
        /// </summary>
        /// <param name="incoming_data">Incoming raw data</param>
        /// <returns>DiscoveredEndpointInfo instance or null if the parsing failed</returns>
        private static Tuple<DiscoverableEndpointInfo, IPEndPoint> TryParseMessage(byte[] incoming_data)
        {
            try
            {
                DiscoverableEndpointInfo info      = new DiscoverableEndpointInfo();
                IPEndPoint               end_point = info.Deserialize(incoming_data);

                return new Tuple<DiscoverableEndpointInfo, IPEndPoint>(info, end_point);
            }
            // Catching any exception, meaning that the parsing failed
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}

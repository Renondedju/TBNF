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

namespace SampleClient
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Shared;
    using TBNF;
    using TBNF.SystemMessages;

    /// <summary>
    ///     Client sample program
    /// </summary>
    internal static class Program
    {
        private static async Task Main()
        {
            // Registering every message class defined in the assembly
            MessageRegister.RegisterAssembly(typeof(StringMessage).Assembly);

            // Looking for available endpoints
            List<Tuple<DiscoverableEndpointInfo, IPEndPoint>> discovered_endpoints = await EndpointDiscoverer.FindEndpoints("Sample Game");

            // Enumerating every endpoint found
            Console.WriteLine($"Found {discovered_endpoints.Count} available endpoints");
            foreach ((DiscoverableEndpointInfo info, IPEndPoint endpoint) in discovered_endpoints)
                Console.WriteLine($" - Endpoint named: {info.Name} ({info.GameIdentifier}) is located at {endpoint.Address}:{endpoint.Port}");

            // Attempting to connect to the first endpoint found
            if (discovered_endpoints.Count >= 1)
            {
                IPEndPoint endpoint = discovered_endpoints[0].Item2;

                Console.WriteLine($"Attempting to connect to the first endpoint found ({endpoint.Address}:{endpoint.Port})");
                await StartClient(endpoint.Address, endpoint.Port);
            }

            Console.WriteLine("Cleaning up...");
        }

        /// <summary>
        ///     Starts the ClientEndpoint and attempts a connection with the given parameters
        /// </summary>
        /// <param name="address">IP address of the server</param>
        /// <param name="port">IP port of the server</param>
        private static async Task StartClient(IPAddress address, int port)
        {
            TestHandler          handler  = new TestHandler();
            using ClientEndpoint endpoint = new ClientEndpoint(handler, address, port)
            {
                InactivityCheckInterval = TimeSpan.FromSeconds(5),
                ConnectionTimeout       = TimeSpan.FromSeconds(10)
            };

            // Creating listeners
            endpoint.OnConnectionSuccess  += client => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Connection attempt succeeded");
            endpoint.OnConnectionFailure  += client => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Connection attempt failed");
            endpoint.OnDisconnection      += client => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Disconnected");
            endpoint.OnRawMessageReceived += (client, message) => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Message received {message}");
            endpoint.OnRawMessageSent     += (client, message) => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Message sent {message}");

            for (int i = 0; i < 5; i++)
            {
                // Enqueuing the message to be sent once the endpoint is connected
                endpoint.EnqueueMessage(new StringMessage
                {
                    Message = "This is a test message!"
                });

                await Task.Delay(2000);
            }

            endpoint.EnqueueMessage(new ClientConnectedMessage());    // We should get kicked here
            endpoint.EnqueueMessage(new ClientDisconnectedMessage()); // And automatically be reconnected here

            await Task.Delay(50000);
        }
    }
}

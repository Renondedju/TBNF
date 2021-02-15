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

namespace SampleHost
{
    using System;
    using Shared;
    
    using TBNF;
    using TBNF.Handlers;

    /// <summary>
    ///     Host sample program
    /// </summary>
    internal static class Program
    {
        private static void Main()
        {
            // Registering every message class defined in the assembly
            MessageRegister.RegisterAssembly(typeof(StringMessage).Assembly);

            // Creating the host endpoint
            DiscoverableEndpointInfo info = new DiscoverableEndpointInfo { Name = Environment.MachineName, GameIdentifier = "Sample Game" };
            
            // Using 0 as the port number to automatically get a new free port if needed
            using DiscoverableEndpointAuthenticator authenticator = new DiscoverableEndpointAuthenticator(new DefaultHandler(), 8865, info)
            {
                InactivityCheckInterval = TimeSpan.FromSeconds(7)
            };

            // Creating listeners
            authenticator.OnNewClientRegistered += (_, endpoint) =>
            {
                // Creating listeners
                endpoint.OnConnectionSuccess  += client => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Connected");
                endpoint.OnConnectionFailure  += client => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Connection attempt failed");
                endpoint.OnDisconnection      += client => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Disconnected");
                //endpoint.OnRawMessageReceived += (client, message) => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Message received {message}");
                //endpoint.OnRawMessageSent     += (client, message) => Console.WriteLine($"Endpoint {client.NetworkIdentifier} : Message sent {message}");
            };

            authenticator.Start();
            
            // The service can now be accessed.
            Console.WriteLine("Press <ENTER> to terminate service.");
            Console.ReadLine();
        }
    }
}

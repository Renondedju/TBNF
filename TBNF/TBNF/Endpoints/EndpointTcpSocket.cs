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
    using System.Net.Sockets;
    using System.Threading;

    /// <summary>
    ///     Wraps a tcp client and a cancellation source to ease cancellations and cleanup
    /// </summary>
    public class EndpointTcpSocket : IDisposable
    {
        public EndpointTcpSocket(TcpClient client, CancellationToken cancellation_token)
        {
            Client             = client;
            CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token);
        }
        
        #region Members

        public readonly TcpClient               Client;
        public readonly CancellationTokenSource CancellationSource; 

        #endregion
        
        #region IDisposable Members

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            CancellationSource.Cancel();
            
            Client            .Close();
            Client            .Dispose();
            CancellationSource.Dispose();
        }

        #endregion
    }
}

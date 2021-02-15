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
    using System.Diagnostics;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class TcpClientExtensions
    {
        internal const int HeaderSize = sizeof(ushort);
        
        /// <summary>
        ///     Reads a given amount of bytes from the passed client.
        ///     The current thread is blocked until the servers closes the connection or the read has been done
        /// </summary>
        /// <remarks>This method is asynchronous</remarks>
        /// <param name="client">Client to read the bytes from</param>
        /// <param name="read_size">Number of bytes to read</param>
        /// <param name="cancellation_token">Cancellation token</param>
        /// <returns>Read bytes, or null if a cancellation has been requested</returns>
        internal static async Task<byte[]> ReadBytes(this TcpClient client, ushort read_size, CancellationToken cancellation_token)
        {
            if (cancellation_token.IsCancellationRequested || read_size == 0)
                return null;

            int    bytes_remaining = read_size;
            int    bytes_read      = 0;
            byte[] buffer          = new byte[read_size];

            try
            {
                do
                {
                    // Attempting to read the data
                    bytes_read      =  await client.GetStream().ReadAsync(buffer, bytes_read, bytes_remaining, cancellation_token);
                    bytes_remaining -= bytes_read;
                } while (bytes_remaining > 0 && bytes_read > 0);
            }
            
            // If the operation has been cancelled or failed in some way or another, returning
            // null to avoid sending an incomplete buffer
            catch (IOException)                { return null; }
            catch (ObjectDisposedException)    { return null; }
            catch (InvalidOperationException)  { return null; }
            catch (OperationCanceledException) { return null; }

            // If the read is incomplete, returning null, else the buffer containing our data
            return bytes_remaining > 0 ? null : buffer;
        }

        /// <summary>
        ///     Sends a message to the passed client
        /// </summary>
        /// <remarks>This method is asynchronous</remarks>
        /// <param name="client">Tcp client to send the message to</param>
        /// <param name="message">Message instance to send</param>
        /// <param name="cancellation_token">Cancellation token</param>
        /// <returns>True if the message has been sent, false otherwise</returns>
        internal static async Task<bool> SendMessage(this TcpClient client, Message message, CancellationToken cancellation_token)
        {
            // If the socket isn't available for now, skipping the message
            if (client == null || cancellation_token.IsCancellationRequested)
                return false;

            try
            {
                NetworkStream   network_stream = client.GetStream();
                PackagedMessage package        = message.Pack();

                // If the stream cannot be written to, returning for now
                if (!network_stream.CanWrite)
                    return false;

                Debug.Assert(package.Size <= ushort.MaxValue, "The maximum supported size of a message is ushort.MaxValue (65 535 bytes)");

                // Creating the package containing the actual message
                // + 2 bytes storing the size of the message
                await network_stream.WriteAsync(BitConverter.GetBytes(package.Size), 0, HeaderSize  , cancellation_token);
                await network_stream.WriteAsync(package.Bytes                      , 0, package.Size, cancellation_token);

                // If the operation has been cancelled, we need to consider that the message has not been sent
                return !cancellation_token.IsCancellationRequested;
            }

            // If the connection has been interrupted in some way or another, we need to catch it here
            catch (IOException)                { }
            catch (ObjectDisposedException)    { }
            catch (InvalidOperationException)  { }
            catch (OperationCanceledException) { }

            return false;
        }
        
        /// <summary>
        ///     Reads a message from that client
        ///     This method will only return if a message has been received
        /// </summary>
        /// <remarks>This method is asynchronous</remarks>
        /// <remarks>If a cancellation has been requested, the returned data will be null and thus the built message will be null too</remarks>
        /// <param name="client">Client</param>
        /// <param name="cancellation_token">Cancellation token</param>
        /// <returns>Message instance or null</returns>
        internal static async Task<Message> ReadMessage(this TcpClient client, CancellationToken cancellation_token)
        {
            if (client == null || cancellation_token.IsCancellationRequested || !client.GetStream().CanRead)
                return null;

            // Fetching our data
            byte[] header = await client.ReadBytes(HeaderSize,                                                  cancellation_token);
            byte[] data   = await client.ReadBytes(BitConverter.ToUInt16(header ?? new byte[] {0x00, 0x00}, 0), cancellation_token);

            // If a cancellation has been requested, the returned data will be null and thus the built message will be null too
            return MessageBuilder.BuildMessage(new PackagedMessage(data));
        }
    }
}

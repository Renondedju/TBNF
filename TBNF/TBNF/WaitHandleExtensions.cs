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
    using System.Threading.Tasks;

    public static class WaitHandleExtensions
    {
        /// <summary>
        ///     Waits for a wait handle asynchronously
        /// </summary>
        /// <param name="handle">Wait handle</param>
        /// <param name="milliseconds_timeout">Timeout</param>
        /// <param name="cancellation_token">Cancellation token</param>
        public static async Task<bool> WaitOneAsync(this WaitHandle handle, int milliseconds_timeout, CancellationToken cancellation_token)
        {
            RegisteredWaitHandle          registered_handle  = null;
            CancellationTokenRegistration token_registration = default;
            try
            {
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                registered_handle = ThreadPool.RegisterWaitForSingleObject(
                    handle,
                    (state, timed_out) => ((TaskCompletionSource<bool>)state)?.TrySetResult(!timed_out),
                    tcs,
                    milliseconds_timeout,
                    true);
                token_registration = cancellation_token.Register(
                    state => ((TaskCompletionSource<bool>)state).TrySetCanceled(),
                    tcs);
                return await tcs.Task;
            }
            finally
            {
                registered_handle?.Unregister(null);
                token_registration.Dispose();
            }
        }

        /// <summary>
        ///     Waits for a wait handle asynchronously
        /// </summary>
        /// <param name="handle">Wait handle</param>
        /// <param name="timeout">Timeout</param>
        /// <param name="cancellation_token">Cancellation token</param>
        public static Task<bool> WaitOneAsync(this WaitHandle handle, TimeSpan timeout, CancellationToken cancellation_token)
        {
            return handle.WaitOneAsync((int)Math.Max(timeout.TotalMilliseconds, 0.0), cancellation_token);
        }

        /// <summary>
        ///     Waits for a wait handle asynchronously
        /// </summary>
        /// <param name="handle">Wait handle</param>
        /// <param name="cancellation_token">Cancellation token</param>
        public static Task<bool> WaitOneAsync(this WaitHandle handle, CancellationToken cancellation_token)
        {
            return handle.WaitOneAsync(Timeout.Infinite, cancellation_token);
        }
    }
}

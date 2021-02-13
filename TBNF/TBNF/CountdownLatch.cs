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

    /// <summary>
    ///     Simple countdown latch implementation using a ManualResetEvent
    /// </summary>
    public class CountdownLatch
    {
        #region Members

        private          int              m_count;
        private readonly ManualResetEvent m_event = new ManualResetEvent(false);

        #endregion

        #region Properties

        public WaitHandle WaitHandle => m_event;

        #endregion
        
        #region Exposed Methods

        /// <summary>
        ///     Decrements the latch and locks threads if every value has been consumed (ie. the counter equals 0)
        ///     /!\ WARNING: The counter isn't restricted to be at a minimum of 0
        /// </summary>
        public void Decrement()
        {
            if (Interlocked.Decrement(ref m_count) == 0)
                m_event.Reset();
        }
        
        /// <summary>
        ///     Increments the latch and unlocks awaiting threads
        /// </summary>
        public void Increment()
        {
            // The last thread to signal also sets the event.
            if (Interlocked.Increment(ref m_count) >= 1)
                m_event.Set();
        }

        #endregion
    }
}

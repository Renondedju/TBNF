﻿/*
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

    /// <summary>
    ///     Helper class that represents a packaged <see cref="Message"/> ready to be sent over the network
    ///     This class is immutable
    /// </summary>
    public class PackagedMessage
    {
        internal PackagedMessage(byte[] bytes)
        {
            Bytes = bytes;
        }

        #region Members

        /// <summary>
        ///     Raw bytes of the package
        /// </summary>
        public readonly byte[] Bytes;

        #endregion

        #region Properties

        /// <summary>
        ///     Returns the size in bytes of the package
        ///     Shorthand for Bytes.Length
        /// </summary>
        public int Size => Bytes.Length;
        
        /// <summary>
        ///     Decodes the name of the packaged message 
        /// </summary>
        public ushort MessageName => BitConverter.ToUInt16(Bytes, 0);
        
        /// <summary>
        ///     Actual data of the package, stripped of any meta-data
        /// </summary>
        public byte[] Data => Bytes[2..];

        #endregion
    }
}
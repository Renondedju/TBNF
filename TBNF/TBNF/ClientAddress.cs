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
    using System.Linq;
    using System.Net.NetworkInformation;

    /// <summary>
    ///     Represents
    /// </summary>
    public class ClientAddress
    {
        #region Members

        public PhysicalAddress PhysicalAddress;
        public ushort          AdditionalIdentifier;

        #endregion
        
        #region Exposed Methods

        /// <summary>
        ///     Converts a client address to a byte array
        /// </summary>
        /// <returns>Data</returns>
        public byte[] GetAddressBytes()
        {
            byte[] physical_bytes   = PhysicalAddress.GetAddressBytes();
            byte[] additional_bytes = BitConverter.GetBytes(AdditionalIdentifier);

            return (byte[]) physical_bytes.Concat(additional_bytes);
        }

        /// <summary>
        ///     Loads a client address from a byte array
        /// </summary>
        /// <param name="bytes">Data</param>
        public void FromBytes(byte[] bytes)
        {
            PhysicalAddress      = new PhysicalAddress(bytes);
            AdditionalIdentifier = BitConverter.ToUInt16(bytes, 8);
        }
        
        #endregion
    }
}

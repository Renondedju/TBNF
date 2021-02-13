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

namespace TBNF.SystemMessages
{
    using System.IO;
    using System.Net.NetworkInformation;

    /// <summary>
    ///     Sent by a <see cref="Endpoint"/> to request a login
    /// </summary>
    [Message]
    public class IdentificationMessage : Message
    {
        #region Members

        // A physical address is used to uniquely identify incoming connection
        // even after a potential application or device reboot
        // This is used to reconnect a device after a network failure
        public PhysicalAddress MacAddress;

        #endregion

        #region Exposed Methods

        /// <summary>
        ///     Serializes any additional data of the message
        ///     This method is used instead of a classic csharp serializer to vastly
        ///     optimize the speed of the operation as well as the size of the output 
        /// </summary>
        /// <param name="binary_writer">Binary writer to write the additional data in</param>
        protected override void SerializeAdditionalData(BinaryWriter binary_writer)
        {
            // Even tho a mac address can be 8 bytes long, we only write 6 here
            // The main purpose of this address is to give the server a unique device identifier
            // Sending the whole address isn't a big deal in this case
            binary_writer.Write(MacAddress.GetAddressBytes(), 0, 6);            
        }

        /// <summary>
        ///     Deserializes any additional data of the message
        ///     This method is used instead of a classic csharp deserializer to vastly
        ///     optimize the speed of the operation as well as the size of the output
        /// </summary>
        /// <param name="binary_reader">Binary reader of the additional data</param>
        protected override void DeserializeAdditionalData(BinaryReader binary_reader)
        {
            MacAddress = new PhysicalAddress(binary_reader.ReadBytes(6));
        }

        #endregion
    }
}

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
    using System.IO;
    using System.Diagnostics;
    using System.Net;

    /// <summary>
    ///     Discoverable endpoint info
    ///     Those info are used for discovery to sort and identify available sessions
    /// </summary>
    public class DiscoverableEndpointInfo
    {
        #region Members

        public string Name;           // Name of the game that the endpoint hosts 
        public string GameIdentifier; // Unique game identifier, this is used to differentiate different games on the same network, if any
        public byte[] AdditionalData; // Additional Data

        #endregion

        #region Exposed Methods

        /// <summary>
        ///     Converts the actual state of the endpoint info into a stream of bytes
        /// </summary>
        /// <param name="ip_address">IP address to pack with the package</param>
        /// <param name="port">Port to pack with the package</param>
        /// <returns>Byte array</returns>
        internal byte[] Serialize(IPAddress ip_address, int port)
        {
            using MemoryStream memory_stream = new MemoryStream();
            using BinaryWriter binary_writer = new BinaryWriter(memory_stream);

            // Writing data
            binary_writer.Write((ushort)(AdditionalData?.Length ?? 0));
            binary_writer.Write(Name);
            binary_writer.Write(GameIdentifier);
            binary_writer.Write(AdditionalData ?? new byte[0]);

            // Writing address
            binary_writer.Write((byte) ip_address.GetAddressBytes().Length);
            binary_writer.Write(       ip_address.GetAddressBytes());
            binary_writer.Write(port);
            
            Debug.Assert(memory_stream.GetBuffer().Length <= 65507,
                         "An UDP package cannot be longer than 65507 bytes long!");
                
            return memory_stream.GetBuffer();
        }

        /// <summary>
        ///     Deserializes the incoming data and fills the class with it
        /// </summary>
        /// <param name="incoming_data">Incoming data</param>
        internal IPEndPoint Deserialize(byte[] incoming_data)
        {
            using MemoryStream memory_stream = new MemoryStream(incoming_data);
            using BinaryReader binary_reader = new BinaryReader(memory_stream);

            ushort additional_length = binary_reader.ReadUInt16();
            
            Name           = binary_reader.ReadString();
            GameIdentifier = binary_reader.ReadString();
            AdditionalData = binary_reader.ReadBytes(additional_length);

            byte      address_length = binary_reader.ReadByte();
            IPAddress ip_address     = new IPAddress(binary_reader.ReadBytes(address_length));
            int       port           = binary_reader.ReadInt32();
            
            return new IPEndPoint(ip_address, port);
        }

        #endregion
    }
}

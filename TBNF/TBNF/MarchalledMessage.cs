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
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    ///     Special kind of messages that serializes everything via marshaling
    /// </summary>
    /// <typeparam name="TData">Type of the data to store. Must be marshal-able</typeparam>
    public class MarchalledMessage<TData> : Message where TData : struct
    {
        #region Members

        public TData Data;

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
            int    size  = Marshal.SizeOf<TData>();
            byte[] bytes = new byte[size];
            IntPtr ptr   = Marshal.AllocHGlobal(size);
            
            // Copy object byte-to-byte to unmanaged memory.
            Marshal.StructureToPtr(Data, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            
            // Writing to memory
            binary_writer.Write(bytes);
        }

        /// <summary>
        ///     Deserializes any additional data of the message
        ///     This method is used instead of a classic csharp deserializer to vastly
        ///     optimize the speed of the operation as well as the size of the output
        /// </summary>
        /// <param name="binary_reader">Binary reader of the additional data</param>
        protected override void DeserializeAdditionalData(BinaryReader binary_reader)
        {
            int    size = Marshal.SizeOf<TData>();
            IntPtr ptr  = Marshal.AllocHGlobal(size);
            
            // Reading bytes from memory, and copying them to the 'Data' structure
            Marshal.Copy(binary_reader.ReadBytes(size), 0, ptr, size);
            Data = (TData) Marshal.PtrToStructure(ptr, typeof(TData));
            Marshal.FreeHGlobal(ptr);
        }

        #endregion
    }
}

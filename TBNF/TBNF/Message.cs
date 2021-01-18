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
    using System.Reflection;
    using System.Diagnostics;

    /// <summary>
    ///     Message class, represents a package that will be sent over the network
    /// </summary>
    public abstract class Message
    {
        protected Message()
        {
            // We are using a bit of reflection here to fetch the message name automatically
            // This wont impact performances by a lot since the request is really targeted (in average the whole allocation of the class takes ~0.013 ms)
            Type             type      = GetType();
            MessageAttribute attribute = (MessageAttribute) type.GetCustomAttribute(typeof(MessageAttribute), true);

            Debug.Assert(attribute != null, $"A message class should have a {nameof(MessageAttribute)} attribute attached");
            
            AuthorType  = attribute.AuthorType;
            MessageName = MessageRegister.GetMessageName(type);
        }

        #region Members

        public readonly EMessageAuthor AuthorType;
        public readonly ushort         MessageName;

        #endregion

        #region Exposed Methods

        /// <summary>
        ///     Serializes any additional data of the message
        ///     This method is used instead of a classic csharp serializer to vastly
        ///     optimize the speed of the operation as well as the size of the output 
        /// </summary>
        /// <param name="binary_writer">Binary writer to write the additional data in</param>
        protected virtual void SerializeAdditionalData(BinaryWriter binary_writer)
        { }

        /// <summary>
        ///     Deserializes any additional data of the message
        ///     This method is used instead of a classic csharp deserializer to vastly
        ///     optimize the speed of the operation as well as the size of the output
        /// </summary>
        /// <param name="binary_reader">Binary reader of the additional data</param>
        protected virtual void DeserializeAdditionalData(BinaryReader binary_reader)
        { }
        
        /// <summary>
        ///     Serializes and packs the message so it can be sent over the network
        /// </summary>
        /// <returns></returns>
        public PackagedMessage Pack()
        {
            using MemoryStream memory_stream = new();
            using BinaryWriter binary_writer = new(memory_stream);
            
            binary_writer.Write(MessageName);
            
            SerializeAdditionalData(binary_writer);
                
            return new PackagedMessage(memory_stream.ToArray());
        }

        /// <summary>
        ///     Deserializes a packaged message into an actual message
        /// </summary>
        /// <remarks>The PackagedMessage.MessageName and MessageName of the message must be the same for this operation to work</remarks>
        /// <param name="package">Package to deserialize</param>
        public void Unpack(PackagedMessage package)
        {
            // Checking for package compatibility
            // This will avoid further errors later on in the program
            Debug.Assert(package.MessageName == MessageName,
                         "The package.MessageName does not correspond to the MessageName of this message." +
                         "Please use the MessageBuilder to avoid this error again"
                         );
            
            using MemoryStream memory_stream = new();
            using BinaryReader binary_reader = new(memory_stream);

            // Writing the data to the stream, and skipping the MessageName part
            memory_stream.Write(package.Bytes, 0, package.Size);
            memory_stream.Seek (sizeof(ushort), SeekOrigin.Begin);
                
            DeserializeAdditionalData(binary_reader);
        }

        #endregion
    }
}

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

    /// <summary>
    ///     Builds a message instance from a serialized version of the message via reflection
    /// </summary>
    public static class MessageBuilder
    {
        #region Exposed Methods

        /// <summary>
        ///     Builds a message instance and fills it from a <see cref="PackagedMessage"/>
        /// </summary>
        /// <param name="package">Raw message data</param>
        /// <returns>Message instance, or null if something failed</returns>
        public static Message BuildMessage(PackagedMessage package)
        {
            if (package?.Bytes == null)
                return null;

            Message message = GetMessageInstance(package.MessageName);
            message?.Unpack(package);

            return message;
        }
        
        /// <summary>
        ///     Returns a message class instance from a message name
        /// </summary>
        /// <param name="message_name">Message name as defined in the <see cref="MessageRegister"/></param>
        /// <returns>Message class instance or null if the passed message name was incorrect</returns>
        public static Message GetMessageInstance(ushort message_name)
        {
            Type type = MessageRegister.GetMessageType(message_name);

            return type != null ? Activator.CreateInstance(type) as Message : null;
        }

        #endregion
    }
}

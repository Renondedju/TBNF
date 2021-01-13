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
    using System.Reflection;
    using System.Collections.Generic;

    /// <summary>
    ///     This class has for purpose to register and assign unique IDs to every message type
    ///     This allows the framework to easily re-instantiate correctly a message when received from the network
    /// </summary>
    /// <remarks>
    ///     This process is done at static time, by scanning all the assemblies and classes loaded in the app domain at the moment
    ///     Since the process can be quite expensive with big applications, everything is parallelized
    /// </remarks>
    public static class MessageRegister
    {
        static MessageRegister()
        {
            // Iterating over every type of the app domain and registering classes marked by the "MessageAttribute" attribute
            // Each assembly and type is sorted by full name to enforce determinism
            // The only condition is that there is the same set of messages both on client and server side
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(assembly => assembly.FullName).AsParallel())
                RegisterAssembly(assembly);
        }

        #region Members

        private static          ushort                   s_index            = 1;
        private static readonly Dictionary<Type, ushort> s_register         = new();
        private static readonly Dictionary<ushort, Type> s_reverse_register = new();

        #endregion

        #region Exposed Methods

        /// <summary>
        ///     Registers all the messages class contained in the passed assembly
        ///     If a type in the assembly has already been registered, this method will skip it
        /// </summary>
        /// <param name="assembly">Assembly to scan and register</param>
        public static void RegisterAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes().OrderBy(type => type.FullName).AsParallel())
            {
                if (type.GetCustomAttribute(typeof(MessageAttribute)) == null || s_register.ContainsKey(type))
                    continue;
                
                ushort index = s_index++;

                s_register        .Add(type, index);
                s_reverse_register.Add(index, type);
            }
        }
        
        /// <summary>
        ///     Returns the unique message identifier (message name) of the passed message type
        ///     The passed type must inherit from the <see cref="Message"/> class and be marked by the <see cref="MessageAttribute"/>
        /// </summary>
        /// <param name="message_type">Type of the message to look for</param>
        /// <returns>Message name or 0 if the type has not been registered</returns>
        public static ushort GetMessageName(Type message_type)
        {
            return s_register.GetValueOrDefault(message_type);
        }

        /// <summary>
        ///     Returns the message type from the message name
        /// </summary>
        /// <param name="message_name">Message name to look for</param>
        /// <returns>Type or null</returns>
        public static Type GetMessageType(ushort message_name)
        {
            return s_reverse_register.GetValueOrDefault(message_name);
        }

        #endregion
    }
}

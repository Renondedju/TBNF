namespace TBNF
{
    using System;
    using System.Reflection;
    using System.Net.Sockets;
    using System.Diagnostics;
    using System.Collections.Generic;
    
    /// <summary>
    ///     Message handler, allows easy message handling via optimized code and reflection
    ///     Handlers methods must be non public, non static, return void and have 2 parameters:
    ///     the first must always be a TcpClient, and the second one must be a Message class.
    ///     Only one handler per message class is allowed.
    /// </summary>
    public abstract class MessageHandler
    {
        protected MessageHandler()
        {
            // Looping over every method in the handler
            foreach (MethodInfo method_info in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method_info.GetParameters();
             
                if (method_info  .ReturnType    == typeof(void)      &&   // Return type must be void
                    parameters   .Length        == 2                 &&  // There must be 2 parameters at most
                    parameters[0].ParameterType == typeof(TcpClient) && // First type must be a TcpClient
                    parameters[1].ParameterType.GetCustomAttribute<MessageAttribute>() != null) // And last type a message type
                {
                    // Extracting the message name of the second type 
                    ushort message_name = MessageRegister.GetMessageName(parameters[1].ParameterType);
                    
                    Debug.Assert(!m_handler_cache.ContainsKey(message_name),
                                 "Two or more handlers for the same message have been defined in the same class." +
                                 " Only one is allowed at a time");
                    
                    // Adding a message handler to the cache
                    m_handler_cache.Add(message_name, method_info);
                }
            }
        }

        #region Members

        private readonly Dictionary<ushort, MethodInfo> m_handler_cache = new();
        
        #endregion
        
        #region Exposed Methods

        /// <summary>
        ///     Handles the message using one of the pre defined custom handlers
        /// </summary>
        /// <param name="emitter">TcpClient that the message has been received from</param>
        /// <param name="message">Received message</param>
        public void HandleMessage(TcpClient emitter, Message message)
        {
            if (m_handler_cache.ContainsKey(message.MessageName))
                m_handler_cache[message.MessageName].Invoke(this, new object[] {emitter, message});
            else
                DefaultHandler(emitter, message);
        }
        
        /// <summary>
        ///     Default handler
        ///     If a message does not have a custom handler, this method will be called instead
        /// </summary>
        /// <param name="emitter">TcpClient that the message has been received from</param>
        /// <param name="message">Received message</param>
        protected virtual void DefaultHandler(TcpClient emitter, Message message)
        {
            Console.WriteLine($"Received an unhandled message: {message}");
        }

        #endregion
    }
}

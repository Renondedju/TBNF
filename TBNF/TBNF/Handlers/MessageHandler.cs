namespace TBNF
{
    using System;
    using System.Reflection;
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
        protected MessageHandler(IEnumerable<Type> ignored_messages = null)
        {
            // Looping over every method in the handler
            foreach (MethodInfo method_info in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                ParameterInfo[] parameters = method_info.GetParameters();

                if (parameters.Length           != 2                ||
                    parameters[0].ParameterType != typeof(Endpoint) ||
                    parameters[1].ParameterType.GetCustomAttribute<MessageAttribute>() == null)
                    
                    continue;

                // Extracting the message name of the second type 
                ushort message_name = MessageRegister.GetMessageName(parameters[1].ParameterType);
                    
                Debug.Assert(!m_handler_cache.ContainsKey(message_name),
                    "Two or more handlers for the same message have been defined in the same class. Only one is allowed at a time");
                    
                // Adding a message handler to the cache
                m_handler_cache.Add(message_name, method_info);
            }

            if (ignored_messages == null)
                return;
            
            // Handling ignored messages
            MethodInfo ignored_handler_info = GetType().GetMethod(nameof(IgnoredMessageHandler));
            foreach (Type ignored_message_type in ignored_messages)
            {
                ushort message_name = MessageRegister.GetMessageName(ignored_message_type);
                
                Debug.Assert(!m_handler_cache.ContainsKey(message_name),
                    $"The message type {ignored_message_type} has been set to be ignored, but a handler has been defined to its name");
                
                m_handler_cache.Add(message_name, ignored_handler_info);
            }
        }

        #region Members

        private readonly Dictionary<ushort, MethodInfo> m_handler_cache = new Dictionary<ushort, MethodInfo>();
        
        #endregion
        
        #region Exposed Methods

        /// <summary>
        ///     Handles the message using one of the pre defined custom handlers
        /// </summary>
        /// <param name="emitter">Endpoint that received the message</param>
        /// <param name="message">Received message</param>
        public void HandleMessage(Endpoint emitter, Message message)
        {
            // In case of cancellation, the received message will be considered as null
            // For code readability and flexibility purposes, we are checking here if everything is good
            if (message == null)
                return;
            
            if (m_handler_cache.ContainsKey(message.MessageName))
                m_handler_cache[message.MessageName].Invoke(this, new object[] {emitter, message});
            else
                DefaultHandler(emitter, message);
        }

        /// <summary>
        ///     Ignored message handler
        ///     Every ignored message will use this empty handler
        /// </summary>
        /// <param name="emitter">Endpoint that received the message</param>
        /// <param name="message">Received message</param>
        protected virtual void IgnoredMessageHandler(Endpoint emitter, Message message)
        { }
        
        /// <summary>
        ///     Default handler
        ///     If a message does not have a custom handler, this method will be called instead
        /// </summary>
        /// <param name="emitter">Endpoint that received the message</param>
        /// <param name="message">Received message</param>
        protected virtual void DefaultHandler(Endpoint emitter, Message message)
        {
            Console.WriteLine($"Received an unhandled message: {message}");
        }

        #endregion
    }
}

namespace SampleProject
{
    using TBNF;
    using System;
    using System.Net.Sockets;

    public class TestHandler : MessageHandler
    {
        private void TestMessageHandler(TcpClient client, TestMessage message)
        {
            Console.WriteLine("Test message custom handler !");
        }
        
        private void TestMessage2Handler(TcpClient client, TestMessage2 message)
        {
            Console.WriteLine("Test message 2 custom handler !");
        }
    }
}

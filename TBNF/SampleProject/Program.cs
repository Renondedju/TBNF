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

namespace SampleProject
{
    using TBNF;
    using System;

    internal static class Program
    {
        private static void Main()
        {
            // Registering every message class defined in the assembly
            MessageRegister.RegisterAssembly(typeof(Program).Assembly);

            // Creating and packing the message to be sent over the network
            PackagedMessage package = new TestMessage {
                Data = {
                    Value1 = 1,
                    Value2 = 0,
                    Test   = "Some long string"
                }
            }.Pack();

            // The packages is received from the network
            TestHandler handler = new();
            Message     message = MessageBuilder.BuildMessage(package);

            handler.HandleMessage(null, message);
            
            Console.WriteLine($"Received message type: {message}");
            Console.WriteLine($"Total size of the (packaged) structure {package.Size} bytes");
        }
    }
}

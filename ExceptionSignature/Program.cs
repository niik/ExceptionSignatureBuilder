using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;

namespace freakcode.Utils
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TestThrow();
            }
            catch (Exception exc)
            {
                ExceptionSignatureBuilder sigBuilder = new ExceptionSignatureBuilder();

                sigBuilder.AddException(exc);
                string signature = sigBuilder.GetSignatureString();

                string indent = "    ";

                Console.WriteLine("TestThrow threw exception");
                Console.WriteLine();
                Console.WriteLine(indent + "Message: " + exc.Message);
                Console.WriteLine(indent + "Signature: " + signature);
            }

            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }

        private static void TestThrow()
        {
            try
            {
                TestThrowWithDynamicMessage();
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Could not complete action, see inner exception", exc);
            }
        }

        private static void TestThrowWithDynamicMessage()
        {
            try
            {
                TestThrowWithErrorCode();
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException(
                    "You can type anything you want within \"theese quotes\" or (theese parenthesis) or " +
                    "[theese brackets] or {theese braces} or: after the semicolon without changing the " +
                    "exception signature.",
                    exc
                );

            }
        }

        private static void TestThrowWithErrorCode()
        {
            // This will produce a localized exception message depending on your
            // current OS language. But the signature won't change since we only
            // look at the error code when available.
            throw new SocketException(10061);
        }
    }
}

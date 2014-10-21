/*
 * Copyright (c) 2008-2014 Markus Olsson
 * var mail = string.Join("@", new string[] {"j.markus.olsson", "gmail.com"});
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this 
 * software and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish, 
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING 
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

// TODO:
//  Detect and remove key-valuepair lines from exception messages?
//      message = "Why?\r\nFoo: Bar\r\n\Bar:Foo\r\n" => "Why?"

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;

namespace freakcode.Utils
{
    /// <summary>
    /// Generates exception signatures useful for grouping exceptions together. 
    /// The signatures are also useful when you wan't to give users an identifier or 
    /// "error-code" to use when they contact customer service. Use the ToString method
    /// to produce small friendly signatures (example: 781fe96d).
    /// </summary>
    public sealed class ExceptionSignatureBuilder : IDisposable
    {
        StringBuilder signatureBuffer;
        bool disposed;

        /// <summary>
        /// Used by the RemoveTextWithinStopChars method to trim unwanted text within exception messages.
        /// </summary>
        static readonly Dictionary<char, char> stopChars = new Dictionary<char, char> 
        {
            { '(', ')' },
            { '{', '}' },
            { '[', ']' },
            { '"', '"' },
            { '\'', '\'' }
        };

        static readonly char[] stopCharLookupChars = new char[] { '(', '{', '[', '"', '\'' };

        /// <summary>
        /// Gets or sets a value indicating whether to do "intelligent" processing of 
        /// exception messages in order to allow for some dynamic data in the messages 
        /// between two exception while still being able to produce the same signature.
        /// Also attempts to use error codes instead of exception messages for exception types
        /// that provide such (e.g. SocketException).
        /// Default is true.
        /// </summary>
        public bool PreprocessExceptionMessages { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use the complete stack trace when
        /// generating the signature. If set to false the builder only looks at the
        /// point of origin for each exception. Defaults to true.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if builder includes complete stack trace; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeCompleteStackTrace { get; set; }

        /// <summary>
        /// Initializes a new instance of the ExceptionSignatureBuilder class.
        /// </summary>
        public ExceptionSignatureBuilder()
        {
            PreprocessExceptionMessages = true;
            IncludeCompleteStackTrace = true;

            signatureBuffer = new StringBuilder();
        }

        /// <summary>
        /// Add one exception to the builder and traverse all its inner exceptions
        /// </summary>
        /// <param name="exc">The exception to add</param>
        public void AddException(Exception exception)
        {
            AddException(exception, true);
        }

        /// <summary>
        /// Add one exception to the builder optionally traversing all of its inner exceptions
        /// </summary>
        /// <param name="exc">The exception to add</param>
        /// <param name="traverseInnerException">Whether or not to include all inner exceptions in the signature</param>
        public void AddException(Exception exception, bool traverseInnerException)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            AssertNotDisposed();

            while (exception != null)
            {
                Type exceptionType = exception.GetType();

                signatureBuffer.Append(exceptionType.Name);
                signatureBuffer.Append(": ");

                AppendExceptionMessage(exception);

                signatureBuffer.AppendLine();

                if (IncludeCompleteStackTrace)
                {
                    AppendStackTrace(exception);
                }
                else
                {
                    AppendMethodInfo(exception.TargetSite);
                }

                signatureBuffer.AppendLine();

                if (!traverseInnerException)
                    break;

                exception = exception.InnerException;
            }
        }

        void AppendStackTrace(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            var st = new StackTrace(exception, false);

            StackFrame[] frames = st.GetFrames();

            if (frames == null || frames.Length == 0)
            {
                return;
            }

            foreach (StackFrame frame in frames)
            {
                if (frame != null)
                {
                    MethodBase mb = frame.GetMethod();

                    if (mb != null)
                    {
                        AppendMethodInfo(mb);
                    }
                }
            }
        }

        string AppendMethodInfo(MethodBase mb)
        {
            if (mb == null)
                throw new ArgumentNullException("mb");

            if (mb.DeclaringType != null)
            {
                signatureBuffer.Append(mb.DeclaringType.FullName);
            }
            else
            {
                signatureBuffer.Append("<unknown type>");
            }

            signatureBuffer.Append('.');
            signatureBuffer.Append(mb.Name);

            if (mb.IsGenericMethod)
            {
                signatureBuffer.Append('<');

                Type[] genericArgumentTypes = mb.GetGenericArguments();
                string[] genericArgumentNames = new string[genericArgumentTypes.Length];

                for (int i = 0; i < genericArgumentTypes.Length; i++)
                    genericArgumentNames[i] = genericArgumentTypes[i].Name;

                signatureBuffer.Append(string.Join(",", genericArgumentNames));

                signatureBuffer.Append('>');
            }

            signatureBuffer.Append('(');

            signatureBuffer.Append(string.Join(",", GetParameterStrings(mb.GetParameters())));

            signatureBuffer.Append(')');

            return signatureBuffer.ToString();
        }

        string[] GetParameterStrings(ParameterInfo[] parameterInfo)
        {
            if (parameterInfo == null)
            {
                throw new ArgumentNullException("parameterInfo");
            }

            var ps = new string[parameterInfo.Length];

            ParameterInfo pi;

            for (int i = 0; i < parameterInfo.Length; i++)
            {
                pi = parameterInfo[i];

                if (pi.ParameterType != null)
                {
                    ps[i] = pi.ParameterType.Name + " " + pi.Name;
                }
                else
                {
                    ps[i] = "<unknown type> " + pi.Name;
                }
            }

            return ps;
        }

        void AssertNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("ExceptionSignatureBuilder");
            }
        }

        /// <summary>
        /// Removes all signature data from the builder
        /// </summary>
        public void Clear()
        {
            signatureBuffer.Length = 0;
        }

        void AppendExceptionMessage(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            if (PreprocessExceptionMessages)
            {
                var se = exception as SocketException;

                if (se != null)
                {
                    AppendErrorCode(se.ErrorCode);
                    return;
                }

                var w32e = exception as Win32Exception;

                if (w32e != null)
                {
                    AppendErrorCode(w32e.ErrorCode);
                    return;
                }

                // ViewState exceptions tack on all debug data in their messages instead of 
                // properly attaching them to the Data property. Thus we need to do some 
                // preprocessing and remove everything after the first line break (if any).
                if (exception is ViewStateException && exception.Message != null)
                {
                    int p = exception.Message.IndexOf("\r\n");

                    if (p != -1)
                    {
                        // TODO: when/if we support removal of key-value pairs from 
                        // multi line exception messages we don't need this special 
                        // case plus we could get rid of the System.Web dependency.
                        signatureBuffer.Append(exception.Message.Substring(0, p));
                        return;
                    }
                }

                if (exception.Message != null)
                {
                    AppendPreprocessedMessage(signatureBuffer, exception.Message);
                }
            }
            else if (exception.Message != null)
            {
                signatureBuffer.Append(exception.Message);
            }
        }

        void AppendErrorCode(int errorCode)
        {
            signatureBuffer.Append("ErrorCode: ");
            signatureBuffer.Append(errorCode.ToString(CultureInfo.InvariantCulture));
        }

        internal static void AppendPreprocessedMessage(StringBuilder sb, string message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (message.Length == 0)
            {
                return;
            }

            int length = message.IndexOf(':');

            if (length == -1)
            {
                sb.Append(RemoveTextWithinStopChars(message));
            }
            else
            {
                sb.Append(RemoveTextWithinStopChars(message.Substring(0, length)));
            }
        }

        internal static string RemoveTextWithinStopChars(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (value.Length == 0)
                return value;

            if (value.IndexOfAny(stopCharLookupChars) == -1)
                return value;

            char[] chars = value.ToCharArray();

            // Read and write positions
            int rp = 0;
            int wp = 0;

            while (rp < chars.Length)
            {
                char c = chars[rp];
                char seekChar;

                if (stopChars.TryGetValue(c, out seekChar))
                {
                    if (c == seekChar)
                    {
                        // No balancing required; e.g. single and double quotes
                        do { rp++; } while (rp < chars.Length && chars[rp] != seekChar);
                    }
                    else
                    {
                        // Balanced stop characters e.g. parenthesis
                        int balanceCounter = 0;

                        while (rp < chars.Length)
                        {
                            if (chars[rp] == c)
                                balanceCounter++;
                            else if (chars[rp] == seekChar)
                                balanceCounter--;

                            if (balanceCounter == 0)
                                break;

                            rp++;
                        }
                    }
                }
                else
                {
                    if (wp != rp)
                        chars[wp] = c;

                    wp++;
                }

                rp++;
            }

            return new string(chars, 0, wp);
        }

        /// <summary>
        /// Returns the signature hash (128bit md5)
        /// </summary>
        public byte[] GetSignatureBytes()
        {
            AssertNotDisposed();

            byte[] buf = Encoding.ASCII.GetBytes(signatureBuffer.ToString());
            var hashProvider = MD5.Create();

            return hashProvider.ComputeHash(buf);
        }

        /// <summary>
        /// Returns a 32 characters long hex digest of the signature bytes
        /// </summary>
        /// <returns></returns>
        public string GetSignatureHashDigest()
        {
            return ToHex(GetSignatureBytes());
        }

        /// <summary>
        /// Computes a 8-character long exception signature taken from the beginning
        /// of the exception signature hash digest (ToSignatureHashDigest)
        /// </summary>
        public string GetSignatureString()
        {
            return GetSignatureHashDigest().Substring(0, 8);
        }

        static readonly char[] hexCharSet = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        /// <summary>
        /// "Hexifies" a byte array
        /// </summary>
        /// <returns>A lower-case hex encoded string representation of the byte array</returns>
        static string ToHex(byte[] buffer)
        {
            return ToHex(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// "Hexifies" a byte array
        /// </summary>
        /// <returns>A lower-case hex encoded string representation of the byte array</returns>
        static string ToHex(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "Offset cannot be less than 0");

            if (offset > buffer.Length)
                throw new ArgumentOutOfRangeException("offset", "Offset cannot be greater than buffer length");

            if (offset + length > buffer.Length)
                throw new ArgumentException("The offset and length values provided exceed buffer length");

            char[] charbuf = new char[checked(length * 2)];

            int c = -1;

            for (int i = offset; i < length + offset; i++)
            {
                charbuf[c += 1] = hexCharSet[buffer[i] >> 4];
                charbuf[c += 1] = hexCharSet[buffer[i] & 0x0F];
            }

            return new string(charbuf);
        }

        /// <summary>
        /// Computes a 8-character long exception signature taken from the beginning
        /// of the exception signature hash digest (ToSignatureHashDigest)
        /// </summary>
        public override string ToString()
        {
            return GetSignatureString();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (signatureBuffer != null)
                {
                    signatureBuffer = null;
                }

                disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}

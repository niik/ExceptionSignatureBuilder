using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace freakcode.Utils.Tests
{
    [TestClass]
    public class ExceptionSignatureBuilderTest : TestBase
    {
        // Simple Exception
        private Exception ExcA;

        // Simple Exception
        private Exception ExcB;

        // Same as ExcB
        private Exception ExcB2;

        // Exception with ExcA as inner
        private Exception ExcCA;
        private Exception ExcCA2;

        // Exception with ExcB as inner
        private Exception ExcCB;
        private Exception ExcCB2;

        private Exception ExcThrown;

        public ExceptionSignatureBuilderTest()
        {
            ExcA = new ApplicationException("foo");

            ExcB = new ApplicationException("bar");
            ExcB2 = new ApplicationException("bar");

            ExcCA = new ApplicationException("foo", ExcA);
            ExcCA2 = new ApplicationException("foo", ExcA);

            ExcCB = new ApplicationException("foo", ExcB);
            ExcCB2 = new ApplicationException("foo", ExcB);

            try
            {
                ThrowException();
            }
            catch (Exception exc)
            {
                ExcThrown = exc;
            }
        }

        private void ThrowException()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void PreProcessExceptionMessagesTest()
        {
            ExceptionSignatureBuilder target = new ExceptionSignatureBuilder();
            target.PreprocessExceptionMessages = true;

            var exc1 = new ArgumentException("foo: bar");
            var exc2 = new ArgumentException("foo: foo");

            target.AddException(exc1);
            var sig1 = target.ToString();

            target.Clear();

            target.AddException(exc2);
            var sig2 = target.ToString();

            Assert.AreEqual(sig1, sig2, "Expected exception signatures to be equal when using message preprocessing");

            target.Clear();
            target.PreprocessExceptionMessages = false;

            target.AddException(exc1);
            var sig3 = target.ToString();

            target.Clear();

            target.AddException(exc2);
            var sig4 = target.ToString();

            Assert.AreEqual(sig1, sig2, "Expected exception signatures to be unequal when not using message preprocessing");
        }

        [TestMethod]
        public void ToStringTest()
        {
            var target = new ExceptionSignatureBuilder();
            target.AddException(ExcA);

            Assert.AreEqual("4e483d7c", target.ToString());
        }


        [TestMethod]
        public void ToSignatureHashDigestTest()
        {
            var target = new ExceptionSignatureBuilder();

            target.AddException(ExcA);

            Assert.AreEqual("4e483d7cb4f2304ff7baf38d59b8822b", target.GetSignatureHashDigest());
        }

        [TestMethod]
        public void GetSignatureBytesTest()
        {
            var target = new ExceptionSignatureBuilder();
            var bytes = target.GetSignatureBytes();

            Assert.AreEqual(16, bytes.Length);
        }

        [TestMethod]
        public void GetExceptionMessageTest()
        {
            var sb = new StringBuilder();

            ExceptionSignatureBuilder_Accessor.AppendPreprocessedMessage(sb, "foo'bar'('bar')(bar)\"bar\"((foo)): bar");

            Assert.AreEqual("foo", sb.ToString());
            sb.Length = 0;

            Assert.AreEqual("", ExceptionSignatureBuilder.RemoveTextWithinStopChars("\'"));

            Assert.AreEqual("foo", ExceptionSignatureBuilder.RemoveTextWithinStopChars("foo"));
            Assert.AreEqual(string.Empty, ExceptionSignatureBuilder.RemoveTextWithinStopChars(string.Empty));

            Assert.AreEqual("foo", ExceptionSignatureBuilder.RemoveTextWithinStopChars("foo((bar) foo"));
            Assert.AreEqual("foo.", ExceptionSignatureBuilder.RemoveTextWithinStopChars("foo(('bar'))."));
            Assert.AreEqual("foo.", ExceptionSignatureBuilder.RemoveTextWithinStopChars("foo'(foo'."));
        }

        [TestMethod]
        public void DisposeTest()
        {
            var target = new ExceptionSignatureBuilder();
            target.Dispose();

            // Should be disposable twice
            target.Dispose();

            AssertException<InvalidOperationException>(() => target.AddException(ExcA));
        }

        [TestMethod]
        public void ClearTest()
        {
            var target = new ExceptionSignatureBuilder();

            var emptySig = target.ToString();

            target.AddException(ExcA);
            var nonEmptySig = target.ToString();
            target.Clear();

            Assert.AreEqual(emptySig, target.ToString());
            Assert.AreNotEqual(emptySig, nonEmptySig);
        }

        [TestMethod]
        public void AssertNotDisposedTest()
        {
            var target = new ExceptionSignatureBuilder_Accessor();
            target.AssertNotDisposed();
            target.Dispose();

            AssertException<InvalidOperationException>(() => target.AssertNotDisposed());
        }

        [TestMethod]
        public void AddExceptionTest()
        {
            var target = new ExceptionSignatureBuilder();

            target.AddException(ExcCA);
            var sig1 = target.ToString();

            target.Clear();
            target.AddException(ExcCA, false);
            var sig2 = target.ToString();

            Assert.IsFalse(sig1 == sig2, "Exception with inner exception should produce different results when toggling traverseException");
        }


    }
}
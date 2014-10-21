using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace freakcode.Utils.Tests
{
    public abstract class TestBase
    {
        protected void AssertException(Action a)
        {
            AssertException<Exception>(a);
        }

        protected void AssertException<T>(Action a) where T : Exception
        {
            AssertException<T>(a, null);
        }

        protected void AssertException<T>(Action a, Func<T, bool> validateException) where T : Exception
        {
            AssertException<T>(a, validateException, null);
        }

        protected void AssertException<T>(Action a, Func<T, bool> validateException, string valFailedMessage) where T : Exception
        {
            Type exceptionType = typeof(T);

            try
            {
                a();
            }
            catch (Exception e)
            {
                var te = e as T;

                if (te != null)
                {
                    if (validateException != null && !validateException(te))
                    {
                        if (valFailedMessage != null)
                            Assert.Fail(valFailedMessage);
                        else
                            Assert.Fail(exceptionType.Name + " did not fulfill requirements");
                    }
                    return;
                }
            }

            Assert.Fail("Method did not throw " + exceptionType.Name);
        }

        protected void AssertArgNullException(Action a)
        {
            AssertException<ArgumentNullException>(a);
        }

        protected void AssertArgNullException(Action a, string paramName)
        {
            if (paramName == null)
                throw new ArgumentNullException(paramName);

            AssertException<ArgumentNullException>(a, e => e.ParamName == paramName, "ArgumentNullException param name mismatch. Expected \"" + paramName + "\"");
        }
    }
}

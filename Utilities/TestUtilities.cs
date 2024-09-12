using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities {
	public class TestUtilities : IDisposable {
        protected static Stack<TestUtilities> TestStack = new Stack<TestUtilities>();

        protected TestUtilities(){
            TestStack.Push(this);
        }

        public static TestUtilities CreateFrame()
        {
            return new TestUtilities();
        }
        protected  List<String> LogList = new List<String>();
        public static void Log(string message)
        {
            if (TestStack.Any()) {
                TestStack.Peek().LogList.Add(message);
            } else {
                //Nothing to do...
            }
        }
        public bool Contains(string message)
        {
            return LogList.Any(x => x == message);
        }
        public bool EnsureContains(string message)
        {
            if (!Contains(message))
                throw new Exception("Test Log does not contain '"+message+"'.");
            return true;
        }

        public static bool TopContains(string message)
        {
            if (TestStack.Any()) {
                return TestStack.Peek().Contains(message);
            } 
            return false;
        }
        
        public void Dispose()
        {
            TestStack.Pop();
        }
    }
}

using RadialReview.Utilities;
using System;
using System.Text;

namespace RadialReview {
	public class Debug {


		public static object lck = new object();
		public static int STACK_LEVEL = 0;

		public static void BeginStack(string line) {
			Debug.WriteLine(line);
			STACK_LEVEL += 1;
		}
		public static void EndStack(string line) {
			STACK_LEVEL -= 1;
			Debug.WriteLine(line);
		}

		private static string GetStackShift() {
			return new StringBuilder().Append('.', STACK_LEVEL).ToString();
		}

		public static void WriteLine(string line) {
			WriteLine("{0}", line);
		}

		public static void WriteStackTrace(string message) {
			WriteLine(message);
			WriteLine(GenerateStackTrace());
		}

		public static void WriteLine(string format, params object[] args) {
#if DEBUG
			if (Config.IsLocal()) {
				lock (lck) {
					Console.WriteLine("[LDEBUG] " + GetStackShift() + " " + format, args);
				}
			}
#endif
		}
		public static string GenerateStackTrace() {
#if DEBUG
			if (Config.IsLocal()) {
				return (new System.Diagnostics.StackTrace()).ToString();
			}
#endif
			return "";
		}

    public static void WriteStackTraceLine(int line)
    {
#if DEBUG
			if (Config.IsLocal()) {
        var st = GenerateStackTrace();
        var split = st.Split("\r\n");
        if (line < split.Length){
          Debug.WriteLine(split[line]);
        }else{
          Debug.WriteLine("Unknown line");
        }
      }
#endif
    }
  }
}

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace RadialReview.Utilities {
	public class ExceptionUtility {
		public static bool IsInException() {
			return Marshal.GetExceptionPointers() != IntPtr.Zero || Marshal.GetExceptionCode() != 0;
		}
	}
}
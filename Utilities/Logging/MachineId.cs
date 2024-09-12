using log4net.Core;
using log4net.Layout.Pattern;
using System;
using System.IO;
using System.Text;

namespace RadialReview.Utilities.Logging {

	[Flags]
	public enum MachineType {
		None = 0,
		Hangfire = 8, //H is 01001000
		Other = 32,	  //makes character lowercase
	}

	public sealed class MachineId : PatternLayoutConverter {

		public MachineId() { }
		public static string MACHINE_ID;

		static MachineId() {
			MACHINE_ID = System.Convert.ToBase64String(Encoding.UTF8.GetBytes("" + Guid.NewGuid()));
			MACHINE_ID = MACHINE_ID.Substring(0, Math.Min(5, MACHINE_ID.Length));
			AddMachineType(MachineType.None);
		}


		override protected void Convert(TextWriter writer, LoggingEvent loggingEvent) {
			writer.Write(MACHINE_ID);
		}

		public static void AddMachineType(MachineType mt) {
			var v = GetMachineTypeCharacter(mt);
			MACHINE_ID = v + MACHINE_ID.Substring(1, MACHINE_ID.Length - 1);
		}

		public static char GetMachineTypeCharacter(MachineType mt) {
			var c= (char)(64+(byte)mt); //makes an ascii char 64 = "@"
			return c;
		}
	}
}

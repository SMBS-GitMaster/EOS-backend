using System;
using System.Security.Cryptography;

namespace RadialReview.Utilities {
	public static class RandomUtil {
		public static double NextNormal(this Random rand, double mean = 0, double stdDev = 1) {
			var u1 = rand.NextDouble(); //these are uniform(0,1) random doubles
			var u2 = rand.NextDouble();
			var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
			return mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
		}

		public static string SecureRandomString(int length = 16) {
			if (length < 0)
				throw new IndexOutOfRangeException();
			RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
			var byteArray = new byte[length];
			provider.GetBytes(byteArray);

			return Convert.ToBase64String(byteArray)
				.Replace("+", "")
				.Replace("/", "")
				.Replace("=", "");
		}

		public static Guid SecureRandomGuid() {
			using (var provider = new RNGCryptoServiceProvider()) {
				var bytes = new byte[16];
				provider.GetBytes(bytes);
				return new Guid(bytes);
			}
		}
	}
}

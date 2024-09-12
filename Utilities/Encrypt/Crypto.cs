using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace RadialReview.Utilities.Encrypt {
	public class Crypto {
		private static byte[] _salt = Encoding.ASCII.GetBytes("0205EE5E-9788-4FAB-BEB4-35FB60260800");
		private const string DEFAULT_UNIQUEHASH_SECRET = "03E29627-61C3-4818-B251-FA7B25689607";


		public static bool Matches(string plainText, string uniqueHash, string sharedSecret = DEFAULT_UNIQUEHASH_SECRET) {
			if (plainText == null || uniqueHash == null)
				return false;
			try {
				return UniqueHash(plainText, sharedSecret) == uniqueHash;
			} catch (Exception e) {
				return false;
			}

		}

		/// <summary>
		/// Unsafe method only used to generate as hash based on input.
		/// </summary>
		/// <param name="plainText"></param>
		/// <param name="sharedSecret"></param>
		/// <returns></returns>
		public static string UniqueHash(string plainText, string sharedSecret = DEFAULT_UNIQUEHASH_SECRET) {
			if (string.IsNullOrEmpty(plainText))
				throw new ArgumentNullException("plainText");
			if (string.IsNullOrEmpty(sharedSecret))
				throw new ArgumentNullException("sharedSecret");

			using (SHA256 mySHA256 = SHA256.Create()) {
				byte[] hashValue = mySHA256.ComputeHash((plainText + "_" + sharedSecret).ToBytes());
				return "Q" + Convert.ToBase64String(hashValue.Take(14).ToArray()).Replace("/", "0").Replace("=", "a").Replace("+", "b");

			}
		}


		/// <summary>
		/// Encrypt the given string using AES.  The string can be decrypted using 
		/// DecryptStringAES().  The sharedSecret parameters must match.
		/// </summary>
		/// <param name="plainText">The text to encrypt.</param>
		/// <param name="sharedSecret">A password used to generate a key for encryption.</param>
		public static string EncryptStringAES(string plainText, string sharedSecret) {
			if (string.IsNullOrEmpty(plainText))
				throw new ArgumentNullException("plainText");
			if (string.IsNullOrEmpty(sharedSecret))
				throw new ArgumentNullException("sharedSecret");

			string outStr = null;                       // Encrypted string to return              
			RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.    

			try {
				// generate the key from the shared secret and the salt
				Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

				// Create a RijndaelManaged object
				aesAlg = new RijndaelManaged();
				aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

				// Create a decryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				using (MemoryStream msEncrypt = new MemoryStream()) {
					// prepend the IV
					msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
					msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write)) {
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt)) {
							//Write all data to the stream.
							swEncrypt.Write(plainText);
						}
					}
					outStr = Convert.ToBase64String(msEncrypt.ToArray());
				}
			} finally {
				// Clear the RijndaelManaged object.
				if (aesAlg != null)
					aesAlg.Clear();
			}

			// Return the encrypted bytes from the memory stream.
			return outStr;
		}

		/// <summary>
		/// Decrypt the given string.  Assumes the string was encrypted using 
		/// EncryptStringAES(), using an identical sharedSecret.
		/// </summary>
		/// <param name="cipherText">The text to decrypt.</param>
		/// <param name="sharedSecret">A password used to generate a key for decryption.</param>
		public static string DecryptStringAES(string cipherText, string sharedSecret) {
			if (string.IsNullOrEmpty(cipherText))
				throw new ArgumentNullException("cipherText");
			if (string.IsNullOrEmpty(sharedSecret))
				throw new ArgumentNullException("sharedSecret");

			// Declare the RijndaelManaged object
			// used to decrypt the data.
			RijndaelManaged aesAlg = null;

			// Declare the string used to hold
			// the decrypted text.
			string plaintext = null;

			try {
				// generate the key from the shared secret and the salt
				Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

				// Create the streams used for decryption.             
				byte[] bytes = Convert.FromBase64String(cipherText);
				using (MemoryStream msDecrypt = new MemoryStream(bytes)) {
					// Create a RijndaelManaged object
					// with the specified key and IV.
					aesAlg = new RijndaelManaged();
					aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
					// Get the initialization vector from the encrypted stream
					aesAlg.IV = ReadByteArray(msDecrypt);
					// Create a decrytor to perform the stream transform.
					ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read)) {
						using (StreamReader srDecrypt = new StreamReader(csDecrypt))

							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							plaintext = srDecrypt.ReadToEnd();
					}
				}
			} finally {
				// Clear the RijndaelManaged object.
				if (aesAlg != null)
					aesAlg.Clear();
			}

			return plaintext;
		}

		private static byte[] ReadByteArray(Stream s) {
			byte[] rawLength = new byte[sizeof(int)];
			if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length) {
				throw new SystemException("Stream did not contain properly formatted byte array");
			}

			byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
			if (s.Read(buffer, 0, buffer.Length) != buffer.Length) {
				throw new SystemException("Did not read byte array properly");
			}

			return buffer;
		}
	}
}

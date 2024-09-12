using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System;
using System.Threading.Tasks;
using RadialReview.Models.Application;
using RadialReview.Variables;
using RadialReview.Utilities.Encrypt;
using System.Net;

namespace RadialReview.Accessors {
	public partial class UserAccessor : BaseAccessor {

		public class VerificationEmailTemplate {
			public string Subject { get; set; }
			public string Body { get; set; }
		}

		public static async Task<bool> ReceiveVerification_Unsafe(string token, string x) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var id = Crypto.DecryptStringAES(x, ENCRYPT_VERIFICATION_ID);
					var found = Unsafe.GetUserModelById(s, id);
					if (found == null) {
						throw new PermissionsException("Could not verify. Please contact support (code 55).");
					}

					if (!Crypto.Matches(found.Email, token))
						throw new PermissionsException("Could not verify. Please contact support (code 65).");
					if (found.EmailNotVerified == false) {
						await NotificationAccessor.DeleteGroupKey_Unsafe(s, NotificationGroupKey.VerifyEmail(id));
						tx.Commit();
						s.Flush();
						return false;
					}

					found.EmailNotVerified = false;
					s.Update(found);
					await NotificationAccessor.DeleteGroupKey_Unsafe(s, NotificationGroupKey.VerifyEmail(id));
					tx.Commit();
					s.Flush();
					return true;
				}
			}
		}

		private static readonly string ENCRYPT_VERIFICATION_ID = "F692D105-21D9-4C59-8A99-025B3B812339";
		public static async Task SendVerificationEmail_Unsafe(ISession s, string email, int timeZoneOffsetMinutes) {
			email = email.ToLower();
			VerificationEmailTemplate template;
			var found = Unsafe.GetUserByEmail(s, email);
			if (found == null) {
				throw new PermissionsException("Could not verify user. Please contact support (code:44).");
			}

			template = s.GetSettingOrDefault(Variable.Names.VERIFICATION_EMAIL_TEMPLATE, () => new VerificationEmailTemplate() { Subject = "Verify your {4} Account", Body = @"<center><img src=""{5}"" width=""300"" height=""auto"" alt=""{4}"" /></center><p>A request was made on {1} at {2} to verify this email address ({0}) with {4}.</p>
<br/><div style='text-align:center;'>" + Emailer.GenerateHtmlButton("{3}", "Verify", "#ffffff", "#033248", 90) + @"</div>
<br/>
<p>If this was not you, please reach out immediately.</p>
<br/>
<p>Thanks!</p>
<p>The {4} Team</p>
<br/>
<p style='color:#4444444;'>If the above button does not work please go here:
<br/><a style='color:#8aace6;' href='{3}'>{3}</a></p>" });
			if (!found.EmailNotVerified)
				throw new PermissionsException("Account is already verified.");
			var now = DateTime.UtcNow;
			var date = now.AddMinutes(timeZoneOffsetMinutes).ToLongDateString();
			var time = now.AddMinutes(timeZoneOffsetMinutes).ToShortTimeString();
			var productLogo = Config.ProductLogoUrl();
			var token = Crypto.UniqueHash(email);
			var idEnc = Crypto.EncryptStringAES(found.Id, ENCRYPT_VERIFICATION_ID);
			var verificationUrl = Config.BaseUrl(null, "/account/verify?token=" + WebUtility.UrlEncode(token) + "&x=" + WebUtility.UrlEncode(idEnc));
			var productName = Config.ProductName();
			try {
				var sign = Math.Sign(timeZoneOffsetMinutes);
				var abs = Math.Abs(timeZoneOffsetMinutes);
				time += string.Format("{0}{1:00}:{2:00}", sign >= 0 ? "+" : "-", abs / 60, abs % 60);
			} catch (Exception) {
				int a = 0;
			}

			var fields = new string[] { email, date, time, verificationUrl, productName, productLogo };
			await Emailer.EnqueueEmail(Mail.To("Verification", email).Subject(template.Subject, fields).Body(template.Body, fields), true);
		}

		public static async Task SendVerificationEmail_Unsafe(string email, int timeZoneOffsetMinutes) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await SendVerificationEmail_Unsafe(s, email, timeZoneOffsetMinutes);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void ValidateUniqueEmail(string email) {
			if (string.IsNullOrEmpty(email))
				throw new PermissionsException("Email must have value.");
			if (!Emailer.IsValid(email))
				throw new PermissionsException(ExceptionStrings.InvalidEmail);


		}
	}
}

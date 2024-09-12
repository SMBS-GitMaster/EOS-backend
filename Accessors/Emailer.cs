using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using RadialReview.Utilities.DataTypes;
using Mandrill.Models;
using System.Text.RegularExpressions;
using Mandrill;
using Mandrill.Requests.Messages;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Hangfire;
using Hangfire;
using System.Diagnostics;
using System.Threading;
namespace RadialReview.Accessors {
	public class EmailResult {
		public int Sent { get; set; }
		public int Unsent { get; set; }
		public int Queued { get; set; }
		public int Total { get; set; }
		public int Faults { get; set; }
		public TimeSpan TimeTaken { get; set; }
		public List<Exception> Errors { get; set; }

		public EmailResult() {
			Errors = new List<Exception>();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="successMessage">
		///     {0} = Sent,<br/>
		///     {1} = Unsent,<br/>
		///     {2} = Total,<br/>
		///     {3} = TimeTaken(InSeconds),<br/>
		///     </param>
		/// <returns></returns>
		public ResultObject ToResults(String successMessage) {
			if (Errors.Count() > 0) {
				var message = String.Join(",\n", Errors.Select(x => x.Message).Distinct());
				return new ResultObject(new RedirectException(Errors.Count() + " errors:\n" + message));
			}
			return ResultObject.Create(false, String.Format(successMessage, Sent, Unsent, Total, TimeTaken.TotalSeconds));

		}
	}



	public class Emailer : BaseAccessor {
		private static Regex _regex = CreateRegEx();

		public static string GenerateHtmlButton(string url, string contents, string hexFillColor, string hexBackgroundColor, int width) {
			return HtmlUtility.GenerateHtmlButton(url, contents, hexFillColor, hexBackgroundColor, width);
		}

		public bool IsValid(object value) {
			if (value == null) {
				return true;
			}

			string valueAsString = value as string;

			// Use RegEx implementation if it has been created, otherwise use a non RegEx version.
			if (_regex != null) {
				return valueAsString != null && _regex.Match(valueAsString).Length > 0;
			} else {
				int atCount = 0;

				foreach (char c in valueAsString) {
					if (c == '@') {
						atCount++;
					}
				}

				return (valueAsString != null
				&& atCount == 1
				&& valueAsString[0] != '@'
				&& valueAsString[valueAsString.Length - 1] != '@');
			}
		}

		private static Regex CreateRegEx() {
			// We only need to create the RegEx if this switch is enabled.


			const string pattern = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
			const RegexOptions options = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

			// Set explicit regex match timeout, sufficient enough for email parsing
			// Unless the global REGEX_DEFAULT_MATCH_TIMEOUT is already set
			TimeSpan matchTimeout = TimeSpan.FromSeconds(2);

			try {
				if (AppDomain.CurrentDomain.GetData("REGEX_DEFAULT_MATCH_TIMEOUT") == null) {
					return new Regex(pattern, options, matchTimeout);
				}
			} catch {
			}

			// Legacy fallback (without explicit match timeout)
			return new Regex(pattern, options);
		}





		#region Helpers
		public static String _EmailBodyWrapper(String htmlBody, int? tableWidth = null, bool? showHeadImg = true, bool? showV3Features = false) {
			var footer = String.Format(EmailStrings.Footer, ProductStrings.CompanyName);
      string imgDisplay = showHeadImg == false? "none" : "block";
      string onlyV1FeaturesDisplay = showV3Features == true ? "none" : "block";

      return String.Format(EmailStrings.BodyWrapper, htmlBody, footer, tableWidth ?? 600, imgDisplay, onlyV1FeaturesDisplay);
		}

		public static bool IsValid(string emailaddress) {
			if (emailaddress == null)
				return false;
			try {
				MailAddress m = new MailAddress(emailaddress);
				return true;
			} catch (FormatException) {
				return false;
			}
		}


		/// <summary>
		/// Display's a link with or without query parameters.
		/// </summary>
		/// <returns></returns>
		public static string ReplaceLink(string text, bool hideQueryParams = false) {
			if (!hideQueryParams)
				return Regex.Replace(text,
					@"((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)",
					"<a target='_blank' href='$1'>$1</a>");

			return Regex.Replace(text,
				@"(((http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@^=%&amp;:/~\+#]*[\w\-\@^=%&amp;/~\+#])?)([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?)",
				"<a target='_blank' href='$1'>$2</a>"
			);
		}


		#endregion
		#region AsyncMailer


		private static Pool<SmtpClient> SmtpPool = new Pool<SmtpClient>(30, TimeSpan.FromMinutes(2), () => new SmtpClient {
			Host = ConstantStrings.SmtpHost,
			Port = int.Parse(ConstantStrings.SmtpPort),
			Timeout = 50000,
			EnableSsl = true,
			Credentials = new System.Net.NetworkCredential(ConstantStrings.SmtpLogin, ConstantStrings.SmtpPassword)
		});

		private static string FixEmail(string email) {
			return Config.FixEmail(email);


		}

		private static SendMessageRequest CreateMandrillMessageRequest(EmailModel email) {
			var message = CreateMandrillMessage(email);
			return new SendMessageRequest(message);
		}

		private static EmailMessage CreateMandrillMessage(EmailModel email) {
			var toAddress = FixEmail(email.ToAddress);

			var toAddresses = new EmailAddress(toAddress).AsList();
			if (email.Bcc != null) {
				foreach (var bcc in email.Bcc.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
					var fixedBcc = FixEmail(bcc);
					toAddresses.Add(new EmailAddress(fixedBcc) { Type = "bcc" });
				}
			}

			var body = email.Body;
			if (email._TrackerId != null) {
				try {
					body += "<br/><img src='" + Config.BaseUrl(null) + "t/mark/" + email._TrackerId + "'/>";
				} catch (Exception e) {
				}
			}

			var oEmail = new EmailMessage() {
				FromEmail = MandrillStrings.FromAddress,
				FromName = email._ReplyToName ?? MandrillStrings.FromName,
				Html = body,
				Subject = email.Subject,
				To = toAddresses,
				TrackOpens = true,
				TrackClicks = true,
				GoogleAnalyticsDomains = Config.GetMandrillGoogleAnalyticsDomain().NotNull(x => x.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList()),
			};

			if (email._Attachments != null && email._Attachments.Any()) {
				oEmail.Attachments = email._Attachments;
			}

			if (!string.IsNullOrWhiteSpace(email._ReplyToEmail)) {
				oEmail.AddHeader("Reply-To", email._ReplyToEmail);
			}


			return oEmail;
		}

		private static async Task<List<Mandrill.Models.EmailResult>> SendMessage(MandrillApi api, EmailModel email, Action<EmailResultStatus> onComplete = null) {
			var sw = Stopwatch.StartNew();
			var status = EmailResultStatus.Queued;
			try {
				var result = await api.SendMessage(CreateMandrillMessageRequest(email));
				email.MandrillId = result.FirstOrDefault().NotNull(x => x.Id);
				status = EmailResultStatus.Scheduled;
				return result;
			} catch (Exception e) {
				log.Error("Failed to send email to mandrill", e);
				var errRes = new Mandrill.Models.EmailResult();
				if (email != null) {
					errRes.Email = email.ToAddress;
					errRes.Id = null;
					errRes.RejectReason = "Fatal error. Failed to send to mandrill";
					errRes.Status = EmailResultStatus.Rejected;
				}
				status = EmailResultStatus.Rejected;
				return errRes.AsList();
			} finally {
				//Status updater
				if (onComplete != null) {
					try {
						onComplete(status);
					} catch (Exception e) {
					}
				}
			}
		}

		private static async Task<int> SendMandrillEmails(List<EmailModel> emails, EmailResult result, bool forceSend = false, Action<Ratio> onStatusUpdate = null) {

			var api = new MandrillApi(ConstantStrings.MandrillApiKey, true);
			var results = new List<Mandrill.Models.EmailResult>();
			var counter = 0;
			var ratio = new Ratio(0, emails.Count);
			Action<EmailResultStatus> updateStatus = null;

			//Status updater
			if (onStatusUpdate != null) {
				updateStatus = new Action<EmailResultStatus>(x => {
					if (onStatusUpdate != null) {
						try {
							var sent = Interlocked.Increment(ref counter);
							ratio.Numerator = sent;
							onStatusUpdate(ratio);
						} catch (Exception e) {
						}
					}
				});
			}

			if (!emails.Any())
				return 1;
			if (Config.SendEmails() || forceSend) {
				results = (await Task.WhenAll(emails.Select(email => SendMessage(api, email, updateStatus)))).SelectMany(x => x).ToList();
			} else {
				results = emails.Select(x => new Mandrill.Models.EmailResult() {
					Status = EmailResultStatus.Sent,
					Email = x.ToAddress,
				}).ToList();
			}
			var now = DateTime.UtcNow;
			foreach (var r in results) {
				switch (r.Status) {
					case EmailResultStatus.Invalid: {
							result.Unsent += 1;
							result.Errors.Add(new Exception("Invalid"));
							break;
						}
					case EmailResultStatus.Queued:
						result.Queued += 1;
						break;
					case EmailResultStatus.Rejected: {
							result.Unsent += 1;
							result.Errors.Add(new Exception(r.RejectReason));
							break;
						}
					case EmailResultStatus.Scheduled:
						result.Queued += 1;
						break;
					case EmailResultStatus.Sent: {
							result.Sent += 1;
							try {
								var found = emails.First(x => x.ToAddress.ToLower() == r.Email.ToLower());
								found.Sent = true;
								found.CompleteTime = now;
							} catch (Exception) {
							}
						}
						break;
					default:
						break;
				}
			}


			return 1;
		}


		[Queue(HangfireQueues.Immediate.EXECUTE_TASKS)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task EnqueueEmail(Mail email, bool wrapped = true) {
			Scheduler.Enqueue(() => Emailer.SendEmail(email, false, wrapped));
		}

		[Queue(HangfireQueues.Immediate.EMAILER)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<EmailResult> SendEmail(Mail email, bool forceSend = false, bool wrapped = true) {
			return await SendEmails(email.AsList(), forceSend: forceSend, wrapped: wrapped);
		}

		public static async Task<EmailResult> SendEmails(IEnumerable<Mail> emails, bool forceSend = false, bool wrapped = true, Action<Ratio> onStatusUpdate = null, int? tableWidth = null, bool? showHeadImg = null, bool? showV3Features = null) {
			return await SendEmailsWrapped(emails, forceSend: forceSend, wrapped: wrapped, onStatusUpdate: onStatusUpdate, tableWidth: tableWidth, showHeadImg: showHeadImg, showV3Features: showV3Features);
		}

		private static async Task<EmailResult> SendEmailsWrapped(IEnumerable<Mail> emails, bool forceSend = false, int? tableWidth = null, bool wrapped = true, Action<Ratio> onStatusUpdate = null, bool? showHeadImg = null, bool? showV3Features = null) {
			//Register emails
			var unsentEmails = new List<EmailModel>();
			var now = DateTime.UtcNow;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var email in emails) {
						try {
							var unsent = new EmailModel() {
								Body = wrapped ? _EmailBodyWrapper(email.HtmlBody, tableWidth, showHeadImg, showV3Features) : email.HtmlBody,
								CompleteTime = null,
								Sent = false,
								Subject = email.Subject,
								ToAddress = email.ToAddress.Trim(),
								Bcc = String.Join(",", email.BccList),
								SentTime = now,
								EmailType = email.EmailType,
								_ReplyToName = email.ReplyToName,
								_ReplyToEmail = email.ReplyToAddress,
								_TrackerId = email.TrackerId,
							};

							if (email.Attachment != null) {
								unsent._Attachments = new List<EmailAttachment>() {
									email.Attachment
								};
							}


							s.Save(unsent);
							unsentEmails.Add(unsent);
						} catch (Exception e) {
							log.Error("Failed to build email", e);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}

			var result = new EmailResult() { Total = unsentEmails.Count };
			//Now send everything
			var startSending = DateTime.UtcNow;

			//And... Go.
			var threads = await SendMandrillEmails(unsentEmails, result, forceSend, onStatusUpdate);

			result.TimeTaken = DateTime.UtcNow - startSending;

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var email in unsentEmails) {
						s.Update(email);
					}
					tx.Commit();
					s.Flush();
				}
			}

			return result;
		}



		#endregion

		#region oldSyncMailer
		#endregion
	}

}

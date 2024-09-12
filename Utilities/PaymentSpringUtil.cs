using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Payments;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities {
	public class PaymentSpringUtil {

		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public class PaymentResult {
			public string id { get; set; }
			public string @class { get; set; }
			public DateTime created_at { get; set; }
			public string status { get; set; }
			public string reference_number { get; set; }
			public decimal amount_refunded { get; set; }
			public decimal amount_settled { get; set; }
			public string card_owner_name { get; set; }
			public string email { get; set; }
			public string description { get; set; }
			public string customer_id { get; set; }
			public string merchant_id { get; set; }
			public string card_number { get; set; }
		}
		public class RefundResult {
			private string _cardNumber;
			public string id { get; set; }
			public string @class { get; set; }
			public DateTime created_at { get; set; }
			public string status { get; set; }
			public string reference_number { get; set; }
			public decimal amount_refunded { get; set; }
			public decimal amount_settled { get; set; }
			public string card_owner_name { get; set; }
			public string email { get; set; }
			public string description { get; set; }
			public string customer_id { get; set; }
			public string merchant_id { get; set; }
			public string card_number { get; set; }
		}

		public static async Task<PaymentResult> ChargeToken(OrganizationModel org, PaymentSpringsToken token, decimal amount, bool forceTest = false) {
			return await ChargeToken(org, token.CustomerToken, amount, forceTest, token.TokenType == PaymentSpringTokenType.BankAccount);
		}

		[Obsolete("Unsafe")]
		public static async Task<PaymentResult> ChargeToken(OrganizationModel org, string token, decimal amount, bool forceTest, bool bankAccount) {
			//CURL 
			using (var client = new HttpClient()) {
				// Create the HttpContent for the form to be posted.

				var requestContent = new FormUrlEncodedContent(new[] {
				new KeyValuePair<string, string>("customer_id", token),
				new KeyValuePair<string, string>("amount", ""+((int)(amount*100))),
				new KeyValuePair<string, string>("charge_bank_account",bankAccount?"true":"false")
			});


				log.Info("ExecutingCharge [" + token + "][" + org.Id + "] " + amount);


				var privateApi = Config.PaymentSpring_PrivateKey(forceTest);
				var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

				//added
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				var response = await client.PostAsync("https://api.paymentspring.com/api/v1/charge", requestContent);
				var responseContent = response.Content;
				using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
					var result = await reader.ReadToEndAsync();
					log.Debug("Charged Card: " + result);
					if (JsonConvert.DeserializeObject<dynamic>(result).errors != null) {
						var builder = new List<string>();
						var errArray = JsonConvert.DeserializeObject<dynamic>(result).errors as JArray;
						if (errArray.HasValues) {
							for (var i = 0; i < errArray.Count; i++) {
								builder.Add(errArray[i]["message"] + " (" + errArray[i]["code"] + ").");
							}
						}
						throw new PaymentException(org, amount, PaymentExceptionType.ResponseError, String.Join(" ", builder));
					}
					if (JsonConvert.DeserializeObject<dynamic>(result).@class != "transaction")
						throw new PermissionsException("Response must be of type 'transaction'. Found " + JsonConvert.DeserializeObject<dynamic>(result).@class);
					return new PaymentResult {
						id = JsonConvert.DeserializeObject<dynamic>(result).id,
						@class = JsonConvert.DeserializeObject<dynamic>(result).@class,
						created_at = (JsonConvert.DeserializeObject<dynamic>(result).created_at as Newtonsoft.Json.Linq.JValue).ToObject<DateTime>(),
						status = JsonConvert.DeserializeObject<dynamic>(result).status,
						reference_number = JsonConvert.DeserializeObject<dynamic>(result).reference_number,
						amount_settled = JsonConvert.DeserializeObject<dynamic>(result).amount_settled,
						amount_refunded = JsonConvert.DeserializeObject<dynamic>(result).amount_refunded,
						card_owner_name = JsonConvert.DeserializeObject<dynamic>(result).card_owner_name,
						email = JsonConvert.DeserializeObject<dynamic>(result).email,
						description = JsonConvert.DeserializeObject<dynamic>(result).description,
						customer_id = JsonConvert.DeserializeObject<dynamic>(result).customer_id,
						merchant_id = JsonConvert.DeserializeObject<dynamic>(result).merchant_id,
						card_number = JsonConvert.DeserializeObject<dynamic>(result).card_number
					};
				}
			}
		}

		[Obsolete("Unsafe")]
		public static async Task<RefundResult> RefundTransaction(OrganizationModel org, string transactionId, decimal amount, bool forceTest) {
			//CURL 
			using (var client = new HttpClient()) {
				// Create the HttpContent for the form to be posted.
				var requestContent = new FormUrlEncodedContent(new[] {
					new KeyValuePair<string, string>("amount", ""+((int)(amount*100))),
				});

				if (string.IsNullOrWhiteSpace(transactionId)) {
					throw new PermissionsException("Transaction id was empty.");
				}


				var privateApi = Config.PaymentSpring_PrivateKey(forceTest);
				var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

				//added
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

				var response = await client.PostAsync("https://api.paymentspring.com/api/v1/charge/" + transactionId + "/cancel", requestContent);
				var responseContent = response.Content;
				using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
					var result = await reader.ReadToEndAsync();
					log.Debug("Refund Card: " + result);
					if (JsonConvert.DeserializeObject<dynamic>(result).errors != null) {
						var builder = new List<string>();
						var errArray = JsonConvert.DeserializeObject<dynamic>(result).errors as JArray;
						if (errArray.HasValues) {
							for (var i = 0; i < errArray.Count; i++) {
								builder.Add(errArray[i]["message"] + " (" + errArray[i]["code"] + ").");
							}
						}
						throw new PaymentException(org, amount, PaymentExceptionType.ResponseError, String.Join(" ", builder));
					}

					if (JsonConvert.DeserializeObject<dynamic>(result).@class != "transaction")
						throw new PermissionsException("Response must be of type 'transaction'. Found " + JsonConvert.DeserializeObject<dynamic>(result).@class);

					return new RefundResult {
						id = JsonConvert.DeserializeObject<dynamic>(result).id, //"id": "e8b63f39d0ae40a19c6ec5ff294d5b8c",
						@class = JsonConvert.DeserializeObject<dynamic>(result).@class, //"class": "transaction",
						created_at = (JsonConvert.DeserializeObject<dynamic>(result).created_at as Newtonsoft.Json.Linq.JValue).ToObject<DateTime>(), //"created_at": "2018-07-23T20:58:51.715Z",
						status = JsonConvert.DeserializeObject<dynamic>(result).status, //"status": "REFUNDED",
						reference_number = JsonConvert.DeserializeObject<dynamic>(result).reference_number, //"reference_number": "3459851",
						amount_settled = JsonConvert.DeserializeObject<dynamic>(result).amount_settled,//"amount_settled": 2006,
						amount_refunded = JsonConvert.DeserializeObject<dynamic>(result).amount_refunded,//"amount_refunded": 2006,


						card_owner_name = JsonConvert.DeserializeObject<dynamic>(result).card_owner_name, //"card_owner_name": "Ada Lovelace",
						email = JsonConvert.DeserializeObject<dynamic>(result).email, //"email": null,
						description = JsonConvert.DeserializeObject<dynamic>(result).description, //"description": null,
						customer_id = JsonConvert.DeserializeObject<dynamic>(result).customer_id, //"customer_id": null,
						merchant_id = JsonConvert.DeserializeObject<dynamic>(result).merchant_id, //"merchant_id": "9ccaa2022007_test",
						card_number = JsonConvert.DeserializeObject<dynamic>(result).card_number //"card_number": "***********9133",

						/*
							
							"payment_method": "credit_card",
							"amount_authorized": 2006,
							"amount_failed": 0,
							"transaction_type": "sale",
							"card_type": "amex",
							"card_number": "***********9133",
							"card_exp_month": "8",
							"card_exp_year": "2021",
							"authorized": true,
							"settled": true,
							"refunded": true,
							"voided": false,
							"system_trace": null,
							"status": "REFUNDED",
							"customer_id": null,
							"receipt": {},
							"company": null,
							"website": null,
							"email_address": null,
							"first_name": null,
							"last_name": null,
							"address_1": null,
							"address_2": null,
							"city": null,
							"state": null,
							"zip": null,
							"country": null,
							"phone": null,
							"fax": null,
							"csc_check": "0",
							"avs_address_check": null,
							"source": "web",
							"successful": true,
							"metadata": null,
							"error_message": null,
							"account_holder_name": "Ada Lovelace",
							"recurring": false,
							"processed_at": null,
							"refunds": [
								{
									"amount": 2006,
									"status": "settled",
									"error_message": {},
									"created_at": "2018-07-23T20:59:13.810Z"
								}
							] 
						 */
					};
				}
			}
		}

		public class LogResult {
			public string id { get; set; }
			public string @class { get; set; }
			public DateTime date { get; set; }
			public string subject_class { get; set; }
			public string subject_id { get; set; }
			public string action { get; set; }
			public string method { get; set; }
			public dynamic request { get; set; }
			public dynamic response { get; set; }


			public static Func<dynamic, LogResult> GetProcessor() {
				return (dynamic x) => new LogResult {
					id = x.id,
					@class = x.@class,
					date = DateTime.Parse(x.date),
					subject_class = x.subject_class,
					subject_id = x.subject_id,
					action = x.action,
					method = x.method,
					request = x.request,
					response = x.response,
				};
			}
		}

		public static PaymentSpringsToken GetToken(ISession s, long organizationId) {
			var tokens = s.QueryOver<PaymentSpringsToken>()
					.Where(x => x.OrganizationId == organizationId && x.Active && x.DeleteTime == null)
					.List().ToList();

			return tokens.OrderByDescending(x => x.CreateTime).FirstOrDefault();
		}
		public static PaymentSpringsToken GetToken(long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return GetToken(s, organizationId);
				}
			}
		}




		public class PaymentSpringError {
			public string Message { get; set; }
			public long Code { get; set; }
		}

		public class Curl<RESULT> {
			public string Url { get; set; }
			public HttpMethod Method { get; set; }
			public List<KeyValuePair<string, string>> Arguments { get; set; }
			public bool ForceTest { get; set; }
			public Func<dynamic, RESULT> ProcessResult { get; set; }
			public Action<List<PaymentSpringError>> ProcessErrors { get; set; }

			public Curl(string url, HttpMethod method, bool forceTest, Func<dynamic, RESULT> processResult) {
				ForceTest = forceTest;
				Url = url;
				Method = method;
				ProcessResult = processResult;
				Arguments = new List<KeyValuePair<string, string>>();
			}

			public Curl(string url, HttpMethod method, Func<dynamic, RESULT> processResult) : this(url, method, false, processResult) {
			}

			public void Add(string key, string val) {
				Arguments.Add(new KeyValuePair<string, string>(key, val));
			}

		}

		protected static async Task<RESULT> MakeRequest<RESULT>(Curl<RESULT> request) {

			var client = new HttpClient();

			// Create the HttpContent for the form to be posted.
			var requestContent = new FormUrlEncodedContent(request.Arguments);
			var privateApi = Config.PaymentSpring_PrivateKey(request.ForceTest);
			var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
			HttpResponseMessage response;

			if (request.Method == HttpMethod.Post) {
				response = await client.PostAsync(request.Url, requestContent);
			} else if (request.Method == HttpMethod.Get) {
				if (request.Arguments != null && request.Arguments.Any()) {
					if (!request.Url.Contains("?"))
						request.Url += "?";
					else
						request.Url += "&";
					request.Url += string.Join("&", request.Arguments.Select(x => x.Key.UrlEncode() + "=" + x.Value.UrlEncode()));
				}
				response = await client.GetAsync(request.Url);
			} else {
				throw new Exception("Unrecognized request method:" + request.Method);
			}
			var responseContent = response.Content;
			using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
				var result = await reader.ReadToEndAsync();
				if (JsonConvert.DeserializeObject<dynamic>(result).errors != null) {
					var builder = new List<string>();
					var errors = new List<PaymentSpringError>();

					for (var i = 0; i < JsonConvert.DeserializeObject<dynamic>(result).errors.Length; i++) {
						errors.Add(new PaymentSpringError {
							Message = (string)JsonConvert.DeserializeObject<dynamic>(result).errors[i].message,
							Code = (long)JsonConvert.DeserializeObject<dynamic>(result).errors[i].code
						});
					}
					if (request.ProcessErrors != null)
						request.ProcessErrors(errors);
					else {
						throw new Exception("Unhandled PaymentSpring Exception");
					}
				}
				dynamic res = JsonConvert.DeserializeObject<dynamic>(result);
				return request.ProcessResult(res);
			}
		}
	}
}

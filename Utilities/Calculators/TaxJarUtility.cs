using NHibernate;
using RadialReview.Models.Payments;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Taxjar;

namespace RadialReview.Utilities.Calculators {

	public class ValidTaxLocation {
		public string Country { get; set; }
		public string State { get; set; }
		public string City { get; set; }
		public string Address { get; set; }
		public string Zip { get; set; }
	}

	public class TaxJarUtility {
		public class TaxJarTransformer {
			public Dictionary<string, string> CountryTransformations { get; set; }
			public Dictionary<string, Dictionary<string, string>> StateTransformations { get; set; }
			public Dictionary<string, Dictionary<string, Dictionary<string, string>>> CityTransformations { get; set; }

			public TaxJarTransformer() {
				CountryTransformations = new Dictionary<string, string>();
				StateTransformations = new Dictionary<string, Dictionary<string, string>>();
				CityTransformations = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
			}

			public void AddCountry(string country, string countryCode) {
				if (country == null)
					return;
				CountryTransformations[Normalize(country)] = countryCode.ToUpper();
			}
			public void AddState(string countryCode, string state, string stateCorrected) {
				if (countryCode == null)
					return;
				StateTransformations[Normalize(countryCode)][Normalize(state)] = stateCorrected;
			}

			public ValidTaxLocation GetValidTaxLocation(PaymentSpringsToken token) {
				if (token == null)
					return null;
				return GetValidTaxLocation(token.Country, token.State, token.City, token.Address_1, token.Zip);
			}

			public ValidTaxLocation GetValidTaxLocation(string country, string state, string city, string address, string zip) {

				var valid = new ValidTaxLocation() { };
				try {
					valid.Country = GetValidCountry(country);
					valid.State = GetValidState(valid, state);
					valid.City = GetValidCity(valid, city);
					valid.Address = address != null ? (address ?? "").Trim() : null;
					valid.Zip = zip != null ? (zip ?? "").Trim() : null;
				} catch (Exception e) {
					int a = 0;
				}
				return valid;


			}
			private string GetValidCity(ValidTaxLocation existing, string city) {
				var country = Normalize(existing.Country);
				var state = Normalize(existing.State);
				var cityNormalized = Normalize(city);
				if (country != "" && state != null && CityTransformations.ContainsKey(country) && CityTransformations[country].ContainsKey(state) && CityTransformations[country][state].ContainsKey(cityNormalized)) {
					return CityTransformations[country][state][cityNormalized];
				}
				return city;
			}

			private string GetValidState(ValidTaxLocation existing, string state) {
				var country = Normalize(existing.Country);

				if (country != "" && StateTransformations.ContainsKey(country)) {
					var stateNormalized = Normalize(state);
					if (StateTransformations[country].ContainsKey(stateNormalized)) {
						return StateTransformations[country][stateNormalized];
					}
				}
				return state;
			}

			private string GetValidCountry(string country) {
				var countryNormalize = Normalize(country);
				if (CountryTransformations.ContainsKey(countryNormalize)) {
					return CountryTransformations[countryNormalize].ToUpper();
				}

				if (countryNormalize.Length == 2) {
					return countryNormalize.ToUpper();
				}
				return null;
			}

			private string Normalize(string x) {
				return (x ?? "").Trim().ToLower();
			}
		}


		private static TimeSpan RETAIN_FOR = TimeSpan.FromSeconds(120);

		private static CachedSetting<Config.TaxJarSettings> _Settings = new CachedSetting<Config.TaxJarSettings>(RETAIN_FOR, () => Config.GetTaxJarSettings());

		private static CachedSetting<TaxjarApi> _LiveClient = new CachedSetting<TaxjarApi>(RETAIN_FOR, () => new TaxjarApi(_Settings.Get().LiveToken));
		//need to specify sandbox url for testing
		private static CachedSetting<TaxjarApi> _SandboxClient = new CachedSetting<TaxjarApi>(RETAIN_FOR, () => new TaxjarApi(_Settings.Get().SandboxToken, new { apiUrl = "https://api.sandbox.taxjar.com" }));
		private static CachedDatabaseSetting<TaxJarTransformer> _Transformer = new CachedDatabaseSetting<TaxJarTransformer>(RETAIN_FOR, s => s.GetSettingOrDefault(Variable.Names.TAXJAR_TRANSFORMER, () => TaxJarDefaults.GetDefault()));
		private static CachedDatabaseSetting<bool> _ApplyTax = new CachedDatabaseSetting<bool>(RETAIN_FOR, s => s.GetSettingOrDefault(Variable.Names.APPLY_SALESTAX, () => false));

		public static void ResetCache() {
			_LiveClient.ResetCache();
			_SandboxClient.ResetCache();
			_Settings.ResetCache();
			_Transformer.ResetCache();
		}

		public static ValidTaxLocation GetValidTaxLocation(ISession s, PaymentSpringsToken token) {
			var transformer = GetCachedTaxJarTransformer(s);
			return transformer.GetValidTaxLocation(token);
		}

		public static TaxJarTransformer GetCachedTaxJarTransformer(ISession s) {
			return _Transformer.Get(s);
		}

		private static TaxjarApi GetClient(bool useSandbox) {
			if (useSandbox) {
				return _SandboxClient.Get();
			} else {
				return _LiveClient.Get();
			}
		}

		public static bool SaleTaxEnabled(ISession s) {
			return _ApplyTax.Get(s);
		}

		public class TaxAndRate {
			public decimal TaxAmount { get; set; }
			public decimal TaxRate { get; set; }
			public bool UsesFallback { get; set; }
		}

		public static TaxAndRate CalculateFallbackSalesTax(decimal subtotal, decimal taxRate) {
			return new TaxAndRate() {
				TaxRate = taxRate,
				TaxAmount = Math.Max(0, subtotal) * (taxRate),
				UsesFallback = true
			};
		}

		private static string GetTaxJarId(long invoiceId, bool useTest) {
			return "" + invoiceId + (useTest ? ("_test_" + DateTime.UtcNow.ToJsMs()) : "");
		}

		public static async Task<TaxAndRate> CalculateSaleTax(long invoiceId, ValidTaxLocation taxLocation, decimal totalBeforeDiscount, decimal discount, bool useTest) {

			if (taxLocation == null) {
				return null;
			}

			var client = GetClient(false);

			try {
				//Discount must bea a positive value for their api. (support call 6/24/21)
				var r = await client.TaxForOrderAsync(new {


					to_country = taxLocation.Country,
					to_zip = taxLocation.Zip,
					to_state = taxLocation.State,
					to_city = taxLocation.City,
					to_street = taxLocation.Address,

					shipping = 0,
					amount = totalBeforeDiscount,
					line_items = new[] {
						new {
						  id = GetTaxJarId(invoiceId,useTest),
						  quantity = 1,
						  product_tax_code = _Settings.Get().ProductTaxCode,
						  unit_price = totalBeforeDiscount,
						  discount = Math.Abs(discount)
						}
					}
				});
				return new TaxAndRate {
					TaxAmount = r.AmountToCollect,
					TaxRate = r.Rate
				};//Amount of tax to collect.
			} catch (Exception e) {
				return null;
			}
		}

		public static async Task SubmitOrder(long invoiceId, PaymentSpringsToken ps, decimal totalBeforeTax, decimal taxCollected, decimal discount, DateTime orderTime, ValidTaxLocation taxLocation, bool useSandbox, bool taxExempt) {
			if (taxLocation == null) {
				taxLocation = new ValidTaxLocation() {
					Address = null,
					City = null,
					Country = null,
					State = null,
					Zip = null
				};
			}

			var client = GetClient(useSandbox);
			var order = await client.CreateOrderAsync(new {
				transaction_id = GetTaxJarId(invoiceId, useSandbox),
				transaction_date = orderTime.ToIso8601(),


				exemption_type = (taxExempt ? "other" : null),

				to_country = taxLocation.Country,
				to_state = taxLocation.State,
				to_zip = taxLocation.Zip,
				to_city = taxLocation.City,
				to_street = taxLocation.Address,


				amount = totalBeforeTax,
				shipping = 0,
				sales_tax = taxCollected,

				line_items = new[] {
					new {
					  quantity = 1,
					  product_identifier = _Settings.Get().ProductIdentifier,
					  description = _Settings.Get().ProductDescription,
					  product_tax_code = _Settings.Get().ProductTaxCode,
					  unit_price = totalBeforeTax,
					  sales_tax = taxCollected,
					}
				}
			});

		}

		public static async Task ApplyRefund(long invoiceId, decimal subtotalRefunded, decimal taxRefunded, DateTime originalOrderExecutionTime, ValidTaxLocation taxLocation, bool useSandbox, bool taxExempt) {
			if (taxLocation == null) {
				taxLocation = new ValidTaxLocation() {
					Address = null,
					City = null,
					Country = null,
					State = null,
					Zip = null
				};
			}


			var client = GetClient(useSandbox);
			var order = await client.CreateRefundAsync(new {
				transaction_id = GetTaxJarId(invoiceId, useSandbox) + "_refund_" + originalOrderExecutionTime.ToJsMs(),
				transaction_date = originalOrderExecutionTime.ToIso8601(),
				transaction_reference_id = GetTaxJarId(invoiceId, useSandbox),

				exemption_type = (taxExempt ? "other" : null),

				to_country = taxLocation.Country,
				to_state = taxLocation.State,
				to_zip = taxLocation.Zip,
				to_city = taxLocation.City,
				to_street = taxLocation.Address,


				amount = -1 * Math.Abs(subtotalRefunded),
				shipping = 0,
				sales_tax = -1 * Math.Abs(taxRefunded),

			});


		}
	}
}

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Utilities.Config;
using System.Collections.Generic;
using Newtonsoft.Json;
using static RadialReview.AgileCrm.AgileCrmConstants;

namespace RadialReview.Hooks.CrossCutting {
	public class AgileCrmConnector {
		private AgileCrmConfig Configs;

		public AgileCrmConnector(AgileCrmConfig config) {
			if (string.IsNullOrEmpty(config.CrmKey))
				throw new ArgumentNullException(nameof(config.CrmKey));
			if (string.IsNullOrEmpty(config.Domain))
				throw new ArgumentNullException(nameof(config.Domain));
			if (string.IsNullOrEmpty(config.Email))
				throw new ArgumentNullException(nameof(config.Email));
			this.Configs = config;
		}

		public string DomainUrl {
			get {
				return $"https://{Configs.Domain}.agilecrm.com/dev/api/";
			}
		}

		public string GetUrl(string route) {
			return DomainUrl + route;
		}

		public class AgileContactModel {
			public long id { get; set; }
			public long ownerId { get; set; }
			public long companyId { get; set; }
		}


		public async Task<AgileContactModel> PullAgileCrmData(long? agileCrmId) {
			var connector = this;
			var response = await connector.RequestAsync($"contacts/{agileCrmId}", HttpMethod.Get, null);
			if (!string.IsNullOrEmpty(response)) {
				dynamic agileOrgId = JsonConvert.DeserializeObject<dynamic>(response);
				var valueId = agileOrgId.id;
				var ownerProp = agileOrgId.owner;
				var ownerId = ownerProp.id;
				var companyId = agileOrgId.contact_company_id;
				return new AgileContactModel {
					id = valueId,
					ownerId = ownerId,
					companyId = companyId,
				};
			}
			throw new Exception("PullAgileCrm Data response was empty");
		}

		public async Task TagsAsync(string eventName, long agileOrganizationId) {
			List<string> listOfeventName = new List<string>(eventName.Split(','));
			await AddTags(agileOrganizationId, listOfeventName);
		}

		public async Task<string> RequestAsync(string route, HttpMethod method, string data, string contenttype = "application/json") {
			var encodedAuthentication = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes($"{Configs.Email}:{Configs.CrmKey}")));

			using (var client = new HttpClient() {
				DefaultRequestHeaders = { Authorization = encodedAuthentication },
			}) {
				client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
				var request = new HttpRequestMessage(method, new Uri(GetUrl(route)));
				if (!string.IsNullOrEmpty(data)) {
					request.Content = new StringContent(data, Encoding.UTF8, contenttype);
				}
				var response = await client.SendAsync(request);
				response.EnsureSuccessStatusCode();
				return await response.Content.ReadAsStringAsync();
			}
		}

		public async Task AddTags(long agileCrmId, List<string> tags) {
			if (agileCrmId == 0) {
				throw new Exception("AgileCrmId response was null");
			}
			var res = RequestAsync("contacts/edit/tags", HttpMethod.Put, JsonConvert.SerializeObject(new {
				id = agileCrmId,
				tags = tags
			}));
			if (res == null) {
				throw new Exception("AddTags response was null");
			}
		}

		public async Task RemoveTag(long agileCrmId, string eventTag) {
			await RequestAsync("contacts/delete/tags", HttpMethod.Put, JsonConvert.SerializeObject(new {
				id = agileCrmId,
				tags = new[] { eventTag },
			}));
		}
	}

}

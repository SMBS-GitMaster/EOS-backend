using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Todo;
using RadialReview.Utilities;


namespace RadialReview.Accessors.TodoIntegrations {
	public class BaseCampAccessor {

		public static String AuthUrl(UserOrganizationModel caller, long recurrence, long userId) {
			PermissionsAccessor.Permitted(caller, x => x.EditL10Recurrence(recurrence).ViewUserOrganization(userId, false));
			throw new NotImplementedException();
		}
		public class BasecampList {
			public String Name { get; set; }
			public string ListId { get; set; }
		}


		public static BasecampTodoCreds Authorize(UserOrganizationModel caller, string tokenId, long recurrenceId, long userId) {
			throw new NotImplementedException();
		}

		public static List<BasecampList> GetLists(UserOrganizationModel caller, long apiId) {
			BasecampTodoCreds authorized;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					authorized = s.QueryOver<BasecampTodoCreds>().Where(x => x.DeleteTime == null && x.ApiId == apiId && x.ApiUrl != null).OrderBy(x => x.CreateTime).Desc.Take(1).SingleOrDefault();
					if (authorized == null)
						throw new PermissionsException("Credentials do not exist");
				}
			}

			var url = authorized.ApiUrl + "todolists.json";
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.UserAgent = Config.Basecamp.GetUserAgent();
			request.Headers.Add("Authorization", "Bearer " + authorized.Token);

			var listsJson = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

			var lists = JArray.Parse(listsJson);
			var output = new List<BasecampList>();
			foreach (var b in lists) {
				try {
					output.Add(new BasecampList() {
						ListId = (string)b["id"] + "~" + (string)b["bucket"]["id"],
						Name = (string)b["name"] + ": " + (string)b["bucket"]["name"]
					});
				} catch (Exception) {
				}
			}

			return output;
		}


		public static void AttachToBasecamp(UserOrganizationModel caller, string uid, long recurrenceId, long userId, string listId_bucketId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditL10Recurrence(recurrenceId).ViewUserOrganization(userId, false);
					var authorized = s.QueryOver<BasecampTodoCreds>().Where(x => x.DeleteTime == null && x.UID == uid && x.ListId == null).SingleOrDefault();
					if (authorized == null)
						throw new PermissionsException("Credentials do not exist");


					var url = authorized.ApiUrl + "people/me.json";
					var request = (HttpWebRequest)WebRequest.Create(url);
					request.UserAgent = Config.Basecamp.GetUserAgent();
					request.Headers.Add("Authorization", "Bearer " + authorized.Token);

					var me = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

					var assigneeId = (string)JObject.Parse(me)["id"];
					var name = (string)JObject.Parse(me)["name"];

					var ids = listId_bucketId.Split('~');


					authorized.ListId = ids[0];
					authorized.BasecampAssigneeId = assigneeId;
					authorized.ProjectId = ids[1];
					authorized.AccountName = name;

					s.Update(authorized);
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}

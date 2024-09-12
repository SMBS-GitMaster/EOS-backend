using RadialReview.Utilities.Serializers;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Html;
using RadialReview.Utilities;
using System;
using RadialReview.Variables;

namespace RadialReview.Controllers {
	public partial class BaseController : Controller {


		protected ServerSettings<int> SoftwareVersion = ServerSettings.Create(s => {
			return s.GetSettingOrDefault(Variable.Names.SOFTWARE_VERSION, () => 1);
		}, TimeSpan.FromMinutes(2));

		protected int? TryGetClientOffset() {
			return (int?)(Request.Query["_tz"].ToString().TryParseLong());
		}



		private static string CleanFileName(string fileName) {
			return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}


		protected string ReadBody() {
			Request.Body.Position = 0;
			var body = new StreamReader(Request.Body).ReadToEnd();
			return body;
		}

		protected HtmlString SafeJsonSerialize(object self) {
			return SafeJsonUtil.SafeJsonSerialize(self);
		}

		protected void SetCurrentRecurrence(long recurrenceId) {
			ViewBag.CurrentRecurrenceId = recurrenceId;
		}



	}
}

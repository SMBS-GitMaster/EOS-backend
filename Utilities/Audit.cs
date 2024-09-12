using System;
using System.IO;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Audit;
using RadialReview.Models.VTO;
using log4net;
using Microsoft.Net.Http.Headers;
using RadialReview.Utilities.RealTime;
using System.Threading.Tasks;

namespace RadialReview.Utilities {
	public class Audit {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


		public static void Log(ISession s, UserOrganizationModel caller) {
			try {
				var audit = new AuditModel();
				TryInjectData(audit);
				audit.UserId = caller.User.NotNull(x=>x.Id);
				audit.UserOrganizationId = caller.NotNull(x => x.Id);
				s.Save(audit);
			} catch (Exception) {

			}
		}

		private static void TryInjectData(AuditModel audit) {
			try {
				if (HttpContextHelper.Current != null && HttpContextHelper.Current.Request != null) {
					var r = HttpContextHelper.Current.Request;
					r.Body.Seek(0, SeekOrigin.Begin);
					var oSR = new StreamReader(r.Body);
					var sContent = oSR.ReadToEnd();
					r.Body.Seek(0, SeekOrigin.Begin);

					audit.Method = r.Method;
					audit.Data = sContent;
					audit.Path = r.Path.Value;
					audit.Query = r.QueryString.Value;
					audit.UserAgent = r.Headers[HeaderNames.UserAgent];
				}
			} catch (Exception e) {
				int a = 0;
			}
		}

		public static void VtoLog(ISession s, UserOrganizationModel caller, long vtoId, string action, string notes = null) {
			try {
				var audit = new VtoAuditModel();
				TryInjectData(audit);
				audit.Action = action;
				audit.Vto = s.Load<VtoModel>(vtoId);

				audit.UserId = caller.User.NotNull(x => x.Id);
				audit.UserOrganizationId = caller.NotNull(x => x.Id);
				audit.Notes = notes;
				s.Save(audit);
			} catch (Exception) {

			}
		}

		public static async Task L10Log(ISession s, UserOrganizationModel caller, long recurrenceId, string action, ForModel forModel, string notes = null) {
			try {
				var audit = new L10AuditModel();

				TryInjectData(audit);

				audit.ForModel = forModel;
				audit.Action = action;
				audit.RecurrenceId = recurrenceId;


				audit.UserId = caller.User.NotNull(x => x.Id);
				if (caller != UserOrganizationModel.ADMIN) {
					audit.UserOrganizationId = caller.NotNull(x => x.Id);
				}
				audit.Notes = notes;
				s.Save(audit);

				if (forModel != null) {
					await using (var rt = RealTimeUtility.Create()) {
						var meetingHub = rt.UpdateRecurrences(recurrenceId);
						var type = forModel.FriendlyType();
						var html = "<div><span class='log-action'>" + action + "</span><span class='log-notes'>" + notes + "</span></div>";
						meetingHub.Call("addOrEditLogRow", type + "_" + forModel.ModelId, html, type);
					}
				} else {
					log.Warn("ForModel was null (" + recurrenceId + ", " + action + ")");
				}
			} catch (Exception e) {
				log.Error(e);
			}
		}

	}
}

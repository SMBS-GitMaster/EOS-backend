using System.Collections.Generic;
using System.Linq;
using NHibernate.Transform;
using RadialReview.Models;
using RadialReview.Models.Audit;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Audit

		public class L10AuditModels {
			public List<L10AuditModel> Items { get; set; }
			public DefaultDictionary<long?, string> UsernameLookup { get; set; }
		}

		public static L10AuditModels GetL10Audit(UserOrganizationModel caller, long recurrenceId, DateRange range) {
			if (range.IsValid()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
						var audits = s.QueryOver<L10AuditModel>()
							.Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId)
							.Where(x => range.StartTime <= x.CreateTime && x.CreateTime <= range.EndTime)
							//.Fetch(x => x.UserOrganization).Eager
							.TransformUsing(Transformers.DistinctRootEntity)
							.List().ToList();

						var userIds = audits.Select(x => x.UserOrganizationId).Distinct().ToList();

						var lookups = s.QueryOver<UserLookup>()
											.WhereRestrictionOn(x => x.UserId).IsIn(userIds)
											.Select(x => x.UserId, x => x.Name)
											.List<object[]>()
											.ToDefaultDictionary(x => (long?)x[0], x => (string)x[1] ?? "", x => "");



						return new L10AuditModels() {
							Items = audits,
							UsernameLookup = lookups
						};
					}
				}
			} else {
				return new L10AuditModels() {
					Items = new List<L10AuditModel>(),
					UsernameLookup = new DefaultDictionary<long?, string>(x => "")
				};
			}
		}
		#endregion
	}
}
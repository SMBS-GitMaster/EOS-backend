using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors {
	public partial class AccountabilityAccessor : BaseAccessor {

		/// <summary>
		/// Surround a session with the UserCacheUpdater. It will update the user's cache afterward during the dispose sequence.
		/// </summary>
		public class UserCacheUpdater : IDisposable {
			public List<long> Ids { get; set; }
			public UserCacheUpdater() {
				Ids = new List<long>();
			}
			public void Add(long id) {
				Ids.Add(id);
			}
			public void AddRange(List<long> ids) {
				Ids.AddRange(ids.Distinct());
			}
			public void Add(UserOrganizationModel user) {
				Ids.Add(user.Id);
			}
			public void AddRange(List<UserOrganizationModel> users) {
				AddRange(users.Select(x => x.Id).ToList());
			}

			public void UpdateUsersOutsideOfSession() {
				if (Ids.Any()) {
					var copy = Ids.ToList();
					Ids.Clear();
					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							foreach (var id in copy.Distinct()) {
								s.Get<UserOrganizationModel>(id).UpdateCache(s);
							}
							tx.Commit();
							s.Flush();
						}
					}
				}
			}

			public void Dispose() {
				UpdateUsersOutsideOfSession();
			}
		}
	}
}

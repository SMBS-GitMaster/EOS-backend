using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Utilities.Permissions.Accessors {
	public class MemberAccessorPermissions : IAccessorPermissions {

		public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {
			return permItems.Select(pi => {

				var title = "a " + pi.ResType;
				var picture = new PictureViewModel();
				var row = new PermRowVM(pi, settings);

				//Only display children if permitted
				if (perms.IsPermitted(x => x.CanView(pi.ResType, pi.ResId))) {
					var resourcePermissions = PermissionsUtility.GetResourcePermissionsForType(pi.ResType);
					var meta = resourcePermissions.GetMetaData(s, pi.ResId);
					title = "Members of " + meta.Name;
					picture = meta.Picture;
					var memberUserIds = resourcePermissions.GetMembersOfResource(s, pi.ResId, -1, false);
					var lookup = this.CreateRgmLookup(s, perms, memberUserIds);
					row.AddChildren(memberUserIds.Select(mid => new PermRowVM(pi, settings.WithAllDisabled()) {
						Title = lookup.GetFieldSkipPermissions(mid, x => x.GetName(), "unnamed"),
						Picture = PictureViewModel.CreateFrom(lookup.GetSkipPermissions(mid)),
					}));
				}
				row.Title = title;
				row.Picture = picture;
				return row;
			});
		}

		public PermItem.AccessType ForAccessorType() {
			return PermItem.AccessType.Members;
		}

		public IEnumerable<IAccessorPermissionTest> PermissionTests() {
			yield return new MemberPermissionTest();
		}

		public class MemberPermissionTest : IAccessorPermissionTest {
			public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
				var caller = perms.GetCaller();
				var isMember_ids = data.ResourcePermissions.GetMembersOfResource(session, data.ResourceId, caller.Id, true).ToList();
				if (isMember_ids.Any(id => id == caller.Id)) {
					return true;
				}
				if (data.IncludeAlternateUsers) {
					var match = isMember_ids.Where(mid => caller.UserIds.Any(uid => uid == mid));
					foreach (var m in match) {
						if (data.PermissionDataCache.UserIsValid(session, m)) {
							return true;
						}
					}
				}
				return false;
			}

			public int OrderOfOperation() {
				return 3;
			}
		}
	}
}

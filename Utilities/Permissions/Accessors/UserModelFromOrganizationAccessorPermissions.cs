using NHibernate;
using RadialReview.Core.Models.Terms;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.Permissions.Accessors {
	public class UserModelAtOrganizationAccessorPermissions : IAccessorPermissions {
		public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {

			var accessorIds = permItems.Select(x => x.AccessorId).ToArray();
			var umfo = s.QueryOver<UserModelAtOrganizationPermItem>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.Id).IsIn(accessorIds)
					.Future();


			return permItems.Select(pi => {
				var uid = umfo.FirstOrDefault(x => x.Id == pi.AccessorId).NotNull(x => x.UserModelId);

				var name = "An unspecified user";
				var picture = new PictureViewModel();
				if (perms.GetCaller().User != null && perms.GetCaller().User.Id == uid) {
					name = perms.GetCaller().GetName();
					picture = PictureViewModel.CreateFrom(perms.GetCaller());
				}

				return new PermRowVM(pi, settings) {
					Title = name,
					Picture = picture
				};
			});
		}

		public PermItem.AccessType ForAccessorType() {
			return PermItem.AccessType.UserModelAtOrganization;
		}

		public IEnumerable<IAccessorPermissionTest> PermissionTests() {
			yield return new UserModelAtOrganizationPermissionTest();
		}

		public class UserModelAtOrganizationPermissionTest : IAccessorPermissionTest {
			public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
				var caller = perms.GetCaller();
				if (caller.User == null || caller.User.DeleteTime != null)
					return false;

				var e = session.Get<UserModelAtOrganizationPermItem>(data.PermItem.AccessorId);
				if (e.DeleteTime != null)
					return false;

				if (caller.User.Id == e.UserModelId) {
					if (caller.User.UserOrganizationIds.Count() == 1 &&
						caller.User.UserOrganizationIds.First() == caller.Id &&
						caller.Organization.Id == e.OrganizationId &&
						caller.DeleteTime == null &&
						caller.Organization.DeleteTime == null
					) {
						//exact match.
						return true;
					} else {
						//search all attached.
						var matchingUsers = session.QueryOver<UserOrganizationModel>()
												.Where(x => x.DeleteTime == null && x.Organization.Id == e.OrganizationId)
												.WhereRestrictionOn(x => x.Id).IsIn(caller.User.UserOrganizationIds)
												.List().ToList();
						return matchingUsers.Any(x => x.DeleteTime == null && x.Organization.DeleteTime == null);
					}
				}
				return false;
			}
			public int OrderOfOperation() {
				return 5;
			}
		}
	}
}


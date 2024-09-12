using NHibernate;
using RadialReview.Core.Models.Terms;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.Permissions.Accessors {
	public class AdminAccessorPermissions : IAccessorPermissions {
		public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {
			return permItems.Select(pi => {
				var resourcePermissions = PermissionsUtility.GetResourcePermissionsForType(pi.ResType);
				var orgId = resourcePermissions.GetOrganizationIdForResource(s, pi.ResId);

				var orgName = "an organization";
				var picture = new PictureViewModel();
				var row = new PermRowVM(pi, settings);
				if (perms.IsPermitted(x => x.ViewOrganization(orgId))) {
					var org = s.Get<OrganizationModel>(orgId);
					var admins = s.QueryOver<UserOrganizationModel>()
						.Where(x => x.DeleteTime == null && x.Organization.Id == orgId && x.ManagingOrganization)
						.List().ToList();
					picture = PictureViewModel.CreateFrom(org);
					orgName = org.GetName();
					row.AddChildren(admins.Select(admin => new PermRowVM(pi, settings.WithAllDisabled()) {
						Title = admin.GetName(),
						Picture = PictureViewModel.CreateFrom(admin),
					}));
				}

				picture.AddClass("admin-icon");
				row.Title = "Admins of " + orgName;
				row.Picture = picture;

				return row;
			});
		}

		public PermItem.AccessType ForAccessorType() {
			return PermItem.AccessType.Admins;
		}

		public IEnumerable<IAccessorPermissionTest> PermissionTests() {
			yield return new AdminAccessorPermissionTest();
		}



		public class AdminAccessorPermissionTest : IAccessorPermissionTest {
			public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
				var caller = perms.GetCaller();
				var orgId = data.ResourcePermissions.GetOrganizationIdForResource(session, data.ResourceId);
				if (orgId == caller.Organization.Id && caller.IsManagingOrganization()) {
					return true;
				}

				if (data.IncludeAlternateUsers) {
					foreach (var uid in caller.UserIds.Where(x => x != caller.Id)) {
						var u = data.PermissionDataCache.GetUser(session, uid);
						if (data.PermissionDataCache.UserIsValid(u) && orgId == u.Organization.Id && u.IsManagingOrganization()) {
							return true;
						}
					}
				}
				return false;
			}

			public int OrderOfOperation() {
				return 0;
			}
		}
	}
}
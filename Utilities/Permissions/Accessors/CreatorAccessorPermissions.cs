using NHibernate;
using RadialReview.Core.Models.Terms;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.Permissions.Accessors {
	public class CreatorAccessorPermissions : IAccessorPermissions {

		public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms,TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {
			var rgms = this.CreateRgmLookup(s, perms, permItems);
			return permItems.Select(pi => new PermRowVM(pi, settings) {
				Title = rgms.GetField(pi.AccessorId, x => x.GetName(), "-unnamed-"),
				Picture = PictureViewModel.CreateFrom(rgms.Get(pi.AccessorId)),
			});
		}

		public PermItem.AccessType ForAccessorType() {
			return PermItem.AccessType.Creator;
		}

		public IEnumerable<IAccessorPermissionTest> PermissionTests() {
			yield return new CreatorPermissionTest();
		}

		public class CreatorPermissionTest : IAccessorPermissionTest {
			public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
				var caller = perms.GetCaller();
				var creatorId = data.ResourcePermissions.GetCreator(session, data.ResourceId);
				if (creatorId == null)
					return false;
				if (creatorId == caller.Id)
					return true;

				if (data.IncludeAlternateUsers) {
					if (caller.UserIds.Contains(creatorId.Value)) {
						var u = data.PermissionDataCache.GetUser(session, creatorId.Value);
						if (u.DeleteTime == null && u.Organization.DeleteTime == null) {
							return true;
						}
					}
				}

				return false;
			}

			public int OrderOfOperation() {
				return 2;
			}
		}

	}
}
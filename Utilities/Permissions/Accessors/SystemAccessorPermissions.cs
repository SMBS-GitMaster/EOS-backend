using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Utilities.Permissions.Accessors {
	public class SystemAccessorPermissions : IAccessorPermissions {
		public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {
			return permItems.Select(x => new PermRowVM(x, settings) {
				Title = "System Managed",
				Picture = PictureViewModel.CreateFromInitials("TT", "System Managed")
			});
		}

		public PermItem.AccessType ForAccessorType() {
			return PermItem.AccessType.System;
		}

		public IEnumerable<IAccessorPermissionTest> PermissionTests() {
			return new List<IAccessorPermissionTest>();
		}
	}
}
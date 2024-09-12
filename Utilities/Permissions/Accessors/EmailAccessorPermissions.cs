using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.ViewModels;
using RadialReview.Core.Properties;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Utilities.Permissions.Accessors {
	public class EmailAccessorPermissions : IAccessorPermissions {

		public IEnumerable<PermRowVM> ConstructRowViewModel(ISession s, PermissionsUtility perms, TermsCollection terms, IEnumerable<PermItem> permItems, PermRowSettings settings) {
			var accessorIds = permItems.Select(x => x.AccessorId).ToArray();
			var emails = s.QueryOver<EmailPermItem>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.Id).IsIn(accessorIds)
					.Future();

			return permItems.Select(pi => {
				var email = emails.FirstOrDefault(x => x.Id == pi.AccessorId).NotNull(x => x.Email) ?? "unknown email";
				return new PermRowVM(pi, settings) {
					Title = email,
					Picture = new PictureViewModel() {
						Title = email,
						Url = (ConstantStrings.AmazonS3Location + ConstantStrings.ImageUserPlaceholder)
					}
				};
			});
		}

		public PermItem.AccessType ForAccessorType() {
			return PermItem.AccessType.Email;
		}

		public IEnumerable<IAccessorPermissionTest> PermissionTests() {
			yield return new EmailPermissionTest();
		}

		public class EmailPermissionTest : IAccessorPermissionTest {
			public bool IsPermitted(ISession session, PermissionsUtility perms, IAccessorPermissionContext data) {
				var caller = perms.GetCaller();
				if (caller.User == null)
					return false;
				if (string.IsNullOrWhiteSpace(caller.User.UserName))
					return false;

				var e = session.Get<EmailPermItem>(data.PermItem.AccessorId);
				return e.Email.ToLower().Trim() == caller.User.UserName.ToLower().Trim();
			}

			public int OrderOfOperation() {
				return 5;
			}
		}
	}
}
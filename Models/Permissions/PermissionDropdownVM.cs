using System.Collections.Generic;

namespace RadialReview.Models.Permissions {




	public class PermissionDropdownVM {
		public string DisplayText { get; set; }
		public List<PermRowVM> Items { get; set; }
		public PermItem.ResourceType ResType { get; set; }
		public long ResId { get; set; }
		public bool Hidden { get; set; }
		public bool CanAdmin { get; set; }

		public PermissionsHeading GetHeading() {
			return PermissionsHeading.GetHeading(ResType);
		}

		public PermissionDropdownVM() {
		}

		public static PermissionDropdownVM NotPermitted {
			get {
				return new PermissionDropdownVM() {
					DisplayText = "Not permitted",
					Items = new List<PermRowVM>(),
					ResType = PermItem.ResourceType.Invalid,
					ResId = -1,
					Hidden = true
				};
			}
		}

	}
}

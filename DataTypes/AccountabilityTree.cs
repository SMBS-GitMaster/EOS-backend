using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Roles;
using RadialReview.Models.Angular.Users;
using System.Collections.Generic;

namespace RadialReview.Utilities.DataTypes {
	public class AccountabilityTree : TreeModel<AccountabilityTree>, IAngularItem {
		public IEnumerable<AngularRole> roles { get; set; }
		public AngularUser user { get; set; }

		public long Id { get { return this.id; } set { this.id = value; } }

		public string Type { get { return "AccountabilityTree"; } }

		public bool Hide { get { return false; } }

		public object GetAngularId() {
			return Id;
		}

		public string GetAngularType() {
			return Type;
		}

	}
}
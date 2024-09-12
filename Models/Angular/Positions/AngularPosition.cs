using RadialReview.Models.Angular.Base;

namespace RadialReview.Models.Angular.Positions {


	public class AngularPosition : BaseAngular {
		public AngularPosition() {
		}

		public AngularPosition(long accountabilityRoleGroupId) : base(accountabilityRoleGroupId) {
		}

		public AngularPosition(long accountabilityRoleGroupId, string name) : base(accountabilityRoleGroupId) {
			Name = name;
		}

		public string Name { get; set; }

	}
}
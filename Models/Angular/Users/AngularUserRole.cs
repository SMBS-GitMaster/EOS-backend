using RadialReview.Models.Angular.Base;

namespace RadialReview.Models.Angular.Users {
	public class AngularUserRole : BaseAngular {
		public string Name { get; set; }
		public string Title { get; set; }
		public string OrganizationName { get; set; }

		public AngularUserRole(long userId) : base(userId) {
		}

		public AngularUserRole(long userId, string name, string title, string orgName) : this(userId) {
			Name = name;
			Title = title;
			OrganizationName = orgName;
		}
	}
}
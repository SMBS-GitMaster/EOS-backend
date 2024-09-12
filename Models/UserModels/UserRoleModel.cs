using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.UserModels {

	public class UserRoleModel : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual string Role { get; set; }
		public virtual bool Deleted { get; set; }
	}

	public class UserRoleModelMap : ClassMap<UserRoleModel> {
		public UserRoleModelMap() {
			Id(x => x.Id);
			Map(x => x.Role);
			Map(x => x.Deleted);
		}
	}
}
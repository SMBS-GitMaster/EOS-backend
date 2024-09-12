using System;
using System.Collections.Generic;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Periods;
using RadialReview.Models.Askables;

namespace RadialReview.Models.UserTemplate {
	public class UserTemplate_Deprecated : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }

		public virtual long AttachId { get; set; }
		public virtual AttachType AttachType { get; set; }
		public virtual Attach _Attach { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual String JobDescription { get; set; }
		public virtual List<RoleModel_Deprecated> _Roles { get; set; }
		public virtual List<UT_User_Deprecated> _Members { get; set; }
		public virtual List<UT_Rock_Deprecated> _Rocks { get; set; }
		public virtual List<UT_Measurable_Deprecated> _Measurables { get; set; }

		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		public UserTemplate_Deprecated() {
			CreateTime = DateTime.UtcNow;
		}

		public class UserTemplateMap : ClassMap<UserTemplate_Deprecated> {
			public UserTemplateMap() {
				Table("UserTemplate");
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.JobDescription);
				Map(x => x.AttachId);
				Map(x => x.AttachType);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				;
			}
		}

		public class UT_Role_Deprecated : ILongIdentifiable, IDeletable, IUserTemplateItem {
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			[Obsolete("Do not use")]
			public virtual String Role { get; set; }
			public virtual long RoleId { get; set; }

			public virtual RoleModel_Deprecated _Role { get; set; }

			public virtual long TemplateId { get; set; }
			public virtual UserTemplate_Deprecated Template { get; set; }
			public class UT_RoleMap : ClassMap<UT_Role_Deprecated> {
				public UT_RoleMap() {
					Table("UT_Role");
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Role);
					Map(x => x.RoleId);
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}

		}

		public class UT_User_Deprecated : ILongIdentifiable, IDeletable, IUserTemplateItem {
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual long TemplateId { get; set; }
			public virtual UserTemplate_Deprecated Template { get; set; }
			public class UT_UserMap : ClassMap<UT_User_Deprecated> {
				public UT_UserMap() {
					Table("UT_User");
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					References(x => x.User).Column("UserId");
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}
		}

		public class UT_Rock_Deprecated : ILongIdentifiable, IDeletable, IUserTemplateItem {
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual String Rock { get; set; }
			public virtual long PeriodId { get; set; }
			public virtual PeriodModel Period { get; set; }
			public virtual long TemplateId { get; set; }
			public virtual UserTemplate_Deprecated Template { get; set; }
			public class UT_RockMap : ClassMap<UT_Rock_Deprecated> {
				public UT_RockMap() {
					Table("UT_Rock");
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Rock);
					Map(x => x.PeriodId).Column("PeriodId");
					References(x => x.Period).Column("PeriodId").LazyLoad().ReadOnly();
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}

		}

		public class UT_Measurable_Deprecated : ILongIdentifiable, IDeletable, IUserTemplateItem {
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual String Measurable { get; set; }
			public virtual LessGreater GoalDirection { get; set; }
			public virtual decimal Goal { get; set; }
			public virtual long TemplateId { get; set; }
			public virtual UserTemplate_Deprecated Template { get; set; }
			public class UT_MeasurableMap : ClassMap<UT_Measurable_Deprecated> {
				public UT_MeasurableMap() {
					Table("UT_Measurable");
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Measurable);
					Map(x => x.GoalDirection);
					Map(x => x.Goal);
					Map(x => x.TemplateId).Column("TemplateId");
					References(x => x.Template).Column("TemplateId").LazyLoad().ReadOnly();
				}
			}
		}
	}
}

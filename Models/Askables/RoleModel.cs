using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Askables {

	public class RoleModel_Deprecated : Askable
	{
		[Obsolete("Do not use. Use RoleLink instead")]
		public virtual long? FromTemplateItemId { get; set; }	
		public virtual long OrganizationId { get; set; }
		[Obsolete("Do not use. Use RoleLink instead")]
		public virtual long? ForUserId { get; set; }
		[Obsolete("Do not use. Use RoleLink instead")]
		public virtual UserOrganizationModel _Owner { get; set; }

		public virtual String Role { get; set; }
		public virtual Attach _Attach { get; set; }

		public override QuestionType GetQuestionType(){
			return QuestionType.GWC;
		}

		public override string GetQuestion(){
			return Role;
		}

		public class RMMap : SubclassMap<RoleModel_Deprecated>
		{
			public RMMap()
			{
				Table("RoleModel");
				Map(x => x.OrganizationId);
				Map(x => x.ForUserId).Column("ForUserId");
				Map(x => x.Role);
				Map(x => x.FromTemplateItemId);
			}
		}
	}

	public class RoleLink_Deprecated : IHistorical {
		public virtual long Id { get; set; }
		public virtual long RoleId { get; set; }
		public virtual long AttachId { get; set; }
		public virtual AttachType AttachType { get; set; }
		public virtual long OrganizationId { get; set; }

		public virtual long? Ordering { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual Attach GetAttach() {
			return new Attach(AttachType, AttachId);
		}

        private static int CtorCalls = 0;

		public RoleLink_Deprecated() {
            CtorCalls += 1;
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<RoleLink_Deprecated> {
			public Map() {
				Table("RoleLink");
				Id(x => x.Id);
				Map(x => x.RoleId);
				Map(x => x.AttachId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Map(x => x.Ordering);
				Map(x => x.AttachType).CustomType<AttachType>();
			}
		}
	}
}

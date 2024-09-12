using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using System;
using NHibernate.Envers.Configuration.Attributes;

namespace RadialReview.Models.UserModels {
	public class PositionDurationModel : IHistorical, ILongIdentifiable {
		[Obsolete("Did you mean Position.Id?")]
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }

		[Obsolete("use PositionName instead")]
		[Audited(TargetAuditMode = RelationTargetAuditMode.NotAudited)]
		public virtual Deprecated.OrganizationPositionModel DepricatedPosition { get; set; }

		public virtual string PositionName { get; set; }

		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long? DeletedBy { get; set; }
		public virtual long PromotedBy { get; set; }
		public virtual long OrganizationId { get; set; }



		public PositionDurationModel() { }


		public PositionDurationModel(string positionName, long orgId, long promotedBy, long forUserId) {
			PositionName = positionName;
			PromotedBy = promotedBy;
			CreateTime = DateTime.UtcNow;
			UserId = forUserId;
			OrganizationId = orgId;
		}


		[Obsolete("do not use", true)]
		public PositionDurationModel(Deprecated.OrganizationPositionModel position, long promotedBy, long forUserId) {

			DepricatedPosition = position;
			PromotedBy = promotedBy;
			CreateTime = DateTime.UtcNow;
			UserId = forUserId;
			OrganizationId = position.Organization.Id;

		}
		public class PositionDurationMap : ClassMap<PositionDurationModel> {
			public PositionDurationMap() {
				Id(x => x.Id);
				Map(x => x.CreateTime).Column("Start");
				Map(x => x.UserId);
				Map(x => x.DeletedBy);
				Map(x => x.DeleteTime);
				Map(x => x.PromotedBy);
				Map(x => x.OrganizationId);
				Map(x => x.PositionName);
				References(x => x.DepricatedPosition).Column("Position_id").Not.LazyLoad();
			}
		}
	}
}

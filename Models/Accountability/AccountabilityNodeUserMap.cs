using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Accountability {
	public class AccountabilityNodeUserMap : IHistorical {
		public const string TABLE_NAME = "AccountabilityNodeUserMap";
		public const string ACCOUNTABILITY_NODE_ID_NAME = "AccountabilityNodeId";
		public const string USER_ID_NAME = "UserId";
		public virtual long Id { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long ChartId { get; set; }
		public virtual long AccountabilityNodeId { get; set; }
		public virtual AccountabilityNode AccountabilityNode { get; set; }

		public virtual long UserId { get; set; }
		public virtual UserOrganizationModel User { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public AccountabilityNodeUserMap() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<AccountabilityNodeUserMap> {
			public Map() {

				Table(AccountabilityNodeUserMap.TABLE_NAME);

				Id(x => x.Id);

				Map(x => x.UserId).Column(USER_ID_NAME).Index("idx__AccountabilityNodeUserMap_UserId");
				References(x => x.User).Column(USER_ID_NAME).LazyLoad().ReadOnly();

				Map(x => x.AccountabilityNodeId).Column(ACCOUNTABILITY_NODE_ID_NAME).Index("idx__AccountabilityNodeUserMap_AccountabilityNodeId");
				References(x => x.AccountabilityNode).Column(ACCOUNTABILITY_NODE_ID_NAME).LazyLoad().ReadOnly();


				Map(x => x.ChartId);
				Map(x => x.OrgId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
			}
		}
	}
}
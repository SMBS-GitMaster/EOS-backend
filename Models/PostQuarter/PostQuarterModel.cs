using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.PostQuarter {
	
	public class PostQuarterModel : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long L10RecurrenceId { get; set; }
        public virtual long OrganizationId { get; set; }
        public virtual DateTime QuarterEndDate { get; set; }
        public virtual long CreatedBy { get; set; }

        public virtual string Name { get; set; }

		public PostQuarterModel() {			
			CreateTime = DateTime.UtcNow;			
		}

		public class Map : ClassMap<PostQuarterModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.L10RecurrenceId);
				Map(x => x.OrganizationId);
				Map(x => x.QuarterEndDate);
				Map(x => x.CreatedBy);
                Map(x => x.Name);
            }
		}
	}
}
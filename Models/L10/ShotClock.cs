using FluentNHibernate.Mapping;
using System;


namespace RadialReview.Models.L10 {
	public class ShotClock {
		public virtual long Id { get; set; }
		public virtual long ForModelId { get; set; }
		public virtual string ForModelType { get; set; }
		public virtual DateTime LastStartTime { get; set; }
		public virtual bool Ended { get; set; }
		public virtual int AccumulatedSeconds { get; set; }
		public virtual bool IsRunning {
			get {
				return !Ended;
			}
		}
		public virtual int CalculatedTotalSeconds {
			get {
				if (Ended) {
					return AccumulatedSeconds;
				} else {
					return AccumulatedSeconds + (int)(DateTime.UtcNow - LastStartTime).TotalSeconds;
				}
			}
		}

		public virtual long OrgId { get; set; }
		public virtual long RecurrenceId { get; set; }
		public virtual long MeetingId { get; set; }
		public virtual long StartedBy { get; set; }

		public ShotClock() {
			LastStartTime = DateTime.UtcNow;
		}


		public class Map : ClassMap<ShotClock> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.ForModelId).Index("IDX_ShotClock_ForModelId");
				Map(x => x.ForModelType);
				Map(x => x.LastStartTime);
				Map(x => x.Ended);
				Map(x => x.AccumulatedSeconds);
				Map(x => x.OrgId);
				Map(x => x.RecurrenceId);
				Map(x => x.MeetingId);
				Map(x => x.StartedBy);

			}


		}
	}
}
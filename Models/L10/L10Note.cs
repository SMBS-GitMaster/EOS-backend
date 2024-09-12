using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.L10 {
	public class L10Note : BaseModel, ILongIdentifiable, IDeletable {
		public virtual long Id { get; set; }
    public virtual DateTime? CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual String Name { get; set; }
		public virtual L10Recurrence Recurrence { get; set; }
		public virtual String Contents { get; set; }
		public virtual String PadId { get; set; }

    public virtual long? OwnerId { get; set; }

		public L10Note() {
			PadId = Guid.NewGuid().ToString();
      CreateTime = DateTime.Now;
		}

		public class L10NoteMap : BaseModelClassMap<L10Note> {
			public L10NoteMap() {
				Id(x => x.Id);
				Map(x => x.Contents).Length(10000);
        Map(x => x.CreateTime);
				Map(x => x.Name);
				Map(x => x.PadId);
				Map(x => x.DeleteTime);
        Map(x => x.OwnerId);
				References(x => x.Recurrence).Column("RecurrenceId");
			}
		}

	}
}
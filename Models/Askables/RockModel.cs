using System;
using System.Collections.Generic;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;
using RadialReview.Models.Angular.Base;
using Newtonsoft.Json;
using RadialReview.Models.Application;
using static RadialReview.Models.L10.L10Recurrence;

namespace RadialReview.Models.Askables {
	public class RockModel : Askable {

		public virtual String Rock { get; set; }

		[JsonIgnore]
		public virtual string Name { get { return Rock; } set { Rock = value; } }

		public virtual long? FromTemplateItemId { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long ForUserId { get; set; }
		///RE-ADD to the map
		[Obsolete("Do not use. Instead use L10Recurrent.Rock")]
		public virtual bool CompanyRock { get; set; }
		public virtual bool _CompanyRock { get; set; }
		public virtual DateTime? DueDate { get; set; }
		public virtual RockState Completion { get; set; }
		public virtual bool _AddedToVTO { get; set; }
		public virtual bool _AddedToL10 { get; set; }
		public virtual List<long> _L10Added { get; set; }
		public virtual List<NameId> _Origins { get; set; }

		public virtual bool HasAudio { get; set; }

		public override QuestionType GetQuestionType() {
			return QuestionType.Rock;
		}

		public virtual long? PeriodId { get; set; }
		public virtual PeriodModel Period { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }

		public virtual String EmailAtOrganization { get; set; }

		public virtual String PadId { get; set; }
		public virtual bool Archived { get; set; }

    // V3 new fields
    public virtual bool AddToDepartmentPlan { get; set; }
    public virtual DateTime? ArchivedTimestamp { get; set; }
    public virtual List<GoalRecurrenceRecord> _GoalRecurenceRecords { get; set; }

    public RockModel() {
			CreateTime = DateTime.UtcNow;
			OnlyAsk = AboutType.Self;
			Completion = RockState.OnTrack;
			_L10Added = new List<long>();
      _GoalRecurenceRecords = new List<GoalRecurrenceRecord>();

    }

		public override string GetQuestion() {
			return Rock;
		}

		public virtual string ToFriendlyString() {
			var b = Rock;
			if (AccountableUser != null)
				b += " (Owner: " + AccountableUser.GetName() + ")";

			var p = "";

			if (!string.IsNullOrWhiteSpace(p))
				b += "[" + p.Trim() + "]";

			return b;
		}

		public class RockModelMap : SubclassMap<RockModel> {
			public RockModelMap() {
				Map(x => x.Rock);
				Map(x => x.Archived);
				Map(x => x.PadId);
				Map(x => x.Completion);
				Map(x => x.DueDate);
				Map(x => x.OrganizationId);
				Map(x => x.FromTemplateItemId);
				Map(x => x.CompanyRock);
				Map(x => x.HasAudio);
				Map(x => x.CompleteTime);
				Map(x => x.PeriodId).Column("PeriodId");
				References(x => x.Period).Column("PeriodId").LazyLoad().ReadOnly();
				Map(x => x.ForUserId).Column("ForUserId");
				References(x => x.AccountableUser).Column("ForUserId").Not.LazyLoad().ReadOnly();

        // V3 new fields
        Map(x => x.AddToDepartmentPlan);
        Map(x => x.ArchivedTimestamp);

      }
		}
		public virtual void Angularize(Angularizer<RockModel> angularizer) {
			angularizer.Add("Name", x => x.Rock);
			angularizer.Add("Owner", x => x.AccountableUser);
			angularizer.Add("DueDate", x => x.DueDate);
			angularizer.Add("Complete", x => x.CompleteTime != null);
			angularizer.Add("Completion", x => x.Completion);
		}

	}
}

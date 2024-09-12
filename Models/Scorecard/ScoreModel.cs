using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Utilities.Extensions;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace RadialReview.Models.Scorecard {
	[DataContract]
    [DebuggerDisplay("{Id} = '{Measured}' @ {DataContract_ForWeek}")]
    public partial class ScoreModel : BaseModel, ILongIdentifiable, IDeletable {

        [DataMember(Order = 0)]
        public virtual long Id { get; set; }
        [DataMember(Name = "MeasurableId", Order = 1)]
        public virtual long MeasurableId { get; set; }
        [DataMember(Name = "ForWeekNumber", Order = 2)]
        public virtual long DataContract_ForWeek { get { return TimingUtility.GetWeekSinceEpoch(ForWeek); } }
        [DataMember(Name = "Value", Order = 3)]
        public virtual decimal? Measured { get; set; }
		public virtual DateTime ForWeek { get; set; }
        public virtual DateTime? DateEntered { get; set; }
		[Obsolete("Not used or updated or saved")]
        public virtual DateTime DateDue { get; set; }
        public virtual decimal? OriginalGoal { get; set; }
        public virtual decimal? AlternateOriginalGoal { get; set; }
        public virtual LessGreater? OriginalGoalDirection { get; set; }
        public virtual MeasurableModel Measurable { get; set; }
        public virtual long OrganizationId { get; set; }
        public virtual long AccountableUserId { get; set; }
        public virtual UserOrganizationModel AccountableUser { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
        public virtual string NoteText { get; set; }  
        public virtual bool _Editable { get; set; }
        public virtual long _OneMeeting { get; set; }


        public ScoreModel() {
            _Editable = true;
		}

        private LessGreater GetDirection() {
            return OriginalGoalDirection ?? Measurable.GoalDirection;
        }

        private decimal GetOriginalGoal() {
            return OriginalGoal ?? Measurable.Goal ?? 0;
        }

        private decimal GetAlternateGoal() {
            return AlternateOriginalGoal ?? Measurable.AlternateGoal ?? 0;
        }

        protected virtual string GetGoalDescription() {
            if(this == null)
                return string.Empty;

            LessGreater direction = GetDirection();
            decimal originalGoal = GetOriginalGoal();
            decimal alternateGoal = GetAlternateGoal();

            string description = $"GOAL {direction.GetDisplayName()} {Measurable.UnitType.Format(originalGoal)}";

            if (direction == LessGreater.Between)
                 description += $" - {Measurable.UnitType.Format(alternateGoal)}";

            return description;
        }

        protected virtual string GetMeasurableState() {
            string name = $"'{Measurable.Title}'";
            if (!Measured.HasValue) {
                return "Enter " + name;
            }

            decimal measured = Measured.Value;
            decimal initialGoal = GetOriginalGoal();
            LessGreater direction = GetDirection();
            int factor = Math.Sign((decimal)direction);

            if (direction == LessGreater.Between) {
                decimal alternateGoal = GetAlternateGoal();
                if (measured.IsBetween(initialGoal, alternateGoal)) {
                    return $"{name} goal was met at {Measurable.UnitType.Format(measured)}";
                }

                decimal missedDifference = measured - initialGoal;
                if (missedDifference < 0)
                    return $"{name} goal was missed by {Measurable.UnitType.Format(missedDifference)}";

                decimal exceededDifference = measured - alternateGoal;
                if (exceededDifference > 0)
                    return $"{name} goal was exceeded by {Measurable.UnitType.Format(exceededDifference)}";

            }

            decimal difference;
            if (MeetGoal()) {
                difference = (measured - initialGoal) * factor;
                if (difference == 0)
                    return name + " goal was met at " + initialGoal;

                if (measured == Math.Floor(measured) && initialGoal == Math.Floor(initialGoal))
                    return name + " goal was exceeded by " + Measurable.UnitType.Format((measured - initialGoal) * factor);

                if (initialGoal == 0)
                    return name + " goal was exceeded by " + (measured - initialGoal) * factor;

                return name + " goal was exceeded by " + ((measured - initialGoal) / initialGoal * factor * 100).ToString("0.####") + "%";
            }

            difference = (initialGoal - measured);
            if (factor == 0)
                difference = Math.Abs(difference);
            else
                difference = difference * factor;

            return name + " goal was missed by " + Measurable.UnitType.Format(difference);
        }

        public virtual bool MeetGoal() {

            LessGreater goalDirection = OriginalGoalDirection ?? Measurable.GoalDirection;
            decimal initialGoal = OriginalGoal ?? Measurable.Goal ?? 0;
            decimal alternateGoal = AlternateOriginalGoal ?? Measurable.AlternateGoal ?? 0;

            return goalDirection.MeetGoal(initialGoal, alternateGoal, Measured);
        }

        public class ScoreMap : BaseModelClassMap<ScoreModel> {
            public ScoreMap() {
                Id(x => x.Id);
                Map(x => x.DateEntered);
				Map(x => x.ForWeek);
				Map(x => x.Measured);
                Map(x => x.OriginalGoal);
                Map(x => x.AlternateOriginalGoal);
                Map(x => x.OriginalGoalDirection);
                Map(x => x.OrganizationId);
                Map(x => x.NoteText);
                Map(x => x.AccountableUserId).Column("AccountableUserId");
                References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();

                Map(x => x.MeasurableId).Column("MeasureableId");
                References(x => x.Measurable).Column("MeasureableId").Not.LazyLoad().ReadOnly();
                Map(x => x.DeleteTime);
            }
        }

        public class DataContract {
            public virtual long Id { get; set; }
            public virtual decimal? Value { get; set; }
            public virtual long ForWeek { get; set; }
            public virtual MeasurableModel Measurable { get; set; }

            public virtual UserOrganizationModel.DataContract AccountableUser { get { return Measurable.AccountableUser.GetUserDataContract(); } }
            public virtual UserOrganizationModel.DataContract AdminUser { get { return Measurable.AdminUser.GetUserDataContract(); } }

            public DataContract(ScoreModel self) {
                Id = self.Id;
                Value = self.Measured;
                ForWeek = self.DataContract_ForWeek;
                Measurable = self.Measurable;
            }
        }

    }
}

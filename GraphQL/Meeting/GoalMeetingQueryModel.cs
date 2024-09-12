using RadialReview.GraphQL.Types;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Accessors.RockAccessor;

namespace RadialReview.GraphQL.Models
{
  public class GoalMeetingQueryModel
  {

    #region Properties

    public long Id { get; set; }

    public string Name { get; set; }

    public long GoalId { get; set; }
    public IQueryable<MeetingAttendeeQueryModel> Attendees { get; set; }
    public long UserId { get; set; }

    #endregion

    #region Public Methods

    public static GoalMeetingQueryModel FromMeetingQueryModel(MeetingQueryModel source, long goalId)
    {
      return new GoalMeetingQueryModel
      {
        Id = source.Id,
        Name = source.Name,
        GoalId = goalId
      };
    }

    public static GoalMeetingQueryModel FromMeetingInstanceQueryModel(MeetingInstanceQueryModel source, long goalId)
    {
      return new GoalMeetingQueryModel
      {
        Id = source.Id,
        Name = string.Empty,
        GoalId = goalId
      };
    }

    public static GoalMeetingQueryModel FromGoalRecurrenceRecord(L10Recurrence.GoalRecurrenceRecord source)
    {
      return new GoalMeetingQueryModel
      {
        Id = source.RecurrenceId,
        Name = source.Name,
        GoalId = source.RecurrenceRockId
      };
    }

    #endregion

  }
}

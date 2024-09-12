using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using FluentNHibernate.Conventions;
using L10PageType = RadialReview.Models.L10.L10Recurrence.L10PageType;
using RadialReview.Models.Application;
using RadialReview.Models.L10.VM;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Core.GraphQL.Models.Mutations;
using Humanizer;
using RadialReview.Models.Enums;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Utilities.DataTypes;
using RadialReview.Core.GraphQL.Enumerations;
using ModelIssue = RadialReview.Models.Issues.IssueModel;

namespace RadialReview.Core.Repositories
{
  public static class MeetingInstanceTransformer
  {

    public static MeetingInstanceQueryModel MeetingInstanceFromRecurrence(this L10Recurrence source)
    {

      L10Meeting meeting = source.L10MeetingInProgress;

      // This is to handle V1 preview meetings not being used in V3.
      // If a V1 Preview Meeting instance is sent then it starts the timer and other things that we don't want to happen
      if (meeting != null && meeting.Preview)
      {
        return null;
      }

      TimeSpan? durationInSeconds = null;
      if (meeting != null && meeting.CompleteTime != null && meeting.StartTime != null)
      {
        durationInSeconds = (meeting.CompleteTime - meeting.StartTime);
      }

      return new MeetingInstanceQueryModel
      {
        Id = meeting == null ? 0 : meeting.Id,
        RecurrenceId = source.Id,
        AverageMeetingRating = meeting == null ? 0 : meeting.AverageMeetingRating != null && meeting.AverageMeetingRating.IsValid() ? Math.Round(meeting.AverageMeetingRating.GetValue(0) * 100) : 0,
        TodosCompletedPercentage = meeting == null ? 0 : meeting.TodoCompletion != null && meeting.TodoCompletion.IsValid() ? Math.Round(meeting.TodoCompletion.GetValue(0) * 100) : 0,
        MeetingDurationInSeconds = durationInSeconds != null ? durationInSeconds.Value.TotalSeconds : 0,
        MeetingStartTime = meeting?.StartTime.ToUnixTimeStamp(),
        LeaderId = meeting == null ? 0 : meeting.MeetingLeaderId,
        IsPaused = source.IsPaused,
        Notes = source._MeetingNotes?.Select(x => RepositoryTransformers.MeetingNoteFromL10Note(x)).ToList(),
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        MeetingConcludedTime = meeting?.CompleteTime.ToUnixTimeStamp(),
        TangentAlertTimestamp = source.TangentAlertTimestamp?.ToUnixTimeStamp(),
        IssueVotingHasEnded = meeting != null ? meeting.IssueVotingHasEnded : false,
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
      };
    }

    public static MeetingInstanceQueryModel MeetingInstanceFromL10Meeting(this L10Meeting source, long recurrenceId, List<ModelIssue.IssueModel_Recurrence> issuesList = null)
    {
      if (source == null)
      {
        return new MeetingInstanceQueryModel()
        {
          Id = recurrenceId,
          RecurrenceId = recurrenceId,
        };
      }

      TimeSpan? durationInSeconds = null;
      if (source.CompleteTime != null && source.StartTime != null)
      {
        durationInSeconds = (source.CompleteTime - source.StartTime);
      }

      return new MeetingInstanceQueryModel
      {
        Id = source.Id,
        RecurrenceId = source.L10RecurrenceId,
        AverageMeetingRating = source.AverageMeetingRating != null && source.AverageMeetingRating.IsValid() ?
                               Math.Round(source.AverageMeetingRating.GetValue(0),1, MidpointRounding.AwayFromZero) : 0,
        TodosCompletedPercentage = source.TodoCompletion != null && source.TodoCompletion.IsValid() ? Math.Round(source.TodoCompletion.GetValue(0) * 100) : 0,
        MeetingDurationInSeconds = durationInSeconds != null ? durationInSeconds.Value.TotalSeconds : 0,
        MeetingStartTime = source.StartTime.ToUnixTimeStamp(),
        LeaderId = source.MeetingLeaderId,
        IsPaused = source.L10Recurrence.IsPaused,
        IssuesSolvedCount = issuesList == null ? 0 : issuesList.Where(x => x.Recurrence.Id == source.L10RecurrenceId && x.CloseTime.HasValue).ToList().Count,
        Notes = source?.L10Recurrence?._MeetingNotes?.Select(x => RepositoryTransformers.MeetingNoteFromL10Note(x)).ToList(),
        Version = source.L10Recurrence.Version,
        LastUpdatedBy = source?.L10Recurrence?.LastUpdatedBy,
        DateLastModified = source?.L10Recurrence?.DateLastModified,
        DateCreated = source?.CreateTime.ToUnixTimeStamp(),
        MeetingConcludedTime = source.CompleteTime.ToUnixTimeStamp(),
        TangentAlertTimestamp = null,
        IssueVotingHasEnded = source.IssueVotingHasEnded,
      };

    }

  }
}

using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.Repositories;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, IQueryable<MeetingAttendeeQueryModel> attendees, CancellationToken cancellationToken);

    Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, long meetingId, CancellationToken cancellationToken);

    MeetingInstanceAttendeeQueryModel GetInstanceFromMeetingAttendee(long userId, long recurrenceId, CancellationToken cancellationToken);

    IQueryable<MeetingRatingModel> GetMeetingAttendeeInstancesForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public MeetingInstanceAttendeeQueryModel GetInstanceFromMeetingAttendee(long userId, long recurrenceId, CancellationToken cancellationToken)
    {
      UserQueryModel user = GetUserById(userId, cancellationToken);
      MeetingRatingModel rating = GetMeetingAttendeeInstancesForMeetings(new[] { recurrenceId }, cancellationToken).Where(x => x.Id == userId).FirstOrDefault();
      MeetingInstanceAttendeeQueryModel result = new MeetingInstanceAttendeeQueryModel();

      result.DateCreated = user.DateCreated;
      result.DateLastModified = user.DateLastModified;
      result.Id = user.Id;
      result.LastUpdatedBy = user.LastUpdatedBy;
      result.NotesText = rating != null ? rating.NotesId.ToString() : null;
      result.Rating = rating != null ? rating.Rating : null;
      result.Version = user.Version;
      result.User = user;

      return result;
    }

    public async Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, long meetingId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        try
        {
          using (var s = HibernateSession.GetCurrentSession())
          {
            var recurrenceAtendees = L10Accessor.GetRecurrenceAttendeesUnsafe(recurrenceId, s);
            List<long> attendeeIds = recurrenceAtendees.Select(x => x.User.Id).ToList();
            var attendeeList = L10Accessor.GetHistoricAttendeesByIdsUnsafe(s, attendeeIds, meetingId);
            return attendeeList.Select(x =>
            {
              var recurrenceAtendee = recurrenceAtendees.FirstOrDefault(a => a.User.Id == x.User.Id);
              MeetingAttendeeQueryModel attendee = MeetingAttendeeTransformer.TransformAttendee(recurrenceAtendee, false, false);
              return new MeetingInstanceAttendeeQueryModel()
              {
                Id = x.Id,
                Attendee = attendee,
                DateCreated = 0,
                HasVotedForIssues = attendee.HasSubmittedVotes, 
                Rating = x.Rating,
                NotesText = x.PadId,
                User = null,
              };
            }
           ).ToList();
          }         
        }
        catch (MeetingException e)
        {
          if (e.MeetingExceptionType == MeetingExceptionType.Unstarted)
            return null;
          throw;
        }

      });
    }

    public async Task<IQueryable<MeetingInstanceAttendeeQueryModel>> GetMeetingInstanceAttendees(long recurrenceId, IQueryable<MeetingAttendeeQueryModel> attendees, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        try
        {
          var attendeeIds = attendees.Select(x => x.Id).ToList();
          var attendeedList = L10Accessor.GetCurrentAttendeesByIds(caller, attendeeIds, recurrenceId);
          return attendeedList.Select(x => new MeetingInstanceAttendeeQueryModel()
          {
            Id = x.Id,
            Attendee = attendees.FirstOrDefault(a => a.Id == x.User.Id),
            DateCreated = 0,
            Rating = x.Rating,
            NotesText = x.PadId,
            User = null,

          }).ToList();
        }
        catch (MeetingException e)
        {
          if (e.MeetingExceptionType == MeetingExceptionType.Unstarted)
            return null;
          throw;
        }

      });
    }

    public IQueryable<MeetingRatingModel> GetAttendeeInstances(long meetingId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        var output = new List<MeetingRatingModel>();
        var attendees = L10Accessor.GetUserMeetingRates(caller, meetingId, false);

        if (attendees == null)
          return output/*.AsQueryable()*/;

        if (attendees.IsFailed)
          return output/*.AsQueryable()*/;

        Uri notesUrl = null;
        MeetingRatingModel meetingRate;
        var firstAttendeeUser = attendees.Value.First().User;
        var businessPlanId = L10Accessor.GetSharedVTOVision(firstAttendeeUser, firstAttendeeUser.Organization.Id);
        attendees.Value.ForEach((attendee) =>
        {

          meetingRate = new MeetingRatingModel()
          {
            Rating = attendee.Rating,
            Attendee = UserQueryModel.FromAttendee(attendee, businessPlanId.Value),
            Id = attendee.Id
          };

          if (!string.IsNullOrEmpty(attendee.PadId) && attendee.Rating.HasValue)
          {
            notesUrl = NoteUtils.BuildURL(padId: attendee.PadId,
                                          showControls: true,
                                          callerName: caller.GetName());
            meetingRate.NotesId = notesUrl.ToString();
          }

          output.Add(meetingRate);
        });

        return output;
      });
    }

    public IQueryable<MeetingRatingModel> GetMeetingAttendeeInstancesForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      var results =
          recurrenceIds.SelectMany(recurrenceId => GetAttendeeInstances(recurrenceId, cancellationToken));

      return results.AsQueryable();
    }


    #endregion

  }

}
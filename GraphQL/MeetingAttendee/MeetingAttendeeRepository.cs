using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    Task<IQueryable<MeetingAttendeeQueryModel>> GetMeetingAttendeesAsync(long recurrenceId, CancellationToken cancellationToken);
    IQueryable<MeetingAttendeeQueryModelLookup> GetMeetingAttendeesLookupAsync(long recurrenceId, CancellationToken cancellationToken);

    Task<MeetingAttendeeQueryModel> GetCallerAsMeetingAttendee(long recurrenceId, CancellationToken cancellationToken);

    Task<IQueryable<MeetingAttendeeQueryModel>> GetAttendeesByMeetingIds(List<long> meetingIds, CancellationToken cancellationToken);
    Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIds(List<long> meetingIds, CancellationToken cancellationToken);
    Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIds(List<long> meetingIds, long userId, CancellationToken cancellationToken);

    List<MeetingAttendeeQueryModel> GetMeetingAttendeeFromCaller(List<long> meetingIds);

    #endregion

    #region Mutations

    Task<GraphQLResponseBase> SetAttendeeHasVoted(long meetingId, bool hasVoted);

    Task<GraphQLResponseBase> SetMeetingAttendeeIsPresent(MeetingAttendeeIsPresentModel model);
    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public async Task<MeetingAttendeeQueryModel> GetCallerAsMeetingAttendee(long recurrenceId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);
          return MeetingAttendeeTransformer.MeetingAttendeeFromUserOrgModel(caller, recurrenceId);

        }
      }

    }

    public async Task<IQueryable<MeetingAttendeeQueryModel>> GetAttendeesByMeetingIds(List<long> meetingIds, CancellationToken cancellation)
    {
      var result = await L10Accessor.GetAttendeesByRecurenceIdsUnsafeAsync(caller, meetingIds);
       var attendees = result.Select(attendee => MeetingAttendeeTransformer.TransformAttendee(attendee, false, false)).AsQueryable();

      return attendees;
    }

    public async Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIds(List<long> meetingIds, CancellationToken cancellation)
    {
      Stopwatch sw = Stopwatch.StartNew();
      var result = await L10Accessor.GetCurrentAttendeesByRecurenceIdsUnsafeAsync(caller, meetingIds);
      var a = sw.ElapsedMilliseconds;
      var attendees = result.Select(attendee => MeetingAttendeeTransformer.TransformAttendee(attendee, false, false)).AsQueryable();
      var b = sw.ElapsedMilliseconds;

      return attendees;
    }

    public async Task<IQueryable<MeetingAttendeeQueryModel>> GetCurrentAttendeesByMeetingIds(List<long> meetingIds, long UserId, CancellationToken cancellation)
    {
      Stopwatch sw = Stopwatch.StartNew();
      List<MeetingAttendeeQueryModel> attendees = new List<MeetingAttendeeQueryModel>();     
      var result = await L10Accessor.GetCurrentAttendeesByRecurenceIdsUnsafeAsync(caller, meetingIds);
      var a = sw.ElapsedMilliseconds;
      attendees = result.Select(attendee => MeetingAttendeeTransformer.TransformAttendee(attendee, false, false)).ToList();
      var b = sw.ElapsedMilliseconds;
      
      var meetingIdsWithNoResult = meetingIds.Where(id => !attendees.Any(m => m.MeetingId == id)).ToList();
      // If the user is not an attendee but has permissions on the meeting
      if (meetingIdsWithNoResult.Count() > 0)
      {
        var meetignWithPermissions = GetMeetingAttendeeFromCaller(meetingIdsWithNoResult);
        attendees = new[] { attendees, meetignWithPermissions }.SelectMany(x => x).ToList();
      }
      // Setting id as unique for each meeting.
      attendees.Select(attendee =>
      {
        attendee.Id = $"{attendee.MeetingId}{attendee.Id}".ToLong();
        return attendee;
      }).ToList();

      return attendees.AsQueryable();
    }

    public List<MeetingAttendeeQueryModel> GetMeetingAttendeeFromCaller(List<long> MeetingIds)
    {
      List<MeetingAttendeeQueryModel> attendees = new();

      foreach (var meetingId in MeetingIds)
      {
        var attendee = caller.MeetingAttendeeFromUserOrgModel(meetingId);
        attendees.Add(attendee);
      }

      return attendees;
    }

    public async Task<IQueryable<MeetingAttendeeQueryModel>> GetMeetingAttendeesAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var attendees = L10Accessor.GetRecurrenceAttendeesUnsafe(recurrenceId, s);
          return attendees.Select(x =>
          {
            return MeetingAttendeeTransformer.MeetingAttendeeFromRecurrenceAttendee(x, recurrenceId);
          }).ToList().AsQueryable();

        }
      }
    }

    public IQueryable<MeetingAttendeeQueryModelLookup> GetMeetingAttendeesLookupAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var attendees = L10Accessor.GetRecurrenceAttendeesUnsafe(recurrenceId, s);
          return attendees.Select(x =>
          {
            return x.TransformAttendeeLookup();
          }).ToList().AsQueryable();

        }
      }
    }

    #endregion

    #region Mutations

    public async Task<GraphQLResponseBase> SetAttendeeHasVoted(long meetingId, bool hasVoted)
    {
      await L10Accessor.EditAttendee(caller, meetingId, caller.Id, null, hasVoted, null);
      return GraphQLResponseBase.Successfully();
    }

    public async Task<GraphQLResponseBase> SetMeetingAttendeeIsPresent(MeetingAttendeeIsPresentModel model)
    {
      try
      {
        await L10Accessor.EditAttendeeIsPresent(caller, model.MeetingId, model.MeetingAttendee, model.IsPresent);
        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(new ErrorDetail(ex.Message,GraphQLErrorType.Validation));
      }
    }

    #endregion

  }
}

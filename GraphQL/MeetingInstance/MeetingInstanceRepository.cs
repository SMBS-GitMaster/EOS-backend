using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Core.Repositories;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.IssueQueryModel.Associations;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    MeetingInstanceQueryModel GetInstanceForMeeting(long? meetingId, long? recurrenceId, CancellationToken cancellationToken);

    MeetingInstanceQueryModel GetMeetingInstanceByRecurrence(long recurrenceId, CancellationToken cancellationToken);

    IQueryable<MeetingInstanceQueryModel> GetInstancesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<GraphQLResponseBase> EditMeetingInstance(MeetingEditMeetingInstanceModel meetingEditModel);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public MeetingInstanceQueryModel GetInstanceForMeeting(long? meetingId, long? recurrenceId, CancellationToken cancellationToken)
    {
      if (meetingId == null)
        return null;
      return MeetingInstanceTransformer.MeetingInstanceFromRecurrence(L10Accessor.GetCurrentL10RecurrenceFromMeeting(caller, (long)meetingId, recurrenceId, loadUsers: false));
    }

    public IQueryable<MeetingInstanceQueryModel> GetInstancesForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {

      List<MeetingInstanceQueryModel> results = new List<MeetingInstanceQueryModel>();
      List<L10Meeting> meetings;

      using (var s = HibernateSession.GetCurrentSession())
      {
        foreach (long id in recurrenceIds)
        {
          meetings = L10Accessor.GetL10Meetings(caller, id);
          var issueList = L10Accessor.GetIssuesForRecurrence(caller, id, includeResolved: true);
          foreach (L10Meeting meeting in meetings)
          {
            results.Add(MeetingInstanceTransformer.MeetingInstanceFromL10Meeting(meeting, id, issueList));
          }
        }
      }

      return results.AsQueryable();
    }

    private MeetingInstanceQueryModel MeetingInstanceFromMeeting(long recurrenceId)
    {

      //probably could optimize this if we knew what graphQL was requesting
      var loadData = true;
      var curMeeting = L10Accessor.GetCurrentL10Meeting(caller, recurrenceId, true, loadData, loadLogs: true);
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var model = MeetingInstanceTransformer.MeetingInstanceFromL10Meeting(curMeeting, recurrenceId);
          return model;
        }
      }
    }

    public MeetingInstanceQueryModel GetMeetingInstanceByRecurrence(long recurrenceId, CancellationToken cancellationToken)
    {
      try
      {
        var currentMeeting = L10Accessor.GetCurrentL10Meeting(caller, recurrenceId);

        return currentMeeting.MeetingInstanceFromL10Meeting(recurrenceId);
      } catch(MeetingException)
      {
        return null;
      }

    }

    #endregion

    #region Mutations

    public async Task<GraphQLResponseBase> EditMeetingInstance(MeetingEditMeetingInstanceModel model)
    {
      try
      {

        if (model.LeaderId.HasValue)
          await L10Accessor.SetMeetingLeader(caller, (long)model.LeaderId, model.MeetingInstanceId, true);

        if (model.issueVotingHasEnded.HasValue)
        {
          await L10Accessor.IssueVotingHasEnded(caller, model.MeetingInstanceId, (bool)model.issueVotingHasEnded, true);
        }

        if (model.CurrentPageId.HasValue)
        {
          var currentLeaderId = await L10Accessor.GetMeetingLeader(caller, model.MeetingInstanceId, isInstanceId: true);
          // Validate the leader's permissions over the recurrence.
          L10Accessor.ValidateLeaderPermission(caller, model.MeetingInstanceId, currentLeaderId);
          await L10Accessor.UpdatePage(caller, caller.Id, model.MeetingInstanceId, "page-" + model.CurrentPageId, null);
        }

        // selected notes
        if (model.SelectedNotes != null)
        {
          var meetingId = L10Accessor.GetCurrentL10RecurrenceFromMeeting(caller, model.MeetingInstanceId, loadUsers: false).Id;

          var oldNotesIds = L10Accessor.GetL10MeetingNotes(caller, model.MeetingInstanceId);
          var set = SetUtility.AddRemove(oldNotesIds, model.SelectedNotes ?? new long[0]);
          var notes = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { meetingId });

          var noteInMeeting = model.SelectedNotes.Except(notes.Select(x => x.Id)).ToList();
          if (noteInMeeting.Any())
            return GraphQLResponse<ConcludeMeetingMutationOutputDTO>.Error(new ErrorDetail($"Note IDs:[{string.Join(", ", noteInMeeting)}] do not correspond to this meeting.", GraphQLErrorType.Validation));

          if (set.RemovedValues.Any()) {
            var notesToDelete = notes.Where(note => set.RemovedValues.Contains(note.Id)).Select(x => x.PadId).ToList();

            await L10Accessor.DeleteL10MeetingNote(caller, model.MeetingInstanceId, notesToDelete);
          }


          if (set.AddedValues.Any())
          {
            var notePadIds = notes.Where(note => set.AddedValues.Contains(note.Id)).Select(x => x.PadId).ToList();
            await L10Accessor.CreateL10MeetingNotes(caller, model.MeetingInstanceId, notePadIds);
          }
        }

        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    #endregion

  }
}
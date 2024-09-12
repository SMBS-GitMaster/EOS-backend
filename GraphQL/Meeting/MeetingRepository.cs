using FluentNHibernate.Testing.Values;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Accessors;
using RadialReview.Core.GraphQL.Common;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.MeetingListLookup;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Models;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<double> AverageMeetingRating(long recurrenceId, CancellationToken cancellationToken);

    MeetingQueryModel GetMeeting(long? id, CancellationToken cancellationToken);

    MeetingQueryModel GetMeeting(long? id, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetings(IEnumerable<long> ids, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsFast(IEnumerable<long> ids, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsForGoals(IEnumerable<long> rockIds, CancellationToken cancellationToken);

    IQueryable<GoalMeetingQueryModel> GetGoalMeetings(IEnumerable<long> rockIds, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsForMetric(long measurableId, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsForUser(CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);

    IQueryable<MeetingListLookupModel> GetMeetingsListLookupForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsByRecurrences(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsByRecurrences(IEnumerable<long> recurrenceIds, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken);

    IQueryable<MeetingMetadataModel> GetMeetingMetadataForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingz(IEnumerable<long> ids, CancellationToken cancellationToken);

    MeetingPermissionsModel GetPermissionsForCallerOnMeeting(long recurrenceId);

    IQueryable<MeetingPageQueryModel> GetPagesByMeetingIds(List<long> meetingIds, CancellationToken cancellationToken);

    IQueryable<MeetingQueryModel> GetMeetingsByIds(IEnumerable<long> meetingIds, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken);

    Task<IQueryable<(long measurableId, MeetingQueryModel meeting)>> GetRecurrenceForMetric(IEnumerable<long> measurableIds, CancellationToken cancellationToken);
    IQueryable<MeetingModeModel> GetMeetingModes(CancellationToken cancellationToken);
    #endregion

    #region Meeting services utility
    List<long> GetRecurrencesIdByCallerOrganizationUnsafe();
    void ViewL10Recurrence(long recurrenceId);
    void EditL10Recurrence(long recurrenceId);
    bool IsMeetingOrOrganizationAdmin(long recurrenceId);

    #endregion

    #region Mutations

    Task<GraphQLResponse<ConcludeMeetingMutationOutputDTO>> ConcludeMeeting(ConcludeMeetingModel conclude);

    Task<IdModel> CreateMeeting(MeetingCreateModel meetingCreateModel);

    Task<IdModel> EditMeeting(MeetingEditModel meetingEditModel);

    Task<IdModel> EditMeetingConcludeActions(MeetingEditConcludeActionsModel meetingEditConcludeActionModel);

    Task<GraphQLResponseBase> RateMeeting(long meetingId, decimal? rating, string notes);

    Task<GraphQLResponseBase> RestartMeeting(RestartMeetingModel restartMeeting);

    Task<IdModel> EditMeetingLastViewedTimestamp(MeetingEditLastViewedTimestampModel meetingEditLastViewedTimestamp);

    Task<GraphQLResponse<StartMeetingMutationOutputDTO>> StartMeeting(StartMeetingModel startMeeting);

    Task SetTangentAlert(SetTangentAlertInput input, CancellationToken cancellationToken);

    #endregion

    #region BusinessPlanMethods
    AngularVTO GetLegacyBusinessPlanForMeeting(long meetingId);

    #endregion
  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<double> AverageMeetingRating(long recurrenceId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return new double[] { (double)L10Accessor.GetAverageMeetingRatingMostRecent(caller, recurrenceId).GetValue(0) };
      });
    }

    public MeetingQueryModel GetMeeting(long? recurrenceId, CancellationToken cancellationToken)
    {
      if (recurrenceId == null) return null;
      return GetMeetingsByRecurrences(new[] { (long)recurrenceId }, cancellationToken).SingleOrDefault();
    }

    public MeetingQueryModel GetMeeting(long? recurrenceId, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken)
    {
      if (recurrenceId == null) return null;
      return GetMeetingsByRecurrences(new[] { (long)recurrenceId }, loadMeetingModel, cancellationToken).SingleOrDefault();
    }

    public IQueryable<MeetingQueryModel> GetMeetings(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
      return GetMeetingsByRecurrences(ids, cancellationToken);
    }

    public IQueryable<MeetingQueryModel> GetMeetingsFast(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
      return GetMeetingsByRecurrencesFast(ids, cancellationToken);
    }

    public IQueryable<MeetingQueryModel> GetMeetingsForGoals(IEnumerable<long> rockIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {

        var recurrenceIds = new List<long>();
        foreach (var rockId in rockIds)
        {
          var recurIds = RockAccessor.GetRecurrencesContainingRock(caller, rockId).Select(x => x.RecurrenceId);
          recurrenceIds.AddRange(recurIds);
        }
        recurrenceIds = recurrenceIds.Distinct().ToList();

        var results = GetMeetingsByRecurrences(recurrenceIds, LoadMeetingModel.False(), cancellationToken);
        return results;
      });
    }

    public IQueryable<GoalMeetingQueryModel> GetGoalMeetings(IEnumerable<long> rockIds, CancellationToken cancellationToken)
    {
      var results = new List<GoalMeetingQueryModel>();
      foreach (var rockId in rockIds)
      {
        var recurIds = RockAccessor.GetRecurrencesContainingRock(caller, rockId).Select(_ => GoalMeetingQueryModel.FromGoalRecurrenceRecord(_));
        results.AddRange(recurIds);
      }

      return results.AsQueryable();
    }

    public IQueryable<MeetingQueryModel> GetMeetingsForMetric(long measurableId, CancellationToken cancellationToken)
    {
      var recurrences =
          L10Accessor.GetMeasurableRecurrences(caller, measurableId)
          .Result // TODO: Elminate use of .Result in this line!
          .Select(x => x.RecurrenceId);

      var results = GetMeetingsByRecurrences(recurrences, LoadMeetingModel.False(), cancellationToken);

      return results;
    }

    public async Task<IQueryable<(long measurableId, MeetingQueryModel meeting)>> GetRecurrenceForMetric(IEnumerable<long> measurableIds, CancellationToken cancellationToken)
    {
      var s = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(s, caller);

      var recurrencesTask = await L10Accessor.GetRecurrencesForMeasurableIds(s, perms, measurableIds);

      ConcurrentBag<(long, MeetingQueryModel)> transformedRecurrences = new ConcurrentBag<(long measurableId, MeetingQueryModel meeting)>();

      Parallel.ForEach(recurrencesTask, recurrence =>
      {
        transformedRecurrences.Add((recurrence.Measurable.Id, RepositoryTransformers.TransformMeasurableRecurrenceToMeetingQueryModel(recurrence.L10Recurrence)));
      });

      return transformedRecurrences.ToList().AsQueryable();
    }

    public IQueryable<MeetingQueryModel> GetMeetingsForUser(CancellationToken cancellationToken)
    {
      return GetMeetingsForUsers(new[] { caller.Id }, cancellationToken);
    }

    public IQueryable<MeetingListLookupModel> GetMeetingsListLookupForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(s, caller);
        var results = userIds
            .SelectMany(userId =>
            {
              var user = s.Get<Models.UserOrganizationModel>(userId);
              var perms = PermissionsUtility.Create(s, user);


              List<TinyRecurrence> recurrences = L10Accessor.GetVisibleL10Recurrences(s, perms, userId, loadFavorites: true);

              //=====MOVED INTO GetVisibleL10Recurrences=====
              //List<FavoriteModel> favorites = FavoriteAccessor.GetFavoriteForMeetingsUserQuery(s, user, Models.FavoriteType.Meeting, recurrencesIds);

              //========DO WE ACTUALLY NEED SETTINGS?========
              //List<MeetingSettingsModel> settingsList = MeetingSettingsAccessor.GetSettingsForMeetings(s, user, recurrencesIds);
              List<MeetingSettingsModel> settingsList = new List<MeetingSettingsModel>();

              return recurrences.Select(rec =>
              {
                //FavoriteModel favorite = favorites.Where(x => x.ParentId == rec.Id).FirstOrDefault();
                MeetingSettingsModel settings = settingsList.Where(x => x.RecurrenceId == rec.Id).FirstOrDefault();
                return RepositoryTransformers.TransformMeetingListLookup(rec, user.Id, rec.Favorite, settings);
              }).Where(x => x != null);
            })
            .ToList();
        return results.AsQueryable();
      }
    }

    public IQueryable<MeetingQueryModel> GetMeetingsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(s, caller);

        var results =
            userIds
            .SelectMany(userId =>
            {
              var user = s.Get<Models.UserOrganizationModel>(userId);
              var perms = PermissionsUtility.Create(s, user);
              return
                L10Accessor
                  .GetVisibleL10Recurrences(s, perms, userId, true)
                  .Select(x =>
                  {
                    try
                    {
                      return GetMeetingModelFast(s, x.Id, user, LoadMeeting.False(), cancellationToken);
                    }
                    catch (RadialReview.Exceptions.PermissionsException _e)
                    {
                      return null;
                    }
                  })
                  .Where(x => x != null);
            })
            .ToList();
        List<long> recIds = results.Select(x => x.Id).ToList();
        List<L10Recurrence.L10Recurrence_Attendee> allAttendees = L10Accessor.GetRecurrenceAttendeesLookupByRecurenceIdsUnsafe(s, recIds);
        foreach (var result in results)
        {
          var attendees = allAttendees.Where(x => x.L10Recurrence.Id == result.Id).ToList();
          result.AttendeesLookup = attendees.Select(x => MeetingAttendeeTransformer.TransformAttendeeLookup(x)).ToList().AsQueryable();
        }

        return results.AsQueryable();
      }
    }

    public IQueryable<MeetingQueryModel> GetMeetingsByRecurrences(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      /*
        Some headlines have a recurrenceId that has already been deleted (DeleteTime of the Recurrence is not null),
        so the perms.ViewL10Recurrence(recId) function is throwing the "Can not view this weekly meeting" exception.
      */
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return recurrenceIds.Select(recurrenceId => GetMeetingModel(recurrenceId, null, cancellationToken)).Where(meeting => meeting != null && meeting.Id != 0).ToList();
      });
    }

    public IQueryable<MeetingQueryModel> GetMeetingsByRecurrences(IEnumerable<long> recurrenceIds, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken)
    {
      /*
        Some headlines have a recurrenceId that has already been deleted (DeleteTime of the Recurrence is not null),
        so the perms.ViewL10Recurrence(recId) function is throwing the "Can not view this weekly meeting" exception.
      */
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return recurrenceIds.Select(recurrenceId => GetMeetingModel(recurrenceId, loadMeetingModel, cancellationToken)).Where(meeting => meeting != null && meeting.Id != 0).ToList();
      });
    }

    public IQueryable<MeetingMetadataModel> GetMeetingMetadataForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        //throw new Exception("fix issues from obsolete tag");
        //throw new Exception("userIds iterater never used");
        return userIds.SelectMany(userId =>
            L10Accessor.GetVisibleL10Recurrences(caller, userId)
            .Select(x => RepositoryTransformers.MeetingMetadataFromTinyRecurrence(x, userId))
        );
      });
    }

    public IQueryable<MeetingQueryModel> GetMeetingz(IEnumerable<long> ids, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        var results = ids.Select(id => new MeetingQueryModel { Id = id });
        return results;
      });
    }

    public IQueryable<MeetingQueryModel> GetMeetingsByRecurrencesFast(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return recurrenceIds.Select(recurrenceId => GetMeetingModelFast(recurrenceId, cancellationToken)).ToList();
      });
    }

    public IQueryable<MeetingModeModel> GetMeetingModes(CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return L10Accessor.GetMeetingModes(TermsAccessor.GetTermsCollection(caller, caller.Organization.Id)).Select(x => MeetingModeModel.ToMeetingModeModel(x)).ToList();
      });
    }

    public MeetingPermissionsModel GetPermissionsForCallerOnMeeting(long recurrenceId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);

          return CreateAttendeePermissionsModel(s, perms, caller.Id, recurrenceId);
        }
      }
    }

    private MeetingQueryModel GetMeetingModel(long recurrenceId, LoadMeetingModel loadMeetingModel, CancellationToken cancellationToken)
    {

      if (cancellationToken.IsCancellationRequested)
        return new MeetingQueryModel();

      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          try
          {
            var perms = PermissionsUtility.Create(s, caller);

            //LoadMeeting tells us what things to resolve about the meeting. If we dont need rocks, we could set it to false
            //Could maybe be optimized if we could figure out what properties were requested at this point.
            var source = L10Accessor.GetL10Recurrence(s, perms, recurrenceId, loadMeetingModel.ToLoadMeeting());

            return MeetingFromL10Recurrence(source, cancellationToken, loadMeetingModel.LoadSettings, loadMeetingModel.LoadFavorites);
          }
          catch (Exception)
          {
            return new MeetingQueryModel();
          }
        }
      }
    }

    private MeetingQueryModel GetMeetingModelFast(long recurrenceId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        LoadMeeting loadMeeting = LoadMeeting.False();
        loadMeeting.LoadUsers = true;
        MeetingQueryModel meeting = GetMeetingModelFast(s, recurrenceId, caller, loadMeeting, cancellationToken);
        List<L10Recurrence.L10Recurrence_Attendee> attendees = L10Accessor.GetRecurrenceAttendeesLookupByRecurenceIdsUnsafe(s, new List<long> { recurrenceId });
        meeting.AttendeesLookup = attendees.Select(x => MeetingAttendeeTransformer.TransformAttendeeLookup(x)).ToList().AsQueryable();
        return meeting;
      }
    }

    private MeetingQueryModel GetMeetingModelFast(ISession s, long recurrenceId, Models.UserOrganizationModel user, LoadMeeting loadMeeting, CancellationToken cancellationToken)
    {

      if (cancellationToken.IsCancellationRequested)
        return new MeetingQueryModel();

      //LoadMeeting tells us what things to resolve about the meeting. If we dont need rocks, we could set it to false
      //Could maybe be optimized if we could figure out what properties were requested at this point.

      loadMeeting.LoadPages = true;
      loadMeeting.LoadConclusionActions = true;
      var source = L10Accessor.GetL10Recurrence(user, recurrenceId, loadMeeting);
      var userOrgId = L10Accessor.GetOrgUserIdFromRecurrenceId(dbContext, user, recurrenceId);

      var favorite = FavoriteAccessor.GetFavoriteForUser(user, Models.FavoriteType.Meeting, source.Id);
      var settings = MeetingSettingsAccessor.GetSettingsForMeeting(user, recurrenceId);
      return source.MeetingFromRecurrence(user, favorite, settings, userOrgId);
    }

    public MeetingQueryModel MeetingFromL10Recurrence(L10Recurrence source, CancellationToken cancellationToken, bool loadSettings = true, bool loadFavorites = true)
    {
      var meetingQueryModel = SlimMeetingFromL10Recurrence(source, cancellationToken);

      var favorite = loadFavorites ? FavoriteAccessor.GetFavoriteForUser(caller, Models.FavoriteType.Meeting, source.Id) : null;
      var settings = loadSettings ? MeetingSettingsAccessor.GetSettingsForMeeting(caller, source.Id) : null;

      meetingQueryModel.OrgId = source.OrganizationId;
      meetingQueryModel.FavoriteId = favorite?.Id;
      meetingQueryModel.FavoritedSortingPosition = favorite?.Position;
      meetingQueryModel.FavoritedTimestamp = favorite?.CreatedDateTime.ToUnixTimeStamp();
      meetingQueryModel.LastViewedTimestamp = settings?.LastViewedTimestamp;
      meetingQueryModel.ScheduledStartTime = source.L10MeetingInProgress != null ? source.L10MeetingInProgress.StartTime.ToUnixTimeStamp() : null;
      meetingQueryModel.ScheduledEndTime = source.L10MeetingInProgress != null ? source.L10MeetingInProgress.CompleteTime.ToUnixTimeStamp() : null;
      meetingQueryModel.MeetingPages = source._Pages?.Select(page => RepositoryTransformers.MeetingPageFromL10RecurrencePage(page)).AsQueryable();
      meetingQueryModel.Notes = source._MeetingNotes?.Select(note => RepositoryTransformers.MeetingNoteFromL10Note(note)).AsQueryable();
      meetingQueryModel.UserIsAttendee = source._DefaultAttendees != null ? source._DefaultAttendees.Any(a => a.User.Id == caller.Id) : false;
      meetingQueryModel.ExpectedMeetingDurationFromAgendaInMinutes = source._Pages != null ? source._Pages.Any() ? source._Pages.Sum(x => (decimal)x.Minutes) : 0.0m : 0;
      return meetingQueryModel;
    }

    public MeetingQueryModel SlimMeetingFromL10Recurrence(L10Recurrence source, CancellationToken cancellationToken)
    {
      return new MeetingQueryModel()
      {
        Id = source.Id,
        Name = source.Name,
        UserId = caller.Id,
        Email = caller.GetEmail(),
        MeetingType = source.MeetingType.ToString(),
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        CreatedTimestamp = source.CreateTime.ToUnixTimeStamp(),
        VideoConferenceLink = source.VideoConferenceLink, // New V3 field, not backwards compatible
        IssueVoting = ((gqlIssueVoting)source.Prioritization).ToString(),
        Version = source.Version,
        LastUpdatedBy = source.LastUpdatedBy,
        DateLastModified = source.DateLastModified,
        //Workspace = GetWorkspaceForMeeting(source.Id),
        CurrentMeetingInstanceId = source.MeetingInProgress,
        Archived = source.DeleteTime != null,
        StartOfWeekOverride = source.StartOfWeekOverride?.ToString(),
        HighlightPreviousWeekForMetrics = source.CurrentWeekHighlightShift == -1,
        ReverseMetrics = source.ReverseScorecard,
        PreventEditingUnownedMetrics = source.PreventEditingUnownedMeasurables,
      };
    }

    public IQueryable<MeetingPageQueryModel> GetPagesByMeetingIds(List<long> meetingIds, CancellationToken cancellationToken)
    {
      var pages = L10Accessor.GetMeetingPagesByRecurrenceIds(meetingIds);
      return pages.Select(page => RepositoryTransformers.MeetingPageFromL10RecurrencePage(page)).AsQueryable();
    }

    public IQueryable<MeetingQueryModel> GetMeetingsByIds(IEnumerable<long> meetingIds, LoadMeetingModel loadMeetingModel, CancellationToken ct)
    {
      //LoadMeeting tells us what things to resolve about the meeting. If we dont need rocks, we could set it to false
      //Could maybe be optimized if we could figure out what properties were requested at this point.
      using (var session = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(session, caller);
        var result = L10Accessor.GetRecurrencesByIds(session, perms, meetingIds.ToList(), LoadMeeting.False());
        return result.Select(x => SlimMeetingFromL10Recurrence(x, ct)).AsQueryable();
      }
    }

    #endregion

    #region Meeting services utility
    public List<long> GetRecurrencesIdByCallerOrganizationUnsafe()
    {
      var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      List<long> recurrenceIds = L10Accessor.GetRecurrencesIdByCallerOrganizationUnsafe(session, perms);
      return recurrenceIds;
    }

    public void ViewL10Recurrence(long recurrenceId)
    {
      var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      perms.ViewL10Recurrence(recurrenceId);
    }

    public void EditL10Recurrence(long recurrenceId)
    {
      var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      perms.EditL10Recurrence(recurrenceId);
    }
    public bool IsMeetingOrOrganizationAdmin(long recurrenceId)
    {
      var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      var l10Admin = perms.IsPermitted(x => x.EditL10Recurrence(recurrenceId));
      var orgAdmin = perms.IsPermitted(x => x.ManagingOrganization(caller.Organization.Id));
      return (l10Admin || orgAdmin) ? true : false;
    }
    #endregion

    #region Mutations

    public async Task<GraphQLResponse<ConcludeMeetingMutationOutputDTO>> ConcludeMeeting(ConcludeMeetingModel concludeMeeting)
    {
      try
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            var sendTo = EnumHelper.ConvertToNonNullableEnum<gqlSendEmailSummaryTo>(concludeMeeting.SendEmailSummaryTo).ToConcludeSendEmail();

            //!! Should be replaced with GetMeeting's info
            var meetingInstances = MeetingInstanceFromMeeting(concludeMeeting.MeetingId);
            //RatinPrivacy and IncludeMeetingNotes parameters have been ignored.
            //await L10Accessor.ConcludeMeeting(caller, _notesProvider, concludeMeeting);

            var recurrenceId = concludeMeeting.MeetingId;

            var notePadIds = new List<string>();
            if (concludeMeeting.IncludeMeetingNotesInEmailSummary && concludeMeeting.SelectedNotes.Any())
            {
              var notes = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { recurrenceId });
              var noteInMeeting = concludeMeeting.SelectedNotes.Except(notes.Select(x => x.Id)).ToList();
              if (noteInMeeting.Any())
                return GraphQLResponse<ConcludeMeetingMutationOutputDTO>.Error(new ErrorDetail($"Note IDs:[{string.Join(", ", noteInMeeting)}] do not correspond to this meeting.", GraphQLErrorType.Validation));

              notePadIds = notes.Where(note => concludeMeeting.SelectedNotes.Contains(note.Id)).Select(x => x.PadId).ToList();
            }

            await ResetIssueStarVoting(recurrenceId);


            var ratings = await L10Accessor.GetMeetingAttendeeRatings(caller, meetingInstances.Id);
            await L10Accessor.ConcludeMeeting(caller, _notesProvider, concludeMeeting.MeetingId, ratings, sendTo,
            concludeMeeting.ArchiveCompletedTodos, concludeMeeting.ArchiveHeadlines, null, meetingNotesIds: notePadIds, isUsingV3: true);

            var concludeActions = RepositoryTransformers.TransformConcludeActionsModelInConcludeMeeting(concludeMeeting);

            await L10Accessor.UpdateRecurrence(caller, recurrenceId, concludeActionsModel: concludeActions);

            var output = new ConcludeMeetingMutationOutputDTO(AverageMeetingRating: meetingInstances.AverageMeetingRating, MeetingDurationInSeconds: meetingInstances.MeetingDurationInSeconds,
                                                          IssuesSolvedCount: meetingInstances.IssuesSolvedCount, TodosCompletedPercentage: meetingInstances.TodosCompletedPercentage,
                                                          FeedbackStyle: concludeActions.FeedbackStyle);
            LoadMeeting loadMeeting = LoadMeeting.False();
            loadMeeting.LoadUsers = true;
            var recur = L10Accessor.GetL10Recurrence(caller, concludeMeeting.MeetingId, loadMeeting);
            if (recur._DefaultAttendees != null)
            {
              foreach (var item in recur._DefaultAttendees)
              {
                await L10Accessor.EditAttendee(caller, recur.Id, item.User.Id, false, null, null, concludeMeeting: true);
              }
            }
            tx.Commit();
            s.Flush();
            return GraphQLResponse<ConcludeMeetingMutationOutputDTO>.Successfully(output);
          }
        }
      }
      catch (Exception ex)
      {
        return GraphQLResponse<ConcludeMeetingMutationOutputDTO>.Error(ex);
      }
    }

    private MeetingPermissionsModel CreateAttendeePermissionsModel(ISession s, PermissionsUtility callerPerms, long forUserOrgId, long recurrenceId)
    {
      MeetingPermissionsModel permissions = MeetingPermissionsModel.CreateDefault();

      Stopwatch sw = Stopwatch.StartNew();
      long a, b, c;
      if (forUserOrgId == caller.Id)
      {
        permissions.Admin = callerPerms.IsPermitted(x => x.AdminL10Recurrence(recurrenceId));
        a = sw.ElapsedMilliseconds;
        permissions.Edit = callerPerms.IsPermitted(x => x.EditL10Recurrence(recurrenceId));
        b = sw.ElapsedMilliseconds;
        permissions.View = callerPerms.IsPermitted(x => x.ViewL10Recurrence(recurrenceId));
        c = sw.ElapsedMilliseconds;
      }

      return permissions;
    }

    public async Task<IdModel> CreateMeeting(MeetingCreateModel meetingCreateModel)
    {
      ErrorOnNonDefault(meetingCreateModel, x => x.ScheduledStartTime);
      ErrorOnNonDefault(meetingCreateModel, x => x.ScheduledEndTime);
      gqlAgendaType _AgendaType;
      gqlMeetingType _MeetingType;

      if(Enum.TryParse(meetingCreateModel.AgendaType, out _AgendaType) && Enum.TryParse(meetingCreateModel.MeetingType, out _MeetingType))
      {
        var agendaType = _AgendaType.ToMeetingType();
        var meetingType = _MeetingType.ToL10TeamType();
        // It is verified whether all attendees have the same permissions to create the 'members' group.
        var attendeesHaveSamePermissions = L10Accessor.ArePermissionEqual(meetingCreateModel.AttendeeIdByPermissions);
        var attendeesPerms = attendeesHaveSamePermissions ? meetingCreateModel.AttendeeIdByPermissions.First().permissions : null;
        var _recurrence = await L10Accessor.
          CreateBlankRecurrence(caller, caller.Organization.Id, false, agendaType, meetingCreateModel.Name, meetingCreateModel.VideoConferenceLink, meetingType, attendeesHaveSamePermissions, attendeesPerms);

        if (meetingCreateModel.AttendeeIdByPermissions != null)
        {
          foreach (var attendee in meetingCreateModel.AttendeeIdByPermissions)
          {
            await L10Accessor.AddAttendee(caller, _recurrence.Id, attendee.Id);
          }
        }

        if (!attendeesHaveSamePermissions && meetingCreateModel.AttendeeIdByPermissions != null)
          await L10Accessor.CreateL10PermsByUser(caller, _recurrence.Id, meetingCreateModel.AttendeeIdByPermissions);

        if (meetingCreateModel.MemberIdByPermissions != null)
          await L10Accessor.CreateL10PermsByUser(caller, _recurrence.Id, meetingCreateModel.MemberIdByPermissions);

        return new IdModel(_recurrence.Id);
      } else
      {
        return null;
      }
    }

    public async Task<IdModel> EditMeetingConcludeActions(MeetingEditConcludeActionsModel meetingEditConcludeActionsModel)
    {
      await L10Accessor.UpdateRecurrenceConcludeActions(caller, meetingEditConcludeActionsModel.MeetingId, meetingEditConcludeActionsModel.ConcludeActions);
      return new IdModel(meetingEditConcludeActionsModel.MeetingId);
    }

    public async Task<IdModel> EditMeeting(MeetingEditModel model)
    {
      ErrorOnNonDefault(model, x => x.ScheduledStartTime);
      ErrorOnNonDefault(model, x => x.ScheduledEndTime);
      ErrorOnNonDefault(model, x => x.MeetingPages);
      ErrorOnNonDefault(model, x => x.MeetingInstances);

      await L10Accessor.UpdateRecurrence(caller, model.MeetingId, null, model.Name,
                                          concludeActionsModel: model.ConcludeActions, meetingEditModel: model,
                                          videoConferenceLink: model.VideoConferenceLink, ignoreTangentAlertTimestamp: true);

      var settings = MeetingSettingsAccessor.GetSettingsForMeeting(caller, model.MeetingId);
      if (model.LastViewedTimestamp != null)
      {
        MeetingSettingsAccessor.EditSettings(caller, settings.Id, model.LastViewedTimestamp);
      }

      if (model.Attendees != null)
      {
        var existingAttendeeIds = (await L10Accessor.GetAttendees(caller, model.MeetingId)).Select(x => x.Id).ToList();
        var ar = SetUtility.AddRemove(existingAttendeeIds, model.Attendees);
        foreach (var added in ar.AddedValues)
        {
          await L10Accessor.AddAttendee(caller, model.MeetingId, added);
        }

        var detachTime = DateTime.UtcNow;

        foreach (var removed in ar.RemovedValues)
        {
          await L10Accessor.RemoveAttendee(caller, model.MeetingId, removed, detachTime);
        }
      }


      return new IdModel(model.MeetingId);
    }

    public async Task<GraphQLResponseBase> RateMeeting(long meetingId, decimal? rating, string notes)
    {
      try
      {
        await L10Accessor.UpdateUserRating(caller, meetingId, rating, notes);
        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    public async Task<IdModel> EditMeetingLastViewedTimestamp(MeetingEditLastViewedTimestampModel meetingEditLastViewed)
    {
      try
      {
        await L10Accessor.UpdateMeetingLastViewedTimestamp(caller, meetingEditLastViewed);
        return new IdModel(meetingEditLastViewed.MeetingId);
      }
      catch(Exception ex)
      {
        throw new Exception(ex.Message);
      }

    }
    public async Task<GraphQLResponseBase> RestartMeeting(RestartMeetingModel restartMeeting)
    {
      try
      {
        await L10Accessor.RestartMeeting(caller, _notesProvider, restartMeeting.TimeRestarted, restartMeeting.MeetingId);

        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    public async Task<GraphQLResponse<StartMeetingMutationOutputDTO>> StartMeeting(StartMeetingModel startMeeting)
    {
      try
      {
        // We will conclude the meeting if it has been initiated in 'preview' mode in v1.
        var currentMeetingInstance = L10Accessor.GetCurrentL10Meeting(caller, startMeeting.MeetingId, true);
        if (currentMeetingInstance is not null && currentMeetingInstance.Preview)
        {
          await L10Accessor.ConcludeMeeting(caller, _notesProvider, startMeeting.MeetingId, new List<Tuple<long, decimal?>>(), ConcludeSendEmail.None, closeTodos: false, closeHeadlines: false, connectionId: null);
        }

        if (!String.IsNullOrEmpty(startMeeting.Mode))
        {
          await L10Accessor.SetMode(caller, TermsAccessor.GetTermsCollection(caller, caller.Organization.Id), startMeeting.MeetingId, startMeeting.Mode, true);
        }

        var allMembers = await L10Accessor.GetAttendeeIdsByRecurrenceIdAsync(caller, startMeeting.MeetingId);

        var startMeetingDateTime = startMeeting.MeetingStartTime.FromUnixTimeStamp();
        var meeting = await L10Accessor.StartMeeting(caller, caller, startMeeting.MeetingId, allMembers, preview: false, forceRefresh: false, startMeetingDateTime);

        var output = new StartMeetingMutationOutputDTO(MeetingId: meeting.Id, StartTime: meeting.StartTime.Value);
        await UpdateSetMeetingPage(startMeeting.MeetingId, startMeeting.MeetingStartTime);

        CancellationToken cancellationToken = new CancellationToken();
        string meetinPageId = GetMeetingCurrentPage(meeting.Id, cancellationToken);

        var page = L10Accessor.GetPage(caller, Convert.ToInt32(meetinPageId));

        page.TimeLastStarted = startMeeting.MeetingStartTime;

        await L10Accessor.EditOrCreatePage(caller, page, false);

        return GraphQLResponse<StartMeetingMutationOutputDTO>.Successfully(output);
      }
      catch (Exception ex)
      {
        return GraphQLResponse<StartMeetingMutationOutputDTO>.Error(ex);
      }
    }

    public async Task SetTangentAlert(SetTangentAlertInput input, CancellationToken cancellationToken)
    {
      await L10Accessor.UpdateRecurrence(caller, input.RecurrenceId, input.TangentAlertTimestamp.FromUnixTimeStamp());
    }

    #endregion

    #region BusinessPlanMethods

    public AngularVTO GetLegacyBusinessPlanForMeeting(long meetingId)
    {
      var s = HibernateSession.GetCurrentSession();

      var recurrence = s.Query<L10Recurrence>()
        .FirstOrDefault(x => x.DeleteTime == null && x.Id == meetingId);

      var sharedBPId = s.Query<L10Recurrence>()
          .FirstOrDefault(x => x.DeleteTime == null && x.OrganizationId == caller.Organization.Id && x.ShareVto == true).VtoId;

      if (recurrence != null && recurrence.ShareVto)
      {

        return VtoAccessor.GetVTO(caller, sharedBPId, true, true, true);

      }
      else if (recurrence.TeamType == L10TeamType.LeadershipTeam)
      {

        var meetingMainBP = VtoAccessor.GetVTO(caller, sharedBPId, true, false, false);
        var meetingBP = VtoAccessor.GetVTO(caller, recurrence.VtoId, true, true, true);

        meetingMainBP.QuarterlyRocks = meetingBP.QuarterlyRocks;
        meetingMainBP.Issues = meetingBP.Issues;
        meetingMainBP.IssuesListTitle = meetingBP.IssuesListTitle;
        meetingMainBP.OneYearPlan = meetingBP.OneYearPlan;

        return meetingMainBP;
      }
      else
      {
        return VtoAccessor.GetVTO(caller, recurrence.VtoId, true, true, true);
      }
    }

    #endregion

  }
}
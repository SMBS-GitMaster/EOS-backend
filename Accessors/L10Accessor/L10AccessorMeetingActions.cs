using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Meeting;
using RadialReview.Models.Todo;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using static RadialReview.Utilities.EventUtil;
using RadialReview.Variables;
using RadialReview.Models.Downloads;
using Microsoft.AspNetCore.Html;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Exceptions;
using RadialReview.Utilities.NHibernate;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.ExtendedProperties;
using FluentResults;
using RadialReview.Utilities.Extensions;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Accessors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Utilities.Types;
using DocumentFormat.OpenXml.Drawing;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using NHibernate.Criterion;
using NHibernate.Dialect.Function;
using RadialReview.Core.Repositories;
using Humanizer;
using RadialReview.GraphQL.Models;
using static RadialReview.GraphQL.Models.IssueQueryModel.Associations;

namespace RadialReview.Accessors {
  public partial class L10Accessor : BaseAccessor {

    public static async Task UpdateRating(UserOrganizationModel caller, List<System.Tuple<long, decimal?>> ratingValues, long meetingId, string connectionId) {
      L10Meeting meeting = null;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var now = DateTime.UtcNow;
          //Make sure we're unstarted
          var perms = PermissionsUtility.Create(s, caller);
          meeting = s.QueryOver<L10Meeting>().Where(t => t.Id == meetingId).SingleOrDefault();
          perms.ViewL10Meeting(meeting.Id);
          var ids = ratingValues.Select(x => x.Item1).ToArray();
          //Set rating for attendees
          var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id).List().ToList();
          var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));
          foreach (var a in raters) {
            a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
            s.Update(a);
          }

          await Audit.L10Log(s, caller, meeting.L10RecurrenceId, "UpdateL10Rating", ForModel.Create(meeting));
          await HooksRegistry.Each<IRateMeetingHooks>((ses, x) => x.UpdateRating(s, meeting));
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task UpdateMeetingAttendee(UserOrganizationModel caller, long meetingId, long attendeeId, HotChocolate.Optional<decimal?> rating, string? notesId = null)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        using (var transaction = session.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(session, caller);
          perms.ViewL10Meeting(meetingId);
          var attendee = session.QueryOver<L10Meeting.L10Meeting_Attendee>() // TODO: Should this be a recurrence relation?
            .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == attendeeId)
            .SingleOrDefault();

          if (attendee is null)
            throw new PermissionsException("User is not an attendee.");

          attendee.Rating = !rating.HasValue ? attendee.Rating : rating.Value;
          attendee.PadId = notesId ?? attendee.PadId;
          session.Update(attendee);
          L10Meeting meeting = session.QueryOver<L10Meeting>().Where(t => t.Id == meetingId).SingleOrDefault();
          await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.UpdateRecurrence(session, caller, meeting.L10Recurrence));

          if(rating.HasValue)
          {
            await HooksRegistry.Each<IMeetingRatingHook>((ses, x) => x.FillUserRating(caller, meeting.L10Recurrence, attendeeId, rating));
          }

          //await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.EditAttendee(sess, meetingId, caller, attendee));
          transaction.Commit();
          session.Flush();
        }
      }
    }

    public static async Task<List<L10Meeting.L10Meeting_Attendee>> GetAttendee(UserOrganizationModel caller, long meetingId) {
      L10Meeting meeting = null;
      using (var s = HibernateSession.GetCurrentSession()) {
        PermissionsUtility.Create(s, caller).ViewL10Meeting(meetingId);
        meeting = s.QueryOver<L10Meeting>().Where(t => t.Id == meetingId).SingleOrDefault();
        var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id).List().ToList();

        return attendees;
      }
    }

    public static async Task<double> GetAverageMeeting(UserOrganizationModel caller, long recurrenceId) {
      L10Meeting meeting = null;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

          var now = DateTime.UtcNow;
          meeting = s.QueryOver<L10Meeting>().Where(t => t.L10RecurrenceId == recurrenceId).Take(1).SingleOrDefault();
          if (meeting == null) { return 0; }

          var valueNumerator = meeting.AverageMeetingRating.Numerator;
          var valueDenominator = meeting.AverageMeetingRating.Denominator;

          if (valueNumerator == 0 && valueDenominator == 0)
            return 0;
          var raterMeeting = valueNumerator / valueDenominator;

          return (double)raterMeeting;
        }
      }
    }

    public static Result<List<L10Meeting.L10Meeting_Attendee>> GetUserMeetingRates(UserOrganizationModel caller, long recurrenceId, bool disposeSession = true) {
      var session = HibernateSession.GetCurrentSession();
      if (disposeSession)
      {
        using (session)
        {
          return Execute();
        }
      }
      else
      {
        return Execute();
      }

      Result <List<L10Meeting.L10Meeting_Attendee>> Execute()
      {
        var perms = PermissionsUtility.Create(session, caller).ViewL10RecurrenceWrapper(recurrenceId);
        if (perms.IsFailed)
          return perms.ToResult();

        var meeting = _GetCurrentL10Meeting(session, perms.Value, recurrenceId, true, false, false);
        if (meeting is null)
          return Result.Fail($"No started meeting found for this recurrence: {recurrenceId}");

        return GetMeetingAttendees_Unsafe(meeting.Id, session);
      }
    }

    public static async Task UpdateUserRating(UserOrganizationModel caller,long recurrenceId,decimal? ratingValue,string notes = "") {
      L10Meeting meeting = null;
      var now = DateTime.UtcNow;

      using (var session = HibernateSession.GetCurrentSession()) {
        using (var transaction = session.BeginTransaction()) {

          //Make sure we're unstarted
          var perms = PermissionsUtility.Create(session, caller);
          meeting = _GetCurrentL10Meeting(session, perms, recurrenceId, true, false, false);
          perms.ViewL10Meeting(meeting.Id);

          var ratingValues = new List<Tuple<long, decimal?, string>>()
          {
            new Tuple<long, decimal?, string>(caller.Id, ratingValue, notes)
          };

          var attendees = GetMeetingAttendees_Unsafe(meeting.Id, session);
          var raters = SetConclusionRatings_Unsafe(ratingValues, meeting, session, attendees);

          await Audit.L10Log(session, caller, meeting.L10RecurrenceId, "UpdateL10Rating", ForModel.Create(meeting));
          transaction.Commit();
          session.Flush();
        }
      }
    }

    public static async Task<L10Recurrence> CreateBlankRecurrence(UserOrganizationModel caller, long orgId, bool addCreator, MeetingType meetingType = MeetingType.L10,string name=null, string videoConferenceLink = null, L10TeamType? v3teamType = null, bool createMemberGroup = true, MeetingPermissionsModel memberPermission = null) {
      L10Recurrence recur;

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          recur = await CreateBlankRecurrence(s, perms, orgId, addCreator, meetingType, name, videoConferenceLink, v3teamType, createMemberGroup, memberPermission);
          tx.Commit();
          s.Flush();
        }
      }

      return recur;
    }

    public static bool ArePermissionEqual(List<UserPermissions> userPermissions)
    {
      if (userPermissions == null || !userPermissions.Any())
        return false;

      var firstPermission = userPermissions.First().permissions;

      return userPermissions.All(up => up.permissions.Equals(firstPermission));
    }

    public static async Task<bool> CreateL10PermsByUser(UserOrganizationModel caller, long recId, List<UserPermissions> userPermissions)
    {
      bool userPermsCreated;

      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          userPermsCreated = await CreateL10PermsByUser(s, perms, recId, userPermissions);
          tx.Commit();
          s.Flush();
        }
      }

      return userPermsCreated;
    }

    public static async Task<bool> CreateL10PermsByUser(ISession s, PermissionsUtility perms, long recId, List<UserPermissions> userPermissions)
    {
      if (userPermissions == null) return false;

      var caller = perms.GetCaller();

      foreach (var userPermission in userPermissions)
      {
        if (!caller.IsRadialAdmin)
        {
          var permissions = userPermission.permissions;
          s.Save(new PermItem() { CanAdmin = permissions.Admin, CanEdit = permissions.Edit, CanView = permissions.View, AccessorType = PermItem.AccessType.RGM, AccessorId = userPermission.Id, ResType = PermItem.ResourceType.L10Recurrence, ResId = recId, CreatorId = caller.Id, OrganizationId = caller.Organization.Id, IsArchtype = false, });
        }
      }

      return true;
    }

    public static async Task<L10Recurrence> CreateBlankRecurrence(ISession s, PermissionsUtility perms, long orgId, bool addCreator, MeetingType meetingType = MeetingType.L10, string name = null, string videoConferenceLink = null, L10TeamType? v3teamType = null, bool createMemberGroup = true, MeetingPermissionsModel memberPermission = null) {
      var defaultGroupPerms = memberPermission == null ?
        new MeetingPermissionsModel() { Admin = true, Edit = true, View = true } :
        memberPermission;

      L10Recurrence recur;
      var caller = perms.GetCaller();
      perms.CreateL10Recurrence(orgId);
      var teamType = L10TeamType.LeadershipTeam;
      var shareVto = true;
      var anyLT = s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.TeamType == L10TeamType.LeadershipTeam).Take(1).RowCount();

      if(v3teamType.HasValue)
      {
        teamType = v3teamType.Value;
        shareVto = false;
      }
      else if (anyLT > 0) {
        teamType = L10TeamType.DepartmentalTeam;
        shareVto = false;
      }

      recur = new L10Recurrence() {
        OrganizationId = orgId,
        Pristine = name == null,
        VideoId = Guid.NewGuid().ToString(),
        EnableTranscription = false,
        HeadlinesId = Guid.NewGuid().ToString(),
        CountDown = true,
        CreatedById = caller.Id,
        CreateTime = DateTime.UtcNow,
        TeamType = teamType,
        Name = name, //Added defaults:
        RockType = orgId.NotEOSW(Models.Enums.L10RockType.Milestones, Models.Enums.L10RockType.Original),
        ReverseScorecard = true,
        ShareVto = shareVto,
        CurrentWeekHighlightShift = -1,
        VideoConferenceLink = videoConferenceLink,
      };
      if (meetingType == MeetingType.SamePage) {
        recur.TeamType = L10TeamType.SamePageMeeting;
      }

      var terms = TermsAccessor.GetTermsCollection(s, perms, orgId);

      s.Save(recur);
      foreach (var page in GenerateMeetingPages(recur.Id, meetingType, recur.CreateTime, terms)) {
        s.Save(page);
      }

      var vto = VtoAccessor.CreateRecurrenceVTO(s, perms, recur.Id);
      // add default values in VTO 3-Year Picture
      string defaultEditString = null; //DisplayNameStrings.defaultEditMessage;
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_ThreeYearPicture, null, key: "Revenue:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_ThreeYearPicture, null, key: "Profit:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_ThreeYearPicture, null, key: "Measurables:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_OneYearPlan, null, key: "Revenue:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_OneYearPlan, null, key: "Profit:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_OneYearPlan, null, key: "Measurables:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_QuarterlyRocks, null, key: "Revenue:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_QuarterlyRocks, null, key: "Profit:", value: defaultEditString);
      await VtoAccessor.AddKV(s, perms, vto.Id, VtoItemType.Header_QuarterlyRocks, null, key: "Measurables:", value: defaultEditString);
      s.Save(new PermItem() { CanAdmin = true, CanEdit = true, CanView = true, AccessorType = PermItem.AccessType.Creator, AccessorId = caller.Id, ResType = PermItem.ResourceType.L10Recurrence, ResId = recur.Id, CreatorId = caller.Id, OrganizationId = caller.Organization.Id, IsArchtype = false, });

      // If the attendees don't have the same permissions, the member group won't be created, this verification is in the createMeeting mutation in v3.
      if (createMemberGroup)
        s.Save(new PermItem() { CanAdmin = defaultGroupPerms.Admin, CanEdit = defaultGroupPerms.Edit, CanView = defaultGroupPerms.View, AccessorType = PermItem.AccessType.Members, AccessorId = -1, ResType = PermItem.ResourceType.L10Recurrence, ResId = recur.Id, CreatorId = caller.Id, OrganizationId = caller.Organization.Id, IsArchtype = false, });
      s.Save(new PermItem() { CanAdmin = true, CanEdit = true, CanView = true, AccessorId = -1, AccessorType = PermItem.AccessType.Admins, ResType = PermItem.ResourceType.L10Recurrence, ResId = recur.Id, CreatorId = caller.Id, OrganizationId = caller.Organization.Id, IsArchtype = false, });
      await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.CreateRecurrence(ses, recur));
      if (addCreator) {
        await using (var rt = RealTimeUtility.Create()) {
          await AddAttendee(s, perms, rt, recur.Id, caller.Id);
        }
      }

      if (name != null) {
        await Depristine_Unsafe(s, caller, recur);
      }

      return recur;
    }

    public static async Task<L10Meeting> StartMeeting(UserOrganizationModel caller, UserOrganizationModel meetingLeader,
                              long recurrenceId, List<long> attendees,
                              bool preview, bool forceRefresh,
                              DateTime? startTime = null
      ) {
      L10Recurrence recurrence;
      L10Meeting meeting;
      await using (var rt = RealTimeUtility.Create()) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var callerPerms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
            if (caller.Id != meetingLeader.Id) {
              PermissionsUtility.Create(s, meetingLeader).ViewL10Recurrence(recurrenceId);
            }

            lock ("Recurrence_" + recurrenceId) {
              //Make sure we're unstarted
              try {
                var perms = PermissionsUtility.Create(s, caller);
                _GetCurrentL10Meeting(s, perms, recurrenceId, false);
                throw new MeetingException(recurrenceId, "Meeting has already started.", MeetingExceptionType.AlreadyStarted);
              } catch (MeetingException e) {
                if (e.MeetingExceptionType != MeetingExceptionType.Unstarted) {
                  throw;
                }
              }

              var now = DateTime.UtcNow;
              recurrence = s.Get<L10Recurrence>(recurrenceId);
              var currentDiff = 0L;
              if (recurrence.WhiteboardId != null) {
                try {
                  currentDiff = WhiteboardAccessor.GetCurrentDiff(s, callerPerms, recurrence.WhiteboardId).DiffId;
                } catch (Exception) {
                  //welp
                }
              }

              meeting = new L10Meeting {
                CreateTime = now,
                StartTime = startTime ?? now,
                L10RecurrenceId = recurrenceId,
                L10Recurrence = recurrence,
                OrganizationId = recurrence.OrganizationId,
                MeetingLeader = meetingLeader,
                MeetingLeaderId = meetingLeader.Id,
                Preview = preview,
                IsRecording = recurrence.CanRecordAudio() && recurrence.RecordAudio == AudioRecording.EnableRecording,
                HasRecording = recurrence.CanRecordAudio() && recurrence.RecordAudio == AudioRecording.EnableRecording,
                BeginWhiteboardDiff = currentDiff,
                CurrentWhiteboardDiff = currentDiff,
              };
              s.Save(meeting);
              tx.Commit();
              s.Flush();
            }
          }
        }

        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            recurrence.MeetingInProgress = meeting.Id;
            s.Update(recurrence);
            _LoadRecurrences(s, LoadMeeting.True(), recurrence);
            foreach (var m in recurrence._DefaultMeasurables) {
              if (m.Id > 0) {
                var mm = new L10Meeting.L10Meeting_Measurable() {
                  L10Meeting = meeting,
                  Measurable = m.Measurable,
                  _Ordering = m._Ordering,
                  IsDivider = m.IsDivider
                };
                s.Save(mm);
                meeting._MeetingMeasurables.Add(mm);
              }
            }

            foreach (var m in attendees) {
              var mm = new L10Meeting.L10Meeting_Attendee() {
                L10Meeting = meeting,
                User = s.Load<UserOrganizationModel>(m),
              };
              s.Save(mm);
              meeting._MeetingAttendees.Add(mm);
            }

            foreach (var r in recurrence._DefaultRocks) {
              var state = RockState.Indeterminate;
              state = r.ForRock.Completion;
              var mm = new L10Meeting.L10Meeting_Rock() {
                ForRecurrence = recurrence,
                L10Meeting = meeting,
                ForRock = r.ForRock,
                Completion = state,
                VtoRock = r.VtoRock,
              };
              s.Save(mm);
              meeting._MeetingRocks.Add(mm);
            }

            var perms2 = PermissionsUtility.Create(s, caller);
            var todos = GetTodosForRecurrence(s, perms2, recurrence.Id, meeting.Id);
            var i = 0;
            foreach (var t in todos.OrderBy(x => x.AccountableUser.NotNull(y => y.GetName()) ?? ("" + x.AccountableUserId)).ThenBy(x => x.Message)) {
              t.Ordering = i;
              s.Update(t);
              i += 1;
            }

            var recAttendees = await L10Accessor.GetRecurrenceAttendees(caller, recurrenceId);
            await L10Accessor.SetIsUsingV3ForAttendess(caller, recAttendees, true);

            await Audit.L10Log(s, caller, recurrenceId, "StartMeeting", ForModel.Create(meeting));
            //Hooks done below..
            tx.Commit();
            s.Flush();
            if (!forceRefresh) {
              rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(meeting)).Call("setupMeeting", meeting.CreateTime.ToJavascriptMilliseconds(), meetingLeader.Id, meeting.Id, preview);
            }
          }
        }

        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.StartMeeting(ses, recurrence, meeting));
            if (recurrence.TeamType == L10TeamType.LeadershipTeam) {
              await Trigger(x => x.Create(s, EventType.StartLeadershipMeeting, caller, recurrence, message: recurrence.Name));
            }

            if (recurrence.TeamType == L10TeamType.DepartmentalTeam) {
              await Trigger(x => x.Create(s, EventType.StartDepartmentMeeting, caller, recurrence, message: recurrence.Name));
            }

            tx.Commit();
            s.Flush();
          }
        }

        if (forceRefresh) {
          rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(meeting)).Call("forceRefreshImmediate");
        }

        return meeting;
      }
    }

    public static async Task<long?> GetMeetingLeader(UserOrganizationModel caller, long recurrenceId, bool isInstanceId = false) {
      L10Meeting meeting;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          L10Meeting getCurrentMeeting;

          if(!isInstanceId) {
            var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
            perms.ViewL10Recurrence(recurrenceId);
            getCurrentMeeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
          } else {
            var perms = PermissionsUtility.Create(s, caller).ViewL10Meeting(recurrenceId);
            perms.ViewL10Meeting(recurrenceId);
            getCurrentMeeting = _GetCurrentL10MeetingInstance(s, perms, recurrenceId);
          }

          if(getCurrentMeeting == null)
            return null;

          meeting = s.Get<L10Meeting>(getCurrentMeeting.Id);

          return meeting.MeetingLeaderId;
        }
      }
    }

    public static bool ValidateLeaderPermission(UserOrganizationModel caller, long instanceId, long? leaderId = 0)
    {
      if(leaderId == 0) throw new PermissionsException("Please provide a valid leader ID"); ;

      using (var s = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(s, caller);
        var recurrenceId = s.Get<L10Meeting>(instanceId).L10RecurrenceId;

        if(caller.Id != leaderId)
        {
          // Validate if the caller has admin permissions. Only an admin can convert another user to a leader
          perms.AdminL10Recurrence(recurrenceId);
        }

        perms.ViewL10Meeting(instanceId);
        return true;
      }
    }

    public static async Task SetMeetingLeader(UserOrganizationModel caller, long meetingLeaderId, long recurrenceId, bool isInstanceId = false) {
      L10Meeting meeting;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          if(!isInstanceId) {
            var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
            perms.ViewUserOrganization(meetingLeaderId, true);

            var meetingLeaderResolved = s.Get<UserOrganizationModel>(meetingLeaderId);
            var meetingLeaderPerms = PermissionsUtility.Create(s, meetingLeaderResolved);
            meetingLeaderPerms.ViewL10Recurrence(recurrenceId);

            var getCurrentMeeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
            meeting = s.Get<L10Meeting>(getCurrentMeeting.Id);

          } else {
            var perms = PermissionsUtility.Create(s, caller).ViewL10Meeting(recurrenceId);
            perms.ViewUserOrganization(meetingLeaderId, true);

            var meetingLeaderResolved = s.Get<UserOrganizationModel>(meetingLeaderId);
            var meetingLeaderPerms = PermissionsUtility.Create(s, meetingLeaderResolved);
            meetingLeaderPerms.ViewL10Meeting(recurrenceId);

            meeting = _GetCurrentL10MeetingInstance(s, perms, recurrenceId);
          }

          var pageId = L10Accessor.GetCurrentL10MeetingLeaderPage(caller, meeting.Id);
          var _ = await UpdatePage(caller, meetingLeaderId, meeting.L10RecurrenceId, pageId, null);

          meeting.MeetingLeaderId = meetingLeaderId;

          s.Update(meeting);

          tx.Commit();
          s.Flush();

          await using (var rt = RealTimeUtility.Create()) {
            rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(meeting)).Call("setupMeeting", meeting.CreateTime.ToJavascriptMilliseconds(), meeting.MeetingLeaderId);
          }

          await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateCurrentMeetingInstance(sess, caller, meeting.L10Recurrence));
        }
      }
    }

    public static async Task IssueVotingHasEnded(UserOrganizationModel caller, long recurrenceId, bool issueVotingHasEnded = false, bool isInstanceId = false)
    {
      L10Meeting meeting;
      using (var session = HibernateSession.GetCurrentSession())
      {
        using (var transaction = session.BeginTransaction())
        {

          if(!isInstanceId)
          {
            var perms = PermissionsUtility.Create(session, caller).ViewL10Recurrence(recurrenceId);
            meeting = _GetCurrentL10Meeting(session, perms, recurrenceId, false);

          }
          else
          {
            var perms = PermissionsUtility.Create(session, caller).ViewL10Meeting(recurrenceId);
            meeting = _GetCurrentL10MeetingInstance(session, perms, recurrenceId);
          }
          meeting.IssueVotingHasEnded = issueVotingHasEnded;

          session.Update(meeting);
          transaction.Commit();
          session.Flush();
          if(transaction.WasCommitted)
          {
            await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateCurrentMeetingInstance(sess, caller, meeting.L10Recurrence));

            //await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateRecurrence(sess, caller, meeting.L10Recurrence));

            //var m = L10Accessor.GetCurrentL10Meeting(caller, recurrenceId);
            //var mi = MeetingInstanceTransformer.MeetingInstanceFromL10Meeting(m, recurrenceId);
            //await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Updated(mi.Id, mi, targets)).ConfigureAwait(false);

          }
        }
      }
    }

    public static async Task RestartMeeting(UserOrganizationModel caller, INotesProvider notesProvider, long timeRestarted, long meetingId) {
      var concludeMeeting = new ConcludeMeetingModel() {
        EndTime = timeRestarted,
        MeetingId = meetingId
      };
      await ConcludeMeeting(caller, notesProvider, concludeMeeting);

      var getMembers = await GetAttendees(caller, meetingId);
      var allMembers = getMembers.Select(x => x.Id).ToList();
      var startMeetingDateTime = Timestamp.ToDateTime(timeRestarted);
      var meeting = await StartMeeting(caller, caller, meetingId, allMembers, preview: false, forceRefresh: true, startMeetingDateTime);

      var tempRecur = GetL10Recurrence(caller, meetingId, LoadMeeting.True());
      var page = GetDefaultStartPage(tempRecur);
      page = page.NotNull(x => x.ToLower());

      await L10Accessor.UpdatePage(caller, caller.Id, meetingId, page, "", startMeetingDateTime);
    }

    public static async Task ConcludeMeeting(UserOrganizationModel caller, INotesProvider notesProvider, ConcludeMeetingModel concludeMeeting) {
      using (var databaseSession = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(databaseSession, caller);
        L10Meeting meeting = _GetCurrentL10Meeting(databaseSession, perms, concludeMeeting.MeetingId, false);
        perms.ViewL10Meeting(meeting.Id);

        var endTime = Timestamp.ToDateTime(concludeMeeting.EndTime);

        if (concludeMeeting.ArchiveCompletedTodos)
          ArchiveTodos(concludeMeeting, meeting, endTime, databaseSession, perms);

        if (concludeMeeting.ArchiveHeadlines)
          ArchiveHeadlines(concludeMeeting, endTime, databaseSession, perms);

      }

      var meetingRatings = GetUserMeetingRates(caller, concludeMeeting.MeetingId);

      if (meetingRatings.IsFailed)
        throw new Exception("Not able to retrieve the user meeting rates");

      var ratingValues = meetingRatings.Value.Select(x => new Tuple<long, decimal?>(x.User.Id, x.Rating)).ToList();

      Models.L10.VM.ConcludeSendEmail sendTo = Models.L10.VM.ConcludeSendEmail.None;
      Enum.TryParse(concludeMeeting.SendEmailSummaryTo, out sendTo);

      await ConcludeMeeting(caller, notesProvider, concludeMeeting.MeetingId, ratingValues,
                                                   sendTo, closeTodos: true,
                                                   closeHeadlines: true, connectionId: null);
    }

    private static void ArchiveHeadlines(ConcludeMeetingModel concludeMeeting, DateTime endTime, ISession databaseSession, PermissionsUtility perms) {
      var headlines = GetHeadlinesForMeeting(databaseSession, perms, concludeMeeting.MeetingId);

      foreach (var headline in headlines) {
        if (headline.CloseTime is not null)
          continue;

        headline.DeleteTime = endTime;
        databaseSession.Update(headline);
      }
    }

    private static void ArchiveTodos(ConcludeMeetingModel concludeMeeting, L10Meeting meeting, DateTime endTime, ISession databaseSession, PermissionsUtility perms) {
      var todos = GetTodosForRecurrence(databaseSession, perms, concludeMeeting.MeetingId, meeting.Id);

      foreach (var todo in todos) {
        if (todo.CloseTime is not null)
          continue;

        todo.DeleteTime = endTime;
        databaseSession.Update(todo);
      }
    }

    public static async Task ConcludeMeeting(UserOrganizationModel caller, INotesProvider notesProvider, long recurrenceId, List<Tuple<long, decimal?>> ratingValues, ConcludeSendEmail sendEmail, bool closeTodos, bool closeHeadlines,
        string connectionId, bool isNewEmailFormat = true, bool revertIssues = false, List<string> meetingNotesIds = null, bool isUsingV3 = false) {
      L10Recurrence recurrence = null;
      L10Meeting meeting = null;
      var sendToExternal = false;
      long? whiteboardFileId = null;
      try {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var now = DateTime.UtcNow;
            //Make sure we're unstarted
            var perms = PermissionsUtility.Create(s, caller);

            meeting = _GetCurrentL10Meeting(s, perms, recurrenceId, false);
            perms.ViewL10Meeting(meeting.Id);

            // Todo updates
            var todoRatio = new Ratio();
            var todos = GetTodosForRecurrence(s, perms, recurrenceId, meeting.Id);
            foreach (var todo in todos) {
              if (todo.CreateTime < meeting.StartTime) {
                if (todo.CompleteTime != null) {
                  todo.CompleteDuringMeetingId = meeting.Id;
                  if (closeTodos) {
                    todo.CloseTime = now;
                  }
                  s.Update(todo);
                }
                todoRatio.Add(todo.CompleteTime != null ? 1 : 0, 1);
              }
            }

            // Headline updates
            var headlines = GetHeadlinesForMeeting(s, perms, recurrenceId);
            if (closeHeadlines) {
              CloseHeadlines_Unsafe(meeting.Id, s, now, headlines);
            }

            // Wrap-up (Conclude) the forum
            recurrence = s.Get<L10Recurrence>(recurrenceId);
            if (recurrence.WhiteboardId != null) {
              try {
                meeting.CurrentWhiteboardDiff = WhiteboardAccessor.GetCurrentDiff(s, perms, recurrence.WhiteboardId).DiffId;
              } catch (Exception) {
                //welp
              }
            }

            // Reset the fields used in v3 for the timer on meeting pages, from both v1 and v3
            _LoadRecurrences(s, new LoadMeeting() { LoadPages = true }, recurrence);
            foreach (var page in recurrence._Pages)
            {
              page.TimeLastStarted = 0;
              page.TimePreviouslySpentS = 0;

              await L10Accessor.EditOrCreatePage(caller, page, false, concludeMeeting: true);
            }

            await SendConclusionTextMessages_Unsafe(recurrenceId, recurrence, s, now);
            CloseIssuesOnConclusion_Unsafe(recurrenceId, meeting, s, now);
            meeting.TodoCompletion = todoRatio;
            meeting.CompleteTime = now;
            meeting.SendConcludeEmailTo = sendEmail;
            s.Update(meeting);
            var attendees = GetMeetingAttendees_Email(meeting.Id, recurrenceId, s);

            var updateRatingValues = new List<Tuple<long, decimal?, string>>();

            foreach (var rating in ratingValues) {
              updateRatingValues.Add(new Tuple<long, decimal?, string>(rating.Item1, rating.Item2, ""));
            }

            var raters = await SetConclusionRatings_Unsafe(updateRatingValues, meeting, s, attendees);
            CloseLogsOnConclusion_Unsafe(meeting, s, now);
            //Close all sub issues
            IssueModel issueAlias = null;
            var issue_recurParents = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.CloseTime >= meeting.StartTime && x.CloseTime <= meeting.CompleteTime && x.Recurrence.Id == recurrenceId).List().ToList();
            _RecursiveCloseIssues(s, issue_recurParents.Select(x => x.Id).ToList(), now);
            recurrence.MeetingInProgress = null;
            recurrence.SelectedVideoProvider = null;
            s.Update(recurrence);
            if (meeting != null && recurrence != null && recurrence.WhiteboardId != null && meeting.BeginWhiteboardDiff != meeting.CurrentWhiteboardDiff) {
              //whiteboard was updated..
              whiteboardFileId = FileAccessor.SaveGeneratedFilePlaceholder_Unsafe(s, caller.Id, DateTime.UtcNow.ToString("yyyyMMdd") + "_whiteboard", "png", "Whiteboard for meeting on " + DateTime.UtcNow.ToString("MMM dd YYYY"), FileOrigin.AutoGenerated, FileOutputMethod.Save, ForModel.Create(recurrence), new[] { PermTiny.InheritedFromL10Recurrence(recurrenceId) }, TagModel.Create(TagModel.Constants.WHITEBOARD, ForModel.Create(recurrence)), TagModel.Create(TagModel.Constants.WHITEBOARD, ForModel.Create(meeting)));
            }

            var sendEmailTo = new List<L10Meeting.L10Meeting_Attendee>();
            //send emails
            if (sendEmail != ConcludeSendEmail.None) {
              switch (sendEmail) {
                case ConcludeSendEmail.AllAttendees:
                  sendEmailTo = attendees;
                  sendToExternal = true;
                  break;
                case ConcludeSendEmail.AllRaters:
                  sendEmailTo = raters.ToList();
                  sendToExternal = true;
                  break;
                default:
                  break;
              }
            }

            var pageSummary = await GetPageSummaryNotes(recurrenceId, s, notesProvider);
            ConclusionItems.Save_Unsafe(recurrenceId, meeting.Id, s, todos.Where(x => x.CloseTime == null).ToList(), headlines, issue_recurParents, pageSummary, sendEmailTo, whiteboardFileId);
            await Trigger(x => x.Create(s, EventType.ConcludeMeeting, caller, recurrence, message: recurrence.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));
            await Audit.L10Log(s, caller, recurrenceId, "ConcludeMeeting", ForModel.Create(meeting));
            tx.Commit();
            s.Flush();
          }
        }

        if (whiteboardFileId != null) {
          Scheduler.Enqueue(() => WhiteboardAccessor.SaveWhiteboard_Hangfire(caller.Id, whiteboardFileId.Value, recurrence.WhiteboardId, default(IHtmlRenderService), default(IBlobStorageProvider)));
        }

        //revert recurrence
        if (recurrence != null && recurrence.MeetingMode != null) {
          await L10Accessor.RevertMode(caller, recurrence.Id, revertIssues);
        }

        if (meeting != null) {
          await using (var rt = RealTimeUtility.Create(connectionId)) {
            rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(meeting)).Call("concludeMeeting");
          }
        }

        if (!meeting.Preview) {
          Scheduler.Enqueue(() => SendConclusionEmail_Unsafe(meeting.Id, null, sendToExternal, default(INotesProvider), isUsingV3, meetingNotesIds));
        }

        if (meetingNotesIds?.Any() == true)
        {
          await L10Accessor.CreateL10MeetingNotes(caller, meeting.Id, meetingNotesIds);
        }

        // Set the `isUsingV3` field to `true` for each attendee.
        var recurrenceAttendees = await L10Accessor.GetRecurrenceAttendees(caller, recurrenceId);
        await L10Accessor.SetIsUsingV3ForAttendess(caller, recurrenceAttendees, true);

        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
             await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.ConcludeMeeting(ses, caller, recurrence, meeting));
            tx.Commit();
            s.Flush();
          }
        }
      } catch (Exception e) {
        int a = 0;
      }
    }

    public static async Task<List<Tuple<long, decimal?>>> GetMeetingAttendeeRatings(UserOrganizationModel caller, long meetingId, bool checkPermissions = false)
    {
      using var session = HibernateSession.GetCurrentSession();


      if (checkPermissions)
      {
        var perms = PermissionsUtility.Create(session, caller);
        perms.ViewL10Meeting(meetingId);
      }

      var ratingValues = new List<Tuple<long, decimal?>>();

      var meetingAttendee = session.QueryOver<L10Meeting.L10Meeting_Attendee>()
            .Where(x => x.L10Meeting.Id == meetingId).List().ToList();

      foreach (var attendee in meetingAttendee)
      {
        if (attendee.Rating != null)
        {
          ratingValues.Add(Tuple.Create(attendee.User.Id, attendee.Rating));
        }
      }

      return ratingValues;
    }
    private static async Task<List<PageSummaryNotes>> GetPageSummaryNotes(long recurrenceId, ISession s, INotesProvider notesProvider) {
      var orderedList = new List<PageSummaryNotes>();
      try {
        var pages = s.QueryOver<L10Recurrence.L10Recurrence_Page>().Where(x => x._SummaryJson != null && x.L10RecurrenceId == recurrenceId && x.DeleteTime == null).List().OrderBy(x => x._Ordering).ToList();
        foreach (var page in pages) {
          try {
            var summary = page.GetSummary();
            orderedList.AddRange(summary.SummaryNotes.Select(x => new PageSummaryNotes() { Title = x.Title, PadId = x.PadId }));
          } catch (Exception e) {
          }
        }

        var lookup = await notesProvider.GetTextForPads(orderedList.Select(x => x.PadId).ToList());
        foreach (var o in orderedList) {
          o.Contents = lookup.GetOrDefault(o.PadId, "");
        }
      } catch (Exception e) {
      }

      return orderedList;
    }

    public static IEnumerable<L10Recurrence.L10Recurrence_Connection> GetConnected(UserOrganizationModel caller, long recurrenceId, bool load = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
          var connections = s.QueryOver<L10Recurrence.L10Recurrence_Connection>().Where(x => x.DeleteTime >= DateTime.UtcNow && x.RecurrenceId == recurrenceId).List().ToList();
          if (load) {
            var userIds = connections.Select(x => x.UserId).Distinct().ToArray();
            var tiny = TinyUserAccessor.GetUsers_Unsafe(s, userIds).ToDefaultDictionary(x => x.UserOrgId, x => x, null);
            foreach (var c in connections) {
              c._User = tiny[c.UserId];
            }
          }

          return connections;
        }
      }
    }

    public static async Task<L10Meeting.L10Meeting_Connection> JoinL10Meeting(IOuterSession s, PermissionsUtility perms, long callerId, long recurrenceId, string connectionId) {
      perms.Self(callerId);
      var safe_caller = s.Get<UserOrganizationModel>(callerId);

      await using (var rt = RealTimeUtility.Create()) {
        //var perms = PermissionsUtility.
        if (recurrenceId == -3) {
          var recurs = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.User.Id).IsIn(safe_caller.UserIds).Select(x => x.L10Recurrence.Id).List<long>().ToList();
          //Hey.. this doesnt grab all visible meetings.. it should be adjusted when we know that GetVisibleL10Meetings_Tiny is optimized
          //GetVisibleL10Meetings_Tiny(s, perms, caller.Id);
          foreach (var r in recurs) {
            await rt.AddToGroup(connectionId, RealTimeHub.Keys.GenerateMeetingGroupId(r));
          }

          await rt.AddToGroup(connectionId, RealTimeHub.Keys.UserId(safe_caller.Id));
        } else {
          perms.ViewL10Recurrence(recurrenceId);
          await rt.AddToGroup(connectionId, RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));
          await Audit.L10Log(s, safe_caller, recurrenceId, "JoinL10Meeting", ForModel.Create(safe_caller));
#pragma warning disable CS0618 // Type or member is obsolete

          var connection = new L10Recurrence.L10Recurrence_Connection() { Id = connectionId, RecurrenceId = recurrenceId, UserId = safe_caller.Id };
#pragma warning restore CS0618 // Type or member is obsolete

          s.SaveOrUpdate(connection);
          connection._User = TinyUser.FromUserOrganization(safe_caller);
          var currentMeeting = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
          if (currentMeeting != null) {
            var isAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.L10Meeting.Id == currentMeeting.Id && x.User.Id == safe_caller.Id && x.DeleteTime == null).RowCount() > 0;
            if (!isAttendee) {
              var potentialAttendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == safe_caller.Id && x.L10Recurrence.Id == recurrenceId).RowCount() > 0;
              if (potentialAttendee) {
                s.Save(new L10Meeting.L10Meeting_Attendee() { L10Meeting = currentMeeting, User = safe_caller, });
              }
            }
          }

          var meetingHub = rt.UpdateRecurrences(recurrenceId);
          meetingHub.Call("userEnterMeeting", connection);
        }

        return null;
      }
    }

    public class PageSummaryNotes {
      public string Title { get; set; }

      public string Contents { get; set; }

      public string PadId { get; set; }
    }

    public class MeetingStats
    {
      public MeetingStats(double? lastYearAvg, double? lastweekTodo, int? lastQuarterIssues, double? averageDuration)
      {
        LastYearAvg = lastYearAvg ?? 0.0;
        LastweekTodo = lastweekTodo ?? 0.0;
        LastQuarterIssues = lastQuarterIssues ?? 0;
        AverageDuration = averageDuration ?? 0;
      }
      public double LastYearAvg { get; set; }
      public double LastweekTodo { get; set; }
      public double AverageDuration { get; set; }
      public int LastQuarterIssues { get; set; }
    }

    public class ConclusionItems {
      public List<IssueModel.IssueModel_Recurrence> ClosedIssues { get; set; }

      public List<TodoModel> OutstandingTodos { get; set; }

      public List<PeopleHeadline> MeetingHeadlines { get; set; }

      public List<L10Meeting.L10Meeting_Attendee> SendEmailsToAttendees { get; set; }

      public List<RadialReview.Models.UserOrganizationModel> AbsentAttendees { get; set; }

      public List<MeetingSummaryWhoModel> SendEmailsToSubscribers { get; set; }

      public List<PageSummaryNotes> PageSummaryNotes { get; set; }

      public MeetingStats MeetingStatus { get; set; }

      public long MeetingId { get; private set; }

      public long? WhiteboardFileId { get; set; }

      public static void Save_Unsafe(long recurrenceId, long meetingId, ISession s, List<TodoModel> todos, List<PeopleHeadline> headlines, List<IssueModel.IssueModel_Recurrence> issue_recurParents, List<PageSummaryNotes> pageSummaryNotes, List<L10Meeting.L10Meeting_Attendee> sendEmailTo, long? whiteboardFileId) {
        //Emails
        foreach (var emailed in sendEmailTo) {
          s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(emailed), L10Meeting.ConclusionDataType.SendEmailSummaryTo));
        }

        //Closed Issues
        foreach (var issue in issue_recurParents) {
          s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(issue), L10Meeting.ConclusionDataType.CompletedIssue));
        }

        //All todos
        foreach (var todo in todos) {
          s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(todo), L10Meeting.ConclusionDataType.OutstandingTodo));
        }

        //All headlines
        foreach (var headline in headlines) {
          s.Save(new L10Meeting.L10Meeting_ConclusionData(recurrenceId, meetingId, ForModel.Create(headline), L10Meeting.ConclusionDataType.MeetingHeadline));
        }

        //All Summaries
        if (pageSummaryNotes != null) {
          foreach (var summary in pageSummaryNotes) {
            s.Save(L10Meeting.L10Meeting_ConclusionData.Create(recurrenceId, meetingId, L10Meeting.ConclusionDataType.Notes, summary));
          }
        }

        if (whiteboardFileId != null) {
          s.Save(L10Meeting.L10Meeting_ConclusionData.Create(recurrenceId, meetingId, L10Meeting.ConclusionDataType.WhiteboardFileId, whiteboardFileId));
        }
      }


      public static MeetingStats Get_MeetingStats(ISession s, long recurrenceId)
      {
        DateTime currentDate = DateTime.Now.Date;
        DateTime weekAgo = currentDate.AddDays(-7).Date;

        DateTime threeMonthsAgo = currentDate.StartOfQuarter();
        DateTime nextDay = currentDate.AddDays(1);

        DateTime currentYear = currentDate.AddMonths(-(DateTime.Now.Date.Month));

        var currentYearAvg = s.QueryOver<L10Meeting>()
            .Where(x => x.L10RecurrenceId == recurrenceId &&
                        x.CompleteTime < nextDay &&
                        x.CompleteTime >= currentYear)
            .Select(Projections.SqlProjection("SUM(AvgRating_Num / AvgRating_Den) / COUNT(*) as currentYearAvg",
                                  new[] { "currentYearAvg" },
                                  new[] { NHibernateUtil.Double }))
            .SingleOrDefault<double>();

        var lastweekTodo = s.QueryOver<L10Meeting>().OrderBy(x => x.Id).Desc
            .Where(x => x.L10RecurrenceId == recurrenceId)
            .Select(Projections.SqlProjection("(TodoCompletion_Num / TodoCompletion_Den) as lastweekTodo",
                                  new[] { "lastweekTodo" },
                                  new[] { NHibernateUtil.Double })).Skip(1).Take(1)
            .SingleOrDefault<double>();

        var lastQuarterIssues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .Where(x => x.Recurrence.Id == recurrenceId &&
                        x.CloseTime != null &&
                        x.CloseTime < nextDay &&
                        x.CloseTime >= threeMonthsAgo)
            .Select(Projections.RowCount())
            .SingleOrDefault<int>();

        var lastMeetingDuration = s.QueryOver<L10Meeting>().OrderBy(x => x.Id).Desc
            .Where(x => x.L10RecurrenceId == recurrenceId)
            .Select(
                Projections.SqlFunction(
                    new SQLFunctionTemplate(NHibernateUtil.Double, "TIMESTAMPDIFF(MINUTE, ?1, ?2)"),
                    NHibernateUtil.Double,
                    Projections.Property<L10Meeting>(x => x.StartTime),
                    Projections.Property<L10Meeting>(x => x.CompleteTime))).Skip(1).Take(1)
            .SingleOrDefault<double?>();

        return new MeetingStats(currentYearAvg, lastweekTodo, lastQuarterIssues, lastMeetingDuration);
      }

      public static ConclusionItems Get_Unsafe(ISession s, long meetingId, long recurrenceId) {
        var meetingItems = s.QueryOver<L10Meeting.L10Meeting_ConclusionData>().Where(x => x.DeleteTime == null && x.L10MeetingId == meetingId).List().ToList();
        var issueIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.CompletedIssue).Select(x => x.ForModel.ModelId).ToArray();
        var headlineIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.MeetingHeadline).Select(x => x.ForModel.ModelId).ToArray();
        var todoIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.OutstandingTodo).Select(x => x.ForModel.ModelId).ToArray();
        var attendeeIds = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.SendEmailSummaryTo).Select(x => x.ForModel.ModelId).ToArray();
        var issueQ = s.QueryOver<IssueModel.IssueModel_Recurrence>().WhereRestrictionOn(x => x.Id).IsIn(issueIds).Future();
        var headlineQ = s.QueryOver<PeopleHeadline>().WhereRestrictionOn(x => x.Id).IsIn(headlineIds).Future();
        var todoQ = s.QueryOver<TodoModel>().WhereRestrictionOn(x => x.Id).IsIn(todoIds).Future();
        var attendeeQ = s.QueryOver<L10Meeting.L10Meeting_Attendee>().WhereRestrictionOn(x => x.Id).IsIn(attendeeIds).Future();
        var meetingSubscribersQ = s.QueryOver<MeetingSummaryWhoModel>().Where(x => x.RecurrenceId == recurrenceId && x.DeleteTime == null).Future();
        var notes = meetingItems.Where(x => x.Type == L10Meeting.ConclusionDataType.Notes).Select(x => x.GetValue<PageSummaryNotes>()).ToList();
        var whiteboardFileId = meetingItems.SingleOrDefault(x => x.Type == L10Meeting.ConclusionDataType.WhiteboardFileId).NotNull(x => x.GetValue<long?>());
        var usersRecur = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
              .Fetch(x => x.User).Eager.List().ToList() ;
        var absent = usersRecur.Where(x => !attendeeQ.ToList().Select(f => f.User.Id).Contains(x.User.Id)).Select(x => x.User).ToList();
        var meetingStats = Get_MeetingStats(s, recurrenceId);
        return new ConclusionItems() { MeetingId = meetingId, ClosedIssues = issueQ.ToList(), MeetingHeadlines = headlineQ.ToList(), OutstandingTodos = todoQ.ToList(), SendEmailsToAttendees = attendeeQ.ToList(), SendEmailsToSubscribers = meetingSubscribersQ.ToList(), PageSummaryNotes = notes, WhiteboardFileId = whiteboardFileId, AbsentAttendees = absent, MeetingStatus = meetingStats };
      }
    }

    private static IEnumerable<L10Recurrence.L10Recurrence_Page> GenerateMeetingPages(long recurrenceId, MeetingType meetingType, DateTime createTime, TermsCollection terms) {
      if (meetingType == MeetingType.L10) {
        #region Weekly Meeting (L10) Pages
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 5, Title = terms.GetTerm(TermKey.CheckIn), Subheading = "Share good news from the last 7 days.<br/> One personal and one business.", PageType = L10Recurrence.L10PageType.Segue, _Ordering = 0, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 5, Title = terms.GetTerm(TermKey.Goals), Subheading = "", PageType = L10Recurrence.L10PageType.Rocks, _Ordering = 1, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 5, Title = terms.GetTerm(TermKey.Metrics), Subheading = "", PageType = L10Recurrence.L10PageType.Scorecard, _Ordering = 2, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 5, Title = terms.GetTerm(TermKey.Headlines), Subheading = "Share headlines about customers/clients and people in the company.<br/> Good and bad. Drop down (to the issues list) anything that needs discussion.", PageType = L10Recurrence.L10PageType.Headlines, _Ordering = 3, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 5, Title = terms.GetTerm(TermKey.ToDos), Subheading = "", PageType = L10Recurrence.L10PageType.Todo, _Ordering = 4, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 60, Title = terms.GetTerm(TermKey.Issues), Subheading = "", PageType = L10Recurrence.L10PageType.IDS, _Ordering = 5, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 5, Title = terms.GetTerm(TermKey.WrapUp), Subheading = "", PageType = L10Recurrence.L10PageType.Conclude, _Ordering = 6, AutoGen = true };
        #endregion
      } else if (meetingType == MeetingType.SamePage) {
        #region Same Page Meeting pages
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 5, Title = "Check-in", Subheading = "How are you doing? State of mind? Business and personal stuff?", PageType = L10Recurrence.L10PageType.Segue, _Ordering = 1, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 2, Title = "To-dos", Subheading = "", PageType = L10Recurrence.L10PageType.Todo, _Ordering = 2, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 50, Title = "Issues", Subheading = "Your issues.", PageType = L10Recurrence.L10PageType.IDS, _Ordering = 4, AutoGen = true };
        yield return new L10Recurrence.L10Recurrence_Page() { CreateTime = createTime, L10RecurrenceId = recurrenceId, Minutes = 3, Title = "Wrap-up", Subheading = "", PageType = L10Recurrence.L10PageType.Conclude, _Ordering = 5, AutoGen = true };
        #endregion
      }
    }

    private static void CloseHeadlines_Unsafe(long meetingId, ISession s, DateTime now, List<PeopleHeadline> headlines) {
      foreach (var headline in headlines) {
        if (headline.CloseTime == null) {
          headline.CloseDuringMeetingId = meetingId;
          headline.CloseTime = now;
        }

        s.Update(headline);
      }
    }

    #region Unsafe conclusion methods
    public static async Task Depristine_Unsafe(ISession s, UserOrganizationModel caller, L10Recurrence recur) {
      if (recur.Pristine == true) {
        recur.Pristine = false;
        s.Update(recur);
        await Trigger(x => x.Create(s, EventType.CreateMeeting, caller, recur, message: recur.Name + "(" + DateTime.UtcNow.Date.ToShortDateString() + ")"));
      }
    }

    public async static Task<HtmlString> GetMeetingSummaryHtml(UserOrganizationModel caller, INotesProvider notesProvider, long meetingId) {
      var unsent = new List<Mail>();
      var meetingNotes = new List<L10Accessor.MeetingSummaryEmailData.MeetingNote>();
      long recurrenceId = 0;
      var error = "";
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var kbUrl = s.GetSettingOrDefault(Variable.Names.KB_URL, () => "https://help.bloomgrowth.com");
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Meeting(meetingId);
          var meeting = s.Get<L10Meeting>(meetingId);
          if (meeting != null && meeting.CompleteTime == null)
            throw new PermissionsException("Meeting has not wrapped up yet.");
          try {
            recurrenceId = meeting.L10RecurrenceId;
            var recurrence = s.Get<L10Recurrence>(recurrenceId);

            var terms = TermsAccessor.GetTermsCollection_Unsafe(s, recurrence.OrganizationId);

            //get meeting subscribers
            var conclusionItems = ConclusionItems.Get_Unsafe(s, meetingId, recurrenceId);
            if (!conclusionItems.SendEmailsToAttendees.Any() && !conclusionItems.SendEmailsToSubscribers.Any()) {
              conclusionItems.SendEmailsToSubscribers.Add(new MeetingSummaryWhoModel() { Id = -1, RecurrenceId = recurrenceId, Who = "nobody@bloomgrowth.com", Type = MeetingSummaryWhoType.Email, CreateTime = DateTime.UtcNow });
            }

            var pads = conclusionItems.ClosedIssues.Select(x => x.Issue.PadId).ToList();
            pads.AddRange(conclusionItems.OutstandingTodos.Select(x => x.PadId));
            pads.AddRange(conclusionItems.MeetingHeadlines.Select(x => x.HeadlinePadId));
            pads.RemoveAll(item => item == null);
            var padTexts = (await notesProvider.GetHtmlForPads(pads)).ToDefaultDictionary(x => x.Key, x => x.Value, x => new HtmlString(""));

            var notes = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { recurrenceId });
            var notesHtml = (await notesProvider.GetHtmlForPads(notes.Select(x => x.PadId).ToArray())).ToDefaultDictionary(x => x.Key, x => x.Value, x => new HtmlString("")).ToArray();
            foreach ((L10Note x, int i) in notes.Select((x, i) => (x, i)))
            {
              meetingNotes.Add(new L10Accessor.MeetingSummaryEmailData.MeetingNote(x.Name, notesHtml[i].Value, x.DateLastModified));
            }
            //Grab all (could use refactor)
            DateTime now = DateTime.UtcNow;
            var conclusionSettings = new MeetingSummaryEmailData.ConclusionEmailSettings(now, true, null, terms, recurrence, meeting, conclusionItems, meetingNotes, padTexts);
            List<MeetingSummaryEmailData> data = MeetingSummaryEmailData.BuildMeetingSummaryEmailList(conclusionSettings);
            //use first
            var d = data.First();
            var r = ViewUtility.RenderView("~/views/Email/MeetingSummary.cshtml", d);
            var html = await r.ExecuteAsync();
            return new HtmlString(html);
          } catch (Exception e) {
            return new HtmlString("Could not generate summary.");
          }
        }
      }
    }

    private static void CloseLogsOnConclusion_Unsafe(L10Meeting meeting, ISession s, DateTime now) {
      //End all logs
      var logs = s.QueryOver<L10Meeting.L10Meeting_Log>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meeting.Id && x.EndTime == null).List().ToList();
      foreach (var l in logs) {
        l.EndTime = now;
        s.Update(l);
      }
    }

    public static async Task<IEnumerable<L10Meeting.L10Meeting_Attendee>> SetConclusionRatings_Unsafe(List<Tuple<long, decimal?, string>> ratingValues, L10Meeting meeting, ISession s, List<L10Meeting.L10Meeting_Attendee> attendees) {
      var ids = ratingValues.Select(x => x.Item1).ToArray();
      //Set rating for attendees
      var raters = attendees.Where(x => ids.Any(y => y == x.User.Id));
      var raterCount = 0m;
      var raterValue = 0m;
      string notes = "";

      foreach (var a in raters) {
        a.Rating = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item2);
        notes = ratingValues.FirstOrDefault(x => x.Item1 == a.User.Id).NotNull(x => x.Item3);
        if (!string.IsNullOrEmpty(notes)) {
          var padId = Guid.NewGuid();
          await PadAccessor.CreatePad(padId.ToString(), notes);

          a.PadId = padId.ToString();
        }

        s.Update(a);
        if (a.Rating != null) {
          raterCount += 1;
          raterValue += a.Rating.Value;
        }
      }

      meeting.AverageMeetingRating = new Ratio(raterValue, raterCount);
      s.Update(meeting);


      // NOTE: Transaction is managed by callers of this method!
      await HooksRegistry.Each<IRateMeetingHooks>((ses, x) => x.UpdateRating(s, meeting));

      return raters;
    }

    public static List<L10Meeting.L10Meeting_Attendee> GetMeetingAttendees_Unsafe(long meetingId, ISession s) {
      return s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId).List().ToList();
    }

    public static List<L10Meeting.L10Meeting_Attendee> GetMeetingAttendees_Email(long meetingId, long recurrenceId, ISession s)
    {
      var meetingAttendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId).List().ToList();
      var recurrenceAttendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId).List().ToList();

      return meetingAttendees.Select(m =>
      {
        return MeetingAttendeeTransformer.TransformAttendeeEmail(recurrenceAttendees.Where(r => r.User.Id == m.User.Id).First(), m);
      }).ToList();
    }

    private static void CloseIssuesOnConclusion_Unsafe(long recurrenceId, L10Meeting meeting, ISession s, DateTime now) {
      var issuesToClose = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.MarkedForClose && x.Recurrence.Id == recurrenceId && x.CloseTime == null).List().ToList();
      foreach (var i in issuesToClose) {
        i.CloseTime = now;
        s.Update(i);
      }
    }

    private static async Task SendConclusionTextMessages_Unsafe(long recurrenceId, L10Recurrence recurrence, ISession s, DateTime now) {
      var externalForumNumbers = s.QueryOver<ExternalUserPhone>().Where(x => x.DeleteTime > now && x.ForModel.ModelId == recurrenceId && x.ForModel.ModelType == ForModel.GetModelType<L10Recurrence>()).List().ToList();
      if (externalForumNumbers.Any()) {
        try {
          var twilioData = Config.Twilio();
          TwilioClient.Init(twilioData.Sid, twilioData.AuthToken);
          var allMessages = new List<Task<MessageResource>>();
          foreach (var number in externalForumNumbers) {
            try {
              if (twilioData.ShouldSendText) {
                var to = new PhoneNumber(number.UserNumber);
                var from = new PhoneNumber(number.SystemNumber);
                var url = Config.BaseUrl(null, "/su?id=" + number.LookupGuid);
                var message = MessageResource.CreateAsync(to, from: from, body: "Thanks for participating in the " + recurrence.Name + "!\nWant a demo of Bloom Growth? Click here\n" + url);
                allMessages.Add(message);
              }
            } catch (Exception e) {
              log.Error("Particular Forum text was not sent", e);
            }

            number.DeleteTime = now;
            s.Update(number);
          }

          await Task.WhenAll(allMessages);
        } catch (Exception e) {
          log.Error("Forum texts were not sent", e);
        }
      }
    }
    #endregion
  }
}
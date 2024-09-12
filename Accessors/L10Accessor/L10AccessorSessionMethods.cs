using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Models;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using NHibernate;
using RadialReview.Models.UserModels;

namespace RadialReview.Accessors {
  public partial class L10Accessor : BaseAccessor {

    #region Session Methods
    public static L10Meeting.L10Meeting_Log _GetCurrentLog(ISession s, UserOrganizationModel caller, long meetingId, long userId, bool nullOnUnstarted = false) {
      var found = s.QueryOver<L10Meeting.L10Meeting_Log>()
        .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == userId && x.EndTime == null)
        .List().OrderByDescending(x => x.StartTime)
        .FirstOrDefault();
      if (found == null && !nullOnUnstarted)
        throw new PermissionsException("Meeting log does not exist");
      return found;
    }

    public static L10Meeting _GetCurrentOrMostRecentL10Meeting(ISession s, PermissionsUtility perms, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
      L10Meeting current=null;
      try {
        current = _GetCurrentL10Meeting(s, perms, recurrenceId, nullOnUnstarted, load, loadLogs);
      } catch (MeetingException e) {
      }

      if (current==null) {
        current = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime==null && x.L10RecurrenceId == recurrenceId && x.StartTime != null)
          .OrderBy(x => x.StartTime).Desc
          .Take(1)
          .List().SingleOrDefault();
        __ProcessCurrentL10Meeting_Unsafe(s, recurrenceId, nullOnUnstarted, load, loadLogs, current);
        perms.ViewL10Meeting(current.Id);
      }
      return current;

    }




    public static L10Meeting _GetCurrentL10Meeting(ISession s, PermissionsUtility perms, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
      var meeting = _GetCurrentL10Meeting_Unsafe(s, recurrenceId, nullOnUnstarted, load, loadLogs);
      if (meeting == null)
        return null;
      perms.ViewL10Meeting(meeting.Id);
      return meeting;
    }

    public static L10Meeting _GetCurrentL10MeetingInstance(ISession s, PermissionsUtility perms, long meetingInstanceId)
    {
      var foundInstance = s.QueryOver<L10Meeting>().Where(x =>
          x.Id == meetingInstanceId).Take(1).List().SingleOrDefault();
      return foundInstance;
    }

    public static L10Meeting _GetCurrentL10Meeting_Unsafe(ISession s, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false) {
      var found = s.QueryOver<L10Meeting>().Where(x =>
          x.StartTime != null &&
          x.CompleteTime == null &&
          x.DeleteTime == null &&
          x.L10RecurrenceId == recurrenceId
        ).OrderBy(x => x.StartTime).Desc.Take(1).List().SingleOrDefault();

      return __ProcessCurrentL10Meeting_Unsafe(s, recurrenceId, nullOnUnstarted, load, loadLogs, found);
    }

    private static L10Meeting __ProcessCurrentL10Meeting_Unsafe(ISession s, long recurrenceId, bool nullOnUnstarted, bool load, bool loadLogs, L10Meeting found) {
      if (found==null) {
        if (nullOnUnstarted)
          return null;
        throw new MeetingException(recurrenceId, "Meeting has not been started.", MeetingExceptionType.Unstarted);
      }
      var meeting = found;
      if (load)
        _LoadMeetings_Unsafe(s, true, true, true, meeting);

      if (loadLogs)
        _LoadMeetingLogs_Unsafe(s, meeting);
      return meeting;
    }

    private static void _RecursiveCloseIssues(ISession s, List<long> parentIssue_RecurIds, DateTime now) {
      if (parentIssue_RecurIds.Count == 0)
        return;

      var children = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.CloseTime == null)
        .WhereRestrictionOn(x => x.ParentRecurrenceIssue.Id)
        .IsIn(parentIssue_RecurIds)
        .List().ToList();
      foreach (var c in children) {
        c.CloseTime = now;

        //Needs updating for RealTime

        s.Update(c);
      }
      _RecursiveCloseIssues(s, children.Select(x => x.Id).ToList(), now);
    }

    //public static List<PermItem> GetAdmins(UserOrganizationModel caller, long recurrenceId) {
    //	using (var s = HibernateSession.GetCurrentSession()) {
    //		using (var tx = s.BeginTransaction()) {
    //			var perms = PermissionsUtility.Create(s, caller);
    //			return perms.GetAdmins(PermItem.ResourceType.L10Recurrence, recurrenceId);
    //		}
    //	}
    //}

    public static List<L10Recurrence> _GetAllL10RecurrenceAtOrganization(ISession s, UserOrganizationModel caller, long organizationId) {
      PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
      return s.QueryOver<L10Recurrence>()
        .Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
        .List().ToList();
    }



    public static List<L10Recurrence> _GetAllConnectedL10Recurrence(ISession s, UserOrganizationModel caller, long recurrenceId, bool excludeSelf, bool isIssue) {
      var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

      IList<L10Recurrence> meetings = new ListRecurrencesAccesor(s, caller, recurrenceId).Get(isIssue);

      if (excludeSelf) {
        meetings = meetings.Where(x => x.Id != recurrenceId).ToList();
      }

      return meetings.ToList();
    }


    #endregion
  }

  public class ListRecurrencesAccesor {

    private ISession _Session;
    private UserOrganizationModel _Caller;
    private long _RecurrenceId;

    public ListRecurrencesAccesor(ISession session, UserOrganizationModel caller, long recurrenceId) {
      _Session = session;
      _Caller = caller;
      _RecurrenceId = recurrenceId;
    }

    public IList<L10Recurrence> Get(bool isIssue) {
      bool isShared = IsShared(isIssue);

      if (isShared)
        return GetAllRecurrences();

      if (_Caller.User.IsRadialAdmin)
        return GetAllRecurrences();

      if (IsSupervisor())
        return GetSupervisorRecurrences();

      return GetUserRecurrences();
    }

    private bool IsShared(bool isIssue) {
      if (isIssue)
        return _Caller.Organization.Settings.UsersCanMoveIssuesToAnyMeeting;
      else
        return _Caller.Organization.Settings.UsersCanSharePHToAnyMeeting;
    }

    private bool IsSupervisor() {
      string managerName = _Caller.GetName(Models.Enums.GivenNameFormat.FirstAndLast);

      return _Session.QueryOver<UserLookup>()
          .Where(x => x.OrganizationId == _Caller.Organization.Id)
          .List().Any(x => IsSupervised(managerName, x.Managers));
    }

    private List<string> SplitManagers(string managerField) {
      if (string.IsNullOrEmpty(managerField))
        return new List<string>();

      List<char> managerSeparator = new List<char>() {
        ','
      };

      return managerField.Split(managerSeparator.ToArray()).ToList();
    }

    private bool IsSupervised(string managerName, string managerField) {
      List<string> managers = SplitManagers(managerField);
      return managers.Contains(managerName);
    }

    private IList<L10Recurrence> GetSupervisorRecurrences() {
      string managerName = _Caller.GetName(Models.Enums.GivenNameFormat.FirstAndLast);

      List<long> directReportUsers = _Session.QueryOver<UserLookup>()
             .Where(x => x.OrganizationId == _Caller.Organization.Id)
             .List()
             .Where(s => IsSupervised(managerName, s.Managers))
             .Select(q => q.UserId)
             .ToList();

      List<long> suppervisedMeetingAttendees = _Session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(s => s.User.Id).IsIn(directReportUsers)
        .Select(x => x.L10Recurrence.Id)
        .List<long>()
        .ToList();

      IList<L10Recurrence> suppervisedMeetings = _Session.QueryOver<L10Recurrence>()
          .Where(x => x.DeleteTime == null)
          .WhereRestrictionOn(s => s.Id).IsIn(suppervisedMeetingAttendees)
          .List();

      IList<L10Recurrence> currentMeetings = GetUserRecurrences();

      return suppervisedMeetings.Union(currentMeetings).ToList();
    }

    private IList<L10Recurrence> GetUserRecurrences() {
      List<long> recurrenceIds = _Session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
      .Where(x => x.DeleteTime == null &&
            x.User.Id == _Caller.Id)
      .Select(x => x.L10Recurrence.Id)
      .List<long>()
      .ToList();

      return _Session.QueryOver<L10Recurrence>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(s => s.Id).IsIn(recurrenceIds)
        .List();
    }

    private IList<L10Recurrence> GetAllRecurrences() {
      return _Session.QueryOver<L10Recurrence>()
          .Where(x => x.DeleteTime == null &&
                x.OrganizationId == _Caller.Organization.Id).List();
    }

    private IList<L10Recurrence> LegacyGetRecurrences() {
      List<long> userIds = _Session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null &&
              x.L10Recurrence.Id == _RecurrenceId)
        .Select(x => x.User.Id)
        .List<long>()
        .ToList();

      List<long> recurrenceIds = _Session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.User.Id).IsIn(userIds)
        .Select(x => x.L10Recurrence.Id)
        .List<long>()
        .ToList();

      IList<L10Recurrence> recurrences = _Session.QueryOver<L10Recurrence>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.Id).IsIn(recurrenceIds)
        .List();

      return recurrences;
    }

  }
}
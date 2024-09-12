using NHibernate;
using NHibernate.Criterion;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;

namespace RadialReview.Accessors
{

  public class LoadMeeting
  {
    public bool LoadUsers { get; set; }
    public bool LoadMeasurables { get; set; }
    public bool LoadRocks { get; set; }
    public bool LoadVideos { get; set; }
    public bool LoadNotes { get; set; }
    public bool LoadPages { get; set; }
    public bool LoadAudio { get; set; }
    public bool LoadConclusionActions { get; set; }

    public bool AnyTrue()
    {
      return LoadUsers || LoadMeasurables || LoadRocks || LoadVideos || LoadNotes || LoadPages || LoadAudio;
    }

    public static LoadMeeting True()
    {
      return new LoadMeeting()
      {
        LoadMeasurables = true,
        LoadVideos = true,
        LoadRocks = true,
        LoadUsers = true,
        LoadPages = true,
        LoadNotes = true,
        LoadAudio = true,
        LoadConclusionActions = true,
      };
    }

    public static LoadMeeting False()
    {
      return new LoadMeeting()
      {
        LoadMeasurables = false,
        LoadVideos = false,
        LoadRocks = false,
        LoadUsers = false,
        LoadNotes = false,
        LoadPages = false,
        LoadAudio = false,
        LoadConclusionActions = false,
      };
    }
  }
  public partial class L10Accessor : BaseAccessor
  {

    #region Get Meeting Data
    public static L10Recurrence GetL10Recurrence(UserOrganizationModel caller, long recurrenceId, LoadMeeting load)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          return GetL10Recurrence(s, perms, recurrenceId, load);
        }
      }
    }

    public static long? GetOrgUserIdFromRecurrenceId(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, UserOrganizationModel caller, long recurrenceId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.SetDbConnection(dbContext.Database, s.Connection);

          var query =
              from uom in dbContext.UserOrganizationModels
              join l10 in dbContext.L10recurrences
                on uom.OrganizationId equals l10.OrganizationId
              join um in dbContext.UserModels
                on uom.UserModelId equals um.Id
              where uom.DeleteTime == null && um.DeleteTime == null && l10.DeleteTime == null
              where System.Text.RegularExpressions.Regex.IsMatch(um.UserOrganizationIds, $"(^|~){caller.Id}(~|$)") && l10.Id == recurrenceId
              select uom.ResponsibilityGroupModelId;

          var result = query.SingleOrDefault();
          return result;
        }
      }
    }


    public static L10Recurrence GetL10Recurrence(ISession s, PermissionsUtility perms, long recurrenceId, LoadMeeting load)
    {
      perms.ViewL10Recurrence(recurrenceId);
      var found = s.Get<L10Recurrence>(recurrenceId);
      if (load.AnyTrue())
      {
        _LoadRecurrences(s, load, found);
      }
      return found;
    }
    public static L10Meeting GetPreviousMeeting(ISession s, PermissionsUtility perms, long recurrenceId)
    {
      perms.ViewL10Recurrence(recurrenceId);
      var previousMeeting = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10RecurrenceId == recurrenceId && x.CompleteTime != null).OrderBy(x => x.CompleteTime).Desc.Take(1).SingleOrDefault();
      return previousMeeting;
    }

    public static DateTime GetLastMeetingEndTime(UserOrganizationModel caller, long recurrenceId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          var last = GetPreviousMeeting(s, perms, recurrenceId);
          if (last == null || !last.CompleteTime.HasValue)
          {
            return DateTime.MinValue;
          }
          return last.CompleteTime.Value;
        }
      }
    }



    /// <summary>
    /// Consider if you want to use GetViewableL10Meetings, as this is faster...
    /// WARNING: Don't be fooled, this returns RECURRANCE ids, not Meeting Ids.
    /// Which is actually helpful, as long as you know what you're getting back.
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="userId"></param>
    /// <param name="onlyPersonallyAttending"></param>
    /// <param name="onlyDashboardRecurrences"></param>
    /// <param name="teamTypeFilter"></param>
    /// <returns></returns>
    public static List<NameId> GetVisibleL10Meetings_Tiny(UserOrganizationModel caller, long userId, bool onlyPersonallyAttending = false, bool onlyDashboardRecurrences = false, L10TeamType? teamTypeFilter = null)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          return GetVisibleL10Meetings_Tiny(s, perms, userId, onlyPersonallyAttending, onlyDashboardRecurrences, teamTypeFilter);
        }
      }
    }

    public static List<TinyRecurrence> GetViewableL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId)
    {
      perms.ViewUsersL10Meetings(userId);
      var allViewPerms = perms.GetAllPermItemsForUser(PermItem.ResourceType.L10Recurrence, userId)
        .Where(x => x.CanView)
        .Select(x => x.ResId)
        .ToArray();

      //Get Attendees
      UserOrganizationModel userAlias = null;
      var attendeeQ = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .JoinAlias(x => x.User, () => userAlias)
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(allViewPerms)
        .Select(x => x.Id, x => x.StarDate, x => x.L10Recurrence.Id)
        .Future<object[]>()
        .Select(x => new
        {
          Id = (long)x[0],
          StarDate = (DateTime?)x[1],
          L10Id = (long)x[2],
        });

      ////Get Attendees
      //L10Recurrence recurAlias = null;
      //var attendeeQ = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
      //    .JoinAlias(x => x.L10Recurrence, () => recurAlias)
      //    .Where(x => x.DeleteTime == null && x.User.Id == userId)
      //    .WhereRestrictionOn(x => x.Id).IsIn(allViewPerms)
      //    .Select(x => x.Id, x => x.StarDate)
      //    .Future<object[]>()
      //    .Select(x => new
      //    {
      //        Id = (long)x[0],
      //        StarDate = (DateTime?)x[1]
      //    });

      //Get Recurrences
      var recurs = s.QueryOver<L10Recurrence>()
        .Where(x => x.DeleteTime == null && !x.Pristine).WhereRestrictionOn(x => x.Id).IsIn(allViewPerms)
        .Select(x => x.Id, x => x.Name, x => x.MeetingInProgress, x => x.MeetingType)
        .List<object[]>()
        .Select(x => new
        {
          Id = (long)x[0],
          Name = (string)x[1],
          MeetingInProgress = (long?)x[2],
          MeetingType = (MeetingType)x[3]
        }).ToList();
      var attendee = attendeeQ.ToList();

      //Smash them together
      return recurs
        .Select(x => new TinyRecurrence()
        {
          Id = x.Id,
          Name = x.Name,
          MeetingInProgress = x.MeetingInProgress,
          IsAttendee = attendee.Any(a => a.L10Id == x.Id),
          StarDate = attendee.FirstOrDefault(a => a.L10Id == x.Id && a.StarDate != null).NotNull(a => a.StarDate),
          MeetingType = x.MeetingType
        }).ToList();
    }

    public static List<NameId> GetVisibleL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId, bool onlyPersonallyAttending = false, bool onlyDashboardRecurrences = false, L10TeamType? teamTypeFilter = null)
    {
      List<long> personallyAttending;
      List<long> dashRecurs;
      var meetings = GetVisibleL10Meetings_Tiny(s, perms, userId, out personallyAttending, out dashRecurs);
      if (onlyPersonallyAttending)
      {
        meetings = meetings.Where(x => personallyAttending.Contains(x.Id)).ToList();
      }
      if (onlyDashboardRecurrences)
      {
        meetings = meetings.Where(x => dashRecurs.Contains(x.Id)).ToList();
      }

      //should be last.
      if (teamTypeFilter != null)
      {
        var acceptedMeetingIds = s.QueryOver<L10Recurrence>()
          .WhereRestrictionOn(x => x.Id).IsIn(meetings.Select(x => x.Id).ToArray())
          .Where(x => x.TeamType == teamTypeFilter.Value)
          .Select(x => x.Id)
          .List<long>().ToList();
        meetings = meetings.Where(x => acceptedMeetingIds.Contains(x.Id)).ToList();
      }


      return meetings;
    }
    public static List<long> GetRecurrencesIdByCallerOrganizationUnsafe(ISession s, PermissionsUtility perms)
    {
      var caller = perms.GetCaller();
      perms.ViewUsersL10Meetings(caller.Id);

      var orgRecurrencesIds = s.QueryOver<L10Recurrence>().Where(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null && !x.Pristine)
          .Select(x => x.Id)
          .List<long>().ToList();
      return orgRecurrencesIds;

    }
    public static List<NameId> GetVisibleL10Meetings_Tiny(ISession s, PermissionsUtility perms, long userId, out List<long> recurrencesPersonallyAttending, out List<long> recurrencesVisibleOnDashboard) {

      //IMPORTANT. Make sure the pristine flag is being set correctly on L10Recurrence.

      var caller = perms.GetCaller();
      perms.ViewUsersL10Meetings(userId);

      //Who should we get this data for? Just Self, or also subordiantes?
      var accessibleUserIds = new[] { userId };
      var user = s.Get<UserOrganizationModel>(userId);
      if (user.Organization.Settings.ManagersCanViewSubordinateL10)
      {
        accessibleUserIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, userId).ToArray(); //DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, userId).ToArray();
      }

      L10Recurrence alias = null;
      //var allRecurrences = new List<L10Recurrence>();
      var allRecurrenceIds = new List<NameId>();
      IEnumerable<object[]> orgRecurrences = null;
      if (caller.ManagingOrganization)
      {
        orgRecurrences = s.QueryOver<L10Recurrence>().Where(x => x.OrganizationId == caller.Organization.Id && x.DeleteTime == null && !x.Pristine)
          .Select(x => x.Name, x => x.Id)
          .Future<object[]>();
      }

      var attendee_ReccurenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.User.Id).IsIn(accessibleUserIds)
        .Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
        .Where(x => alias.DeleteTime == null)
        .Select(x => alias.Name, x => alias.Id, x => x.User.Id)
        .Future<object[]>();

      var admin_MeasurableIdsCriteria = QueryOver.Of<MeasurableModel>().Where(x => x.AdminUserId == userId && x.DeleteTime == null).Select(Projections.Property<MeasurableModel>(x => x.Id));

      var admin_RecurrenceIdsQ = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null)
        .WithSubquery.WhereProperty(x => x.Measurable.Id).In(admin_MeasurableIdsCriteria)
        .Left.JoinQueryOver(x => x.L10Recurrence, () => alias)
        .Where(x => alias.DeleteTime == null)
        .Select(x => alias.Name, x => alias.Id)
        .Future<object[]>().Select(x => new NameId((string)x[0], (long)x[1]));

      //prefetch
      var teamDurationQ = s.QueryOver<TeamDurationModel>().Where(x => x.UserId == userId && x.DeleteTime == null).Future();
      //endprefetch


      //From future
      var attendee_recurrences = attendee_ReccurenceIds.ToList().Select(x => new NameId((string)x[0], (long)x[1])).ToList();
      recurrencesPersonallyAttending = attendee_ReccurenceIds.Where(x => (long)x[2] == userId).Select(x => (long)x[1]).ToList();
      recurrencesPersonallyAttending = recurrencesPersonallyAttending.Distinct().ToList();
      recurrencesVisibleOnDashboard = recurrencesPersonallyAttending.ToList();
      var admin_RecurrenceIds = admin_RecurrenceIdsQ.ToList();



      allRecurrenceIds.AddRange(attendee_recurrences);
      allRecurrenceIds.AddRange(admin_RecurrenceIds);



      var allViewPerms = PermissionsAccessor.GetExplicitPermItemsForUser(s, perms, userId, PermItem.ResourceType.L10Recurrence).Where(x => x.CanView);
      var allViewPermsRecurrences = allRecurrenceIds.Where(allRecurrenceId => allViewPerms.Any(y => allRecurrenceId.Id == y.ResId)).ToList();
      recurrencesVisibleOnDashboard.AddRange(allViewPermsRecurrences.Select(x => x.Id));

      //Outside the company
      var additionalRecurrenceIdsFromPerms = allViewPerms.Where(allViewPermId => !allRecurrenceIds.Any(y => y.Id == allViewPermId.ResId)).ToList();
      var additionalRecurrenceFromViewPerms = s.QueryOver<L10Recurrence>()
        .Where(x => !x.Pristine && x.DeleteTime == null)
        .WhereRestrictionOn(x => x.Id).IsIn(additionalRecurrenceIdsFromPerms.Select(x => x.ResId).ToArray())
        .Select(x => x.Name, x => x.Id)
        .List<object[]>().Select(x => new NameId((string)x[0], (long)x[1])).ToList();
      allRecurrenceIds.AddRange(additionalRecurrenceFromViewPerms);
      recurrencesVisibleOnDashboard.AddRange(additionalRecurrenceFromViewPerms.Select(x => x.Id));

      if (orgRecurrences != null)
      {
        allRecurrenceIds.AddRange(orgRecurrences.ToList().Select(x => new NameId((string)x[0], (long)x[1])));
      }

      allRecurrenceIds = allRecurrenceIds.Distinct(x => x.Id).ToList();
      recurrencesVisibleOnDashboard = recurrencesVisibleOnDashboard.Distinct().ToList();

      var lookup = L10Accessor.GetStarredMeetingsLookup_Unsafe(s, userId);
      if (caller.ManagingOrganization)
      {
        return allRecurrenceIds.OrderBy(x => lookup[x.Id] ?? DateTime.MaxValue).ToList();
      }


      var available = new List<NameId>();
      foreach (var r in allRecurrenceIds)
      {
        try
        {
          perms.CanView(PermItem.ResourceType.L10Recurrence, r.Id);
          available.Add(r);
        }
        catch
        {
        }
      }
      return available.OrderBy(x => lookup[x.Id] ?? DateTime.MaxValue).ToList();
    }

    public static List<TinyRecurrence> GetVisibleL10Recurrences(UserOrganizationModel caller, long userId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          return GetVisibleL10Recurrences(s, perms, userId);
        }
      }
    }


    ///// <summary>
    ///// Assesor Used to get MeetingListLookup in V3
    ///// </summary>
    //public static List<TinyRecurrence> GetVisibleL10RecurrencesLookup(ISession s, PermissionsUtility perms, long userId, bool checkPerms = false)
    //{
    //  List<long> attendee_recurrences;
    //  List<long> _nil;
    //  var uniqueL10NameIds = GetVisibleL10Meetings_Tiny(s, perms, userId, out attendee_recurrences, out _nil);
    //  var uniqueL10Ids = uniqueL10NameIds.Select(x => x.Id).ToList();

    //  var allRecurrencesQ = s.QueryOver<L10Recurrence>()
    //    .Where(x => x.DeleteTime == null)
    //    .WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids)
    //    .Select(x => x.Id, x => x.Name, x => x.MeetingInProgress,
    //    x =>x.CreateTime, x=>x.LastUpdatedBy, x=> x.DateLastModified
    //    )
    //    .Future<object[]>().Select(x => new TinyRecurrence
    //    {
    //      Id = (long)x[0],
    //      Name = (string)x[1],
    //      MeetingInProgress = (long?)x[2],
    //      CreateTime = (DateTime)x[3],
    //      LastUpdatedBy = (string)x[4],
    //      DateLastModified = (double)x[5]
    //    });

    //  var allRecurrences = allRecurrencesQ.ToList();
    //  var allAttendees = GetRecurrenceAttendeesLookupByRecurenceIdsUnsafe(s, uniqueL10Ids);

    //  foreach (var a in allRecurrences)
    //  {
    //    IEnumerable<L10Recurrence.L10Recurrence_Attendee> attendees = allAttendees.Where(x => x.L10Recurrence.Id == a.Id);
    //    a.L10Recurrence_Attendees = attendees.ToList();
    //    a.IsAttendee = attendee_recurrences.Any(y => y == a.Id);
    //    a.StarDate   = attendees.Where(x => x.User.Id == userId).FirstOrDefault().NotNull(x => x.StarDate);
    //  }
    //  allRecurrences = checkPerms ? perms.FilterTinyRecurrencesWithCanViewPermission(allRecurrences) : allRecurrences;

    //  return allRecurrences;
    //}

    public static List<TinyRecurrence> GetVisibleL10Recurrences(ISession s, PermissionsUtility perms, long userId, bool loadPages = false, bool loadFavorites = false)
    {
      List<long> attendee_recurrences;
      List<long> _nil;
      var uniqueL10NameIds = GetVisibleL10Meetings_Tiny(s, perms, userId, out attendee_recurrences, out _nil);
      var uniqueL10Ids = uniqueL10NameIds.Select(x => x.Id).ToList();

      var allRecurrencesQ = s.QueryOver<L10Recurrence>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids)
        .Select(x => x.Id, x => x.Name, x => x.MeetingInProgress)
        .Future<object[]>().Select(x => new TinyRecurrence
        {
          Id = (long)x[0],
          Name = (string)x[1],
          MeetingInProgress = (long?)x[2],
        });

      IEnumerable<FavoriteModel> favoritesQ = new List<FavoriteModel>();

      if (loadFavorites)
      {
        favoritesQ = s.QueryOver<FavoriteModel>()
                        .Where(x => x.DeleteTime == null && x.UserId == userId && x.ParentType == FavoriteType.Meeting)
                        //.Select(x => x.Id, x => x.Position)
                        .Future();
      }


      UserOrganizationModel userAlias = null;
      var allAttendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .JoinAlias(x => x.User, () => userAlias)
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(uniqueL10Ids)
        .List()
        .ToList();

      List<L10Recurrence.L10Recurrence_Page> pages = null;
      if (loadPages)
      {
        pages = s.QueryOver<L10Recurrence.L10Recurrence_Page>()
          .Where(x => x.DeleteTime == null)
          .WhereRestrictionOn(x => x.L10RecurrenceId).IsIn(uniqueL10Ids)
          .List()
          .ToList();
      }

      var allRecurrences = allRecurrencesQ.ToList();
      var favoriteLookup = favoritesQ.ToList().ToDefaultDictionary(x => x.ParentId, x => x, x => null);


      foreach (var a in allRecurrences)
      {
        a._DefaultAttendees = allAttendees.Where(x => x.L10Recurrence.Id == a.Id)
                          .Select(x => TinyUser.FromUserOrganization(x.User))
                          .ToList();
        a.IsAttendee = attendee_recurrences.Any(y => y == a.Id);
        a.StarDate = allAttendees.Where(x => x.L10Recurrence.Id == a.Id && x.User.Id == userId).FirstOrDefault().NotNull(x => x.StarDate);
        a.Favorite = favoriteLookup[a.Id];
        if (loadPages && pages != null)
        {
          a._Pages = pages.Where(x => x.L10RecurrenceId == a.Id).ToList();
        }
      }

      //Make a lookup for self attendance
      return allRecurrences;
    }

    public static List<L10VM> GetVisibleL10RecurrencesVM(UserOrganizationModel caller, long userId, bool loadUsers)
    {
      return GetVisibleL10Recurrences(caller, userId).Select(x => new L10VM(x)).ToList();
    }
    public static string GetCurrentL10MeetingLeaderPage(UserOrganizationModel caller, long meetingId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var leaderId = s.Get<L10Meeting>(meetingId).MeetingLeader.Id;
          var leaderpage = s.QueryOver<L10Meeting.L10Meeting_Log>()
            .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.User.Id == leaderId && x.EndTime == null)
            .List().OrderByDescending(x => x.StartTime)
            .Where(x => x.Page != "nopage")
            .FirstOrDefault();
          return leaderpage.NotNull(x => x.Page);
        }
      }
    }

    public static long GetCurrentPageIdMeeting(UserOrganizationModel caller, long recurrenceId)
    {
      var currentMeeting = L10Accessor.GetCurrentL10Meeting(caller, recurrenceId);
      var currentPageId = L10Accessor.GetCurrentL10MeetingLeaderPage(caller, currentMeeting.Id);
      var pageId = new string(currentPageId.Where(char.IsDigit).ToArray());
      return pageId.ToLong();
    }

    public static L10Meeting GetCurrentL10Meeting(UserOrganizationModel caller, long recurrenceId, bool nullOnUnstarted = false, bool load = false, bool loadLogs = false)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          return _GetCurrentL10Meeting(s, perms, recurrenceId, nullOnUnstarted, load, loadLogs);
        }
      }
    }
    public static List<L10Meeting> GetL10Meetings(UserOrganizationModel caller, long recurrenceId, bool load = false, bool excludePreviewMeeting = false)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

          var o = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId);


          if (excludePreviewMeeting)
          {
            o = o.Where(x => x.Preview == false);
          }

          var oResolved = o.List().ToList();

          if (load)
          {
            _LoadMeetings_Unsafe(s, true, true, true, oResolved.ToArray());
          }

          return oResolved;
        }
      }
    }

    //Finds all first degree connectioned L10Recurrences
    public static List<L10Recurrence> GetAllConnectedL10Recurrence(UserOrganizationModel caller, long recurrenceId, bool excludeSelf, bool isIssue)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          return _GetAllConnectedL10Recurrence(s, caller, recurrenceId, excludeSelf, isIssue);
        }
      }
    }

    public static List<L10Recurrence> GetAllL10RecurrenceAtOrganization(UserOrganizationModel caller, long organizationId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          return _GetAllL10RecurrenceAtOrganization(s, caller, organizationId);
        }
      }
    }

    public static L10Recurrence GetCurrentL10RecurrenceFromMeeting(UserOrganizationModel caller, long l10MeetingId, long? l10RecurrenceId = null, bool loadUsers = true)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller).ViewL10Meeting(l10MeetingId);
          var recurrence = l10RecurrenceId ?? s.Get<L10Meeting>(l10MeetingId).L10RecurrenceId;
          var load = LoadMeeting.False();
          load.LoadUsers = loadUsers;
          return GetL10Recurrence(s, perms, recurrence, load);
        }
      }
    }

    public static long GetVtoIdForL10(UserOrganizationModel caller, long recurrenceId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          var recur = s.Get<L10Recurrence>(recurrenceId);
          var vtoId = recur.VtoId;
          perms.Or(x => x.ViewVTOVision(vtoId), x => x.ViewVTOTraction(vtoId));
          return vtoId;
        }
      }

    }

    public static long? GetSharedVTOVision(UserOrganizationModel caller, long orgId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          //var perms = PermissionsUtility.Create(s, caller);
          //perms.ViewOrganization(orgId);

          var found = s.QueryOver<L10Recurrence>()
            .Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.ShareVto == true)
            .Select(x => x.Id)
            .Take(1).SingleOrDefault<long>();

          return found == 0 ? null : (long?)found;
        }
      }
    }

    public static List<L10Recurrence> GetMeetingsContainingGoal(long goalId, DateTime? deleteTime = null)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          RadialReview.Models.Askables.RockModel rock = null;
          RadialReview.Models.L10.L10Recurrence rec = null;

          var q =
              s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
              .JoinAlias(x => x.L10Recurrence, () => rec)
              .JoinAlias(x => x.ForRock, () => rock)
              .Where(x => x.ForRock.Id == goalId);

          if (deleteTime.HasValue)
          {
            q = q.Where(Restrictions.Eq("DeleteTime", deleteTime));
          }
          else
          {
            q = q.Where(x => x.DeleteTime == deleteTime && rock.DeleteTime == null);
          }

          var found = q.List().ToList();

          //var userPerms = PermissionsUtility.Create(s, caller);

          if (found != null)
          {
            var results =
                found
                  //.Where(x => userPerms.IsPermitted(perm => perm.ViewL10Recurrence(x.L10Recurrence.Id)))
                  .Select(x => x.L10Recurrence)
                  .ToList();

            return results;
          }

          return null;
        }
      }
    }

    public static L10Recurrence GetRemovedRecurrenceInGoal(long goalId, long recurrenceId, DateTime deleteTime)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          RadialReview.Models.Askables.RockModel rock = null;
          RadialReview.Models.L10.L10Recurrence rec = null;

          var q =
              s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
              .JoinAlias(x => x.L10Recurrence, () => rec)
              .JoinAlias(x => x.ForRock, () => rock)
              .Where(x => x.ForRock.Id == goalId)
              .Where(Restrictions.Eq("DeleteTime", deleteTime));

          var found = q.List().Take(1).ToList();

          //var userPerms = PermissionsUtility.Create(s, caller);

          if (found != null)
          {
            var results =
                found
                  //.Where(x => userPerms.IsPermitted(perm => perm.ViewL10Recurrence(x.L10Recurrence.Id)))
                  .Select(x => x.L10Recurrence)
                  .ToList();

            return results.FirstOrDefault();
          }

          return null;
        }
      }
    }

    public static List<L10Recurrence.L10Recurrence_Measurable> GetMeetingsContainingMeasurable(long measurableId, DateTime? deleteTime = null)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          RadialReview.Models.Scorecard.MeasurableModel measurable = null;
          var q =
              s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
              .JoinAlias(x => x.Measurable, () => measurable)
              .Where(x => x.Measurable.Id == measurableId);

          q =
            deleteTime == null
            ? q.Where(x => x.DeleteTime == null && measurable.DeleteTime == null)
            : q.Where(x => x.DeleteTime == deleteTime.Value && measurable.DeleteTime == deleteTime.Value);

          q = q.Fetch(SelectMode.Fetch, x => x.L10Recurrence);
          q = q.Fetch(SelectMode.Fetch, x => x.Measurable.AccountableUser);

          var found = q.List().ToList();

          //var userPerms = PermissionsUtility.Create(s, caller);

          if (found != null)
          {
            var results =
                found
                  //.Where(x => userPerms.IsPermitted(perm => perm.ViewL10Recurrence(x.L10Recurrence.Id)))
                  // .Select(x => x.L10Recurrence)
                  .ToList();

            return results;
          }

          return null;
        }
      }
    }

    public static List<L10Recurrence> GetMeetingsByL10measurableId(long measurableId, long l10MeasurableId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {

        RadialReview.Models.Scorecard.MeasurableModel measurable = null;
        RadialReview.Models.L10.L10Recurrence recurrence = null;

        var q =
            s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
            .JoinAlias(x => x.Measurable, () => measurable)
            .JoinAlias(x => x.L10Recurrence, () => recurrence)
            .Where(x => x.Measurable.Id == measurableId);

        q = q.Where(x => x.Id == l10MeasurableId);

        var found = q.List().ToList();

        //var userPerms = PermissionsUtility.Create(s, caller);

        if (found != null)
        {
          var results =
              found
                //.Where(x => userPerms.IsPermitted(perm => perm.ViewL10Recurrence(x.L10Recurrence.Id)))
                .Select(x => x.L10Recurrence)
                .ToList();

          return results;
        }

        return null;

      }
    }

    public static List<L10Recurrence> GetMeetingsByL10measurableId(long measurableId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {

        RadialReview.Models.Scorecard.MeasurableModel measurable = null;
        var q =
            s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
            .JoinAlias(x => x.Measurable, () => measurable)
            .Where(x => x.Measurable.Id == measurableId);

        q = q.Where(x => x.DeleteTime == null);

        var found = q.List().ToList();

        //var userPerms = PermissionsUtility.Create(s, caller);

        if (found != null)
        {
          var results =
              found
                //.Where(x => userPerms.IsPermitted(perm => perm.ViewL10Recurrence(x.L10Recurrence.Id)))
                .Select(x => x.L10Recurrence)
                .ToList();

          return results;
        }

        return null;

      }
    }

    public static IEnumerable<L10Recurrence.L10Recurrence_Page> GetMeetingPagesByRecurrenceIds(List<long> recurrenceIds)
    {
      using var session = HibernateSession.GetCurrentSession();
      IEnumerable<L10Recurrence.L10Recurrence_Page> allPagesQ = null;
      allPagesQ = session.QueryOver<L10Recurrence.L10Recurrence_Page>()
            .Where(x => x.DeleteTime == null)
            .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
            .List();

      if (allPagesQ is null)
        return new List<L10Recurrence.L10Recurrence_Page>();

      return allPagesQ.ToList();
    }

    public static List<L10Recurrence> GetRecurrenceByCustomGoalId(long customGoalId)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {

        MeasurableModel MeasurableAlias = null;
        MetricCustomGoal MetricGoalAlias = null;
        var query = session.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
          .JoinAlias(x => x.Measurable, () => MeasurableAlias)
          .JoinAlias(x => MeasurableAlias.CustomGoals, () => MetricGoalAlias)
          .Where(x => x.DeleteTime == null && MetricGoalAlias.Id == customGoalId)
          .Select(x => x.L10Recurrence)
          .List<L10Recurrence>();

        return query.ToList();

      }
    }


    public static List<L10Recurrence> GetRecurrencesByIds(UserOrganizationModel caller, IReadOnlyList<long> recurrenceIds, LoadMeeting loadMeeting)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(s, caller);
        return GetRecurrencesByIds(s, perms, recurrenceIds, loadMeeting);
      }
    }

    public static List<L10Recurrence> GetRecurrencesByIds(ISession session, PermissionsUtility perms, IReadOnlyList<long> recurrenceIds, LoadMeeting loadMeeting)
    {
      var recurrences = session.QueryOver<L10Recurrence>()
          .WhereRestrictionOn(rec => rec.Id).IsIn(recurrenceIds.ToList())
          .List()
          .ToList();

      var recurrencesWithViewPermissions = recurrences.Where(rec =>
      {
        try
        {
          perms.ViewL10Recurrence(rec.Id);
          return true;
        }
        catch (Exception)
        {
          return false;
        }
      });

      if (loadMeeting.AnyTrue())
      {
        _LoadRecurrences(session, loadMeeting, recurrencesWithViewPermissions.ToArray());
      }

      return recurrencesWithViewPermissions.ToList();
    }


  }

  #endregion

}

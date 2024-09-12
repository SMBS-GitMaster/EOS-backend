using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Periods;
using RadialReview.Models.Rocks;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Query;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Accessors.StrictlyAfterExecutors;

namespace RadialReview.Accessors {
  public class RockAndMilestones {
    public RockModel Rock { get; set; }

    public List<Milestone> Milestones { get; set; }

    public bool AnyMilestoneMeetings { get; set; }
  }

  public class RockAccessor {
    #region Milestones
    public static async Task<Milestone> AddMilestone(UserOrganizationModel caller, long rockId, string milestone, DateTime dueDate, bool complete = false, BloomMilestoneStatus? bloomStatus = BloomMilestoneStatus.Incompleted) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            var perm = PermissionsUtility.Create(s, caller);
            perm.EditRock(rockId, false);
            var ms = new Milestone() { DueDate = dueDate, Name = milestone, Required = true, RockId = rockId, Status = complete ? MilestoneStatus.Done : MilestoneStatus.NotDone, CompleteTime = complete ? (DateTime?)DateTime.UtcNow : null, bloomStatus = bloomStatus };
            s.Save(ms);
            var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.ForRock.Id == rockId).Select(x => x.L10Recurrence.Id).List<long>();
            tx.Commit();
            s.Flush();
            rt.UpdateRecurrences(recurrenceIds).Call("setMilestone", ms);
            await HooksRegistry.Each<IMilestoneHook>((ses, x) => x.CreateMilestone(ses, ms));
            return ms;
          }
        }
      }
    }

    public static async Task<List<RockModel>> Search(UserOrganizationModel caller, long orgId, string search, long[] excludeLong, int take = int.MaxValue) {
      excludeLong = excludeLong ?? new long[] { };
      var visible = RockAccessor.GetAllVisibleRocksAtOrganization(caller, orgId, true).Where(x => !excludeLong.Any(y => y == x.Id)).Where(x => x.Id > 0);
      var splits = search.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      var dist = new DiscreteDistribution<RockModel>(0, 9, true);
      foreach (var u in visible) {
        var fname = false;
        var lname = false;
        var ordered = false;
        var fnameStart = false;
        var lnameStart = false;
        var wasFirst = false;
        var exactFirst = false;
        var exactLast = false;
        var containsText = false;
        var names = new List<string[]>();
        names.Add(new string[] { u.AccountableUser.GetFirstName().ToLower(), u.AccountableUser.GetLastName().ToLower(), });
        foreach (var n in names) {
          var f = n[0];
          var l = n[1];
          foreach (var t in splits) {
            if (f.Contains(t)) {
              fname = true;
            }

            if (f == t) {
              exactFirst = true;
            }

            if (f.StartsWith(t)) {
              fnameStart = true;
            }

            if (l.Contains(t)) {
              lname = true;
            }

            if (l.StartsWith(t)) {
              lnameStart = true;
            }

            if (fname && !wasFirst && lname) {
              ordered = true;
            }

            if (l == t) {
              exactLast = true;
            }

            if (u.Rock != null && u.Rock.ToLower().Contains(t)) {
              containsText = true;
            }

            wasFirst = true;
          }
        }

        var score = fname.ToInt() + lname.ToInt() + ordered.ToInt() + fnameStart.ToInt() + lnameStart.ToInt() + exactFirst.ToInt() + exactLast.ToInt() + containsText.ToInt() * 2;
        if (score > 0) {
          dist.Add(u, score);
        }
      }

      return dist.GetProbabilities().OrderByDescending(x => x.Value).Select(x => x.Key).Take(take).ToList();
    }

    public static List<L10Recurrence.GoalRecurrenceRecord> GetRecurrencesContainingRock(UserOrganizationModel caller, long rockId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetRecurrencesContainingRock(s, perms, rockId);
        }
      }
    }

    public static L10Recurrence.GoalRecurrenceRecord GetRemovedGoalRecurrenceRecord_Unsafe(long goalId, long recurrenceId, DateTime deleteTime)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          L10Recurrence recurAlias = null;

          return s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
                  .JoinAlias(x => x.L10Recurrence, () => recurAlias)
                  .Where(x => x.DeleteTime == deleteTime && x.ForRock.Id == goalId && x.L10Recurrence.Id == recurrenceId)
                  .Select(x => x.L10Recurrence.Id, x => recurAlias.RockType, x => recurAlias.Name, x => recurAlias.TeamType, x => x.VtoRock, x => x.Id)
                  .List<object[]>()
                  .Take(1)
                  .Select(x => new L10Recurrence.GoalRecurrenceRecord()
                  {
                    RecurrenceId = (long)x[0],
                    MilestonesEnabled = ((L10RockType)x[1]) == L10RockType.Milestones,
                    Name = x[2] + "",
                    TeamType = ((L10TeamType)x[3]),
                    VtoRock = (bool)x[4],
                    RecurrenceRockId = (long)x[5]
                  }).ToList()
                  .FirstOrDefault();
        }
      }
    }

    public static List<L10Recurrence.GoalRecurrenceRecord> GetGoalRecurrenceRecords_Unsafe(long rockId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          L10Recurrence recurAlias = null;
          return s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
            .JoinAlias(x => x.L10Recurrence, () => recurAlias)
            .Where(x => x.DeleteTime == null && x.ForRock.Id == rockId && recurAlias.DeleteTime == null)
            .Select(x => x.L10Recurrence.Id, x => recurAlias.RockType, x => recurAlias.Name, x => recurAlias.TeamType, x => x.VtoRock, x => x.Id)
            .List<object[]>()
            .Select(x => new L10Recurrence.GoalRecurrenceRecord()
            {
              RecurrenceId = (long)x[0],
              MilestonesEnabled = ((L10RockType)x[1]) == L10RockType.Milestones,
              Name = x[2] + "",
              TeamType = ((L10TeamType)x[3]),
              VtoRock = (bool)x[4],
              RecurrenceRockId = (long)x[5]
            }).ToList();
        }
      }
    }

    public static List<L10Recurrence.GoalRecurrenceRecord> GetRecurrencesContainingRocks(UserOrganizationModel caller, IReadOnlyList<long> rockIds)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          return GetRecurrencesContainingRocks(s, perms, rockIds);
        }
      }
    }

    public static List<L10Recurrence.GoalRecurrenceRecord> GetRecurrencesContainingRock(ISession s, PermissionsUtility perms, long rockId) {
      perms.ViewRock(rockId);
      L10Recurrence recurAlias = null;
      return s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .JoinAlias(x => x.L10Recurrence, () => recurAlias)
        .Where(x => x.DeleteTime == null && x.ForRock.Id == rockId && recurAlias.DeleteTime == null)
        .Select(x => x.L10Recurrence.Id, x => recurAlias.RockType, x => recurAlias.Name, x => recurAlias.TeamType, x => x.VtoRock, x=>x.Id)
        .List<object[]>()
        .Select(x => new L10Recurrence.GoalRecurrenceRecord() {
          RecurrenceId = (long)x[0],
          MilestonesEnabled = ((L10RockType)x[1]) == L10RockType.Milestones,
          Name = x[2] + "",
          TeamType = ((L10TeamType)x[3]),
          VtoRock = (bool)x[4],
          RecurrenceRockId = (long)x[5]
        }).ToList();
    }

    public static List<L10Recurrence.GoalRecurrenceRecord> GetRecurrencesContainingRocks(ISession s, PermissionsUtility perms, IReadOnlyList<long> rockIds)
    {
      var permittedRockIds = rockIds.Where(rockId => perms.TryViewRock(rockId)).ToArray();
      L10Recurrence recurAlias = null;
      return s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .JoinAlias(x => x.L10Recurrence, () => recurAlias)
        .WhereRestrictionOn(x => x.ForRock.Id).IsIn(permittedRockIds)
        .And(x => x.DeleteTime == null && recurAlias.DeleteTime == null)
        .Select(x => x.L10Recurrence.Id, x => recurAlias.RockType, x => recurAlias.Name, x => recurAlias.TeamType, x => x.VtoRock, x => x.Id, x => x.ForRock.Id)
        .List<object[]>()
        .Select(x => new L10Recurrence.GoalRecurrenceRecord()
        {
          RecurrenceId = (long)x[0],
          MilestonesEnabled = ((L10RockType)x[1]) == L10RockType.Milestones,
          Name = x[2] + "",
          TeamType = ((L10TeamType)x[3]),
          VtoRock = (bool)x[4],
          RecurrenceRockId = (long)x[5],
          RockId = (long)x[6],
        }).ToList();
    }

    public static async Task EditMilestone(UserOrganizationModel caller, long milestoneId, string name = null, DateTime? duedate = null, bool? required = null, MilestoneStatus? status = null, string connectionId = null, BloomMilestoneStatus? bloomStatus = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create(connectionId)) {
            var ms = s.Get<Milestone>(milestoneId);
            var perm = PermissionsUtility.Create(s, caller);
            perm.EditRock(ms.RockId, false);
            ms.Name = name ?? ms.Name;
            ms.DueDate = duedate ?? ms.DueDate;
            ms.Required = required ?? ms.Required;
            if (status != null) {
              if (status == MilestoneStatus.Done && ms.Status != MilestoneStatus.Done) {
                ms.CompleteTime = DateTime.UtcNow;
              }

              if (status == MilestoneStatus.NotDone && ms.Status != MilestoneStatus.NotDone) {
                ms.CompleteTime = null;
              }
            }

            ms.Status = status ?? ms.Status;
            ms.bloomStatus = bloomStatus ?? ms.bloomStatus;
            s.Update(ms);
            var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.ForRock.Id == ms.RockId).Select(x => x.L10Recurrence.Id).List<long>();
            tx.Commit();
            s.Flush();
            rt.UpdateRecurrences(recurrenceIds).Call("setMilestone", ms);
            await HooksRegistry.Each<IMilestoneHook>((ses, x) => x.UpdateMilestone(ses, caller, ms, new IMilestoneHookUpdates()));
          }
        }
      }
    }

    public static async Task DeleteMilestone(UserOrganizationModel caller, long milestoneId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            var ms = s.Get<Milestone>(milestoneId);
            var perm = PermissionsUtility.Create(s, caller);
            perm.EditRock(ms.RockId, false);
            ms.DeleteTime = ms.DeleteTime ?? DateTime.UtcNow;
            s.Update(ms);
            var recurrenceIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.DeleteTime == null && x.ForRock.Id == ms.RockId).Select(x => x.L10Recurrence.Id).List<long>();
            tx.Commit();
            s.Flush();
            rt.UpdateRecurrences(recurrenceIds).Call("deleteMilestone", milestoneId);
            await HooksRegistry.Each<IMilestoneHook>((ses, x) => x.UpdateMilestone(ses, caller, ms, new IMilestoneHookUpdates() { IsDeleted = true }));
          }
        }
      }
    }

    public static Milestone GetMilestone(UserOrganizationModel caller, long milestoneId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller);
          var ms = s.Get<Milestone>(milestoneId);
          perm.ViewRock(ms.RockId);
          ms._Rock = s.Get<RockModel>(ms.RockId);
          return ms;
        }
      }
    }

    public static List<Milestone> GetMilestonesForRock(UserOrganizationModel caller, long rockId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ViewRock(rockId);
          //Debug.WriteLine("CONNECTION:"+ ((SingleRequestSession)s).GetGuid());
          try {
            var ms = s.QueryOver<Milestone>().Where(x => x.RockId == rockId && x.DeleteTime == null).List().ToList();
            return ms;
          } catch (Exception e) {
            int a = 0;
            throw;
          }
        }
      }
    }

    public static Dictionary<long, List<Milestone>> GetMilestonesForRocks(UserOrganizationModel caller, IEnumerable<long> rockIds) {
      if (rockIds==null || !rockIds.Any())
        return new Dictionary<long, List<Milestone>>();

      var o = new Dictionary<long, List<Milestone>>();
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var rIds = rockIds.Distinct().ToList();
          var perms = PermissionsUtility.Create(s, caller);
          var permittedRockIds = new List<long>();

          foreach (var rid in rIds) {
            o.Add(rid, new List<Milestone>());
            try {
              perms.ViewRock(rid);
              permittedRockIds.Add(rid);
            } catch (PermissionsException e) {
            }
          }
          try {
            var ms = s.QueryOver<Milestone>().Where(x => x.DeleteTime == null)
              .WhereRestrictionOn(x => x.RockId).IsIn(permittedRockIds)
              .List()
              .ToList();
            foreach (var rockMilestones in ms.GroupBy(x => x.RockId)) {
              o[rockMilestones.Key] = rockMilestones.ToList();
            }
            return o;
          } catch (Exception e) {
            int a = 0;
            throw;
          }
        }
      }
    }

    public static RockAndMilestones GetRockAndMilestones(UserOrganizationModel caller, long rockId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ViewRock(rockId);
          var ms = s.QueryOver<Milestone>().Where(x => x.RockId == rockId && x.DeleteTime == null).List().ToList();
          var rock = s.Get<RockModel>(rockId);
          var l10Ids = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rockId && x.DeleteTime == null).Select(x => x.L10Recurrence.Id).List<long>().ToList();
          var rockTypes = s.QueryOver<L10Recurrence>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(l10Ids).Select(x => x.RockType).List<L10RockType?>().ToList();
          return new RockAndMilestones() { Milestones = ms, Rock = rock, AnyMilestoneMeetings = rockTypes.Any(x => x == L10RockType.Milestones) || ms.Any() };
        }
      }
    }

    #endregion
    #region Getters
    public static RockModel GetRock(UserOrganizationModel caller, long rockId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller).ViewRock(rockId);
          var rock = s.Get<RockModel>(rockId);
          return rock;
        }
      }
    }

    public static List<RockModel> GetRocksForUser(UserOrganizationModel caller, long forUserId, DateRange range = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller);
          return GetRocksForUser(s.ToQueryProvider(true), perm, forUserId, range);
        }
      }
    }

    public static List<RockModel> GetRocksForUser(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId, DateRange range) {
      perms.ViewUserOrganization(forUserId, false);
      return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId).FilterRange(range).ToList();
    }

    [Obsolete("Use GetRocksForUser instead")]
    public static List<RockModel> GetAllRocks_AvoidUsing(UserOrganizationModel caller, long forUserId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller);
          return GetAllRocks_AvoidUsing(s.ToQueryProvider(true), perm, forUserId);
        }
      }
    }

    [Obsolete("Use GetRocksForUser instead")]
    public static List<RockModel> GetAllRocks_AvoidUsing(ISession s, PermissionsUtility perms, long forUserId) {
      return GetAllRocks_AvoidUsing(s.ToQueryProvider(true), perms, forUserId);
    }

    [Obsolete("Use GetRocksForUser instead")]
    public static List<RockModel> GetAllRocks_AvoidUsing(AbstractQuery queryProvider, PermissionsUtility perms, long forUserId) {
      perms.Or(x => x.ViewUserOrganization(forUserId, false), x => x.ViewOrganization(forUserId));
      return queryProvider.Where<RockModel>(x => x.ForUserId == forUserId && x.DeleteTime == null);
    }

    public static List<RockModel> GetAllVisibleRocksAtOrganization(ISession s, PermissionsUtility perm, long orgId, bool populateUsers) {
      perm.ViewOrganization(orgId);
      var caller = perm.GetCaller();
      IQueryOver<RockModel, RockModel> q;
      var managing = caller.Organization.Id == orgId && caller.ManagingOrganization;
      if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou && !managing) {
        var userIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);
        q = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).WhereRestrictionOn(x => x.ForUserId).IsIn(userIds);
      } else {
        q = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null);
      }

      if (populateUsers) {
        q = q.Fetch(x => x.AccountableUser).Eager;
      }

      return q.List().ToList();
    }

    public static List<RockModel> GetAllVisibleRocksAtOrganization(UserOrganizationModel caller, long orgId, bool populateUsers) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          //Todo permissions not enough
          var perm = PermissionsUtility.Create(s, caller);
          return GetAllVisibleRocksAtOrganization(s, perm, orgId, populateUsers);
        }
      }
    }

    public static L10Meeting.L10Meeting_Rock GetRockInMeeting(UserOrganizationModel caller, long rockId, long meetingId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var p = PermissionsUtility.Create(s, caller).ViewL10Meeting(meetingId);
          var found = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId && x.ForRock.Id == rockId).Take(1).SingleOrDefault();
          if (found == null) {
            throw new PermissionsException("Goal not available.");
          }

          var a = found.ForRock.AccountableUser.GetName();
          var b = found.ForRock.AccountableUser.ImageUrl(true);
          var c = found.Completion;
          var d = found.L10Meeting.CreateTime;
          return found;
        }
      }
    }

    public static List<RockModel> GetPotentialMeetingRocks(UserOrganizationModel caller, long recurrenceId, bool loadUsers) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
          var rocks = s.QueryOver<RockModel>();
          if (loadUsers) {
            rocks = rocks.Fetch(x => x.AccountableUser).Eager;
          }

          var userIds = L10Accessor.GetL10Recurrence(s, perms, recurrenceId, LoadMeeting.True())._DefaultAttendees.Select(x => x.User.Id).ToList();
          if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
            userIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id).Intersect(userIds).ToList();
          }

          return rocks.Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.AccountableUser.Id).IsIn(userIds).List().ToList();
        }
      }
    }

    public static List<RockModel> GetArchivedRocks(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).EditUserDetails(caller.Id);
          var archived = s.QueryOver<RockModel>().Where(x => x.Archived == true && x.AccountableUser.Id == userId).List().ToList();
          return archived;
        }
      }
    }

    public static Csv Listing(UserOrganizationModel caller, long organizationId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);
          var rocksQ = s.QueryOver<RockModel>().Where(x => (x.DeleteTime == null || x.Archived) && x.OrganizationId == organizationId);
          var rocks = rocksQ.List().ToList();
          var csv = new Csv();
          csv.SetTitle("Goals");
          foreach (var r in rocks) {
            csv.Add("" + r.Id, "Goal", r.Rock ?? "");
            csv.Add("" + r.Id, "Owner", r.AccountableUser.GetName() ?? "not specified");
            csv.Add("" + r.Id, "Status", "" + RockStateExtensions.GetCompletionVal(r.Completion));
            csv.Add("" + r.Id, "CreateTime", "" + r.CreateTime);
            csv.Add("" + r.Id, "CompleteTime", "" + r.CompleteTime);
            csv.Add("" + r.Id, "Archived", "" + r.Archived);
          }

          return csv;
        }
      }
    }

    public static List<L10Recurrence.L10Recurrence_Rocks> GetRecurrenceRocksForUser(UserOrganizationModel caller, long userId, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
          RockModel rock = null;
          var q = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().JoinAlias(x => x.ForRock, () => rock).Where(x => x.L10Recurrence.Id == recurrenceId && rock.ForUserId == userId);
          q = q.Where(x => x.DeleteTime == null && rock.DeleteTime == null);
          var found = q.Fetch(x => x.ForRock).Eager.List().ToList();
          foreach (var f in found) {
            if (f.ForRock.AccountableUser != null) {
              var a = f.ForRock.AccountableUser.GetName();
              var b = f.ForRock.AccountableUser.ImageUrl(true, ImageSize._32);
            }
          }

          return found;
        }
      }
    }

    #endregion
    public static async Task<RockModel> CreateRock(UserOrganizationModel caller, long ownerId, string message = null, long? templateId = null, string notes = null, DateTime? dueDate = null, RockState? completion = null, string padId = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var rock = await CreateRock(s, perms, ownerId, message, templateId, notes: notes, dueDate: dueDate, completion: completion, padId: padId);
          tx.Commit();
          s.Flush();
          return rock;
        }
      }
    }

    public static async Task<RockModel> CreateRock(ISession s, PermissionsUtility perms, long ownerId, string message = null, long? templateId = null, long? permittedForRecurrenceId = null, string notes = null, DateTime? dueDate = null, RockState? completion = null, string padId = null) {
      perms.CreateRocksForUser(ownerId, permittedForRecurrenceId);
      var owner = s.Get<UserOrganizationModel>(ownerId);
      var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
      var rock = new RockModel() { CreateTime = DateTime.UtcNow, OrganizationId = owner.Organization.Id, Category = category, OnlyAsk = AboutType.Self, ForUserId = ownerId, AccountableUser = s.Load<UserOrganizationModel>(ownerId), Rock = message?.Replace("&amp;", "&"), FromTemplateItemId = templateId, DueDate = dueDate, Completion = completion ?? RockState.OnTrack };
      if (padId != null) {
        rock.PadId = padId;
        s.Save(rock);
      } else {
        rock.PadId = Guid.NewGuid().ToString();
        s.Save(rock);
        await PadAccessor.CreatePad(rock.PadId, notes);
      }

      var cc = perms.GetCaller();
      await HooksRegistry.Each<IRockHook>((ss, x) => x.CreateRock(ss, cc, rock));
      return rock;
    }

    public static async Task<EditRockViewModel> BuildRockVM(UserOrganizationModel caller, long? rockId, TermsCollection terms, List<SelectListItem> potentialUsers = null, List<SelectListItem> possibleRecurrences = null, bool populateManaging = false, long? recurrenceId = null, long? defaultUserId = null, bool lockUser = false) {
      if (recurrenceId<=0) {
        recurrenceId = null;
      }

      defaultUserId = defaultUserId ?? caller.Id;
      RockModel rock = null;
      if (rockId != null) {
        rock = RockAccessor.GetRock(caller, rockId.Value);
      }

      if (potentialUsers == null) {
        potentialUsers = SelectListAccessor.GetUsersWeCanCreateRocksFor(caller, terms, x => (rock != null && x.Id == rock.ForUserId) || (rock == null && x.Id == defaultUserId));
      }

      var defaultSelectedUser = potentialUsers.LastOrDefault(x => x.Selected);
      if (defaultSelectedUser == null) {
        defaultSelectedUser = potentialUsers.First();
      }

      //var run = false;
      //if (run) {
      //  var c = 4;
      //  for (var i = 0; i < c; i++) {
      //    potentialUsers.AddRange(potentialUsers);
      //  }
      //}


      if (recurrenceId!=null) {
        var userInMeeting = await L10Accessor.TestAttendees(caller, recurrenceId.Value, potentialUsers.SelectNoException(x => long.Parse(x.Value)).ToList());

        var toAdd = new List<SelectListItem>();
        var meetingGroup = new SelectListGroup() { Name = "In Meeting" };
        var everyoneGroup = new SelectListGroup() { Name = "Everyone" };
        foreach (var pu in potentialUsers) {
          var userId = long.Parse(pu.Value);
          if (userInMeeting[userId]) {
            //promote to meeting group
            pu.Group = meetingGroup;
            //add another to the everyone group
            toAdd.Add(new SelectListItem() {
              Disabled = pu.Disabled,
              Selected = false,
              Text = pu.Text,
              Value = pu.Value,
              Group = everyoneGroup
            });
          } else {
            //make sure in the everyone group
            pu.Group = everyoneGroup;
          }
        }
        potentialUsers.AddRange(toAdd);
        potentialUsers = potentialUsers.OrderBy(x => x.Group.NotNull(y => y.Name == meetingGroup.Name)).ToList();
      }


      if (populateManaging) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);
            foreach (var item in potentialUsers) {
              if (!perms.IsPermitted(x => x.CanAdminMeetingItemsForUser(Convert.ToInt64(item.Value), recurrenceId.Value))) {
                item.Disabled = true;
                item.Text = item.Text + "(You do not manage this user)";
              }
            }
          }
        }
      }

      var rocksStates = new List<EditRockViewModel.RockStatesVm>();
      rocksStates.Add(new EditRockViewModel.RockStatesVm { id = "AtRisk", name = "Off Track" });
      rocksStates.Add(new EditRockViewModel.RockStatesVm { id = "OnTrack", name = "On Track" });
      rocksStates.Add(new EditRockViewModel.RockStatesVm { id = "Complete", name = "Done" });
      var rockTypes = new List<EditRockViewModel.RockTypesVm>();
      rockTypes.Add(new EditRockViewModel.RockTypesVm { id = "False", name = "Individual" });
      var qtr = await QuarterlyAccessor.GetQuarterDoNotGenerate(caller, caller.Organization.Id);
      var endDate = DateTime.UtcNow.AddDays(90);
      if (qtr != null) {
        endDate = qtr.EndDate ?? endDate;
      }

      if (endDate < DateTime.UtcNow) {
        endDate = DateTime.UtcNow.AddDays(90);
      }

      //CONSTRUCT THE MODEL
      var model = new EditRockViewModel() {
        AccountableUser = defaultSelectedUser.Value.ToLong(),
        PossibleRecurrences = possibleRecurrences,
        PotentialUsers = potentialUsers,
        RecurrenceIds = new long[] { },
        DueDate = endDate,
        Completion = RockState.OnTrack,
        Milestones = new List<EditRockViewModel.MilestoneVM>(),
        RockStates = rocksStates,
        RockTypes = rockTypes,
        CanArchive = false,
        LockUser = lockUser,
        PadId = Guid.NewGuid().ToString()
      };
      //ALTER THE MODEL IF THE GOAL ALREADY EXISTS
      if (rock != null) {
        List<SelectListItem> editableUsers;
        if (recurrenceId != null) {
          editableUsers = (await L10Accessor.GetAttendees(caller, recurrenceId.Value)).ToSelectList(x => x.GetName(), x => x.Id, rock.ForUserId);
        } else {
          editableUsers = SelectListAccessor.GetUsersWeCanCreateRocksFor(caller, terms, x => x.Id == rock.ForUserId);
        }

        var selectedRecurrences = RockAccessor.GetRecurrencesContainingRock(caller, rockId.Value);
        var rockAndMs = RockAccessor.GetRockAndMilestones(caller, rockId.Value);
        if (possibleRecurrences == null) {
          possibleRecurrences = SelectListAccessor.GetL10RecurrenceAdminable(caller, caller.Id, x => selectedRecurrences.Any(y => x.CanAdmin && y.RecurrenceId == x.Id));
        }

        if (selectedRecurrences.Any(x => x.TeamType == L10TeamType.LeadershipTeam)) {
          rockTypes.Add(new EditRockViewModel.RockTypesVm { id = "True", name = "Company (Added to your team's "+terms.GetTerm(TermKey.BusinessPlan)+")" });
        } else {
          rockTypes.Add(new EditRockViewModel.RockTypesVm { id = "True", name = "Departmental (Added to your team's "+terms.GetTerm(TermKey.BusinessPlan)+")" });
        }

        rockTypes.Insert(0, new EditRockViewModel.RockTypesVm { id = "", name = "Unchanged" });
        model.Id = rockId.Value;
        model.PossibleRecurrences = possibleRecurrences;
        model.DueDate = rock.DueDate ?? model.DueDate;
        model.AccountableUser = rock.ForUserId;
        model.Title = rock.Name;
        model.RecurrenceIds = selectedRecurrences.Select(x => x.RecurrenceId).ToArray();
        model.Completion = rock.Completion;
        model.RockTypes = rockTypes;
        model.Milestones = rockAndMs.Milestones.Select(x => new EditRockViewModel.MilestoneVM() { Complete = x.Status == Models.Rocks.MilestoneStatus.Done, DueDate = x.DueDate, Name = x.Name, Id = x.Id, }).OrderBy(x => x.DueDate).ToList();
        model.CanArchive = possibleRecurrences.Where(x => x.Selected).All(x => !x.Disabled);
        try {
          model.CannotAttachToMeeting = selectedRecurrences != null && selectedRecurrences.Any() && !possibleRecurrences.Any(x => x.Selected);
        } catch (Exception e) {
        }

        //PROBABLY WRONG
        var vtoRock = false;
        model.AddToVTO = null;
        model.PadId = rock.PadId;
        if (!editableUsers.Any(x => !x.Disabled) || !PermissionsAccessor.IsPermitted(caller, x => x.EditRock(rockId.Value, false))) {
          model.CanEdit = false;
          model.CanArchive = false;
        }
      }

      //IF WE'RE CREATING THE GOAL
      if (rock == null) {
        if (possibleRecurrences == null) {
          possibleRecurrences = SelectListAccessor.GetL10RecurrenceAdminable(caller, caller.Id, x => x.CanAdmin && x.Id == recurrenceId);
        }

        model.PossibleRecurrences = possibleRecurrences;
        if (recurrenceId != null) {
          model.RecurrenceIds = new long[] { recurrenceId.Value };
          if (possibleRecurrences.Any(x => x.Value == "" + recurrenceId.Value && x.Disabled)) {
            model.RecurrenceIds = new long[] { };
          }
        }

        if (!potentialUsers.Any(x => !x.Disabled)) {
          model.CanCreate = false;
        }

        model.IsCreate = true;
        rockTypes.Add(new EditRockViewModel.RockTypesVm { id = "True", name = "Add to your teams' "+terms.GetTerm(TermKey.BusinessPlan) });
        model.RockTypes = rockTypes;
        try {
          model.CannotAttachToMeeting = recurrenceId != null && !possibleRecurrences.Any(x => x.Selected);
        } catch (Exception e) {
        }
      }

      return model;
    }

    public static async Task UpdateRock(UserOrganizationModel caller, long rockId, string message = null, long? ownerId = null, RockState? completion = null, DateTime? dueDate = null, DateTime? now = null) {
      var executor = new UpdateRockExecutor(rockId, message, ownerId, completion, dueDate, now);
      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateRockCompletion(rockId), executor);
    }

    //[Obsolete("Update for StrictlyAfter", true)]
    //[Untested("StrictlyAfter")]
    /// <summary>
    /// SyncAction.UpdateRockCompletion(rockId)
    /// </summary>
    /// <param name = "s"></param>
    /// <param name = "perms"></param>
    /// <param name = "rockId"></param>
    /// <param name = "message"></param>
    /// <param name = "ownerId"></param>
    /// <param name = "completion"></param>
    /// <param name = "dueDate"></param>
    /// <param name = "now"></param>
    /// <returns></returns>
    ///
    [Obsolete("Use StrictlyAfterExecutors", true)]
    public static async Task UpdateRock(IOrderedSession s, PermissionsUtility perms, long rockId, string message = null, long? ownerId = null, RockState? completion = null, DateTime? dueDate = null, DateTime? now = null) {
      //PERMISSIONS AT THE END OF THE FUNCTION
      //Make sure you update shouldExecute if you change this method.

      //bool shouldExecute = false;
      //now = now ?? DateTime.UtcNow;
      //var updates = new IRockHookUpdates();
      //bool anyNonStausUpdates = false;
      //var rock = s.Get<RockModel>(rockId);
      //message = message?.Replace("&amp;", "&");
      //if (message != null && rock.Name != message) {
      //  perms.EditRock(rockId, false);
      //  shouldExecute = true;
      //  rock.Name = message;
      //  updates.MessageChanged = true;
      //  anyNonStausUpdates = true;
      //}

      //updates.OriginalAccountableUserId = rock.ForUserId;
      //if (ownerId != null && rock.ForUserId != ownerId) {
      //  perms.EditRock(rockId, false);  /*Must be done here. Permissions need to be checked everywhere*/
      //  perms.CreateRocksForUser(ownerId.Value);
      //  shouldExecute = true;
      //  rock.AccountableUser = s.Load<UserOrganizationModel>(ownerId.Value);
      //  rock.ForUserId = ownerId.Value;
      //  updates.AccountableUserChanged = true;
      //  anyNonStausUpdates = true;
      //}

      //if (dueDate != null && rock.DueDate != dueDate) {
      //  perms.EditRock(rockId, false);  /*Must be done here. Permissions need to be checked everywhere*/
      //  shouldExecute = true;
      //  rock.DueDate = dueDate;
      //  updates.DueDateChanged = true;
      //  anyNonStausUpdates = true;
      //}

      //if (completion != null) {
      //  if (completion != RockState.Indeterminate && rock.Completion != completion) {
      //    if (completion == RockState.Complete) {
      //      rock.CompleteTime = now;
      //    }

      //    perms.EditRock(rockId, true); /*Must be done here. Permissions need to be checked everywhere*/
      //    shouldExecute = true;
      //    shouldExecute = true;
      //    rock.Completion = completion.Value;
      //    updates.StatusChanged = true;
      //  } else if ((completion == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
      //    perms.EditRock(rockId, true); /*Must be done here. Permissions need to be checked everywhere*/
      //    shouldExecute = true;
      //    rock.Completion = RockState.Indeterminate;
      //    rock.CompleteTime = null;
      //    updates.StatusChanged = true;
      //  }
      //}
      //if (shouldExecute) {
      //  perms.EditRock(rockId, !anyNonStausUpdates); /*Permissions are also with the Update method. This call cannot be relied on however.*/
      //  s.Update(rock);
      //  var cc = perms.GetCaller();
      //  await HooksRegistry.Each<IRockHook>((ss, x) => x.UpdateRock(ss, cc, rock, updates));
      //}
    }

    [Obsolete("Use DetachRockFromAllMeetings instead", true)]
    public static async Task ArchiveRock(UserOrganizationModel caller, long rockId, DateTime? now = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await ArchiveRock(s, perms, rockId, now);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task DetachRockFromAllMeetings(UserOrganizationModel caller, long rockId, DateTime? now = null, bool? archived = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          DetachRockFromAllMeetings(s, perms, rockId, now, archived);
          tx.Commit();
          s.Flush();
        }
      }
    }


    public static async Task DetachRockFromAllMeetings(ISession s, PermissionsUtility perms, long rockId, DateTime? now = null, bool? archived = null) {
      perms.EditRock(rockId, false);
      var recurIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
        .Where(x => x.ForRock.Id==rockId && x.DeleteTime ==null)
        .Select(x => x.L10Recurrence.Id)
        .List<long>().ToList();

      var detachTime = DateTime.UtcNow;

      await using (var rt = RealTimeUtility.Create()) {
        foreach (var rid in recurIds) {
          try {
            await L10Accessor.RemoveRock(s, perms, rt, rid, rockId, detachTime: detachTime, true, now, archived);
          } catch (PermissionsException e) {
            //probably should do something different here.
          }
        }
      }

    }

    [Obsolete("Use DetachRockFromAllMeetings instead")]
    public static async Task ArchiveRock(ISession s, PermissionsUtility perm, long rockId, DateTime? now = null) {
      perm.EditRock(rockId, false);
      now = now  ?? DateTime.UtcNow;

      //await DetachRockFromAllMeetings(s, perm, rockId, now);

      var rock = s.Get<RockModel>(rockId);
      rock.Archived = true;
      rock.DeleteTime = now.Value;
      s.Update(rock);


      if (rock.ForUserId != null) {
        s.Get<UserOrganizationModel>(rock.ForUserId).UpdateCache(s);
      }


      await HooksRegistry.Each<IRockHook>((ss, x) => x.ArchiveRock(ss, rock, false));
    }

    public static async Task UnArchiveRock(UserOrganizationModel caller, long rockId, DateTime? now = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await UnArchiveRock(s, perms, rockId);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task UnArchiveRock(ISession s, PermissionsUtility perm, long rockId) {
      perm.EditRock_UnArchive(rockId);
      var rock = s.Get<RockModel>(rockId);
      rock.Archived = false;
      rock.DeleteTime = null;
      s.Update(rock);

      if (rock.ForUserId != null) {
        s.Get<UserOrganizationModel>(rock.ForUserId).UpdateCache(s);
      }


      await HooksRegistry.Each<IRockHook>((ss, x) => x.UnArchiveRock(ss, rock, false));
    }

    public static async Task UndeleteRock(UserOrganizationModel caller, long rockId, bool? archived = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var rock = s.Get<RockModel>(rockId);
          var perm = PermissionsUtility.Create(s, caller).EditRock_UnArchive(rock.Id);
          var deleteTime = rock.DeleteTime;
          rock.DeleteTime = null;
          rock.Archived = false;
          s.Update(rock);
          var rrs = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rockId && x.DeleteTime > deleteTime.Value.AddMinutes(-3) && x.DeleteTime < deleteTime.Value.AddMinutes(3) && x.DeleteTime != null).List().ToList();
          foreach (var rr in rrs) {
            rr.DeleteTime = null;
            rr.Archived = archived;
            s.Update(rr);
          }

          var mrs = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.ForRock.Id == rockId && x.DeleteTime > deleteTime.Value.AddMinutes(-3) && x.DeleteTime < deleteTime.Value.AddMinutes(3) && x.DeleteTime != null).List().ToList();
          foreach (var mr in mrs) {
            mr.DeleteTime = null;
            mr.Archived = archived;
            s.Update(mr);
          }

          await HooksRegistry.Each<IRockHook>((ss, x) => x.UndeleteRock(ss, rock));
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task<List<RockAndMilestones>> AllVisibleRocksAndMilestonesAtOrganization(UserOrganizationModel caller, long orgId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var rocks = GetAllVisibleRocksAtOrganization(s, perms, orgId, true);
          var rockIds = rocks.Select(x => x.Id).Distinct().ToList();
          var milestones = s.QueryOver<Milestone>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.RockId).IsIn(rockIds).List().GroupBy(x => x.RockId).ToDefaultDictionary(x => x.Key, x => x.ToList(), x => new List<Milestone>());
          return rocks.Select(x => new RockAndMilestones() { Rock = x, Milestones = milestones[x.Id] }).ToList();
        }
      }
    }

    [Untested("EditRocks", "AttachRock", "Does this correctly add to L10", "Does this correctly add to VTO", "Remove the Company Goal flag")]
    [Obsolete("stop using, rock updates not updated")]
    public static async Task<List<PermissionsException>> EditRocks(UserOrganizationModel caller, long userId, List<RockModel> rocks, bool updateOutstandingReviews, bool updateAllL10s) {
      var output = new List<PermissionsException>();
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          if (rocks.Any(x => x.ForUserId != userId)) {
            throw new PermissionsException("Goal UserId does not match UserId");
          }

          var perm = PermissionsUtility.Create(s, caller);
          var user = s.Get<UserOrganizationModel>(userId);
          long orgId = -1;
          perm.EditQuestionForUser(userId);
          orgId = user.Organization.Id;
          List<ReviewsModel> outstanding = null;
          if (updateOutstandingReviews) {
            outstanding = ReviewAccessor.OutstandingReviewsForOrganization_Unsafe(s, orgId);
          }

          List<L10Recurrence.L10Recurrence_Attendee> allL10s = null;
          if (updateAllL10s) {
            allL10s = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userId).List().Where(x => x.L10Recurrence.DeleteTime == null).ToList();
          }

          var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
          foreach (var r in rocks) {
            r.OnlyAsk = AboutType.Self;
            r.Category = category;
            r.OrganizationId = orgId;
            r.Period = r.PeriodId == null ? null : s.Get<PeriodModel>(r.PeriodId);
            r.AccountableUser = s.Load<UserOrganizationModel>(r.ForUserId);
            var added = r.Id == 0;
            if (added) {
              s.Save(r);
              var cc = perm.GetCaller();
              await HooksRegistry.Each<IRockHook>((ses, x) => x.CreateRock(ses, cc, r));
            } else {
              if (r.DeleteTime != null && r.Archived) {
                await ArchiveRock(s, perm, r.Id, r.DeleteTime);
              } else {
                var updates = new IRockHookUpdates();
                s.Merge(r);
                var cc = perm.GetCaller();
                await HooksRegistry.Each<IRockHook>((ses, x) => x.UpdateRock(ses, cc, r, updates));
              }
            }

            if (updateOutstandingReviews && added) {
              var r1 = r;
            }

            if (updateAllL10s && added) {
              var r1 = r;
              foreach (var o in allL10s.Select(x => x.L10Recurrence)) {
                if (o.OrganizationId != caller.Organization.Id) {
                  throw new PermissionsException("Cannot access the Weekly Meeting");
                }

                perm.UnsafeAllow(PermItem.AccessLevel.View, PermItem.ResourceType.L10Recurrence, o.Id);
                perm.UnsafeAllow(PermItem.AccessLevel.Edit, PermItem.ResourceType.L10Recurrence, o.Id);
                await L10Accessor.AttachRock(s, perm, o.Id, r1.Id, false, AttachRockType.Existing);
                r1._AddedToL10 = false;
                r1._AddedToVTO = false;
              }
            }
          }

          user.UpdateCache(s);
          tx.Commit();
          s.Flush();
          return output;
        }
      }
    }

  }
}

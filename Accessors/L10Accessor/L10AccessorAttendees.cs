using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Hangfire;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using RadialReview.Exceptions.MeetingExceptions;
using NHibernate.Criterion;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using System.Diagnostics;
using RadialReview.Repositories;
using RestSharp.Extensions;

namespace RadialReview.Accessors {
  public partial class L10Accessor : BaseAccessor {
    #region Attendees
    public static async Task<List<UserOrganizationModel>> GetAttendees(UserOrganizationModel caller, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

            var usersRecur = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
              .Fetch(x => x.User).Eager
              .List().ToList();
            var users = usersRecur.Select(x => x.User).ToList();
            foreach (var u in users) {
              try {
                var a = u.GetName();
              } catch (Exception) {
              }
            }
            return users;
          }
        }
      }
    }

    public static async Task<List<long>> GetAttendeeIdsByRecurrenceIdAsync(UserOrganizationModel caller, long recurrenceId) {
      using (var session = HibernateSession.GetCurrentSession()) {
        var perms = PermissionsUtility.Create(session, caller).ViewL10Recurrence(recurrenceId);

        L10Recurrence.L10Recurrence_Attendee l10RecurrenceAttendeeAlias = null;

        var attendeeIds = await session.QueryOver<L10Recurrence.L10Recurrence_Attendee>(() => l10RecurrenceAttendeeAlias)
                                 .Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
                                 .Select(x => x.User.Id)
                                 .ListAsync<long>();

        return attendeeIds.ToList();
      }
    }

    public static async Task ResetAttendeesHasVotedFlag(UserOrganizationModel caller, long recurrenceId, bool hasVoted = false)
    {

      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          await using (var rt = RealTimeUtility.Create())
          {
            var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);


            var usersRecur = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
              .Fetch(x => x.User).Eager
              .List().ToList();

            List<L10Recurrence.L10Recurrence_Attendee> updatedAttendees = new List<L10Recurrence.L10Recurrence_Attendee>();
            foreach (var attendee in usersRecur)
            {
              if (attendee.HasSubmittedVotes != hasVoted)
              {
                attendee.HasSubmittedVotes = hasVoted;
                updatedAttendees.Add(attendee);
                s.Update(attendee);
                s.Flush();
              }
            }
            tx.Commit();

            if (tx.WasCommitted)
            {
              foreach (var attendee in updatedAttendees)
              {
                await HooksRegistry.Each<IMeetingAttendeeHooks>((ses, x) => x.UpdateAttendee(ses, caller, recurrenceId, attendee));
              }
            }

          }
        }
      }

    }

    public static async Task<List<L10Recurrence.L10Recurrence_Attendee>> GetRecurrenceAttendees(UserOrganizationModel caller, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

        L10Recurrence.L10Recurrence_Attendee attendeeAlias = null;
        var attendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
        .Fetch(x => x.User).Eager
        .List()
        .ToList();
        return attendees;
      }
    }

    public static List<L10Recurrence.L10Recurrence_Attendee> GetRecurrenceAttendeesUnsafe(long recurrenceId, ISession session)
    {
      return session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
      .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
      .List().ToList();
    }

    public static async Task SetIsUsingV3ForAttendee(UserOrganizationModel caller, long attendeeId, long recurrenceId, bool isUsingV3 = false)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        var attendee = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.User.Id == attendeeId)
        .Fetch(x => x.User).Eager
        .SingleOrDefault();
        if (attendee != null) {
          using (var transaction = session.BeginTransaction()) {
            attendee.IsUsingV3 = isUsingV3;
            session.Update(attendee);
            transaction.Commit();
          }
        }
      }
    }

    public static async Task SetIsUsingV3ForAttendess(UserOrganizationModel caller, List<L10Recurrence.L10Recurrence_Attendee> attendees, bool isUsingV3 = false) {
      using (var session = HibernateSession.GetCurrentSession()) {
        using (var transaction = session.BeginTransaction()) {
          foreach (var attendee in attendees) {
            attendee.IsUsingV3 = isUsingV3;
            session.Update(attendee);
          }
          transaction.Commit();
        }
      }
    }

    public static async Task<Dictionary<long, bool>> TestAttendees(UserOrganizationModel caller, long recurrenceId, IEnumerable<long> userIds) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);
          var matchedUserIds = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
            .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
            .Select(x => x.User.Id)
            .List<long>().ToList();

          var o = new Dictionary<long, bool>();
          return userIds.ToDictionary(x => x, x => matchedUserIds.Any(y => y==x));
        }
      }
    }


    public static async Task OrderAngularMeasurable(UserOrganizationModel caller, long measurableId, long recurrenceId, int oldOrder, int newOrder) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            var perms = PermissionsUtility.Create(s, caller);
            perms.EditL10Recurrence(recurrenceId);
            if (measurableId > 0) {
              perms.EditMeasurable(measurableId);
            }

            var recurMeasureables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
                    .Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
                    .List().ToList();
            recurMeasureables = recurMeasureables.Where(x => x.Measurable == null || x.Measurable.DeleteTime == null).ToList();

            if (measurableId < 0) { //Dividers are negative..
              measurableId = -1 * measurableId;
            }

            var ctx = Reordering.CreateRecurrence(recurMeasureables, measurableId, recurrenceId, oldOrder, newOrder, x => x._Ordering, x => (x.Measurable == null) ? x.Id : x.Measurable.Id);
            ctx.ApplyReorder(rt, s, (id, order, item) => AngularMeasurable.Create(item));

            var measurablesOrderList = new List<AngularMeasurableOrder>();
            foreach (var m in recurMeasureables) {
              if (m.Measurable != null) {
                //BAD. need to correctly handle when there is a divider.
                measurablesOrderList.Add(new AngularMeasurableOrder(recurrenceId, m.Measurable.Id, m._Ordering));
              }
            }
            foreach (var r in measurablesOrderList.GroupBy(x => x.ScorecardId)) {
              var updater = rt.UpdateRecurrences(r.First().ScorecardId);
              foreach (var m in r) {
                updater.Update(m);
              }
            }



            tx.Commit();
            s.Flush();
          }
        }
      }
    }

    public static async Task AddAttendee(UserOrganizationModel caller, long recurrenceId, long userorgid) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            var perms = PermissionsUtility.Create(s, caller);
            await AddAttendee(s, perms, rt, recurrenceId, userorgid);
            tx.Commit();
            s.Flush();
          }

        }
      }
    }

    public static async Task AddAttendee(ISession s, PermissionsUtility perms, RealTimeUtility rt, long recurrenceId, long userorgid) {
      perms.AdminL10Recurrence(recurrenceId);
      perms.ViewUserOrganization(userorgid, false);
      var user = s.Get<UserOrganizationModel>(userorgid);
      var caller = perms.GetCaller();

      var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Recurrence.Id == recurrenceId).List().ToList();
      if (existing.Any()) {
        throw new PermissionsException("User is already an attendee.");
      }

      var recur = s.Get<L10Recurrence>(recurrenceId);
      //recur.Pristine = false;
      await L10Accessor.Depristine_Unsafe(s, caller, recur);
      s.Update(recur);

      var attendee = new L10Recurrence.L10Recurrence_Attendee() {
        L10Recurrence = recur,
        User = user,
      };

      s.Save(attendee);

      if (caller.Organization.Settings.DisableUpgradeUsers && user.EvalOnly) {
        throw new PermissionsException("This user is set to participate in " + Config.ReviewName() + " only.");
      }

      if (user.EvalOnly) {
        perms.CanUpgradeUser(user.Id);
        user.EvalOnly = false;
        s.Update(user);
        user.UpdateCache(s);
      }

      var curr = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
      if (curr != null) {
        s.Save(new L10Meeting.L10Meeting_Attendee() {
          L10Meeting = curr,
          User = user,
        });
      }

      await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.AddAttendee(ses, recurrenceId, user, attendee));
    }

    public static async Task EditAttendeeHasVoted(UserOrganizationModel caller, long recurrenceId, bool hasSubmittedVotes) {
      using (var session = HibernateSession.GetCurrentSession()) {
        using (var transaction = session.BeginTransaction()) {
          var perms = PermissionsUtility.Create(session, caller);

          perms.ViewUserOrganization(caller.Id, false);

          var attendee = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == caller.Id && x.L10Recurrence.Id == recurrenceId).SingleOrDefault();
          if (attendee is null) {
            throw new PermissionsException("User is not an attendee.");
          }

          attendee.HasSubmittedVotes = hasSubmittedVotes;

          session.Update(attendee);

          transaction.Commit();
          session.Flush();

          if (transaction.WasCommitted) {
            await HooksRegistry.Each<IMeetingAttendeeHooks>((ses, x) => x.UpdateAttendee(ses, caller, recurrenceId, attendee));
          }

        }
      }
    }

    public static async Task EditAttendee(UserOrganizationModel caller, long recurrenceId, long attedeeId, bool? isPresent, bool? hasSubmittedVotes, bool? isUsingV3, bool? concludeMeeting = false) {
      using (var session = HibernateSession.GetCurrentSession()) {
        using (var transaction = session.BeginTransaction()) {
          var perms = PermissionsUtility.Create(session, caller);

          if(concludeMeeting.GetValueOrDefault())
          {
            verifyEditViewRecurrencePermissions(perms, recurrenceId);
          }
          else {
            perms.AdminL10Recurrence(recurrenceId);
            perms.ViewUserOrganization(attedeeId, false);
          }

          var attendee = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == attedeeId && x.L10Recurrence.Id == recurrenceId).SingleOrDefault();
          if (attendee is null) {
            throw new PermissionsException("User is not an attendee.");
          }

          attendee.IsPresent = isPresent ?? attendee.IsPresent;
          attendee.HasSubmittedVotes = hasSubmittedVotes ?? attendee.HasSubmittedVotes;
          attendee.IsUsingV3 = isUsingV3 ?? attendee.IsUsingV3;

          session.Update(attendee);

          transaction.Commit();
          session.Flush();

          if (transaction.WasCommitted) {
            await HooksRegistry.Each<IMeetingAttendeeHooks>((ses, x) => x.UpdateAttendee(ses, caller, recurrenceId, attendee));
          }

        }
      }
    }

    public static async Task EditAttendeeIsPresent(UserOrganizationModel caller, long recurrenceId, long attendeeId, bool? isPresent) {
      var session = HibernateSession.GetCurrentSession();
      var transaction = session.BeginTransaction();

      var perms = PermissionsUtility.Create(session, caller);

      perms.ViewUserOrganization(attendeeId, false);

      var attendee = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
          .Where(x => x.DeleteTime == null
          && x.User.Id == attendeeId
          && x.L10Recurrence.Id == recurrenceId
        ).SingleOrDefault();

      if (attendee is null)
        throw new PermissionsException($"User is not an attendee. ID:{attendeeId}");

      var editCaller = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
          .Where(x => x.DeleteTime == null
          && x.User.Id == caller.Id
          && x.L10Recurrence.Id == recurrenceId
        ).SingleOrDefault();

      if (editCaller is null)
        throw new PermissionsException($"User is not an attendee. ID:{caller.Id}");

      attendee.IsPresent = isPresent ?? attendee.IsPresent;
      session.Update(attendee);
      transaction.Commit();
      session.Flush();

      if (transaction.WasCommitted)
        await HooksRegistry.Each<IMeetingAttendeeHooks>((ses, x) => x.UpdateAttendee(ses, caller, recurrenceId, attendee));
    }

    public class VtoSharable {
      public bool CanShareVto { get; set; }
      public string ErrorMessage { get; set; }
    }
    public static VtoSharable IsVtoSharable(UserOrganizationModel caller, long? recurrenceId = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var orgId = caller.Organization.Id;
          var anySharable = s.QueryOver<L10Recurrence>()
                    .Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.Id != recurrenceId)
                    .Select(x => x.ShareVto, x => x.Name, x => x.Id)
                    .List<object[]>()
                    .Select(x => new {
                      Shared = ((bool?)x[0]) ?? false,
                      Name = (string)x[1],
                      Id = (long)x[2]
                    }).ToList();
          var onlyShared = anySharable.FirstOrDefault(x => x.Shared);
          var output = new VtoSharable() {
            CanShareVto = onlyShared == null,
            ErrorMessage = onlyShared.NotNull(x => "You can only share one Business Plan. Unshare the Business Plan associated with <a href='/l10/edit/" + x.Id + "'>" + x.Name + "</a>.")
          };
          return output;
        }
      }
    }

    public static async Task RemoveAttendee(ISession s, PermissionsUtility perms, RealTimeUtility rt, long recurrenceId, long userorgid, DateTime detachTime) {
      perms.AdminL10Recurrence(recurrenceId);
      perms.ViewUserOrganization(userorgid, false);
      var user = s.Get<UserOrganizationModel>(userorgid);

      var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Recurrence.Id == recurrenceId).List().ToList();
      if (!existing.Any()) {
        throw new PermissionsException("User is not an attendee.");
      }

      var curAttendee = default(List<L10Meeting.L10Meeting_Attendee>);
      var curr = _GetCurrentL10Meeting(s, perms, recurrenceId, true, false, false);
      if (curr != null) {
        curAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userorgid && x.L10Meeting.Id == curr.Id).List().ToList();

        foreach (var e in curAttendee) {
          e.DeleteTime = detachTime;
          s.Update(e);
        }
      }
      foreach (var e in existing) {
        e.DeleteTime = detachTime;
        s.Update(e);
      }

      await HooksRegistry.Each<IMeetingEvents>((ses, x) => x.RemoveAttendee(ses, recurrenceId, userorgid, existing, curAttendee));
    }


    public static async Task RemoveAttendee(UserOrganizationModel caller, long recurrenceId, long userorgid, DateTime detachTime) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            var perms = PermissionsUtility.Create(s, caller);
            await RemoveAttendee(s, perms, rt, recurrenceId, userorgid, detachTime: detachTime);
            tx.Commit();
            s.Flush();
          }
        }
      }
    }


    public static long GuessUserId(IssueModel issueModel, long deflt = 0) {
      try {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            if (issueModel == null) {
              return deflt;
            }

            if (issueModel.ForModel != null && issueModel.ForModel.ToLower() == "issuemodel" && issueModel.Id == issueModel.ForModelId) {
              return deflt;
            }

            var found = GetModel_Unsafe(s, issueModel.ForModel, issueModel.ForModelId);
            if (found == null) {
              return deflt;
            }

            if (found is MeasurableModel) {
              return ((MeasurableModel)found).AccountableUserId;
            }

            if (found is TodoModel) {
              return ((TodoModel)found).AccountableUserId;
            }

            if (found is IssueModel) {
              return GuessUserId((IssueModel)found, deflt);
            }

            return deflt;
          }
        }
      } catch (Exception) {
        return deflt;
      }
    }

    public class L10StarDate {
      public long RecurrenceId { get; set; }
      public DateTime StarDate { get; set; }
    }

    public static async Task<List<L10StarDate>> GetStarredRecurrences(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return await GetStarredRecurrences(s, perms, userId);
        }
      }
    }

    public static async Task<List<L10StarDate>> GetStarredRecurrences(ISession s, PermissionsUtility perms, long userId) {
      perms.Self(userId);
      return s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
          .Where(x => x.DeleteTime == null && x.StarDate != null && x.User.Id == userId)
          .Select(x => x.L10Recurrence.Id, x => x.StarDate)
          .List<object[]>()
          .Select(x => new L10StarDate {
            StarDate = ((DateTime?)x[1]).Value,
            RecurrenceId = (long)x[0]
          }).ToList();
    }

    public static async Task AddStarToMeeting(UserOrganizationModel caller, long recurrenceId, long userid, bool starred) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);
          perms.Self(userid);

          var found = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
            .Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null && x.User.Id == userid)
            .List().ToList();

          if (!found.Any()) {
            throw new PermissionsException("Not an attendee");
          }

          DateTime? now = DateTime.UtcNow;
          foreach (var f in found) {
            f.StarDate = starred ? now : null;
            s.Update(f);
          }
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static DefaultDictionary<long, DateTime?> GetStarredMeetingsLookup_Unsafe(ISession s, long userId) {
      return s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .Where(x => x.DeleteTime == null && x.User.Id == userId && x.StarDate != null)
        .Select(x => x.L10Recurrence.Id, x => x.StarDate)
        .List<object[]>()
        .ToDefaultDictionary(x => (long)x[0], x => (DateTime?)x[1], x => null);
    }


    #endregion

    public static async Task NotifyOthersOfMeeting(UserOrganizationModel caller, long recurrenceId) {
      //Scheduler.Enqueue(() => NotifyOthersOfMeeting_Hangfire(caller.Id, recurrenceId));
      await NotifyOthersOfMeeting_Hangfire(caller.Id, recurrenceId);
    }

    public static async Task<IEnumerable<L10Recurrence.L10Recurrence_Attendee>> GetAttendeesByRecurenceIdsUnsafeAsync(UserOrganizationModel caller, List<long> recurrenceIds) {
      using var session = HibernateSession.GetCurrentSession();

      UserOrganizationModel userAlias = null;
      IEnumerable<L10Recurrence.L10Recurrence_Attendee> allAttendQ = new List<L10Recurrence.L10Recurrence_Attendee>();

      var allAttendSubQ = QueryOver.Of<L10Recurrence.L10Recurrence_Attendee>()
          .JoinAlias(x => x.User, () => userAlias)
          .Where(x => x.DeleteTime == null && userAlias.DeleteTime == null)
          .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
          .Select(Projections.Property<L10Recurrence.L10Recurrence_Attendee>(x => x.User.Id));


      allAttendQ = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .JoinAlias(x => x.User, () => userAlias)
        .Where(x => x.DeleteTime == null && userAlias.DeleteTime == null)
        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
        .Future<L10Recurrence.L10Recurrence_Attendee>();


      return allAttendQ.ToList();
    }

    public static async Task<IEnumerable<L10Recurrence.L10Recurrence_Attendee>> GetCurrentAttendeesByRecurenceIdsUnsafeAsync(UserOrganizationModel caller, List<long> recurrenceIds) {
      var sw = Stopwatch.StartNew();
      long a, b, c, d;
      IEnumerable<L10Recurrence.L10Recurrence_Attendee> allAttendQ;
      using (var s = HibernateSession.GetCurrentSession()) {
        a = sw.ElapsedMilliseconds;
        using (var tx = s.BeginTransaction()) {
          b = sw.ElapsedMilliseconds;
          UserOrganizationModel userAlias = null;
          allAttendQ=s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
                        .JoinAlias(x => x.User, () => userAlias)
                        .Where(x => x.DeleteTime == null && userAlias.DeleteTime == null && userAlias.Id == caller.Id)
                        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
                        .List().ToList();
          c = sw.ElapsedMilliseconds;
        }
      }
      d = sw.ElapsedMilliseconds;
      return allAttendQ;
    }

    public static List<L10Recurrence.L10Recurrence_Attendee> GetRecurrenceAttendeesLookupByRecurenceIdsUnsafe(ISession s, List<long> recurrenceIds) {
      UserOrganizationModel userAlias = null;
      var allAttendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        .JoinAlias(x => x.User, () => userAlias)
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(recurrenceIds)
        .Select(x => x.Id, x => x.User, x => x.L10Recurrence.Id
        )
        .Future<object[]>().Select(x => new L10Recurrence.L10Recurrence_Attendee {
          Id = (long)x[0],
          User = (UserOrganizationModel)x[1],
          L10Recurrence = new L10Recurrence { Id = (long)x[2] }
        });

      return allAttendees.ToList();
    }

    public static IEnumerable<L10Meeting.L10Meeting_Attendee> GetCurrentAttendeesByIds(UserOrganizationModel caller, IEnumerable<long> userIds, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);

          var recur = s.Get<L10Recurrence>(recurrenceId);
          if (recur.MeetingInProgress==null) {
            throw new MeetingException(recurrenceId, MeetingExceptionType.Unstarted);
          }

          return s.QueryOver<L10Meeting.L10Meeting_Attendee>()
            .Where(x => x.DeleteTime==null && x.L10Meeting.Id == recur.MeetingInProgress.Value)
            .WhereRestrictionOn(x => x.User.Id).IsIn(userIds.ToArray())
            .List()
            .ToList();
        }
      }
    }

    public static IEnumerable<L10Meeting.L10Meeting_Attendee> GetHistoricAttendeesByIdsUnsafe(ISession s, IEnumerable<long> userIds, long meetingId)
    {
      return s.QueryOver<L10Meeting.L10Meeting_Attendee>()
        .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId)
        .WhereRestrictionOn(x => x.User.Id).IsIn(userIds.ToArray())
        .List()
        .ToList();
    }

    public static IEnumerable<L10Meeting.L10Meeting_Attendee> GetHistoricAttendeesByIds(UserOrganizationModel caller, IEnumerable<long> userIds, long recurrenceId, long meetingId, bool? shouldCheckInProgress = true) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);

          return s.QueryOver<L10Meeting.L10Meeting_Attendee>()
            .Where(x => x.DeleteTime == null && x.L10Meeting.Id == meetingId)
            .WhereRestrictionOn(x => x.User.Id).IsIn(userIds.ToArray())
            .List()
            .ToList();
        }
      }
    }


    [Queue(HangfireQueues.Immediate.NOTIFY_MEETING_START)]
    [AutomaticRetry(Attempts = 0)]
    public static async Task NotifyOthersOfMeeting_Hangfire(long callerId, long recurrenceId) {
      try {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var caller = s.Get<UserOrganizationModel>(callerId);
            var perms = PermissionsUtility.Create(s, caller);
            perms.ViewL10Recurrence(recurrenceId);

            var recurName = s.Get<L10Recurrence>(recurrenceId).Name;

            UserOrganizationModel uoAlias = null;
            UserModel uAlias = null;
            var attendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
              .JoinAlias(x => x.User, () => uoAlias)
              .JoinAlias(x => uoAlias.User, () => uAlias)
              .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId)
              .Select(x => uoAlias.Id, x => uAlias.UserName)
              .List<object[]>()
              .Select(x => new {
                UserId = (long)x[0],
                Email = (string)x[1]
              }).ToList();

            var attendees_emails = attendees.Select(x => x.Email).ToArray();
            if (attendees_emails.Length != 0) {
              // We only need to update when there is results
              await using (var rt = RealTimeUtility.Create()) {
                var group = rt.UpdateUsers(attendees.Select(x => x.Email).ToArray());
                group.Call("NotifyOfMeetingStart", recurrenceId, recurName);
              }
            }
          }
        }
      } catch (Exception) {
        // The controller will catch this exception so there's nothing to worry about.
        throw;
      }
    }
  }
}
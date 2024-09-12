using Amazon.Runtime.Internal.Transform;
using DocumentFormat.OpenXml.Vml.Office;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Controllers;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L10Controller = RadialReview.Core.Controllers.L10Controller;

namespace RadialReview.Accessors {
  public partial class L10Accessor : BaseAccessor {


    #region Issues
    public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, DateTime? meetingStart = null, bool includeResolve = false, bool includeArchived = false) {
      var mstart = meetingStart ?? DateTime.MaxValue;
      perms.ViewL10Recurrence(recurrenceId);
      //TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.

      var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
        .Where(x => (includeArchived==true || x.DeleteTime == null) && x.Recurrence.Id == recurrenceId
        && (includeResolve == true || (x.CloseTime == null || x.CloseTime >= mstart))
        ).Fetch(x => x.Issue).Eager
        .List().ToList();

      var populated = _PopulateChildrenIssues(issues);


      try {
        //attach the meeting name to moved issues
        var lookup = populated.Where(x => x.AwaitingSolve).Select(x => x.Id).ToList();
        if (lookup.Any()) {
          var nameLookup = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                  .Where(x => includeArchived==true || x.DeleteTime == null)
                  .WhereRestrictionOn(x => x.CopiedFrom.Id).IsIn(lookup)
                  .Fetch(x => x.Recurrence).Eager
                  .List()
                  .ToDefaultDictionary(x => x.CopiedFrom.Id, x => x.Recurrence.NotNull(y => y.Name), x => null);

          foreach (var p in populated) {
            if (p.AwaitingSolve) {
              p._MovedToMeetingName = nameLookup[p.Id];
              p._MovedToIssueId = p.Id;
            }
          }
        }
      } catch (Exception) {
        //eat it
      }


      return populated;
    }

    public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrences(UserOrganizationModel caller, IReadOnlyList<long> recurrenceIds, DateTime? meetingStart = null, bool excludeSentTo = true)
    {
      using var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      return GetIssuesForRecurrences(session, perms, recurrenceIds, meetingStart, excludeSentTo);
    }

    public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrences(ISession session, PermissionsUtility perms, IReadOnlyList<long> recurrenceIds, DateTime? meetingStart = null, bool excludeSentTo = true)
    {

      var validRecurrenceIds = recurrenceIds.Where(recId => perms.IsL10RecurrenceViewable(recId));

      var mstart = meetingStart ?? DateTime.MaxValue;

      var issuesQuery = session.QueryOver<IssueModel.IssueModel_Recurrence>()
            .WhereRestrictionOn(ir => ir.Recurrence.Id)
            .IsIn(validRecurrenceIds.ToArray())
            .And(x => x.DeleteTime == null && (x.CloseTime == null || x.CloseTime >= mstart));

      if (excludeSentTo)
      {
        // If AwaitingSolve is true, it means that it was copied to another meeting
        issuesQuery.And(x => !x.AwaitingSolve);
      }

      var issues = issuesQuery
          .Fetch(SelectMode.Fetch, x => x.Issue)
          .Fetch(SelectMode.Fetch, x => x.Owner)
          .List()
          .ToList();

      var populated = _PopulateChildrenIssues(issues);

      if (!excludeSentTo)
        AttachCopiedIssues(session, populated);

      return populated;
    }

    public static List<IssueModel.IssueModel_Recurrence> GetArchivedIssuesForRecurrences(UserOrganizationModel caller, IReadOnlyList<long> recurrenceIds)
    {
      using(var session = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(session, caller);
        return GetArchivedIssuesForRecurrences(session, perms, recurrenceIds);
      }
    }

    public static List<IssueModel.IssueModel_Recurrence> GetArchivedIssuesForRecurrences(ISession session, PermissionsUtility perms, IReadOnlyList<long> recurrenceIds)
    {
      var validRecurrenceIds = recurrenceIds.Where(recId => perms.IsL10RecurrenceViewable(recId)).ToArray();

      var issues = session.QueryOver<IssueModel.IssueModel_Recurrence>()
        .WhereRestrictionOn(ir => ir.Recurrence.Id)
        .IsIn(validRecurrenceIds)
        .And(x =>  x.DeleteTime != null)
        .Fetch(SelectMode.Fetch, x => x.Issue)
        .Fetch(SelectMode.Fetch, x => x.Recurrence)
        .Fetch(SelectMode.Fetch, x => x.CopiedFrom)
        .Fetch(SelectMode.Fetch, x => x.Owner)
        .List().ToList();

      var populated = _PopulateChildrenIssues(issues);
      AttachCopiedIssues(session, populated);

      return populated;
    }

    public static List<IssueModel.IssueModel_Recurrence> GetSentToIssuesForRecurrences(UserOrganizationModel caller, IReadOnlyList<long> recurrenceIds)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(session, caller);
        return GetSentToIssuesForRecurrences(session, perms, recurrenceIds);
      }
    }

    public static List<IssueModel.IssueModel_Recurrence> GetSentToIssuesForRecurrences(ISession session, PermissionsUtility perms, IReadOnlyList<long> recurrenceIds)
    {

      var recIds = recurrenceIds.Where(recId => perms.IsL10RecurrenceViewable(recId)).ToList();

      //TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.
      var issueIds = session.QueryOver<IssueModel.IssueModel_Recurrence>()
                      .WhereRestrictionOn(ir => ir.Recurrence.Id).IsIn(recIds.ToArray())
                      .And(ir => ir.DeleteTime == null && ir.CloseTime == null)
                      .And(ir => ir.AwaitingSolve)
                      .Select(ir => ir.Id)
                      .List<long>()
                      .ToList();

      IssueModel.IssueModel_Recurrence copiedFromAlias = null;

      var copiedIssues = session.QueryOver<IssueModel.IssueModel_Recurrence>()
          .WhereRestrictionOn(ir => ir.CopiedFrom.Id).IsIn(issueIds)
          .And(ir => ir.DeleteTime == null)
          .Fetch(SelectMode.Fetch, ir => ir.Recurrence)
          .Fetch(SelectMode.Fetch, ir => ir.Owner)
          .JoinAlias(ir => ir.CopiedFrom, () => copiedFromAlias)
          .Fetch(SelectMode.Fetch, () => copiedFromAlias.Issue)
          .Fetch(SelectMode.Fetch, () => copiedFromAlias.Recurrence)
          .Fetch(SelectMode.Fetch, () => copiedFromAlias.Owner)
          .List<IssueModel.IssueModel_Recurrence>()
          .ToList();

      _PopulateChildrenIssues(copiedIssues);

      var uniqueCopiedIssues = copiedIssues
        .GroupBy(issue => issue.CopiedFrom.Id)
        .Select(group => group.First())
        .ToList();

      List<IssueModel.IssueModel_Recurrence> sentToIssues = new();
      foreach (var copiedIssue in uniqueCopiedIssues)
      {
        var parent = copiedIssue.CopiedFrom;
        if ((parent is not null) && parent.AwaitingSolve)
        {
          parent._MovedToMeetingName = copiedIssue.Recurrence.Name;
          parent._MovedToIssueId = copiedIssue.Id;
          parent._SentToIssue = copiedIssue;
          sentToIssues.Add(parent);
        }
      }

      sentToIssues = _PopulateChildrenIssues(sentToIssues);

      return sentToIssues;
    }

    public static List<IssueModel.IssueModel_Recurrence> GetSolvedIssuesForRecurrences(UserOrganizationModel caller, IReadOnlyList<long> recurrenceIds, DateRange range = null)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        var perms = PermissionsUtility.Create(session, caller);
        return GetSolvedIssuesForRecurrences(session, perms, recurrenceIds, range);
      }
    }

    public static List<IssueModel.IssueModel_Recurrence> GetSolvedIssuesForRecurrences(ISession session, PermissionsUtility perms, IReadOnlyList<long> recurrenceIds, DateRange range = null)
    {
      var validRecurenceIds = recurrenceIds.Where(recId => perms.IsL10RecurrenceViewable(recId)).ToList();

      var issuesQuery = session.QueryOver<IssueModel.IssueModel_Recurrence>()
        .WhereRestrictionOn(ir => ir.Recurrence.Id).IsIn(validRecurenceIds)
        .And(x => x.DeleteTime == null);

      if (range is not null)
      {
        issuesQuery.And(x => x.CloseTime >= range.StartTime && x.CloseTime <= range.EndTime);
      }
      else
      {
        issuesQuery.And(x => x.CloseTime != null);
      }

      var issues = issuesQuery
        .Fetch(SelectMode.Fetch, x => x.Issue)
        .Fetch(SelectMode.Fetch, x => x.Recurrence)
        .Fetch(SelectMode.Fetch, x => x.CopiedFrom)
        .Fetch(SelectMode.Fetch, x => x.Owner)
        .List()
        .ToList();

      var populated = _PopulateChildrenIssues(issues);

      AttachCopiedIssues(session, populated);

      return populated;
    }

    public static List<IssueModel.IssueModel_Recurrence> GetSolvedIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, DateRange range) {
      perms.ViewL10Recurrence(recurrenceId);

      var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
        .Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
        .Where(x => x.CloseTime >= range.StartTime && x.CloseTime <= range.EndTime)
        .Fetch(x => x.Issue).Eager
        .List().ToList();

      return _PopulateChildrenIssues(issues);
    }

    public static List<IssueModel.IssueModel_Recurrence> GetIssuesForMeeting(UserOrganizationModel caller, long meetingId, bool includeResolved) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var meeting = s.Get<L10Meeting>(meetingId);
          var recurrenceId = meeting.L10RecurrenceId;
          var perms = PermissionsUtility.Create(s, caller);
          return GetIssuesForRecurrence(s, perms, recurrenceId, meeting.StartTime);
        }
      }
    }

    public static async Task ResetStarVoting(UserOrganizationModel caller, long recurrenceId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);

          var meeting = L10Accessor.GetCurrentL10Meeting(caller, recurrenceId);
          if(meeting != null)
          {
            meeting.IssueVotingHasEnded = false;
            s.Update(meeting);
            s.Flush();
          }

          Dictionary<IssueModel.IssueModel_Recurrence, IIssueHookUpdates> hookUpdates = new Dictionary<IssueModel.IssueModel_Recurrence, IIssueHookUpdates>();
          foreach (var issue in GetIssuesForRecurrence(s, perms, recurrenceId, includeResolve: true))
          {
            if (issue.Stars != 0)
            {
              hookUpdates.Add(issue, new IIssueHookUpdates { oldStars = issue.Stars, StarsChanged = true });

              issue.Stars = 0;
              s.Update(issue);
              s.Flush();
            }
          }

          await L10Accessor.ResetAttendeesHasVotedFlag(caller, recurrenceId, false);

          tx.Commit();
          if (tx.WasCommitted)
          {
            foreach (var entry in hookUpdates)
            {
              await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, caller, entry.Key, entry.Value));
            }

            await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateRecurrence(sess, caller, meeting.L10Recurrence));
          }
        }
      }
    }

    public static List<IssueModel.IssueModel_Recurrence> GetIssuesForRecurrence(UserOrganizationModel caller, long recurrenceId, bool includeResolved = false, bool includeArchived = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetIssuesForRecurrence(s, perms, recurrenceId, includeResolve: includeResolved, includeArchived: includeArchived);
        }
      }
    }

    public static List<IssueModel.IssueModel_Recurrence> GetAllIssuesForRecurrence(ISession s, PermissionsUtility perms, long recurrenceId, bool includeCompleted = true, DateRange range = null) {
      perms.ViewL10Recurrence(recurrenceId);

      //TODO optimize this call. Some issueRecurrence's parents are closed, but children are not.
      var issuesQ = s.QueryOver<IssueModel.IssueModel_Recurrence>()
        .Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId);

      if (range != null && includeCompleted) {
        var st = range.StartTime.AddDays(-1);
        var et = range.EndTime.AddDays(1);
        issuesQ = issuesQ.Where(x => x.CloseTime == null || (x.CloseTime >= st && x.CloseTime <= et));
      }

      if (!includeCompleted) {
        issuesQ = issuesQ.Where(x => x.CloseTime == null);
      }

      var issues = issuesQ.Fetch(x => x.Issue).Eager.List().ToList();

      return _PopulateChildrenIssues(issues);
    }

    public static List<AngularIssue> GetLongTermIssuesForRecurrence(UserOrganizationModel caller, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);
          var vtoId = s.Get<L10Recurrence>(recurrenceId).VtoId;
          perms.ViewVTOTraction(vtoId);
          var vtoIssues = s.QueryOver<VtoItem_String>().Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null).List().ToList();

          //var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
          //    .Where(x => x.DeleteTime != null && x.Recurrence.Id == recurrenceId && vtoIssues.Contains(x.Id)).List().ToList();
          var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .Where(x => x.DeleteTime != null && x.Recurrence.Id == recurrenceId)
            .WhereRestrictionOn(x => x.Id).IsIn(vtoIssues.Where(x => x.ForModel != null && x.ForModel.Is<IssueModel.IssueModel_Recurrence>()).Select(x => x.ForModel.ModelId).ToArray())
            .List().ToList();

          var list = issues.Select(x => new AngularIssue(x)).ToList();

          var notIncluded = vtoIssues.Where(x => x.ForModel == null || !x.ForModel.Is<IssueModel.IssueModel_Recurrence>())
                         .Select(x => new AngularIssue() {
                           Id = -x.Id,
                           Name = x.Data,
                           CreateTime = x.CreateTime,

                         });
          list.AddRange(notIncluded);
          return list;
        }
      }

    }

    public static async Task CompleteIssue(UserOrganizationModel caller, long recurrenceIssue) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await CompleteIssue(s, perms, recurrenceIssue);
          tx.Commit();
          s.Flush();
        }
      }
    }



    public static async Task CompleteIssue(ISession s, PermissionsUtility perm, long recurrenceIssue) {
      var issue = s.Get<IssueModel.IssueModel_Recurrence>(recurrenceIssue);
      perm.EditL10Recurrence(issue.Recurrence.Id);
      if (issue.CloseTime != null) {
        throw new PermissionsException("Issue already deleted.");
      }
      await SyncUtil.ExecuteNonAtomically(s, perm, IssuesAccessor.BuildEditIssueExecutor(recurrenceIssue, complete: true));
      //await IssuesAccessor.EditIssue(OrderedSession.Indifferent(s), perm, recurrenceIssue, complete: true);
    }

    public static async Task UpdateIssues(UserOrganizationModel caller, long recurrenceId, /*IssuesDataList*/L10Controller.IssuesListVm model) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          var perm = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
          var ids = model.GetAllIds();
          var found = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.DeleteTime == null && x.Recurrence.Id == recurrenceId)
            .WhereRestrictionOn(x => x.Id).IsIn(ids)
            //.Fetch(x=>x.Issue).Eager
            .List().ToList();

          if (model.orderby != null) {
            var recur = s.Get<L10Recurrence>(recurrenceId);
            recur.OrderIssueBy = model.orderby;
            s.Update(recur);
          }


          var ar = SetUtility.AddRemove(ids, found.Select(x => x.Id));

          if (ar.RemovedValues.Any()) {
            throw new PermissionsException("You do not have permission to edit this issue.");
          }

          if (ar.AddedValues.Any()) {
            throw new PermissionsException("Unreachable.");
          }

          var recurrenceIssues = found.ToList();

          foreach (var e in model.GetIssueEdits()) {
            var f = recurrenceIssues.First(x => x.Id == e.RecurrenceIssueId);
            var update = false;
            if (f.ParentRecurrenceIssue.NotNull(x => x.Id) != e.ParentRecurrenceIssueId) {
              f.ParentRecurrenceIssue = (e.ParentRecurrenceIssueId == null) ? null : recurrenceIssues.First(x => x.Id == e.ParentRecurrenceIssueId);
              update = true;
            }

            if (f.Ordering != e.Order) {
              f.Ordering = e.Order;
              update = true;
            }

            if (update) {
              s.Update(f);
            }
          }


          await using (var rt = RealTimeUtility.Create(model.connectionId)) {
            var json = JsonConvert.SerializeObject(model);
            var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

            //group.deserializeIssues(".issues-list", model);
            group.Call("setIssueOrder", model.issues);
            var issues = GetAllIssuesForRecurrence(s, perm, recurrenceId)
              .OrderBy(x => x.Ordering)
              .Select(x => new AngularIssue(x))
              .ToList();

            group.Update(new AngularRecurrence(recurrenceId) {
              IssuesList = new AngularIssuesList(recurrenceId) {
                Issues = AngularList.Create(AngularListType.ReplaceAll, issues)
              }
            });
          }


          await Audit.L10Log(s, caller, recurrenceId, "UpdateIssues", ForModel.Create<L10Recurrence>(recurrenceId));

          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task<VtoItem_String> MoveIssueToVto(UserOrganizationModel caller, long issue_recurrence, string connectionId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create(connectionId)) {
            var perm = PermissionsUtility.Create(s, caller);

            var str = await IssuesAccessor.MoveIssueToVto(s, perm, issue_recurrence, connectionId);


            tx.Commit();
            s.Flush();
            return str;
          }
        }
      }
    }

    public static async Task<IssueModel.IssueModel_Recurrence> MoveIssueFromVto(UserOrganizationModel caller, long vtoIssue) {

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var now = DateTime.UtcNow;
          var perm = PermissionsUtility.Create(s, caller);

          var issueRecur = await IssuesAccessor.MoveIssueFromVto(s, perm, vtoIssue);


          tx.Commit();
          s.Flush();
          return issueRecur;
        }
      }
    }

    private static void AttachCopiedIssues(ISession session, List<IssueModel.IssueModel_Recurrence> issues)
    {
      try
      {
        //attach the meeting name to moved issues
        var lookup = issues.Where(x => x.AwaitingSolve).ToArray().Select(x => x.Id).ToList();
        if (lookup.Any())
        {
          var results = session.QueryOver<IssueModel.IssueModel_Recurrence>()
                  .Where(x => x.DeleteTime == null)
                  .WhereRestrictionOn(x => x.CopiedFrom.Id).IsIn(lookup)
                  .Fetch(SelectMode.Fetch, x => x.Recurrence)
                  .List();

          var nameLookup = results
              .GroupBy(x => x.CopiedFrom.Id)
              .ToDictionary(
                  g => g.Key,
                  g => g.Select(x => new
                  {
                    RecurrenceName = x.Recurrence.NotNull(y => y.Name),
                    IssueId = x.Id
                  }).First()
              );

          foreach (var issue in issues)
          {
            if (issue.AwaitingSolve)
            {
              issue._MovedToMeetingName = nameLookup[issue.Id].RecurrenceName;
              issue._MovedToIssueId = nameLookup[issue.Id].IssueId;
            }
          }
        }
      }
      catch (Exception)
      {
        //eat it
      }
    }

    #endregion
  }
}

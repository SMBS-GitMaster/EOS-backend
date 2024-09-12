using System;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Utilities;
using RadialReview.Models.Issues;
using RadialReview.Exceptions;
using RadialReview.Utilities.NHibernate;
using RadialReview.Models.L10;
using System.Linq;
using RadialReview.Utilities.Hooks;
using RadialReview.Models;
using RadialReview.Crosscutting.Hooks;
using static RadialReview.Accessors.IssuesAccessor;
using RadialReview.Accessors;
using RadialReview.Utilities.Synchronize;

namespace RadialReview.Core.Accessors.StrictlyAfterExecutors {
  public class EditIssueExecutor : IStrictlyAfter {
    public StrictlyAfterBehavior Behavior => new StrictlyAfterBehavior(true);

    public long issueRecurrenceId { get; }
    public string message { get; }
    public bool? complete { get; }
    public long? owner { get; }
    public int? priority { get; }
    public int? rank { get; }
    public int? stars { get; }
    public bool? awaitingSolve { get; }
    public DateTime? now { get; }
    public IssueCompartment? status { get; }
    public IIssueHookUpdates updates { get; }
    private bool EXECUTEMARKCLOSE { get; set; }
    private bool? COMPLETIONADDEDINDICATOR { get; set; }
    private bool AWAITINGSOLVE { get; set; }
    private string noteId { get; set; }
    private bool? archived { get; set; }
    private bool? addToDepartmentPlan { get; set; }

    public EditIssueExecutor(long issueRecurrenceId, string message = null,
    bool? complete = null, long? owner = null, int? priority = null, int? rank = null, bool? awaitingSolve = null,
    DateTime? now = null, IssueCompartment? status = null, string noteId = null, bool? archived = null, int? stars = null, bool? addToDepartmentPlan = null) {
      now = Math2.Min(DateTime.UtcNow.AddSeconds(3), now ?? DateTime.UtcNow);
      this.issueRecurrenceId=issueRecurrenceId;
      this.message=message;
      this.complete=complete;
      this.owner=owner;
      this.priority=priority;
      this.rank=rank;
      this.awaitingSolve=awaitingSolve;
      this.now=now;
      this.status=status;
      this.noteId = noteId;
      this.archived = archived;
      this.stars = stars;
      this.addToDepartmentPlan = addToDepartmentPlan;
      this.updates = new IIssueHookUpdates();
    }
    public async Task EnsurePermitted(ISession s, PermissionsUtility perms) {
      var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
      if (issue == null)
        throw new PermissionsException("Issue does not exist.");

      var recurrenceId = issue.Recurrence.Id;
      if (recurrenceId == 0)
        throw new PermissionsException("Meeting does not exist.");
      perms.EditL10Recurrence(recurrenceId);

      if (owner != null && (issue.Owner == null || owner != issue.Owner.Id) && owner > 0) {

        // Check to make sure that the owner is actually apart of the organization 
        perms.ViewUserOrganization(owner.Value, false);

        // Leaving the code below commented out because we want to double check that
        // this doesn't have any unintended consequences with permissions if we remove it.
        // Like being able to see information in the metrics from meetings you're not in

        //var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        //              .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner)
        //              .Take(1).List().ToList();
        //if (!any.Any())
        //  throw new PermissionsException("Specified Owner cannot see meeting");

      }
    }

    public async Task AtomicUpdate(IOrderedSession s) {
      var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);

      if (message != null && message != issue.Issue.Message) {
        issue.Issue.Message = message;
        updates.MessageChanged = true;
      }
      if (owner != null && (issue.Owner == null || owner != issue.Owner.Id) && owner > 0) {
        //Checked above.
        //var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner).Take(1).List().ToList();
        //if (!any.Any())
        //  throw new PermissionsException("Specified Owner cannot see meeting");

        issue.Owner = s.Get<UserOrganizationModel>(owner);
        updates.OwnerChanged = true;
      }
      if (priority != null && priority != issue.Priority && issue.LastUpdate_Priority < now) {
        issue.LastUpdate_Priority = now.Value;
        updates.oldPriority = issue.Priority;
        issue.Priority = priority.Value;
        s.Update(issue);
        updates.PriorityChanged = true;
      }
      if (rank != null && rank != issue.Rank && issue.LastUpdate_Priority < now) {
        issue.LastUpdate_Priority = now.Value;
        updates.oldRank = issue.Rank;
        issue.Rank = rank.Value;
        s.Update(issue);
        updates.RankChanged = true;
      }
      if(stars != null && issue.LastUpdate_Stars < now)
      {
        // Always increment on non-zero
        // Always reset on zero
        int newStars = stars.Value == 0 ? 0 : issue.Stars + stars.Value;
        issue.LastUpdate_Stars = now.Value;
        updates.oldStars = issue.Stars;
        issue.Stars = newStars;
        s.Update(issue);
        updates.StarsChanged = true;
      }

      //Moved to afterUpdate.
      //if (status != null) {
      //  if (status == IssueCompartment.ShortTerm && issue.DeleteTime != null) {
      //    updates.CompartmentChanged = true;
      //    await MoveIssueFromVtoViaIssueRecurrenceId(s, perms, issue.Id);
      //  } else if (status == IssueCompartment.LongTerm && issue.DeleteTime == null) {
      //    updates.CompartmentChanged = true;
      //    await MoveIssueToVto(s, perms, issue.Id, perms.GetCaller().NotNull(x => x.GetClientRequestId()));
      //  }
      //}

      var now1 = DateTime.UtcNow;
      if (complete != null) {
        if (complete.Value && issue.CloseTime == null) {
          updates.CompletionChanged = true;
        } else if (!complete.Value && issue.CloseTime != null) {
          updates.CompletionChanged = true;
        }
        if (complete.Value && issue.CloseTime == null) {
          issue.CloseTime = now1;
          //moved code to after update
          COMPLETIONADDEDINDICATOR = false;
        } else if (!complete.Value && issue.CloseTime != null) {
          issue.CloseTime = null;

          //moved code to afterUpdate
          EXECUTEMARKCLOSE = true;
          COMPLETIONADDEDINDICATOR = true;
        }
      }

      if (awaitingSolve != null && awaitingSolve != issue.AwaitingSolve) {
        issue.AwaitingSolve = awaitingSolve.Value;
        s.Update(issue);
        AWAITINGSOLVE = true;
      }

      if(noteId != null)
      {
        var topIssue = s.Get<IssueModel>(issue.Issue.Id);
        if(topIssue.PadId != noteId)
        {
          topIssue.PadId = noteId;
          s.Update(topIssue);
        }
      }

      if (archived != null) {
        issue.DeleteTime = (bool)archived ? DateTime.UtcNow : null;
        s.Update(issue);
      }

    }


    /// <summary>
    /// AfterAtomicUpdate
    /// </summary>
    /// <param name="s"></param>
    /// <param name="perms"></param>
    /// <returns></returns>
    public async Task AfterAtomicUpdate(ISession s, PermissionsUtility perms) {
      var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
      var vtoIssue = VtoAccessor.GetVTOIssueByIssueId(s, perms, issue.Id);

      if (status.HasValue && !issue.DeleteTime.HasValue)
      {
        updates.CompartmentChanged = true;
        issue.IssueCompartment = status;
      }

      if (EXECUTEMARKCLOSE) {
        var childIssue = s.QueryOver<IssueModel.IssueModel_Recurrence>()
             .Where(x => x.CopiedFrom.Id == issue.Id)
             .List().FirstOrDefault();
        issue.MarkedForClose = childIssue != null && childIssue.CloseTime != null;
      }
      s.Update(issue);

      if (COMPLETIONADDEDINDICATOR != null) {
        var others = s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .Where(x => x.DeleteTime == null && x.Issue.Id == issue.Issue.Id)
            .List().ToList();

        //Not sure what I was thinking here...
        foreach (var o in others) {
          if (o.Id != issue.Id) {
            o.MarkedForClose = complete.Value;
            if (complete.Value)
              o.AwaitingSolve = true;
            s.Update(o);
          }
        }
      }

      if (AWAITINGSOLVE) {
        var sentToMeeting = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                  .Where(x => x.DeleteTime == null && x.CopiedFrom.Id == issueRecurrenceId)
                  .Select(x => x.Recurrence.Id).List<long>()
                  .FirstOrDefault();
        updates.MovedToRecurrence = sentToMeeting == 0 ? null : (long?)sentToMeeting;
        updates.AwaitingSolveChanged = true;
      }

      // Ensure the current status of the issue in the Department Plan using vtoIssue,
      // as the issue.AddToDepartmentPlan property in the DB may be incorrect for older records.
      issue.AddToDepartmentPlan = vtoIssue is not null;
      updates.AddToDepartmentPlan = issue.AddToDepartmentPlan;

      if (addToDepartmentPlan.HasValue)
      {
        // When attempting to add to Department Plan, ensure it's not already added to avoid duplicates
        if (addToDepartmentPlan.Value)
        {
          if (issue.AddToDepartmentPlan)
          {
            throw new PermissionsException("The issue is already added to Department Plan");
          }
          await IssuesAccessor.MoveIssueToVto(s, perms, issueRecurrenceId, connectionId: null, notify: true);
        }
        else
        {
          if (!issue.AddToDepartmentPlan)
          {
            throw new PermissionsException("The issue is not in the Department Plan");
          }
          await IssuesAccessor.MoveIssueFromVto(s, perms, vtoIssue.Id, notify: true);
        }

        updates.AddToDepartmentPlan = addToDepartmentPlan.Value;
      } else
      {
        if(archived.HasValue && archived.Value && issue.AddToDepartmentPlan)
        {
          vtoIssue.DeleteTime = DateTime.UtcNow;
          s.Update(vtoIssue);
          updates.AddToDepartmentPlan = false;
        }
      }

      var cc = perms.GetCaller();
      await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, cc, issue, updates));
    }

  }
}
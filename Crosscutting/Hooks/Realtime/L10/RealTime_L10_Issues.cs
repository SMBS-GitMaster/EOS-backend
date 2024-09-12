using NHibernate;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10
{
  public class RealTime_L10_Issues : IIssueHook
  {
    public bool CanRunRemotely()
    {
      return true;
    }
    public bool AbsorbErrors()
    {
      return false;
    }
    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task CreateIssue(ISession s, UserOrganizationModel callr, IssueModel.IssueModel_Recurrence issueRecurrenceModel)
    {

      await using (var rt = RealTimeUtility.Create())
      {
        var caller = issueRecurrenceModel.CreatedBy;
        var recurrenceId = issueRecurrenceModel.Recurrence.Id;
        var r = s.Get<L10Recurrence>(recurrenceId);

        var meetingHub = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

        meetingHub.Call("appendIssue", ".issues-list", IssuesData.FromIssueRecurrence(issueRecurrenceModel), r.OrderIssueBy);

        TermsCollection terms = TermsCollection.DEFAULT;
        try
        {
          terms = TermsAccessor.GetTermsCollection(s, PermissionsUtility.Create(s, callr), caller.Organization.Id);
        }
        catch (Exception e)
        {

        }

        var message = $@"Created {terms.GetTermSingular(TermKey.Issues)}.";
        var showWhoCreatedDetails = true;
        if (showWhoCreatedDetails)
        {
          try
          {
            if (caller != null && caller.GetFirstName() != null)
            {
              message = caller.GetFirstName() + $@" created {terms.GetTermSingular(TermKey.Issues)}.";
            }
          }
          catch (Exception)
          {
          }
        }

        meetingHub.Call("showAlert", message, 1500);

        var updates = new AngularRecurrence(recurrenceId);
        updates.IssuesList.Issues = AngularList.Create(AngularListType.Add, new[] { new AngularIssue(issueRecurrenceModel) });
        meetingHub.Update(updates);

        if (RealTimeHelpers.GetConnectionString() != null)
        {
          var me = rt.UpdateConnection(RealTimeHelpers.GetConnectionString());
          me.Update(new AngularRecurrence(recurrenceId)
          {
            Focus = "[data-issue='" + issueRecurrenceModel.Id + "'] input:visible:first"
          });
        }
      }

    }

    public async Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issueRecurrence, IIssueHookUpdates updates)
    {
      var updatesText = new List<string>();
      var recurrenceId = issueRecurrence.Recurrence.Id;
      var issueRecurrenceId = issueRecurrence.Id;
      var now = DateTime.UtcNow;
      await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString()))
      {
        var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

        if (updates.MessageChanged)
          group.Call("updateIssueMessage", issueRecurrenceId, issueRecurrence.Issue.Message);

        if (updates.OwnerChanged)
          group.Call("updateIssueOwner", issueRecurrenceId, issueRecurrence.Owner.Id, issueRecurrence.Owner.GetName(), issueRecurrence.Owner.ImageUrl(true, ImageSize._32));

        if (updates.PriorityChanged)
          group.Call("updateIssuePriority", issueRecurrenceId, issueRecurrence.Priority);

        if (updates.RankChanged)
        {
          group.Call("updateIssueRank", issueRecurrenceId, issueRecurrence.Rank, true);

          if (issueRecurrence.Rank == 3)
          {
          }
        }

        if (updates.CompletionChanged)
        {
          var added = issueRecurrence.CloseTime == null;
          var completed = issueRecurrence.CloseTime != null;

          var others = s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .Where(x => x.DeleteTime == null && x.Issue.Id == issueRecurrence.Issue.Id)
            .List().ToList();

          foreach (var o in others)
          {
            //rt.UpdateRecurrences(o.Recurrence.Id).Call("updateModedIssueSolve", o.Id, completed);
            if (issueRecurrence.Id == o.Id)
            {
              rt.UpdateRecurrences(o.Recurrence.Id).Call("updateModedIssueSolve", o.Id, completed, true);
            }
            else
            {
              rt.UpdateRecurrences(o.Recurrence.Id).Call("updateModedIssueSolve", o.Id, completed, false);
            }

            var recur = new AngularRecurrence(o.Recurrence.Id);

            var issue = new AngularIssue(issueRecurrence);
            if (issue.CloseTime == null)
            {
              issue.CloseTime = Removed.Date();
            }
            recur.IssuesList.Issues = AngularList.CreateFrom(added ? AngularListType.ReplaceIfNewer : AngularListType.Remove, issue);
            rt.UpdateRecurrences(o.Recurrence.Id).Update(recur);
          }
        }

        if (updates.AwaitingSolveChanged)
        {
          var name = "";
          if (updates.MovedToRecurrence.HasValue)
          {
            name = s.Get<L10Recurrence>(updates.MovedToRecurrence.Value).NotNull(x => x.Name);
          }
          group.Call("updateIssueAwaitingSolve", issueRecurrence.Id, issueRecurrence.AwaitingSolve);
          group.Call("setIssueMoveLocation", issueRecurrence.Id, name);
        }

        group.Update(new AngularIssue(issueRecurrence));
      }
    }

    public async Task SendIssueTo(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence sourceIssue, IssueModel.IssueModel_Recurrence destIssue)
    {
      //noop
    }

  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.DataTypes;
using System.Text;
using RadialReview.Utilities.RealTime;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Todo;
using SpreadsheetLight;
using static RadialReview.Accessors.IssuesAccessor;
using RadialReview.Utilities.Synchronize;
using RadialReview.Utilities.NHibernate;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.VTO;
using Microsoft.AspNetCore.Html;
using RadialReview.Middleware.Services.NotesProvider;
using static RadialReview.Accessors.L10Accessor;
using RadialReview.Core.Accessors.StrictlyAfterExecutors;

namespace RadialReview.Accessors {

  public class IssueCreation {
    private string Message { get; set; }
    private string Details { get; set; }
    private long? OwnerId { get; set; }
    private long? CreatedDuringMeetingId { get; set; }
    private long RecurrenceId { get; set; }
    private DateTime? Now { get; set; }
    private string ForModelType { get; set; }
    private long ForModelId { get; set; }
    private bool _ensured { get; set; }
    private int Priority { get; set; }
    private string PadId { get; set; }
    private string ContextTitle { get; set; }
    private string ContextType { get; set; }
    private bool? AddToDepartmentPlan { get; set; }
    private MergedIssueData MergedIssueData { get; set; }

    private IssueCreation(string message, string details, long? ownerId, long? createdDuringMeetingId, int priority, long recurrenceId, DateTime? now, string forModelType, long forModelId, string padId, string contextTitle, string contextType, bool? addToDepartmentPlan = null, MergedIssueData mergedIssueData = null) {
      Message = message;
      Details = details;
      OwnerId = ownerId;
      CreatedDuringMeetingId = createdDuringMeetingId;
      RecurrenceId = recurrenceId;
      Now = now;
      ForModelType = forModelType;
      ForModelId = forModelId;
      Priority = priority;
      PadId = padId;
      ContextTitle = contextTitle;
      ContextType = contextType;
      AddToDepartmentPlan = addToDepartmentPlan;
      MergedIssueData = mergedIssueData;
    }

    public static IssueCreation CreateL10Issue(string message, string details, long? ownerId, long recurrenceId, long? createdDuringMeeting = null, int priority = 0, string modelType = "IssueModel", long modelId = -1, DateTime? now = null, string padId = null, string contextTitle = null, string contextType = null, bool? addToDepartmentPlan = null, MergedIssueData MergedIssueData = null) {
      if (padId == null) {
        padId = Guid.NewGuid().ToString();
      }

      return new IssueCreation(message, details, ownerId, createdDuringMeeting, priority, recurrenceId, now, modelType, modelId, padId, contextTitle, contextType, addToDepartmentPlan: addToDepartmentPlan, mergedIssueData: MergedIssueData);
    }

    public IssueOutput Generate(ISession s, PermissionsUtility perms) {
      UserOrganizationModel creator = perms.GetCaller();
      EnsurePermitted(perms, creator.Organization.Id);

      var duringMeeting = CreatedDuringMeetingId > 0 ? CreatedDuringMeetingId : null;
      Now = Now ?? DateTime.UtcNow;

      var issue = new IssueModel {
        CreatedById = OwnerId ?? creator.Id,
        CreatedBy = s.Load<UserOrganizationModel>(OwnerId ?? creator.Id),
        CreatedDuringMeetingId = duringMeeting,
        CreatedDuringMeeting = duringMeeting.NotNull(x => s.Load<L10Meeting>(x)),
        CreateTime = Now.Value,
        Description = Details,
        ForModel = ForModelType,
        ForModelId = ForModelId,
        Message = Message,
        Organization = creator.Organization,
        OrganizationId = creator.Organization.Id,
        _Priority = Priority,
        PadId = PadId,
        ContextNodeTitle = ContextTitle,
        ContextNodeType = ContextType,
      };

      var issueRecur = new IssueModel.IssueModel_Recurrence() {
        CopiedFrom = null,
        Issue = issue,
        CreatedBy = issue.CreatedBy,
        CreateTime = issue.CreateTime,
        Recurrence = s.Load<L10Recurrence>(RecurrenceId),
        Owner = s.Load<UserOrganizationModel>(OwnerId ?? creator.Id),
        Priority = issue._Priority,
        AddToDepartmentPlan = AddToDepartmentPlan ?? false,
        MergedIssueData = MergedIssueData,
      };

      var issueHistoryEntry = new Models.Issues.IssueHistoryEntry() {
        EventType = IssueHistoryEventType.Created,

        CreateTime = issue.CreateTime,

        Issue = issueRecur.Issue,
        Meeting = issueRecur.Recurrence,

        ValidFrom = issueRecur.CreateTime,
        ValidUntil = null,
      };

      return new IssueOutput {
        IssueModel = issue,
        IssueRecurrenceModel = issueRecur,
        IssueHistoryEntry = issueHistoryEntry,
      };

    }

    public bool TryGetAddToDepartmentPlan(out bool value)
    {
      if (AddToDepartmentPlan.HasValue)
      {
        value = AddToDepartmentPlan.Value;
        return true;
      }
      else
      {
        value = false;
        return false;
      }
    }
    private void EnsurePermitted(PermissionsUtility perms, long orgId) {
      _ensured = true;

      if (CreatedDuringMeetingId != null && CreatedDuringMeetingId > 0)
        perms.ViewL10Meeting(CreatedDuringMeetingId.Value);
      perms.ViewOrganization(orgId);
      if (OwnerId != null)
        perms.ViewUserOrganization(OwnerId.Value, false);

      perms.EditL10Recurrence(RecurrenceId);
    }

  }



  public partial class IssuesAccessor : BaseAccessor {



    public class IssueOutput {
      public IssueModel IssueModel { get; set; }
      public IssueModel.IssueModel_Recurrence IssueRecurrenceModel { get; set; }

      public Models.Issues.IssueHistoryEntry IssueHistoryEntry { get; set; }
    }

    public static async Task<IssueOutput> CreateIssue(ISession s,
                              PermissionsUtility perms,
                              IssueCreation issueCreator,
                              bool createNotePad = false) {

      var io = issueCreator.Generate(s, perms);

      s.Save(io.IssueModel);
      s.Save(io.IssueRecurrenceModel);

      io.IssueHistoryEntry.IssueId = io.IssueModel.Id;
      io.IssueHistoryEntry.MeetingId = io.IssueRecurrenceModel.Recurrence.Id;
      io.IssueHistoryEntry.Issue = s.Load<IssueModel>(io.IssueHistoryEntry.IssueId);
      io.IssueHistoryEntry.Meeting = s.Load<L10Recurrence>(io.IssueHistoryEntry.MeetingId);

      s.Save(io.IssueHistoryEntry);

      var recurrenceId = io.IssueRecurrenceModel.Recurrence.Id;
      var r = s.Get<L10Recurrence>(recurrenceId);

      if (r.OrderIssueBy == "data-priority") {
        var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
          .Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Priority > io.IssueModel._Priority && x.ParentRecurrenceIssue == null)
          .Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
        var max = -1L;
        if (order.Any())
          max = order.Max() ?? -1;
        max += 1;
        io.IssueRecurrenceModel.Ordering = max;
        s.Update(io.IssueRecurrenceModel);
      }
      if (r.OrderIssueBy == "data-rank") {
        var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
          .Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Rank > io.IssueModel._Rank && x.ParentRecurrenceIssue == null)
          .Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
        var max = -1L;
        if (order.Any())
          max = order.Max() ?? -1;
        max += 1;
        io.IssueRecurrenceModel.Ordering = max;
        s.Update(io.IssueRecurrenceModel);
      }

      if (createNotePad && !string.IsNullOrWhiteSpace(io.IssueModel.Description))
        await PadAccessor.CreatePad(io.IssueModel.PadId, io.IssueModel.Description);

      if (issueCreator.TryGetAddToDepartmentPlan(out var addToDepartmentPlan))
      {
        if (addToDepartmentPlan)
        {
          await IssuesAccessor.MoveIssueToVto(s, perms, io.IssueRecurrenceModel.Id, connectionId: null, notify: false);
        }
      }

      // Trigger webhook events
      var cc = perms.GetCaller();
      await HooksRegistry.Each<IIssueHook>((ses, x) => x.CreateIssue(ses, cc, io.IssueRecurrenceModel));

      return io;

    }


    public static async Task<IssueOutput> CreateIssue(UserOrganizationModel caller, IssueCreation creation, bool createNotePad = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var o = await CreateIssue(s, perms, creation, createNotePad);
          s.Flush();
          tx.Commit();
          return o;
        }
      }
    }

    public static async Task EditIssueVotes(UserOrganizationModel caller, long recurrenceId, long issueId, int numberOfVotes)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);
          var updates = new IIssueHookUpdates();
          var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueId);
          DateTime now = Math2.Min(DateTime.UtcNow.AddSeconds(3), DateTime.UtcNow);
          if (issue.LastUpdate_Stars < now)
          {
            // Always increment on non-zero
            // Always reset on zero
            int newStars = numberOfVotes == 0 ? 0 : issue.Stars + numberOfVotes;
            issue.LastUpdate_Stars = now;
            updates.oldStars = issue.Stars;
            issue.Stars = newStars;
            s.Update(issue);
            updates.StarsChanged = true;
          }

          var cc = perms.GetCaller();
          await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, cc, issue, updates));
          s.Flush();
          tx.Commit();
       
        }
      }
    }


    public enum IssueCompartment {
      ShortTerm = 1,
      LongTerm = 2,
    }

    public static async Task EditIssue(UserOrganizationModel caller, long issueRecurrenceId, string message = null, bool? complete = null,
      long? owner = null, int? priority = null, int? rank = null, bool? awaitingSolve = null, DateTime? now = null, IssueCompartment? compartment = null,
      string noteId = null, bool? archived = null, int? stars = null, bool? addToDepartmentPlan = null) {

      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateIssueMessage(issueRecurrenceId), BuildEditIssueExecutor(issueRecurrenceId, message, complete, owner, priority, rank, awaitingSolve, now, compartment, noteId, archived, stars, addToDepartmentPlan));
    }

    /// <summary>
    /// This function should create a single query to update the rank property, something like:
    /// string hql = "UPDATE Issue SET Rank = 0 WHERE MeetingId = :MeetingId";
    ///
    ///but to keep the SignalR functionality, it was done like this 
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="meetingId"></param>
    /// <returns></returns>
    public static async Task ResetPriorityByRank(UserOrganizationModel caller, long meetingId) {
      var issues = L10Accessor.GetIssuesForRecurrence(caller, meetingId);
      foreach (var issue in issues.Where(i => i.Rank != 0).ToList()) {
        await EditIssue(caller, issue.Id, rank: 0);
      }
    }

    /// <summary>
    /// SyncAction.UpdateIssueMessage(issue.Issue.Id)
    /// </summary>
    /// <param name="issueRecurrenceId"></param>
    /// <param name="message"></param>
    /// <param name="complete"></param>
    /// <param name="owner"></param>
    /// <param name="priority"></param>
    /// <param name="rank"></param>
    /// <param name="awaitingSolve"></param>
    /// <param name="now"></param>
    /// <param name="status"></param>
    /// <param name="noteId"></param>
    /// <param name="archived"></param>
    /// <param name="stars"></param>
    /// <returns></returns>
    public static EditIssueExecutor BuildEditIssueExecutor(long issueRecurrenceId, string message = null,
      bool? complete = null, long? owner = null, int? priority = null, int? rank = null, bool? awaitingSolve = null,
      DateTime? now = null, IssueCompartment? status = null, string noteId = null, bool? archived = null, int? stars = null, bool? addToDepartmentPlan = null) {

      return new EditIssueExecutor(issueRecurrenceId, message, complete, owner, priority, rank, awaitingSolve, now, status, noteId, archived, stars, addToDepartmentPlan);

      //now = Math2.Min(DateTime.UtcNow.AddSeconds(3), now ?? DateTime.UtcNow);
      //var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
      //if (issue == null)
      //  throw new PermissionsException("Issue does not exist.");
      //var recurrenceId = issue.Recurrence.Id;
      //if (recurrenceId == 0)
      //  throw new PermissionsException("Meeting does not exist.");
      //perms.EditL10Recurrence(recurrenceId);
      //var updates = new IIssueHookUpdates();
      //if (message != null && message != issue.Issue.Message) {
      //  issue.Issue.Message = message;
      //  updates.MessageChanged = true;
      //}
      //if (owner != null && (issue.Owner == null || owner != issue.Owner.Id) && owner > 0) {
      //  var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner).Take(1).List().ToList();
      //  if (!any.Any())
      //    throw new PermissionsException("Specified Owner cannot see meeting");

      //  issue.Owner = s.Get<UserOrganizationModel>(owner);
      //  updates.OwnerChanged = true;
      //}
      //if (priority != null && priority != issue.Priority && issue.LastUpdate_Priority < now) {
      //  issue.LastUpdate_Priority = now.Value;
      //  updates.oldPriority = issue.Priority;
      //  issue.Priority = priority.Value;
      //  s.Update(issue);
      //  updates.PriorityChanged = true;
      //}
      //if (rank != null && rank != issue.Rank && issue.LastUpdate_Priority < now) {
      //  issue.LastUpdate_Priority = now.Value;
      //  updates.oldRank = issue.Rank;
      //  issue.Rank = rank.Value;
      //  s.Update(issue);
      //  updates.RankChanged = true;
      //}

      //if (status != null) {
      //  if (status == IssueCompartment.ShortTerm && issue.DeleteTime != null) {
      //    updates.CompartmentChanged = true;
      //    await MoveIssueFromVtoViaIssueRecurrenceId(s, perms, issue.Id);
      //  } else if (status == IssueCompartment.LongTerm && issue.DeleteTime == null) {
      //    updates.CompartmentChanged = true;
      //    await MoveIssueToVto(s, perms, issue.Id, perms.GetCaller().NotNull(x => x.GetClientRequestId()));
      //  }
      //}
      //var now1 = DateTime.UtcNow;
      //if (complete != null) {
      //  if (complete.Value && issue.CloseTime == null) {
      //    updates.CompletionChanged = true;
      //  } else if (!complete.Value && issue.CloseTime != null) {
      //    updates.CompletionChanged = true;
      //  }
      //  _UpdateIssueCompletion_Unsafe(s, issue, complete.Value, now1);
      //}
      //if (awaitingSolve != null && awaitingSolve != issue.AwaitingSolve) {
      //  issue.AwaitingSolve = awaitingSolve.Value;
      //  s.Update(issue);
      //  var sentToMeeting = s.QueryOver<IssueModel.IssueModel_Recurrence>()
      //            .Where(x => x.DeleteTime == null && x.CopiedFrom.Id == issueRecurrenceId)
      //            .Select(x => x.Recurrence.Id).List<long>()
      //            .FirstOrDefault();
      //  updates.MovedToRecurrence = sentToMeeting == 0 ? null : (long?)sentToMeeting;
      //  updates.AwaitingSolveChanged = true;
      //}
      //var cc = perms.GetCaller();
      //await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, cc, issue, updates));

    }

    public static async Task<VtoItem_String> MoveIssueToVto(ISession s, PermissionsUtility perm, long issue_recurrence, string connectionId, bool notify = true) {
      await using (var rt = RealTimeUtility.Create(connectionId)) {

        var recurIssue = s.Get<IssueModel.IssueModel_Recurrence>(issue_recurrence);

        recurIssue.Rank = 0;
        recurIssue.Priority = 0;
        recurIssue.DeleteTime = DateTime.UtcNow;
        recurIssue.AddToDepartmentPlan = true; // This is set to true because it becomes a VTO/Business plan Long Term Issue

        if (recurIssue.Issue != null && recurIssue.Issue.Message != null && recurIssue.Issue.Message.StartsWith(ModeHelpers.LONGTERM_PREFIX)) {
          recurIssue.Issue.Message = recurIssue.Issue.Message.SubstringAfter(ModeHelpers.LONGTERM_PREFIX);
        }

        s.Update(recurIssue);

        var recur = s.Get<L10Recurrence>(recurIssue.Recurrence.Id);

        //remove from list
        rt.UpdateRecurrences(recur.Id).Call("removeIssueRow", recurIssue.Id);
        var arecur = new AngularRecurrence(recur.Id);
        arecur.IssuesList.Issues = AngularList.CreateFrom(AngularListType.Remove, new AngularIssue(recurIssue));
        rt.UpdateRecurrences(recur.Id).Update(arecur);

        if (notify)
        {
          var cc = perm.GetCaller();
          IIssueHookUpdates updates = new IIssueHookUpdates();
          updates.AddToDepartmentPlanChanged = true;
          updates.AddToDepartmentPlan = true;
          await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, cc, recurIssue, updates));
        }

        perm.EditVTO(recur.VtoId);
        perm.ViewVTOTraction(recur.VtoId);
         var vto = s.Get<VtoModel>(recur.VtoId);

        var str = await VtoAccessor.AddString(s, perm, recur.VtoId, VtoItemType.List_Issues,
          (v, list) => new AngularVTO(v.Id) { Issues = list },
          forModel: ForModel.Create(recurIssue), value: recurIssue.Issue.Message);

        return str;
      }
    }

    public async static Task<IssueModel.IssueModel_Recurrence> MoveIssueFromVtoViaIssueRecurrenceId(ISession s, PermissionsUtility perms, long issue_recurrence) {
      var modelType = ForModel.GetModelType<IssueModel.IssueModel_Recurrence>();
      var found = s.QueryOver<VtoItem_String>().Where(x => x.DeleteTime == null && x.ForModel.ModelId == issue_recurrence && x.ForModel.ModelType == modelType).Take(1).SingleOrDefault();

      return await MoveIssueFromVto(s, perms, found.Id);
    }


    public async static Task<IssueModel.IssueModel_Recurrence> MoveIssueFromVto(ISession s, PermissionsUtility perm, long vtoIssue, bool notify = true) {
      var now = DateTime.UtcNow;
      var vtoIssueStr = s.Get<VtoItem_String>(vtoIssue);

      IssueModel.IssueModel_Recurrence issueRecur;
      perm.EditVTO(vtoIssueStr.Vto.Id);

      vtoIssueStr.DeleteTime = now;
      s.Update(vtoIssueStr);

      if (vtoIssueStr.ForModel != null) {
        if (vtoIssueStr.ForModel.ModelType != ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
          throw new PermissionsException("ModelType was unexpected");
        issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(vtoIssueStr.ForModel.ModelId);

        var recur = s.Get<L10Recurrence>(issueRecur.Recurrence.Id);

        perm.EditL10Recurrence(issueRecur.Recurrence.Id);

        issueRecur.DeleteTime = null;
        s.Update(issueRecur);
        //Add back to issues list (does not need to be added below. CreateIssue calls this.
        await using (var rt = RealTimeUtility.Create()) {
          var meetingHub = rt.UpdateRecurrences(issueRecur.Recurrence.Id);
          meetingHub.Call("appendIssue", ".issues-list", IssuesData.FromIssueRecurrence(issueRecur), recur.OrderIssueBy);
        }
      } else {
        var vto = s.Get<VtoModel>(vtoIssueStr.Vto.Id);
        if (vto.L10Recurrence == null)
          throw new PermissionsException("Expected L10Recurrence was null");
        var creation = IssueCreation.CreateL10Issue(vtoIssueStr.Data, null, perm.NotNull(x => x.GetCaller().Id), vto.L10Recurrence.Value);
        var issue = await IssuesAccessor.CreateIssue(s, perm, creation);
        var recur = s.Get<L10Recurrence>(vto.L10Recurrence.Value);

        issueRecur = issue.IssueRecurrenceModel;
      }
      //Remove from vto
      await using (var rt = RealTimeUtility.Create()) {
        var group = rt.UpdateVtos(vtoIssueStr.Vto.Id);
        vtoIssueStr.Vto = null;
        group.Update(AngularVtoString.Create(vtoIssueStr));
      }

      if (notify)
      {
        var cc = perm.GetCaller();
        IIssueHookUpdates updates = new IIssueHookUpdates();
        updates.AddToDepartmentPlanChanged = true;
        updates.AddToDepartmentPlan = false;
        await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, cc, issueRecur, updates));
      }

      return issueRecur;
    }

    //[Obsolete("Do not use",true)]
    //public static void _UpdateIssueCompletion_Unsafe(ISession s, IssueModel.IssueModel_Recurrence issue, bool complete, DateTime? now = null) {
    //  now = now ?? DateTime.UtcNow;
    //  bool? added = null;
    //  if (complete && issue.CloseTime == null) {
    //    issue.CloseTime = now;
    //    added = false;
    //  } else if (!complete && issue.CloseTime != null) {
    //    issue.CloseTime = null;
    //    var childIssue = s.QueryOver<IssueModel.IssueModel_Recurrence>()
    //        .Where(x => x.CopiedFrom.Id == issue.Id)
    //        .List().FirstOrDefault();
    //    issue.MarkedForClose = childIssue != null && childIssue.CloseTime != null;
    //    added = true;
    //  }

    //  s.Update(issue);
    //  if (added != null) {
    //    var others = s.QueryOver<IssueModel.IssueModel_Recurrence>()
    //        .Where(x => x.DeleteTime == null && x.Issue.Id == issue.Issue.Id)
    //        .List().ToList();

    //    //Not sure what I was thinking here...
    //    foreach (var o in others) {
    //      if (o.Id != issue.Id) {
    //        o.MarkedForClose = complete;
    //        if (complete)
    //          o.AwaitingSolve = true;
    //        s.Update(o);
    //      }
    //    }
    //  }
    //}

    public static IssueModel GetIssue(UserOrganizationModel caller, long issueId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ViewIssue(issueId);
          return s.Get<IssueModel>(issueId);
        }
      }
    }

    public static List<IssueModel.IssueModel_Recurrence> GetVisibleIssuesForUser(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);

          // only get meetings visible to me.
          var list = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, caller.Id, true, false).Select(x => x.Id).ToList();

          return s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .Where(x => x.DeleteTime == null
            && x.CloseTime == null
            && x.Owner.Id == userId).WhereRestrictionOn(x => x.Recurrence.Id).IsIn(list).Fetch(x => x.Issue).Eager.List().ToList();
        }
      }
    }


    public static List<IssueModel.IssueModel_Recurrence> GetRecurrenceIssuesForUser(UserOrganizationModel caller, long userId, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);

          return s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .Where(x => x.DeleteTime == null
            && x.Recurrence.Id == recurrenceId
            && x.CloseTime == null
            && x.Owner.Id == userId).Fetch(x => x.Issue).Eager.List().ToList();
        }
      }
    }

    public static IssueModel.IssueModel_Recurrence GetIssue_Recurrence(UserOrganizationModel caller, long recurrence_issue, bool disposeSession = true, bool loadSentTo = false) {
      var s = HibernateSession.GetCurrentSession();
      if (disposeSession) {
        using (s) {
          return DoIt();
        }
      } else {
        return DoIt();
      }

      IssueModel.IssueModel_Recurrence DoIt() {
        using (var tx = s.BeginTransaction()) {
          var found = s.Get<IssueModel.IssueModel_Recurrence>(recurrence_issue);

          PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(found.Recurrence.Id);

          found.Issue = s.Get<IssueModel>(found.Issue.Id);
          found.Recurrence = s.Get<L10Recurrence>(found.Recurrence.Id);

          if (loadSentTo) {
            var result = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.CopiedFrom != null && x.CopiedFrom.Id == recurrence_issue).SingleOrDefault();
            if (result != null) {
              found._MovedToIssueId = result.Id;
              found._MovedToMeetingName = result.Recurrence.Name;
            }
          }

          return found;
        }
      }
    }

    public static List<IssueModel.IssueModel_Recurrence> GetIssues_MovedToFix(List<IssueModel.IssueModel_Recurrence> models) {
      var s = HibernateSession.GetCurrentSession();
      List<long> modelIds = models.Select(x => x.Id).ToList();
      L10Recurrence recurrenceAlias = null;
      var result = s.QueryOver<IssueModel.IssueModel_Recurrence>().Where(x => x.CopiedFrom != null)
        .JoinAlias(x => x.Recurrence, () => recurrenceAlias)
        .WhereRestrictionOn(x => x.CopiedFrom.Id).IsIn(modelIds)
        .Select(x => x.Id, x => x.CopiedFrom.Id, x => x.Recurrence.Id, x => recurrenceAlias.Name)
        .Future<object[]>().Select(x => new IssueModel.IssueModel_Recurrence {
          Id = (long)x[0],
          CopiedFrom = new IssueModel.IssueModel_Recurrence {
            Id = (long)x[1]
          },
          Recurrence =  new L10Recurrence {
            Id = (long)x[2],
            Name = (string)x?[3]
          }
        })
        .ToList();
      foreach (var model in models) {
        var issue = result.Where(x => x.CopiedFrom.Id == model.Id).LastOrDefault();
        if (issue != null) {
          model._MovedToIssueId = issue.Id;
          model._MovedToMeetingName = issue.Recurrence.Name;
        }
      }

      return models;
    }

    public static async Task<IssueModel.IssueModel_Recurrence> CopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId, bool AwaitingSolveParent = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var now = DateTime.UtcNow;
          var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);

          PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(parent.Recurrence.Id)
            .ViewIssue(parent.Issue.Id);

          var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

          if (childRecur.Organization.Id != caller.Organization.Id)
            throw new PermissionsException("You cannot copy an issue into this meeting.");
          if (parent.DeleteTime != null)
            throw new PermissionsException("Issue does not exist.");

          var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);
          if (possible.All(x => x.Id != childRecurrenceId)) {
            throw new PermissionsException("You do not have permission to copy this issue.");
          }

          if (!L10Accessor._GetAllConnectedL10Recurrence(s, caller, parent.Recurrence.Id, false, true).Any(x => x.Id == childRecurrenceId)) {
            throw new PermissionsException("You do not have permission to copy this issue.");
          }

          var parentRecur = s.Get<L10Recurrence>(parent.Recurrence.Id);


          var issue_recur = new IssueModel.IssueModel_Recurrence() {
            ParentRecurrenceIssue = null,
            CreateTime = now,
            CopiedFrom = parent,
            CreatedBy = caller,
            Issue = s.Load<IssueModel>(parent.Issue.Id),
            Recurrence = s.Load<L10Recurrence>(childRecurrenceId),
            Owner = parent.Owner,
            FromWhere = parentRecur.Name
          };

          // Update the AwaitingSolve property of the parent issue in case the parameter "AwaitingSolveParent" is true
          if (AwaitingSolveParent)
          {
            parent.AwaitingSolve = true;
            s.Save(parent);
          }

          s.Save(issue_recur);
          var viewModel = IssuesData.FromIssueRecurrence(issue_recur);
          _RecurseCopy(s, viewModel, caller, parent, issue_recur, now);
          tx.Commit();
          s.Flush();

          await using (var rt = RealTimeUtility.Create())
          {
            var childMeetingHub = rt.UpdateRecurrences(childRecurrenceId);
            childMeetingHub.Call("appendIssue", ".issues-list", viewModel);
          }
          var issue = s.Get<IssueModel>(parent.Issue.Id);
          await Audit.L10Log(s, caller, parent.Recurrence.Id, "CopyIssue", ForModel.Create(issue_recur), issue.NotNull(x => x.Message) + " copied into " + childRecur.NotNull(x => x.Name));
          return issue_recur;
        }
      }
    }


    private static void _RecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, IssueModel.IssueModel_Recurrence parent, DateTime now) {
      var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
        .Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
        .List();
      var childrenVMs = new List<IssuesData>();
      foreach (var child in children) {
        var issue_recur = new IssueModel.IssueModel_Recurrence() {
          ParentRecurrenceIssue = parent,
          CreateTime = now,
          CopiedFrom = child,
          CreatedBy = caller,
          Issue = s.Load<IssueModel>(child.Issue.Id),
          Recurrence = s.Load<L10Recurrence>(parent.Recurrence.Id),
          Owner = s.Load<UserOrganizationModel>(parent.Owner.Id)
        };
        s.Save(issue_recur);
        var childVM = IssuesData.FromIssueRecurrence(issue_recur);
        childrenVMs.Add(childVM);
        _RecurseCopy(s, childVM, caller, child, issue_recur, now);
      }
      viewModel.children = childrenVMs.ToArray();
    }

    public static async Task<IssueModel.IssueModel_Recurrence> UnCopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var now = DateTime.UtcNow;

          var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);

          PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(parent.Recurrence.Id)
            .ViewIssue(parent.Issue.Id);

          var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

          if (childRecur.Organization.Id != caller.Organization.Id)
            throw new PermissionsException("You cannot Uncopy an issue into this meeting.");
          if (parent.DeleteTime != null)
            throw new PermissionsException("Issue does not exist.");

          var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);
          if (possible.All(x => x.Id != childRecurrenceId)) {
            throw new PermissionsException("You do not have permission to uncopy this issue.");
          }

          var getL10RecurrenceChild = s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .Where(x => x.DeleteTime == null && x.Recurrence.Id == childRecurrenceId && x.Issue.Id == parent.Issue.Id)
            .SingleOrDefault();

          if (getL10RecurrenceChild == null) {
            throw new PermissionsException("Issue Recurrence does not exist.");
          }

          getL10RecurrenceChild.DeleteTime = now;
          s.Update(getL10RecurrenceChild);

          var viewModel = IssuesData.FromIssueRecurrence(getL10RecurrenceChild);
          _UnRecurseCopy(s, viewModel, caller, parent, now);
          tx.Commit();
          s.Flush();

          await using (var rt = RealTimeUtility.Create()) {
            var meetingHub = rt.UpdateRecurrences(childRecurrenceId);
            meetingHub.Call("removeIssueRow", getL10RecurrenceChild.Id);
          }
          var issue = s.Get<IssueModel>(parent.Issue.Id);
          await Audit.L10Log(s, caller, parent.Recurrence.Id, "UnCopyIssue", ForModel.Create(getL10RecurrenceChild), issue.NotNull(x => x.Message) + " Uncopied from " + childRecur.NotNull(x => x.Name));
          return getL10RecurrenceChild;
        }
      }
    }

    private static void _UnRecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, DateTime now) {
      var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
        .Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
        .List();
      var childrenVMs = new List<IssuesData>();
      foreach (var child in children) {
        child.DeleteTime = now;
        s.Update(child);
        var childVM = IssuesData.FromIssueRecurrence(child);
        childrenVMs.Add(childVM);
        _UnRecurseCopy(s, childVM, caller, child, now);
      }
      viewModel.children = childrenVMs.ToArray();
    }

    public static Csv Listing(UserOrganizationModel caller, long organizationId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

          var sb = new StringBuilder();
          sb.Append("Id,Depth,Owner,Created,Closed,Issue");
          var csv = new Csv();
          IssueModel issueA = null;

          var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .JoinAlias(x => x.Issue, () => issueA)
            .Where(x => x.DeleteTime == null)
            .Where(x => issueA.OrganizationId == organizationId)
            .Fetch(x => x.Issue).Eager
            .List().ToList();

          foreach (var t in issues) {
            var time = "";
            csv.Add("" + t.Id, "Owner", t.Owner.NotNull(x => x.GetName()));
            csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
            if (t.CloseTime != null)
              time = t.CloseTime.Value.ToShortDateString();
            csv.Add("" + t.Id, "Completed", time);
            csv.Add("" + t.Id, "Issue", "" + t.Issue.Message);

          }

          csv.SetTitle("Issues");
          return csv;
        }
      }
    }

    public class IssueAndTodos {
      public IssueModel.IssueModel_Recurrence Issue { get; set; }
      public List<TodoModel> Todos { get; set; }
    }
    public static async Task<SLDocument> GetIssuesAndTodosSpreadsheetAtOrganization(UserOrganizationModel caller, INotesProvider notesProvider, long orgId, bool loadDetails = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ManagingOrganization(orgId);

          IssueModel issueAlias = null;
          var issuesQ = s.QueryOver<IssueModel.IssueModel_Recurrence>()
            .JoinAlias(x => x.Issue, () => issueAlias)
            .Where(x => x.DeleteTime == null && issueAlias.OrganizationId == orgId && issueAlias.DeleteTime == null)
            .Future();


          var todosQ = s.QueryOver<TodoModel>()
            .Where(x => x.ForModel == "IssueModel" && x.DeleteTime == null && x.OrganizationId == orgId)
            .Future();

          var result = new List<IssueAndTodos>();

          var allTodos = todosQ.ToList();


          var pads = new List<string>();
          foreach (var issue in issuesQ) {
            var iat = new IssueAndTodos();
            iat.Issue = issue;
            iat.Todos = allTodos.Where(x => x.ForModelId == issue.Issue.Id && x.ForModel == "IssueModel").ToList();
            pads.Add(issue.Issue.PadId);
            pads.AddRange(iat.Todos.Select(x => x.PadId));
            result.Add(iat);
          }

          var padLookup = new Dictionary<string, string>();

          if (loadDetails) {
            padLookup = await notesProvider.GetTextForPads(pads);
          }

          var issuesSheet = new Csv("Issues");
          foreach (var iat in result) {
            var ir = iat.Issue;
            var issue = ir.Issue;
            var id = issue.Id;
            issuesSheet.Add("" + id, "Issue", issue.Message);
            if (loadDetails) {
              var details = padLookup.GetOrDefault(issue.PadId, "");
              issuesSheet.Add("" + id, "Details", details);
            }
            issuesSheet.Add("" + id, "Owner", ir.Owner.Name);
            issuesSheet.Add("" + id, "Completed", ir.CloseTime.NotNull(x => x.Value.ToShortDateString()));
            issuesSheet.Add("" + id, "Created", issue.CreateTime.ToShortDateString());

            issuesSheet.Add("" + id, "# Todos", "" + iat.Todos.Count());

          }

          var todoSheet = new Csv("Todos");
          foreach (var todo in result.SelectMany(x => x.Todos)) {
            var id = todo.Id;
            todoSheet.Add("" + id, "Todo", todo.Message);
            if (loadDetails) {
              var details = padLookup.GetOrDefault(todo.PadId, "");
              todoSheet.Add("" + id, "Details", details);
            }
            todoSheet.Add("" + id, "Owner", todo.AccountableUser.Name);
            todoSheet.Add("" + id, "Completed", todo.CompleteTime.NotNull(x => x.Value.ToShortDateString()));
            todoSheet.Add("" + id, "Created", todo.CreateTime.ToShortDateString());
            todoSheet.Add("" + id, "IssueId", "" + todo.ForModelId);
          }
          return CsvUtility.ToXls(true, todoSheet, issuesSheet);
        }
      }
    }

    public static async Task CopyMultipleIssues(UserOrganizationModel caller, CopyMultipleIssuesVM data) {
      using (var session = HibernateSession.GetCurrentSession()) {
        var now = DateTime.UtcNow;
        IssueModel.IssueModel_Recurrence parentIssueRecurrence = session.Get<IssueModel.IssueModel_Recurrence>(data.ParentIssue_RecurrenceId);

        PermissionsUtility.Create(session, caller)
          .ViewL10Recurrence(parentIssueRecurrence.Recurrence.Id)
          .ViewIssue(parentIssueRecurrence.Issue.Id);

        IList<L10Recurrence> recurrences = session.QueryOver<L10Recurrence>()
                    .WhereRestrictionOn(x => x.Id).IsIn(data.RecurrenceIds)
                    .List();

        if (recurrences.Any(x => x.Organization.Id != caller.Organization.Id))
          throw new PermissionsException("You cannot copy an issue into this meeting.");

        if (recurrences.Any(x => x.DeleteTime != null))
          throw new PermissionsException("You cannot copy an issue into this meeting.");

        if (parentIssueRecurrence.DeleteTime != null)
          throw new PermissionsException("Issue does not exist.");

        if (!data.RecurrenceIds.ContainsAll(recurrences.SelectId()))
          throw new PermissionsException("You cannot copy an issue into this meeting.");

        L10Recurrence parentRecurrence = session.Get<L10Recurrence>(parentIssueRecurrence.Recurrence.Id);

        using (var tx = session.BeginTransaction()) {
          IssueModel.IssueModel_Recurrence issureRecurrence;
          IssuesData issueRecord;

          foreach (var recurrenceId in data.RecurrenceIds) {
            issureRecurrence = new IssueModel.IssueModel_Recurrence() {
              ParentRecurrenceIssue = null,
              CreateTime = now,
              CopiedFrom = parentIssueRecurrence,
              CreatedBy = caller,
              Issue = parentIssueRecurrence.Issue,
              Recurrence = recurrences.FirstOrDefault(x => x.Id == recurrenceId),
              Owner = parentIssueRecurrence.Owner,
              FromWhere = parentRecurrence.Name
            };

            session.Save(issureRecurrence);
            issueRecord = IssuesData.FromIssueRecurrence(issureRecurrence);
            _RecurseCopy(session, issueRecord, caller, parentIssueRecurrence, issureRecurrence, now);

            await using (var rt = RealTimeUtility.Create()) {
              var childMeetingHub = rt.UpdateRecurrences(recurrenceId);
              //childMeetingHub.Call("appendIssue", ".issues-list", issueRecord);
              childMeetingHub.Call("appendIssue", ".issues-list", new {
                accountable = issueRecord.accountable,
                awaitingsolve = issueRecord.awaitingsolve,
                @checked = issueRecord.@checked,
                children = issueRecord.children,
                createdDuringMeetingId = issueRecord.createdDuringMeetingId,
                createtime = issueRecord.createtime,
                details = issueRecord.details,
                imageUrl = issueRecord.imageUrl,
                issue = issueRecord.issue,
                markedforclose = issueRecord.markedforclose,
                message = issueRecord.message,
                owner = issueRecord.owner,
                priority = issueRecord.priority,
                rank = issueRecord.rank,
                recurrence_issue = issueRecord.recurrence_issue,
                FromWhere = issureRecurrence.FromWhere,
                _MovedToMeetingName = issureRecurrence._MovedToMeetingName,
              });
            }
          }

          IssueModel issue = session.Get<IssueModel>(parentIssueRecurrence.Issue.Id);
          string logMessage = issue.NotNull(x => x.Message) + " copied into " + string.Join(",", recurrences.Select(x => x.Name));
          await Audit.L10Log(session, caller, parentIssueRecurrence.Recurrence.Id, "CopyIssue", ForModel.Create(issue), logMessage);

          tx.Commit();
          session.Flush();
        }
      }
    }

    public static IEnumerable<GraphQL.Models.IssueSentToMeetingDTO> GetIssuesSentToForRecurrence(UserOrganizationModel caller, long recurrenceId) {

      //throw new NotImplementedException("TODO");

      return Enumerable.Empty<GraphQL.Models.IssueSentToMeetingDTO>();

    }

    public static List<GraphQL.Models.IssueHistoryEntryQueryModel> GetVisibleIssueHistoryEntries(UserOrganizationModel caller, long[] issueIds) {

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);

          if (!issueIds.Any())
            return new List<GraphQL.Models.IssueHistoryEntryQueryModel>();

          issueIds = issueIds.Distinct().ToArray();

          //Permissions below
          IssueModel issueAlias = null;
          var issueHistoryEntries = s.QueryOver<IssueHistoryEntry>()
                  .JoinAlias(x => x.Issue, () => issueAlias)
                  .Where(x => issueAlias.DeleteTime==null)
                  .WhereRestrictionOn(x => x.IssueId).IsIn(issueIds.ToArray())
                  .List().ToDefaultDictionary(x => x.Id, x => x, x => null);

          //var recurrenceViewable = issueHistoryEntries.Select(x => x.MeetingId).Distinct().ToDictionary(x=>x,
          //  rid=> {
          //    try {
          //      perms.ViewL10Recurrence(rid);
          //      return true;
          //    } catch (Exception) {
          //      return false;
          //    }
          //  });

          var reducedIssueIds = issueHistoryEntries.Keys.Distinct().ToList();


          //THIS IS NOT OPTIMIZED AT ALL....
          var issueViewable = reducedIssueIds.ToDictionary(x => x, x => {
            try {
              perms.ViewIssue(x);
              return true;
            } catch (Exception ex) {
              return false;
            }
          });

          //
          return
            issueIds
            .Select(issueId => issueHistoryEntries[issueId])
            .Where(issueHistoryEntry => issueHistoryEntry != null)
            .Select(issueHistoryEntry => {
              var issueId = issueHistoryEntry.IssueId;

              //Override if doesnt exist or is not viewable
              if (!issueViewable[issueId])
                return new GraphQL.Models.IssueHistoryEntryQueryModel() {
                  IssueId = issueHistoryEntry.IssueId,
                  MeetingId = issueHistoryEntry.MeetingId,
                  EventType = IssueHistoryEventType.InformationPrivileged
                };

              return new GraphQL.Models.IssueHistoryEntryQueryModel() {
                Id = issueHistoryEntry.Id,
                // DateCreated = x.DateCreated,
                EventType = issueHistoryEntry.EventType,
                ValidFrom = issueHistoryEntry.ValidFrom,
                ValidUntil = issueHistoryEntry.ValidUntil,
                IssueId = issueHistoryEntry.IssueId,
                MeetingId = issueHistoryEntry.MeetingId,
              };
            }).ToList();
        }
      }
    }
  }
}

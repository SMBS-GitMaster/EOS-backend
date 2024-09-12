using Amazon.S3.Model;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    Task<bool> GetGoalAddToDepartmentPlan(long id, CancellationToken cancellationToken);

    IQueryable<GoalQueryModel> GetGoalsForUser(long userId, CancellationToken cancellationToken);

    GoalQueryModel GetGoalById(long id, CancellationToken cancellationToken);

    IQueryable<GoalQueryModel> GetGoalsForMeetings(IEnumerable<long> meetingRecurrenceIds, CancellationToken cancellationToken);

    IQueryable<DepartmentPlanRecordQueryModel> GetGoalDepartmentPlanRecords(IEnumerable<long> goalIds, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateGoal(GoalCreateModel goalCreateModel);

    Task<IdModel> EditGoal(GoalEditModel goalEditModel);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public Task<bool> GetGoalAddToDepartmentPlan(long id, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var goal = s.Get<L10Meeting.L10Meeting_Rock>(id);

          // Check permissions
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(goal.ForRecurrence.Id);
          perms.ViewL10Meeting(goal.L10Meeting.Id);

          return Task.FromResult(goal.VtoRock);
        }
      }
    }

    public GoalQueryModel GetGoalById(long id, CancellationToken cancellationToken)
    {
      var goal = RockAccessor.GetRock(caller, id);
      var recurrencesInGoal = RockAccessor.GetRecurrencesContainingRock(caller, id);
      goal._GoalRecurenceRecords = recurrencesInGoal;
      return RepositoryTransformers.TransformRock(goal, null);
    }

    public IQueryable<GoalQueryModel> GetGoalsForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      List<GoalQueryModel> results = new List<GoalQueryModel>();
      results.AddRange(recurrenceIds.SelectMany(recurrenceId =>
      {
        return GetGoalsForMeeting(recurrenceId, cancellationToken);
      }));

      return results.AsQueryable();
    }

    public IQueryable<GoalQueryModel> GetGoalsForUser(long userId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        var goals = RockAccessor.GetRocksForUser(caller, userId);
        var goalIds = goals.Select(x => x.Id).ToList();

        var recInGoals = RockAccessor.GetRecurrencesContainingRocks(caller, goalIds);

        var recInGoalsDic = recInGoals.GroupBy(x => x.RockId).ToDictionary(g => g.Key, g => g.ToList());

        return goals.Select(x =>
        {
            if (recInGoalsDic.TryGetValue(x.Id, out var recurrencesInGoal))
                x._GoalRecurenceRecords = recurrencesInGoal;

            return RepositoryTransformers.TransformRock(x, null/*TODO what to do here?*/);
        }).ToList().AsQueryable();
        //throw new Exception("Why is this returing measurables?");
        //throw new Exception("This method (correctly) does not use userId. I recommend removing it from the interface.");
        //throw new Exception("Never ever rely on user supplied userIds");
        //return ScorecardAccessor.GetUserMeasurables(caller, userId).Select(goal => RepositoryTransformers.GoalFromMeasurable(goal));
      });
    }

    private IQueryable<GoalQueryModel> GetGoalsForMeeting(long recurrenceId, CancellationToken cancellationToken)
    {
      // New query for FE based on meeting
      // Get Goals (rocks) for current or latest meeting
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          // Ensure we can see this recurrence
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewL10Recurrence(recurrenceId);

          // Get active meeting (or latest meeting if none active)
          long meetingId = 0;
          var activeMeeting = s.QueryOver<L10Recurrence>().Where(x => x.Id == recurrenceId).SingleOrDefault();
          if (activeMeeting.MeetingInProgress != null)
            meetingId = (long)activeMeeting.MeetingInProgress;

          if (meetingId > 0)
          {
            // Ensure we can see this meeting
            perms.ViewL10Meeting(meetingId);

            // Get the data
            var goals = s.QueryOver<L10Meeting.L10Meeting_Rock>()
              .Where(x => x.L10Meeting.Id == meetingId && x.ForRecurrence.Id == recurrenceId)
              .List().ToList()
              ;

            var goalIds = goals.Select(x => x.ForRock.Id).ToList();

            var recInGoals = RockAccessor.GetRecurrencesContainingRocks(s, perms, goalIds);

            var recInGoalsDic = recInGoals.GroupBy(x => x.RockId).ToDictionary(g => g.Key, g => g.ToList());

            return goals.Select(x =>
            {
              if (recInGoalsDic.TryGetValue(x.ForRock.Id, out var recurrencesInGoal))
                x._recurrenceMilestoneSettings = recurrencesInGoal;
              
              return RepositoryTransformers.TransformRock(x);
            }).ToList().AsQueryable();
          }

          // Never started this meeting
          var g = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
            .Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null)
          .List().ToList()
          ;

          var gIds = g.Select(x => x.ForRock.Id).ToList();

          var recurrencesInGoals = RockAccessor.GetRecurrencesContainingRocks(s, perms, gIds);

          var recurrencesInGoalDic = recurrencesInGoals.GroupBy(x => x.RockId).ToDictionary(g => g.Key, g => g.ToList());

          return g.Select(x =>
          {
            if (recurrencesInGoalDic.TryGetValue(x.ForRock.Id, out var recurrencesInGoal))
              x._GoalRecurrenceRecords = recurrencesInGoal;

            return RepositoryTransformers.TransformRock(x);
          }).ToList().AsQueryable();
        }
      }
    }

    public IQueryable<DepartmentPlanRecordQueryModel> GetGoalDepartmentPlanRecords(IEnumerable<long> goalIds, CancellationToken cancellationToken)
    {
      List<DepartmentPlanRecordQueryModel> results = new List<DepartmentPlanRecordQueryModel>();
      var recurrences = RockAccessor.GetRecurrencesContainingRocks(caller, goalIds.ToList());
      foreach (var recurrence in recurrences)
      {
        results.Add(new DepartmentPlanRecordQueryModel
        {
          IsInDepartmentPlan = recurrence.VtoRock,
          MeetingId = recurrence.RecurrenceId,
          Id = recurrence.RecurrenceRockId
        });
      }

      return results.AsQueryable();
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateGoal(GoalCreateModel body)
    {

      gqlGoalStatus status = gqlGoalStatus.COMPLETED;
      bool validNonNull = Enum.TryParse(body.Status, out status);

      var rock = await RockAccessor.CreateRock(caller, body.Assignee,
        message: body.Title,
        dueDate: body.DueDate.FromUnixTimeStamp(),
        completion: validNonNull ? status.ToRockState() : null,
        padId: body.NotesId
      );

      if (body.MeetingsAndPlans != null)
      {
        foreach (var m in body.MeetingsAndPlans)
        {
          await L10Accessor.AttachRock(caller, m.MeetingId, rock.Id, m.AddToDepartmentPlan ?? false, AttachRockType.Create);
        }
      }

      if(body.Milestones != null)
      {
        foreach (var m in body.Milestones)
        {
          var milestone = await RockAccessor.AddMilestone(caller, rock.Id, m.Title, m.DueDate.FromUnixTimeStamp(), m.Completed, MilestoneStatusExtensions.FromString(m.Status).ToBloomMilestoneStatus());
        }
      }


      return new IdModel(rock.Id);
    }

    public async Task<IdModel> EditGoal(GoalEditModel model)
    {
      gqlGoalStatus status = gqlGoalStatus.COMPLETED;
      bool validNonNull = Enum.TryParse(model.Status, out status);

      await L10Accessor.UpdateRock(
        caller,
        model.GoalId,
        model.Title,
        validNonNull ? status.ToRockState() : null,
        model.Assignee,
        null,
        dueDate: model.DueDate.FromUnixTimeStamp(),
        vtoRock: model.AddToDepartmentPlan,
        noteId: model.NotesId
      );

      if (model.MeetingsAndPlans != null)
      {
        List<long> meetingIds = model.MeetingsAndPlans.Select(_ => _.MeetingId).ToList();
        var existing = RockAccessor.GetRecurrencesContainingRock(caller, model.GoalId);
        var existingRecurIds = existing.Select(x => x.RecurrenceId).ToList();
        var ar = SetUtility.AddRemove(existingRecurIds, meetingIds);

        foreach (var map in model.MeetingsAndPlans)
        {
          if (ar.AddedValues.Contains(map.MeetingId))
          {
            await L10Accessor.AttachRock(caller, map.MeetingId, model.GoalId, map.AddToDepartmentPlan ?? false, AttachRockType.Existing);
          }
          else if (ar.RemainingValues.Contains(map.MeetingId))
          {
            var recurrence = existing.Where(_ => _.RecurrenceId == map.MeetingId).First();
            await L10Accessor.SetVtoRock(caller, recurrence.RecurrenceRockId, map.AddToDepartmentPlan.HasValue ? map.AddToDepartmentPlan.Value : false);
          }  
        }

        var detachTime = DateTime.UtcNow;
        foreach (var removedRecurId in ar.RemovedValues)
        {
          await L10Accessor.RemoveRock(caller, removedRecurId, model.GoalId, detachTime: detachTime);
        }
      }

      if (model.Archived == true)
      {
        var archivedTime = DateTime.UtcNow;
        archivedTime = DateTime.ParseExact(archivedTime.ToString("yyyy-MM-dd HH:mm:ss"), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        await RockAccessor.DetachRockFromAllMeetings(caller, model.GoalId, archivedTime, model.Archived);
      }
      else if (model.Archived == false)
      {
        await RockAccessor.UndeleteRock(caller, model.GoalId, model.Archived);
      }


      if (model.Milestones != null)
      {
        foreach (var m in model.Milestones)
        {
          var _ = await EditMilestone(m);
        }
      }

      return new IdModel(model.GoalId);
    }

    #endregion

  }
}
using Microsoft.AspNetCore.Http;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.Repositories;
using RadialReview.Crosscutting.Hooks;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.WebPages;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    Task<NodeCompletionStatsQueryModel> GetNodeCompletionStats(NodeCompletionArgumentsQueryModel input, CancellationToken cancellationToken);

    List<NodeStatDataQueryModel> GetTodoStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    List<NodeStatDataQueryModel> GetIssueStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    List<NodeStatDataQueryModel> GetMilestoneStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    List<NodeStatDataQueryModel> GetGoalStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public Task<NodeCompletionStatsQueryModel> GetNodeCompletionStats(NodeCompletionArgumentsQueryModel input, CancellationToken cancellationToken)
    {
      NodeCompletionStatsQueryModel result = new NodeCompletionStatsQueryModel
      {
        UserId = caller.Id,
        RecurrenceId = input.RecurrenceId,
        EndDate = input.EndDate.FromUnixTimeStamp(),
        GroupBy = input.GroupBy,
      };

      if(input.StartDate.HasValue)
      {
        result.StartDate = input.StartDate.Value.FromUnixTimeStamp();
      }
      else
      {
        if(input.RecurrenceId.HasValue)
        {
          // Get meeting start date
          result.StartDate = L10Accessor.GetL10Recurrence(caller, input.RecurrenceId.Value, LoadMeeting.False()).CreateTime;
        }
        else
        {
          result.StartDate = result.EndDate;
        }
      }

      return Task.FromResult(result);
    }

    public List<NodeStatDataQueryModel> GetTodoStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      gqlGroupBy enumGroup = (gqlGroupBy)Enum.Parse(typeof(gqlGroupBy), groupBy);
      Dictionary<DateTime, int> counts = new Dictionary<DateTime, int>();

      if (recurrenceId != null)
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          var perms = PermissionsUtility.Create(s, caller);
          return GroupResults(L10Accessor.GetCompletedTodoCountsForRecurrence(s, perms, recurrenceId.Value, new DateRange(startDate, endDate)), startDate, endDate, enumGroup);
        }
      }
      else
      {
        // Personal workspace
      }

      return new List<NodeStatDataQueryModel>();
    }

    public List<NodeStatDataQueryModel> GetIssueStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      gqlGroupBy enumGroupBy = (gqlGroupBy)Enum.Parse(typeof(gqlGroupBy), groupBy);
      Dictionary<DateTime, int> counts = new Dictionary<DateTime, int>();
      DateTime dayStart;

      if (recurrenceId != null)
      {
        // Meeting workspace
        var issues = L10Accessor.GetIssuesForRecurrence(caller, recurrenceId.Value, true, false);

        foreach (var issue in issues)
        {
          if (issue.CloseTime != null)
          {
            dayStart = issue.CloseTime.Value;
            dayStart = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day);
            if (!counts.ContainsKey(dayStart))
            {
              counts.Add(dayStart, 1);
            }
            else
            {
              counts[dayStart] += 1;
            }

          }
        }

        return GroupResults(counts, startDate, endDate, enumGroupBy);
      }
      else
      {
        // Personal workspace
      }

      return new List<NodeStatDataQueryModel>();
    }

    public List<NodeStatDataQueryModel> GetMilestoneStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      gqlGroupBy enumGroupBy = (gqlGroupBy)Enum.Parse(typeof(gqlGroupBy), groupBy);
      Dictionary<DateTime, int> counts = new Dictionary<DateTime, int>();
      DateTime dayStart;

      if (recurrenceId != null)
      {
        // Meeting workspace
        var rocks = L10Accessor.GetRocksForRecurrence(caller, recurrenceId.Value, true);
        var milestones = RockAccessor.GetMilestonesForRocks(caller, rocks.Select(_ => _.ForRock.Id).ToList());
        foreach(var milestoneList in milestones)
        {
          foreach(var milestone in milestoneList.Value)
          {
            if(milestone.Status == Models.Rocks.MilestoneStatus.Done)
            {
              dayStart = milestone.CompleteTime.Value;
              dayStart = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day);
              if (!counts.ContainsKey(dayStart))
              {
                counts.Add(dayStart, 1);
              }
              else
              {
                counts[dayStart] += 1;
              }
            }
          }
        }

        var results = GroupResults(counts, startDate, endDate, enumGroupBy);
        return results;
      }
      else
      {
        // Personal workspace
      }

      return new List<NodeStatDataQueryModel>();
    }

    public List<NodeStatDataQueryModel> GetGoalStats(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      gqlGroupBy enumGroupBy = (gqlGroupBy)Enum.Parse(typeof(gqlGroupBy), groupBy);
      Dictionary<DateTime, int> counts = new Dictionary<DateTime, int>();
      DateTime dayStart;

      if (recurrenceId != null)
      {
        // Meeting workspace
        var rocks = L10Accessor.GetRocksForRecurrence(caller, recurrenceId.Value, true);

        foreach (var rock in rocks)
        {
          if (rock.ForRock.Completion == Models.Enums.RockState.Complete)
          {
            dayStart = rock.ForRock.CompleteTime.HasValue ? rock.ForRock.CompleteTime.Value : rock.CreateTime; // !!! This should have a value if complete but doesn't in db
            dayStart = new DateTime(dayStart.Year, dayStart.Month, dayStart.Day);
            if (!counts.ContainsKey(dayStart))
            {
              counts.Add(dayStart, 1);
            }
            else
            {
              counts[dayStart] += 1;
            }
          }
        }

        var results = GroupResults(counts, startDate, endDate, enumGroupBy);
        return results;
      }
      else
      {
        // Personal workspace
      }

      return new List<NodeStatDataQueryModel>();
    }

    #endregion

    #region Private Methods

    private List<NodeStatDataQueryModel> GroupResults(Dictionary<DateTime, int> counts, DateTime startDate, DateTime endDate, gqlGroupBy groupBy)
    {
      List<NodeStatDataQueryModel> results = new List<NodeStatDataQueryModel>();
      List<Tuple<DateTime, DateTime>> groupings = new List<Tuple<DateTime, DateTime>>();
      DateTime baseStart, baseEnd;

      // Create groupings
      switch (groupBy)
      {
        case gqlGroupBy.DAY:
          int days = (endDate.Date - startDate.Date).Days;
          baseStart = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
          baseEnd = new DateTime(startDate.Year, startDate.Month, startDate.Day, 23, 59, 59);

          for(int i=0; i <= days; i++)
          {
            groupings.Add(new Tuple<DateTime, DateTime>(baseStart, baseEnd));
            baseStart = baseStart.AddDays(1);
            baseEnd = baseEnd.AddDays(1);
          }
          break;
        case gqlGroupBy.WEEK:
          baseStart = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
          baseStart = baseStart.AddDays(-(int)baseStart.DayOfWeek);
          baseEnd = baseStart.AddDays(6).AddHours(23).AddMinutes(59).AddSeconds(59);
          endDate = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);

          while(baseStart <= endDate)
          {
            groupings.Add(new Tuple<DateTime, DateTime>(baseStart, baseEnd));
            baseStart = baseStart.AddDays(7);
            baseEnd = baseEnd.AddDays(7);
          }
          break;
        case gqlGroupBy.MONTH:
          baseStart = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0);
          baseEnd = baseStart.AddMonths(1).AddSeconds(-1);

          while (baseStart <= endDate)
          {
            groupings.Add(new Tuple<DateTime, DateTime>(baseStart, baseEnd));
            baseStart = baseStart.AddMonths(1);
            baseEnd = baseStart.AddMonths(1).AddSeconds(-1);
          }

          break;
        case gqlGroupBy.YEAR:
          baseStart = new DateTime(startDate.Year, 1, 1, 0, 0, 0);
          baseEnd = new DateTime(startDate.Year, 12, 31, 23, 59, 59);

          while(baseStart.Year <= endDate.Year)
          {
            groupings.Add(new Tuple<DateTime, DateTime>(baseStart, baseEnd));
            baseStart = new DateTime(baseStart.Year + 1, 1, 1, 0, 0, 0);  // Add year does not work XD
            baseEnd = new DateTime(baseEnd.Year + 1, 12, 31, 23, 59, 59);
          }

          break;
      }

      // Select grouping values
      int id = 0;
      foreach(var grouping in groupings)
      {
        NodeStatDataQueryModel statDataQueryModel = new NodeStatDataQueryModel
        {
          Id = id++,
          BucketDate = grouping.Item1.ToUnixTimeStamp(),
          BucketValue = 0
        };

        foreach(var group in counts.Where(_ => _.Key >= grouping.Item1 && _.Key <= grouping.Item2))
        {
          statDataQueryModel.BucketValue += group.Value;
        }

        results.Add(statDataQueryModel);
      }


      return results;
    }

    #endregion

  }

}

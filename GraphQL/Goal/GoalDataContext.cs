using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    #region Queries

    Task<IQueryable<GoalQueryModel>> GetGoalsForMeetingsAsync(IEnumerable<long> meetingRecurrenceIds, CancellationToken cancellationToken);

    Task<IQueryable<GoalQueryModel>> GetGoalsForUserAsync(long userId, CancellationToken cancellationToken);

    Task<GoalQueryModel> GetGoalByIdAsync(long id, CancellationToken cancellationToken);

    Task<bool> GetGoalAddToDepartmentPlanAsync(long id, CancellationToken cancellationToken);

    Task<IQueryable<DepartmentPlanRecordQueryModel>> GetGoalDepartmentPlanRecordsAsync(IEnumerable<long> goalIds, CancellationToken cancellationToken);

    #endregion

  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public async Task<IQueryable<GoalQueryModel>> GetGoalsForUserAsync(long userId, CancellationToken cancellationToken)
    {
      return repository.GetGoalsForUser(userId, cancellationToken);
    }

    public async Task<GoalQueryModel> GetGoalByIdAsync(long id, CancellationToken cancellationToken)
    {
      return repository.GetGoalById(id, cancellationToken);
    }

    public async Task<IQueryable<GoalQueryModel>> GetGoalsForMeetingsAsync(IEnumerable<long> meetingRecurrenceIds, CancellationToken cancellationToken)
    {
      return repository.GetGoalsForMeetings(meetingRecurrenceIds, cancellationToken);
    }

    public async Task<bool> GetGoalAddToDepartmentPlanAsync(long id, CancellationToken cancellationToken)
    {
      return await repository.GetGoalAddToDepartmentPlan(id, cancellationToken);
    }

    public async Task<IQueryable<DepartmentPlanRecordQueryModel>> GetGoalDepartmentPlanRecordsAsync(IEnumerable<long> goalIds, CancellationToken cancellationToken)
    {
      return repository.GetGoalDepartmentPlanRecords(goalIds, cancellationToken);
    }


    #endregion

  }

}

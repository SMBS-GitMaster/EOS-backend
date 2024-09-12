using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<IQueryable<MilestoneQueryModel>> GetMilestonesForGoalAsync(long goalId, CancellationToken cancellationToken);

    Task<IQueryable<MilestoneQueryModel>> GetMilestonesForGoalsAsync(IEnumerable<long> rockIds, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<IQueryable<MilestoneQueryModel>> GetMilestonesForGoalAsync(long goalId, CancellationToken cancellationToken)
    {
      return repository.GetMilestonesForGoal(goalId, cancellationToken);
    }

    public async Task<IQueryable<MilestoneQueryModel>> GetMilestonesForGoalsAsync(IEnumerable<long> rockIds, CancellationToken cancellationToken)
    {
      return repository.GetMilestonesForGoals(rockIds, cancellationToken);
    }

  }

}

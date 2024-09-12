using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries
    IQueryable<MetricCustomGoalQueryModel> GetCustomGoalsForMetrics(List<long> measurableIds, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateCustomGoal(CustomGoalCreateModel customGoalCreateModel);

    Task<IdModel> DeleteCustomGoal(CustomGoalDeleteModel customGoalDeleteModel);

    Task<IdModel> EditCustomGoal(CustomGoalEditModel customGoalEditModel);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<MetricCustomGoalQueryModel> GetCustomGoalsForMetrics(List<long> measurableIds, CancellationToken cancellationToken)
    {
      return
        MetricCustomGoalAccessor
          .GetCustomGoalsForMetrics(measurableIds, cancellationToken)
          .Select(x => x.TransformMetricCustomGoal())
          .AsQueryable();
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateCustomGoal(CustomGoalCreateModel customGoalCreateModel)
    {
      var customGoal = await ScorecardAccessor.CreateCustomGoal(caller, customGoalCreateModel);
      return new IdModel(customGoal.Id);
    }

    public async Task<IdModel> EditCustomGoal(CustomGoalEditModel customGoalEditModel)
    {
      var customGoal = await ScorecardAccessor.EditCustomGoal(caller, customGoalEditModel);
      return new IdModel(customGoal.Id);
    }

    public async Task<IdModel> DeleteCustomGoal(CustomGoalDeleteModel customGoalDeleteModel)
    {
      var customGoal = await ScorecardAccessor.DeleteCustomGoal(caller, customGoalDeleteModel);
      return new IdModel(customGoal.Id);
    }

    #endregion

  }
}
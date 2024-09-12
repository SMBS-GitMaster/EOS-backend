using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MilestoneStatus = RadialReview.Models.Rocks.MilestoneStatus;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<MilestoneQueryModel> GetMilestonesForGoal(long goalId, CancellationToken cancellationToken);

    IQueryable<MilestoneQueryModel> GetMilestonesForGoals(IEnumerable<long> rockIds, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateMilestone(MilestoneCreateModel milestoneCreateModel);

    Task<GraphQLResponseBase> DeleteMilestone(long milestoneId);

    Task<IdModel> EditMilestone(MilestoneEditModel todoEditModel);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries
    public IQueryable<MilestoneQueryModel> GetMilestonesForGoal(long goalId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return RockAccessor.GetMilestonesForRock(caller, goalId).Select(x => RepositoryTransformers.TransformMilestone(x));
      });
    }

    public IQueryable<MilestoneQueryModel> GetMilestonesForGoals(IEnumerable<long> rockIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {

        return RockAccessor.GetMilestonesForRocks(caller, rockIds)
        .SelectMany(x => x.Value)
        .Select(x => RepositoryTransformers.TransformMilestone(x))
        .ToList();

      });
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateMilestone(MilestoneCreateModel model)
    {
      var ms = await RockAccessor.AddMilestone(caller, model.RockId, model.Title, model.DueDate.FromUnixTimeStamp(), model.Completed, (MilestoneStatusExtensions.FromString(model.Status)).ToBloomMilestoneStatus());
      return new IdModel(ms.Id);
    }

    public async Task<IdModel> EditMilestone(MilestoneEditModel model)
    {
      MilestoneStatus? milestoneStatus = null;
      if (model.Completed != null)
        milestoneStatus = model.Completed == true ? MilestoneStatus.Done : MilestoneStatus.NotDone;
      await RockAccessor.EditMilestone(caller, model.MilestoneId, model.Title, duedate: model.DueDate.FromUnixTimeStamp(), status: milestoneStatus, bloomStatus: MilestoneStatusExtensions.FromString(model.Status).ToBloomMilestoneStatus());
      return new IdModel(model.MilestoneId);
    }

    public async Task<GraphQLResponseBase> DeleteMilestone(long milestoneId)
    {
      try
      {
        await RockAccessor.DeleteMilestone(caller, milestoneId);
        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    #endregion

  }
}
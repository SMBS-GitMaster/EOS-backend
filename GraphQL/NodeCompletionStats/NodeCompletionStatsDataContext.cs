using HotChocolate.Data.Filters;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IDataContext
  {

    #region Queries

    Task<NodeCompletionStatsQueryModel> GetNodeCompletionStatsAsync(NodeCompletionArgumentsQueryModel input, CancellationToken cancellationToken);

    Task<List<NodeStatDataQueryModel>> GetTodoStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    Task<List<NodeStatDataQueryModel>> GetIssueStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    Task<List<NodeStatDataQueryModel>> GetMilestoneStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    Task<List<NodeStatDataQueryModel>> GetGoalStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken);

    #endregion

  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public Task<NodeCompletionStatsQueryModel> GetNodeCompletionStatsAsync(NodeCompletionArgumentsQueryModel input, CancellationToken cancellationToken)
    {
      return repository.GetNodeCompletionStats(input, cancellationToken);
    }

    public Task<List<NodeStatDataQueryModel>> GetTodoStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetTodoStats(recurrenceId, startDate, endDate, groupBy, cancellationToken));
    }

    public Task<List<NodeStatDataQueryModel>> GetIssueStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetIssueStats(recurrenceId, startDate, endDate, groupBy, cancellationToken));
    }

    public Task<List<NodeStatDataQueryModel>> GetMilestoneStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetMilestoneStats(recurrenceId, startDate, endDate, groupBy, cancellationToken));
    }

    public Task<List<NodeStatDataQueryModel>> GetGoalStatsAsync(long? recurrenceId, DateTime startDate, DateTime endDate, string groupBy, CancellationToken cancellationToken)
    {
      return Task.FromResult(repository.GetGoalStats(recurrenceId, startDate, endDate, groupBy, cancellationToken));
    }

    #endregion

  }
}
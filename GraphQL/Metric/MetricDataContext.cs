namespace RadialReview.Repositories
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using RadialReview.Core.GraphQL.MetricAddExistingLookup;
  using RadialReview.Core.GraphQL.MetricFormulaLookup;
  using RadialReview.GraphQL.Models;

  public partial interface IDataContext
  {

    #region Queries

    Task<MetricQueryModel> GetMetricByIdAsync(long id, CancellationToken cancellationToken);

    Task<IQueryable<MetricQueryModel>> GetMetricsForUserAsync(CancellationToken cancellationToken);

    Task<MetricQueryModel> GetMetricById(long id, CancellationToken cancellationToken);

    Task<MetricDataModel> GetMetricData(MetricQueryModel metricQueryModel, CancellationToken cancellationToken);

    Task<IQueryable<KeyValuePair<long, MetricDataModel>>> GetMetricDataByMetricIds(IReadOnlyList<long> metricIds, CancellationToken cancellationToken, long? recurrenceId);

    Task<IQueryable<MetricQueryModel>> GetMetricsForMeetingAsync(long recurrenceId, CancellationToken cancellationToken);
    Task<IQueryable<MetricQueryModelLookup>> GetMetricsForMeetingLookupAsync(long recurrenceId, string frequency, CancellationToken cancellationToken);

    Task<IQueryable<((long recurrenceId, long measurableId) keys, MetricDividerQueryModel divider)>> GetMetricDividersAsync(IReadOnlyList<(long recurrenceId, long measurableId)> keys, CancellationToken cancellationToken);
    Task<IQueryable<(long recurrenceId, MetricDividerQueryModel divider)>> GetMetricDividersForMeetingsAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken);

    Task<IQueryable<MetricFormulaLookupQueryModel>> GetMetricFormulaLookup(long userId, CancellationToken cancellationToken);

    Task<IQueryable<MetricAddExistingLookupQueryModel>> GetMetricAddExistingLookup(CancellationToken cancellationToken, long recurrenceId);
    Task<IQueryable<MetricQueryModel>> GetMetricsByIds(List<long> ids, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    #endregion


  }

  public partial class DataContext : IDataContext
  {

    #region Queries

    public async Task<MetricQueryModel> GetMetricByIdAsync(long id, CancellationToken cancellationToken)
    {
      return repository.GetMetricById(id, cancellationToken);
    }

    public async Task<IQueryable<MetricQueryModel>> GetMetricsForUserAsync(CancellationToken cancellationToken)
    {
      return repository.GetMetricsForUser(cancellationToken);
    }

    public async Task<MetricQueryModel> GetMetricById(long id, CancellationToken cancellationToken)
    {
      return repository.GetMetricById(id, cancellationToken);
    }

    public Task<MetricDataModel> GetMetricData(MetricQueryModel metricQueryModel, CancellationToken cancellationToken)
    {
      return repository.GetMetricData(metricQueryModel, cancellationToken);
    }

    public Task<IQueryable<KeyValuePair<long, MetricDataModel>>> GetMetricDataByMetricIds(IReadOnlyList<long> metricIds, CancellationToken cancellationToken, long? recurrenceId)
    {
      return repository.GetMetricDataByMetricIds(metricIds, cancellationToken, recurrenceId);
    }

    public async Task<IQueryable<MetricQueryModel>> GetMetricsForMeetingAsync(long recurrenceId, CancellationToken cancellationToken)
    {
      return repository.GetMetricsForMeeting(recurrenceId, cancellationToken);
    }

    public Task<IQueryable<((long recurrenceId, long measurableId) keys, MetricDividerQueryModel divider)>> GetMetricDividersAsync(IReadOnlyList<(long recurrenceId, long measurableId)> keys, CancellationToken cancellationToken)
    {
      var result = repository.GetMetricDividers(keys);
      return Task.FromResult(result);
    }

    public Task<IQueryable<(long recurrenceId, MetricDividerQueryModel divider)>> GetMetricDividersForMeetingsAsync(IReadOnlyList<long> keys, CancellationToken cancellationToken)
    {
      var result = repository.GetMetricDividersForMeetings(keys);
      return Task.FromResult(result);
    }


    public async Task<IQueryable<MetricQueryModelLookup>> GetMetricsForMeetingLookupAsync(long recurrenceId, string frequency, CancellationToken cancellationToken)
    {
      return repository.GetMetricsForMeetingLookup(recurrenceId, frequency, cancellationToken);
    }

    public async Task<IQueryable<MetricFormulaLookupQueryModel>> GetMetricFormulaLookup(long userId, CancellationToken cancellationToken)
    {
      return repository.GetMetricFormulaLookup(userId, cancellationToken);
    }

    public async Task<IQueryable<MetricAddExistingLookupQueryModel>> GetMetricAddExistingLookup(CancellationToken cancellationToken, long recurrenceId)
    {
      return repository.GetMetricAddExistingLookup(cancellationToken, recurrenceId);
    }

    public async Task<IQueryable<MetricQueryModel>> GetMetricsByIds(List<long> ids, CancellationToken cancellationToken){
      return repository.GetMetricsByIds(ids, cancellationToken);
    }

  #endregion

  #region Mutations

  #endregion

}

}

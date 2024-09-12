using RadialReview.Accessors;
using RadialReview.Core.Accessors.StrictlyAfterExecutors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.Core.Utilities.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static RadialReview.Accessors.ScorecardAccessor;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<MetricScoreQueryModel> GetScoresForMeasurables(IEnumerable<long> measurableId, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateMetricScore(MetricScoreCreateModel metricScoreCreateModel);

    Task<IdModel> EditMetricScore(MetricScoreEditModel metricScoreEditModel);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<MetricScoreQueryModel> GetScoresForMeasurables(IEnumerable<long> measurableIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        var measurables = ScorecardAccessor.GetMeasurablesScores(caller, measurableIds.ToList());
        return measurables.Select(m => RepositoryTransformers.TransformScore(m)).ToList();
      });
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateMetricScore(MetricScoreCreateModel model)
    {
      decimal value;
      double timePlusAWeek = model.Timestamp + 604800;
      var dateTimeWeek = timePlusAWeek.FromUnixTimeStamp();

      if (decimal.TryParse(model.Value, out value))
      {
        using var session = HibernateSession.GetCurrentSession();
        var metric = session.Get<MeasurableModel>(model.MetricId);
        if (metric.Frequency == Models.Enums.Frequency.DAILY)
        {
          var perms = PermissionsUtility.Create(session, caller);
          perms.ViewMeasurable(model.MetricId);
          using var transaction = session.BeginTransaction();
          var forWeek = model.Timestamp.FromUnixTimeStamp();
          var score = await ScorecardAccessor._GenerateScoreModel_Unsafe(session, metric, forWeek, value);

          // Updated_Metric and Updated_MetricScore subscription
          //var scoreExecutor = new UpdateScoreExecutor(score.Id, value, score.NoteText);
          //await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateScore(score.Id), scoreExecutor);
          await Restricted.UpdateScore_Unsafe(session, score.Id, value, notesText: score.NoteText);
          transaction.Commit();
          session.Flush();

          return new IdModel(score.Id);
        }

        var metricScore = await ScorecardAccessor.UpdateScore(caller, model.MetricId, dateTimeWeek, value);

        return new IdModel(metricScore.Id);
      }

      return null;
    }

    public async Task<IdModel> EditMetricScore(MetricScoreEditModel model)
    {
      TOptional<string> notesText = default;
      TOptional<decimal?> value = default;

      if (model.Value.HasValue)
        value = TypeParser.ConvertToValidNullable<decimal>(model.Value);

      if (model.NotesText.HasValue)
        notesText = model.NotesText.Value;

      var metricScore = await ScorecardAccessor.UpdateScore(caller, model.Id, value, notesText);
      return new IdModel(id: metricScore.Id);
    }

    #endregion

  }
}
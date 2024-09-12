namespace RadialReview.Repositories
{
  using global::NHibernate;
  using Humanizer;
  using RadialReview.Accessors;
  using RadialReview.Core.Accessors;
  using RadialReview.Core.GraphQL.Common.DTO;
  using RadialReview.Core.GraphQL.Enumerations;
  using RadialReview.Core.GraphQL.MetricAddExistingLookup;
  using RadialReview.Core.GraphQL.MetricFormulaLookup;
  using RadialReview.Core.GraphQL.Models.Mutations;
  using RadialReview.Core.Repositories;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Models.Mutations;
  using RadialReview.Middleware.BackgroundServices;
  using RadialReview.Models.Enums;
  using RadialReview.Models.L10;
  using RadialReview.Models.Scorecard;
  using RadialReview.Utilities;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using RadialReview.Models;

  public partial interface IRadialReviewRepository
  {

    #region Queries

    MetricQueryModel GetMetricById(long measurableId, CancellationToken cancellationToken);

    Task<MetricDataModel> GetMetricData(MetricQueryModel metricQueryModel, CancellationToken cancellationToken);
    Task<IQueryable<KeyValuePair<long, MetricDataModel>>> GetMetricDataByMetricIds(IReadOnlyList<long> metricIds, CancellationToken cancellationToken, long? recurrenceId);
    IQueryable<MetricQueryModel> GetMetricsForUser(CancellationToken cancellationToken);

    IQueryable<MetricQueryModel> GetMetricsForMeeting(long recurrenceId, CancellationToken cancellationToken);
    IQueryable<MetricQueryModelLookup> GetMetricsForMeetingLookup(long recurrenceId, string frecuency, CancellationToken cancellationToken);

    IQueryable<MetricFormulaLookupQueryModel> GetMetricFormulaLookup(long userId, CancellationToken cancellationToken);

    IQueryable<MetricAddExistingLookupQueryModel> GetMetricAddExistingLookup(CancellationToken cancellationToken, long recurrenceId);
    IQueryable<MetricQueryModel> GetMetricsByIds(List<long> ids, CancellationToken cancellationToken);

    L10Recurrence.L10Recurrence_Measurable EnsureAccountableUserLoaded(L10Recurrence.L10Recurrence_Measurable l10recurrenceMeasurable);

    #endregion

    #region Mutations

    Task<IdModel> AddExistingMetricToMeeting(MetricAddExistingToMeetingModel addExistingToMeetingModel);

    Task<IdModel> CreateMetric(MetricCreateModel metricCreateModel);

    Task<IdModel> EditMetric(MetricEditModel metricEditModel);

    Task<IdModel> SortMetric(MetricSortModel metricSortModel);

    Task<GraphQLResponseBase> updateMetricByMeetingIds(MetricByMeetingIdModel metricByMeeting);

    #endregion

  }

  public partial class RadialReviewRepository
  {

    #region Queries

    public MetricQueryModel GetMetricById(long measurableId, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          PermissionsUtility.Create(s, caller).ViewMeasurable(measurableId);

          // Get the measurable
          var result = s.Get<MeasurableModel>(measurableId).TransformMeasurableToMetric();

          // Fill in associated L10Recurrence data
          MeasurableModel measurable = null;

          var q = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
            .JoinAlias(x => x.Measurable, () => measurable).Where(x => x.Measurable.Id == measurableId);

          q = q.Where(x => measurable.DeleteTime == null);
          var found = q.Fetch(x => x.Measurable).Eager.List().ToList();

          if (found != null && found.Count > 0)
          {
            result.RecurrenceId = found[0].L10Recurrence.Id;
            result.IndexInTable = found[0].IndexInTable ?? found[0]._Ordering;
          }

          // Return
          return result;
        }
      }
    }

    public IQueryable<MetricQueryModel> GetMetricsForMeeting(long recurrenceId, CancellationToken cancellationToken)
    {
      return L10Accessor.GetMeasurablesForRecurrence(caller, recurrenceId)
        .Select(x => EnsureAccountableUserLoaded(x))
        .Select(m => RepositoryTransformers.TransformL10RecurrenceMeasurable(m)).ToList().AsQueryable();
    }

    public L10Recurrence.L10Recurrence_Measurable EnsureAccountableUserLoaded(L10Recurrence.L10Recurrence_Measurable l10recurrenceMeasurable)
    {
      UserOrganizationModel userOrgModel = null;

      if (l10recurrenceMeasurable.Measurable.AccountableUser == null)
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          var measurable = s.Query<MeasurableModel>().FirstOrDefault(x => x.Id == l10recurrenceMeasurable.Measurable.Id && x.DeleteTime == null);
          userOrgModel = measurable.AccountableUser;
        }
      }
      else
      {
        userOrgModel = l10recurrenceMeasurable.Measurable.AccountableUser;
      }

      l10recurrenceMeasurable.Measurable.AccountableUser = userOrgModel;

      return l10recurrenceMeasurable;
    }

    public IQueryable<MetricQueryModelLookup> GetMetricsForMeetingLookup(long recurrenceId, string frequency, CancellationToken cancellationToken)
    {
      return L10Accessor.GetMeasurablesForRecurrenceLookupUnsafe(caller, recurrenceId, frequency)
        .Select(m => m.Measurable.TransformMeasurableToMetricLookup()).ToList().AsQueryable();
    }

    public IQueryable<MetricQueryModel> GetMetricsForUser(CancellationToken cancellationToken)
    {
      return ScorecardAccessor.GetUserMeasurables(caller, caller.Id).Select(x => x.TransformMeasurableToMetric()).AsQueryable();
    }

    public Task<MetricDataModel> GetMetricData(MetricQueryModel metricQueryModel, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller).ViewMeasurable(metricQueryModel.Id);

          MetricDataModel result = CreateMetricDataFromMeasurable(s, metricQueryModel);

          return Task.FromResult(result);
        }
      }
    }

    public Task<IQueryable<KeyValuePair<long, MetricDataModel>>> GetMetricDataByMetricIds(IReadOnlyList<long> metricIds, CancellationToken cancellationToken, long? recurrenceId)
    {
      using var session = HibernateSession.GetCurrentSession();

      var meetingStartWeek = recurrenceId != null ? session.Query<L10Recurrence>()
        .SingleOrDefault(x => x.Id == recurrenceId).StartOfWeekOverride : null;

      var metricDataDict = MetricAccessor.InitializeMetricDataDict(session, metricIds);

      var averageResults = MetricAccessor.GetScoreAverageDataByMetricIds(session, metricIds, caller, meetingStartWeek);
      var cumulativeResults = MetricAccessor.GetScoreCumulativeDataByMetricIds(session, metricIds, caller, meetingStartWeek);
      var progressiveResults = MetricAccessor.GetScoreProgressiveDataByMetricIds(session, metricIds);


      foreach (var avg in averageResults)
      {
        if (metricDataDict.ContainsKey(avg.MetricId))
        {
          metricDataDict[avg.MetricId].AverageData.Average = avg.AverageScore;
        }
      }

      foreach (var cum in cumulativeResults)
      {
        if (metricDataDict.ContainsKey(cum.MetricId))
        {
          metricDataDict[cum.MetricId].CumulativeData.Sum = cum.CumulativeScore;
        }
      }

      foreach (var prog in progressiveResults)
      {
        if (metricDataDict.ContainsKey(prog.MetricId))
        {
          metricDataDict[prog.MetricId].ProgressiveData.Sum = prog.ProgressiveScore;
        }
      }


      var finalResults = metricDataDict.Select(pair => new KeyValuePair<long, MetricDataModel>(pair.Key, pair.Value)).AsQueryable();

      return Task.FromResult(finalResults.AsQueryable());
    }

    public MetricDataModel CreateMetricDataFromMeasurable(ISession session, MetricQueryModel metric)
    {
      MetricDataModel result = new MetricDataModel
      {
        CumulativeData = new MetricCumulativeDataModel
        {
          StartDate = metric.ShowCumulative == true ? metric.CumulativeRange.ToUnixTimeStamp() : null
        },
        AverageData = new MetricAverageDataModel
        {
          StartDate = metric.ShowAverage == true ? metric.AverageRange.ToUnixTimeStamp() : null
        },
        ProgressiveData = new MetricProgressiveDataModel
        {
          TargetDate = metric.ProgressiveDate.ToUnixTimeStamp()
        }
      };

      var scoresByMeasurable = new Dictionary<long, CalcScores>();
      {
        var scoreFutures = new Dictionary<long, IEnumerable<CalcScores.TinyScore>>();

        //Grab interesting scores
        var minDate = DateTime.MaxValue;

        if (metric.CumulativeRange.HasValue)
        {
          var cmvMinDate = Math2.Min(minDate, metric.CumulativeRange.Value.AddDays(-7));
          minDate = minDate > cmvMinDate ? cmvMinDate : minDate;
        }

        if (metric.AverageRange.HasValue && metric.AverageRange != DateTime.MinValue)
        {
          var avgMinDate = Math2.Min(minDate, metric.AverageRange.Value.AddDays(-7));
          minDate = minDate > avgMinDate ? avgMinDate : minDate;
        }

        if (metric.ProgressiveDate.HasValue)
        {
          minDate = DateTime.MinValue;
        }

        scoreFutures[metric.Id] = session.QueryOver<RadialReview.Models.Scorecard.ScoreModel>()
        .Where(x => x.MeasurableId == metric.Id && x.DeleteTime == null && x.Measured != null && (x.ForWeek > minDate))
        .Select(x => x.ForWeek, x => x.Measured)
        .Future<object[]>()
        .Select(x => new CalcScores.TinyScore()
        {
          ForWeek = (DateTime)x[0],
          Measured = (decimal?)x[1]
        });

        scoresByMeasurable[metric.Id] = new CalcScores()
        {
          HasAverage = metric.ShowAverage && metric.AverageRange.HasValue,
          HasCumulative = metric.ShowCumulative && metric.CumulativeRange.HasValue,
          HasProgressive = metric.ProgressiveDate != null,
          MeasurableId = metric.Id,
        };

        foreach (var k in scoresByMeasurable.Keys)
        {
          scoresByMeasurable[k].Scores = scoreFutures[k].ToList();
        }
      }

      var startOfWeek = metric.StartOfWeek;

      //Set Cumulative Values
      foreach (var k in scoresByMeasurable.Keys)
      {
        var measCalc = scoresByMeasurable[k];

        if (measCalc.HasCumulative)
        {
          var foundScores = measCalc.Scores.Where(x => x.ForWeek > metric.CumulativeRange.Value.AddDays(-(int)startOfWeek)).ToList();
          result.CumulativeData.Sum = foundScores.GroupBy(x => x.ForWeek)
                    .Select(x => x.FirstOrDefault(y => y.Measured != null).NotNull(y => y.Measured))
                    .Where(x => x != null)
                    .Sum();
        }
        if (measCalc.HasAverage)
        {
          var foundScores = measCalc.Scores.Where(x => x.ForWeek > metric.AverageRange.Value.AddDays(-(int)startOfWeek)).ToList();
          var interesting = foundScores.GroupBy(x => x.ForWeek)
                    .Select(x => x.FirstOrDefault(y => y.Measured != null).NotNull(y => y.Measured))
                    .Where(x => x != null);

          result.AverageData.Average = interesting.Any() ? interesting.Average() : null;

        }
        if (measCalc.HasProgressive)
        {
          var found = measCalc.Scores.Where(x => x.Measured != null).Select(x => x.Measured).ToList();
          result.ProgressiveData.Sum = found.Sum();
        }

        if (result.ProgressiveData.TargetDate == null && result.ProgressiveData.Sum == null)
        {
          result.ProgressiveData = null;  // NOTE: Make ProgressiveData null when there is no data to meet FE's expectation.
        }

        if (result.AverageData.StartDate == null && result.AverageData.Average == null)
        {
          result.AverageData = null;  // NOTE: Make AverageData null when there is no data to meet FE's expectation.
        }

        if (result.CumulativeData.StartDate == null && result.CumulativeData.Sum == null)
        {
          result.CumulativeData = null;  // NOTE: Make CumulativeData null when there is no data to meet FE's expectation.
        }
      }

      return result;
    }

    public IQueryable<MetricFormulaLookupQueryModel> GetMetricFormulaLookup(long userId, CancellationToken cancellationToken)
    {
      var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      var metrics = ScorecardAccessor.GetVisibleMeasurables(session, perms, caller.Organization.Id, false).Select(x => x.TransformMeasurableToMetricFormulaLookup());

      return metrics.AsQueryable();
    }

    public IQueryable<MetricAddExistingLookupQueryModel> GetMetricAddExistingLookup(CancellationToken cancellationToken, long recurrenceId)
    {
      var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      var result = ScorecardAccessor.GetVisibleMeasurables(session, perms, caller.Organization.Id, false, recurrenceId)
                                    .Select(x => x.TransformMeasurableToMetricAddExistingLookup());
      return result.AsQueryable();
    }

    public IQueryable<MetricQueryModel> GetMetricsByIds(List<long> measurableIds, CancellationToken cancellationToken)
    {
      var session = HibernateSession.GetCurrentSession();
      List<MeasurableModel> metrics = L10Accessor.GetMeasurablesByIdsUnsafe(measurableIds, session);
      return metrics.Select(x => x.TransformMeasurableToMetric()).AsQueryable();
    }

    #endregion

    #region Mutations

    public async Task<GraphQLResponseBase> updateMetricByMeetingIds(MetricByMeetingIdModel metricByMeeting)
    {
      long meetingIdInProgress = 0;

      try
      {
        foreach (var meetingId in metricByMeeting.MeetingIds)
        {
          meetingIdInProgress = meetingId;

          var formulaMesurable = ScorecardAccessor.UpdateScoreEmptyValue(meetingId);

          foreach (var m in formulaMesurable.updateFormula)
          {
            await EditMetric(m);
          }

          foreach (var m in formulaMesurable.basicFormula)
          {
            await EditMetric(m);
          }

          Console.WriteLine($"Metric scores have been successfully recalculated for meeting: {meetingId}");
        }

        return new GraphQLResponseBase(true, "Metric scores have been successfully recalculated for meetings");

      } catch(Exception ex)
      {
        Console.WriteLine($"Error recalculating scores for meeting metrics: {meetingIdInProgress}");
        throw ex;
      }
    }

    public async Task<IdModel> AddExistingMetricToMeeting(MetricAddExistingToMeetingModel model)
    {
      await L10Accessor.AttachMeasurable(redLockFactory, caller, model.MeetingId, model.MetricId);

      return new IdModel(model.MetricId);
    }

    public async Task<IdModel> CreateMetric(MetricCreateModel body)
    {
      var goalDirection = (LessGreater)((int)EnumHelper.ConvertToNonNullableEnum<gqlLessGreater>(body.Rule));
      var frequency = EnumHelper.ConvertToEnumOrDefaultOnNull<Frequency>(body.Frequency);

      if (goalDirection != LessGreater.Between && (!String.IsNullOrEmpty(body.MaxGoalValue) || !String.IsNullOrEmpty(body.MinGoalValue)))
        throw new Exception("If Rule is different than 'Between', the field maxGoalValue and minGoalValue should not be sent.");

      if (goalDirection == LessGreater.Between && !String.IsNullOrEmpty(body.SingleGoalValue))
        throw new Exception("If Rule is equal to 'Between', the field SingleGoalValue should not be sent.");

      var isDecimalSingleGoalValue = decimal.TryParse(body.SingleGoalValue, out decimal singleGoalValue);
      var isDecimalMinGoalValue = decimal.TryParse(body.MinGoalValue, out decimal minGoalValue);
      var isDecimalMaxGoalValue = decimal.TryParse(body.MaxGoalValue, out decimal maxGoalValue);

      var dataCustomGoal = body.CustomGoals.Select(x => RepositoryTransformers.TransformCustomGoal(x)).ToList();
      bool yesNoGoal = body.Units.Equals("YESNO");
      if (yesNoGoal)
      {
        singleGoalValue = isDecimalSingleGoalValue ? singleGoalValue > 0 ? 1 : 0 : 0;
      }

      var builder = MeasurableBuilder.Build(
        body.Title,
        body.Assignee,
        adminUserId: null,
        type: (UnitType)(EnumHelper.ConvertToNonNullableEnum<gqlUnitType>(body.Units)),
        showCumulative: body.CumulativeData != null,
        cumulativeRange: body.CumulativeData == null ? null : body.CumulativeData.StartDate.FromUnixTimeStamp(),
        showAverage: body.AverageData != null,
        averageRange: body.AverageData == null ? null : body.AverageData.StartDate.FromUnixTimeStamp(),
        progressiveData: body.ProgressiveData?.TargetDate.FromUnixTimeStamp(),
        frequency: frequency,
        goalDirection: goalDirection,
        customGoals: dataCustomGoal,
        notesId: body.NotesId,
        goal: yesNoGoal ? singleGoalValue :
              isDecimalSingleGoalValue ? singleGoalValue :
              isDecimalMinGoalValue ? minGoalValue : null,
        alternateGoal: isDecimalMaxGoalValue ? maxGoalValue : null,
        hasV3Config: true
      );
      var measurable = await ScorecardAccessor.CreateMeasurable(caller, builder);

      if (body.Formula != null)
      {
        await ScorecardAccessor.SetFormula(caller, measurable.Id, body.Formula);
      }

      foreach (var mid in body.Meetings ?? new long[0])
      {
        await L10Accessor.AttachMeasurable(redLockFactory, caller, mid, measurable.Id);
      }

      return new IdModel(measurable.Id);
    }

    public async Task<IdModel> EditMetric(MetricEditModel model)
    {
      var direction = (LessGreater?)EnumHelper.ConvertToNullableEnum<gqlLessGreater>(model.Rule);

      if ((direction != null && direction != LessGreater.Between) && (!String.IsNullOrEmpty(model.MaxGoalValue) || !String.IsNullOrEmpty(model.MinGoalValue)))
        throw new Exception("If Rule is different than 'Between', the field maxGoalValue and minGoalValue should not be sent.");

      if (direction == LessGreater.Between && !String.IsNullOrEmpty(model.SingleGoalValue))
        throw new Exception("If Rule is equal to 'Between', the field SingleGoalValue should not be sent.");

      var isDecimalSingleGoalValue = decimal.TryParse(model.SingleGoalValue, out decimal singleGoalValue);
      var isDecimalMinGoalValue = decimal.TryParse(model.MinGoalValue, out decimal minGoalValue);
      var isDecimalMaxGoalValue = decimal.TryParse(model.MaxGoalValue, out decimal maxGoalValue);

      // This needs to be called before UpdateMeasurable
      // Otherwise the formula is already set and will not update
      if (model.Formula.HasValue)
      {
        if (model.Formula == "-notset-")
          throw new Exceptions.PermissionsException("Formula was empty");
        var formula = model.Formula;
        var user = caller;
        var modelId = model.MetricId;
        await ScorecardAccessor.SetFormula(user, modelId, formula);

        if (model.Meetings != null)
        {
          var old = ScorecardAccessor.GetMeasurablesRecurrenceIds(caller, model.MetricId);
          var set = SetUtility.AddRemove(old, model.Meetings ?? new long[0]);
          foreach (var i in set.AddedValues)
          {
            await L10Accessor.AttachMeasurable(redLockFactory, caller, i, model.MetricId);
          }

          var detachTime = DateTime.UtcNow;
          foreach (var i in set.RemovedValues)
          {
            await L10Accessor.DetachMeasurable(dbContext, caller, i, model.MetricId, detachTime, archiveIfNoLongerInMeetings: false);
          }
        }
      }


      await ScorecardAccessor.UpdateMeasurable(
        dbContext,
        caller,
        model.MetricId,
        name: model.Title,
        direction: direction,
        unitType: (UnitType?)(EnumHelper.ConvertToNullableEnum<gqlUnitType>(model.Units)),
        accountableId: model.Assignee,
        updateFutureOnly: true,//todo
        showCumulative: model.CumulativeData == null ? null : model.CumulativeData.Value?.StartDate != null ? true : false,
        showAverage: model.AverageData == null ? null : model.AverageData.Value?.StartDate != null ? true : false,
        cumulativeRange: model.CumulativeData == null ? null : new NullableField<DateTime?>(model.CumulativeData.Value?.StartDate.FromUnixTimeStamp()),
        averageRange: model.AverageData == null ? null : new NullableField<DateTime?>(model.AverageData.Value?.StartDate.FromUnixTimeStamp()),
        target: isDecimalSingleGoalValue ? singleGoalValue :
                isDecimalMinGoalValue ? minGoalValue : null,
        altTarget: isDecimalMaxGoalValue ? maxGoalValue : null,
        metricEditModel: model,
        hasV3Config: true
      );

      try
      {
        if (model.Archived == true)
        {
          var trackedMetricsForMetric = MetricTabAccessor.GetTrackedMetricsByMetricId(caller, model.MetricId);
          foreach (var trackedMetric in trackedMetricsForMetric)
          {
            // Another method was created without permissions checking because the above functions check if you have edit permissions on the meeting.
            // When a metric is deleted, the metric tabs are also deleted, to avoid permission errors on the metric tab, the metric tab permission is omitted.
            await MetricTabAccessor.RemoveMetricFromTabUnsafe(caller, caller.Id, trackedMetric.Id);
          }
        }
      }
      catch (Exception e)
      {
        throw e;
      }

      if (model.Meetings != null)
      {
        var oldMetrics = ScorecardAccessor.GetMeasurablesRecurrenceIds(caller, model.MetricId);
        if (!oldMetrics.SequenceEqual(model.Meetings))
        {
          var set = SetUtility.AddRemove(oldMetrics, model.Meetings ?? new long[0]);
          foreach (var newMeetingId in set.AddedValues)
          {
            await L10Accessor.AttachMeasurable(redLockFactory, caller, newMeetingId, model.MetricId);
          }

          var detachTime = DateTime.UtcNow;
          foreach (var oldMeetingId in set.RemovedValues)
          {
            await L10Accessor.DetachMeasurable(dbContext, caller, oldMeetingId, model.MetricId, detachTime, archiveIfNoLongerInMeetings: false);
          }
        }
      }

      return new IdModel(model.MetricId);
    }

    public async Task<IdModel> SortMetric(MetricSortModel metricSortModel)
    {
      throw new NotImplementedException();
    }

    #endregion

  }


}

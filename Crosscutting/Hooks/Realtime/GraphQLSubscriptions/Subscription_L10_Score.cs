using DocumentFormat.OpenXml.Office2010.Excel;
using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;
using static RadialReview.Models.ViewModels.EditRockViewModel;
using GQL = RadialReview.GraphQL;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Score : IScoreHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Score(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    public bool AbsorbErrors()
    {
      return false;
    }

    public bool CanRunRemotely()
    {
      return false;
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public async Task PreSaveRemoveFormula(ISession s, long measurableId)
    {
      var response = SubscriptionResponse<long>.Added(measurableId);

      await _eventSender.SendAsync(ResourceNames.MilestoneEvents, response).ConfigureAwait(false);
    }

    public async Task PreSaveUpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates)
    {
      var id = scoreAndUpdates.Select(x => x.score.MeasurableId).First();
      var response = SubscriptionResponse<long>.Updated(id);

      await _eventSender.SendAsync(ResourceNames.MilestoneEvents, response).ConfigureAwait(false);
    }

    public async Task RemoveFormula(ISession ses, long measurableId)
    {
      var response = SubscriptionResponse<long>.Archived(measurableId);

      await _eventSender.SendAsync(ResourceNames.MilestoneEvents, response).ConfigureAwait(false);
    }

    public async Task CreateScores(ISession session, List<ScoreAndUpdates> scoreAndUpdates)
    {
      foreach (var scoreAndUpdate in scoreAndUpdates)
      {
        var measurableId = scoreAndUpdate.score.MeasurableId;
        var s = scoreAndUpdate.score.TransformScore();
        var recurrences = L10Accessor.GetMeetingsByL10measurableId(measurableId);

        // metric tabs
        var metricTabIds = await MetricTabAccessor.GetMetricTabIdsByMetricIdUnsafe(measurableId);

        foreach (var metricTabId in metricTabIds)
        {
          await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(metricTabId), Change<IMeetingChange>.Inserted(Change.Target(measurableId, GQL.Models.MetricQueryModel.Collections.MetricScore1.MetricScores), s.Id, s)).ConfigureAwait(false);
        }
        foreach (var recurrence in recurrences)
        {
          var targets = new[] {
            new ContainerTarget
            {
              Id = scoreAndUpdate.score.MeasurableId,
              Property = "SCORES_NON_PAGINATED",
            }
          };
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Created(s.Id, s)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(measurableId, MeasurableQueryModel.Collections.MetricScore.Scores), s.Id, s)).ConfigureAwait(false);
        }
      }
    }

    public async Task UpdateScores(ISession session, List<ScoreAndUpdates> scoreAndUpdates)
    {
      var id = scoreAndUpdates.Select(x => x.score.Id).First();
      var response = SubscriptionResponse<long>.Updated(id);

      // data for metric tabs
      var measurable = scoreAndUpdates.Select(x => x.score.Measurable).First();
      var metricTabIds = await MetricTabAccessor.GetMetricTabIdsByMetricIdUnsafe(measurable.Id);

      foreach (var scoreAndUpdate in scoreAndUpdates)
      {
        var s = scoreAndUpdate.score.TransformScore();
        var recurrence_Measurables = L10Accessor.GetMeetingsContainingMeasurable(scoreAndUpdate.score.MeasurableId);
        
        var targets = new[] {
            new ContainerTarget
            {
              Id = scoreAndUpdate.score.MeasurableId,
              Property = "SCORES_NON_PAGINATED",
            }
          };

        var metricTargets =
          recurrence_Measurables.Select(rm =>
            new ContainerTarget
            {
              Type = "meeting",
              Id = rm.L10Recurrence.Id,
              Property = "METRICS", //
            }
          )
          .ToArray();

        var change = Change<IMeetingChange>.Updated(s.Id, s, targets);

        foreach (var rm in recurrence_Measurables)
        {
          var metric = rm.TransformL10RecurrenceMeasurable();
          var metricChange = Change<IMeetingChange>.Updated(metric.Id, metric, metricTargets);
          await _eventSender.SendChangeAsync(ResourceNames.Metric(metric.Id), metricChange).ConfigureAwait(false);

          await _eventSender.SendChangeAsync(ResourceNames.Meeting(rm.L10Recurrence.Id), Change<IMeetingChange>.Updated(s.Id, s, targets)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(rm.L10Recurrence.Id), metricChange).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.User(rm.Measurable.AccountableUserId), Change<IMeetingChange>.Updated(s.Id, s, targets)).ConfigureAwait(false);
        }
        // Count is 0 when the metric is personal. Sending the change to the user and metric channels.
        if (recurrence_Measurables.Count == 0)
        {
          var userId = scoreAndUpdate.score.AccountableUserId;
          // Get the measurable
          var metric = session.Get<MeasurableModel>(measurable.Id).TransformMeasurableToMetric();
          var metricChange = Change<IMeetingChange>.Updated(metric.Id, metric, metricTargets);

          await _eventSender.SendChangeAsync(ResourceNames.Metric(metric.Id), metricChange).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.User(userId), Change<IMeetingChange>.Updated(s.Id, s, targets)).ConfigureAwait(false);
        }

        // metrics tab update message
        foreach (var metricTabId in metricTabIds)
        {
          await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(metricTabId), change).ConfigureAwait(false);
        }
      }
    }
  }
}

using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Accessors;
using GQL = RadialReview.GraphQL;
using RadialReview.Core.GraphQL.MetricFormulaLookup;
using RadialReview.Core.GraphQL.MetricAddExistingLookup;
using RadialReview.GraphQL.Models;
using static RadialReview.GraphQL.Models.IssueQueryModel.Associations;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Measurables : IMeasurableHook
  {
    private readonly ITopicEventSender _eventSender;

    public Subscription_L10_Measurables(ITopicEventSender eventSender)
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

    public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> createdScores)
    {
      var response = SubscriptionResponse<long>.Added(measurable.Id);

      await _eventSender.SendAsync(ResourceNames.MeasurableEvents, response).ConfigureAwait(false);

      var m = measurable.TransformMeasurableToMetric();
      var metricFormula = measurable.TransformMeasurableToMetricFormulaLookup();
      var metricAddExisting = measurable.TransformMeasurableToMetricAddExistingLookup();

      // TODO: Parallelize if necessary!
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.Created(m.Id, m)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(m.Assignee.Id, GQL.Models.MetricQueryModel.Associations.User13.Assignee), m.Id, m)).ConfigureAwait(false);

      // inserted/updated-association metric formula lookup
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(m.Assignee.Id, GQL.Models.UserQueryModel.Collections.MetricFormulaLookup.MetricFormulaLookup), m.Id, metricFormula)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(m.Assignee.Id, MetricFormulaLookupQueryModel.Associations.User16.Assignee), m.Id, m.Assignee)).ConfigureAwait(false);

      // inserted/updated user metric
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(m.Assignee.Id, GQL.Models.UserQueryModel.Collections.UserMetric.Metrics), m.Id, m)).ConfigureAwait(false);

      var recurrenceMeasurables = L10Accessor.GetMeetingsContainingMeasurable(measurable.Id);
      foreach (var rm in recurrenceMeasurables)
      {
        var recurrence = rm.L10Recurrence;
        var metric = rm.TransformL10RecurrenceMeasurable();

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Created(metric.Id, metric)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(m.Assignee.Id, GQL.Models.MetricQueryModel.Associations.User13.Assignee), metric.Id, metric)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrence.Id, GQL.Models.MeetingQueryModel.Collections.Metric.Metrics), metric.Id, metric)).ConfigureAwait(false);

        await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrence.Id, GQL.Models.MeetingQueryModel.Collections.Metric.Metrics), metric.Id, metric)).ConfigureAwait(false);

        // inserted/updated-association metric add existing lookup
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(recurrence.Id, GQL.Models.MeetingQueryModel.Collections.MetricAddExistingLookup.MetricAddExistingLookup), metric.Id, metricAddExisting)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.UpdatedAssociation(Change.Target(recurrence.Id, MetricAddExistingLookupQueryModel.Associations.User17.Assignee), m.Id, m.Assignee)).ConfigureAwait(false);
      }
    }

    public async Task DeleteMeasurable(ISession s, MeasurableModel measurable)
    {
      var response = SubscriptionResponse<long>.Archived(measurable.Id);

      await _eventSender.SendAsync(ResourceNames.MeasurableEvents, response).ConfigureAwait(false);

      var recurrenceMeasurables = L10Accessor.GetMeetingsContainingMeasurable(measurable.Id, deleteTime: measurable.DeleteTime);

      var m = measurable.TransformMeasurable();

      var targets =
          recurrenceMeasurables.Select(rm =>
            new ContainerTarget {
              Type = "meeting",
              Id = rm.L10Recurrence.Id,
              Property ="METRICS", //
            }
          )
          .ToArray();

      var change = Change<IMeetingChange>.Updated(m.Id, m, targets);

      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.Deleted<GQL.Models.MetricQueryModel>(m.Id)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Owner.Id), Change<IMeetingChange>.Deleted<GQL.Models.MetricQueryModel>(m.Id)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Metric(m.Id), Change<IMeetingChange>.Deleted<GQL.Models.MetricQueryModel>(m.Id)).ConfigureAwait(false);

      // removed user metric
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.Removed(Change.Target(m.Assignee.Id, GQL.Models.UserQueryModel.Collections.UserMetric.Metrics), m.Id, m)).ConfigureAwait(false);

      foreach (var rm in recurrenceMeasurables)
      {
        var recurrence = rm.L10Recurrence;
        var metric = rm.TransformL10RecurrenceMeasurable();
        var metric_change = Change<IMeetingChange>.Updated(metric.Id, metric, targets);

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Deleted<GQL.Models.MeasurableQueryModel>(m.Id)).ConfigureAwait(false);
      }
    }


    public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates)
    {
      var response = SubscriptionResponse<long>.Updated(measurable.Id);

      await _eventSender.SendAsync(ResourceNames.MeasurableEvents, response).ConfigureAwait(false);

      var recurrenceMeasurables = L10Accessor.GetMeetingsContainingMeasurable(measurable.Id, measurable.DeleteTime);

      var m = measurable.TransformMeasurableToMetric();

      var metricTabIds = await MetricTabAccessor.GetMetricTabIdsByMetricIdUnsafe(m.Id);

      var targets =
          recurrenceMeasurables.Select(rm =>
            new ContainerTarget {
              Type = "meeting",
              Id = rm.L10Recurrence.Id,
              Property ="METRICS", //
            }
          )
          .ToList();
      targets.Add(new ContainerTarget()
      {
        Type = "user",
        Id = measurable.AccountableUser.Id,
        Property = "METRICS", //
      });

      var change = Change<IMeetingChange>.Updated(m.Id, m, targets.ToArray());

      await _eventSender.SendChangeAsync(ResourceNames.User(measurable.AccountableUser.Id),
        Change<IMeetingChange>.UpdatedAssociation(Change.Target(m.Assignee.Id,
        UserQueryModel.Associations.UserMetrics.Metrics), m.Id, m)).ConfigureAwait(false);

      #region updatedMetricFormulaLookup
      var targetsMetricFormularLookup = new[] {
        new ContainerTarget
        {
          Type = "meeting",
          Id = m.Id,
          Property = "METRIC_FORMULA_LOOKUP"
        }
      };
      var metricFormularLookup = measurable.TransformMeasurableToMetricFormulaLookup();
      var changeMetricFormularLookup = Change<IMeetingChange>.Updated(m.Id, metricFormularLookup, targetsMetricFormularLookup);

      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), changeMetricFormularLookup).ConfigureAwait(false);
      #endregion updatedMetricFormulaLookup

      #region updatedMetricAddExistingLookup
      var targetsMetricAddExistongLookup =
        recurrenceMeasurables.Select(rm =>
          new ContainerTarget
          {
            Type = "meeting",
            Id = rm.L10Recurrence.Id,
            Property = "METRIC_ADD_EXISTING_LOOKUP"
          }
        ).ToArray();

      var metricAddExistingLookup = measurable.TransformMeasurableToMetricAddExistingLookup();
      var changeMetricAddExistingLookup = Change<IMeetingChange>.Updated(m.Id, metricAddExistingLookup, targetsMetricAddExistongLookup);
      #endregion updatedMetricAddExistingLookup

      if (updates.AccountableUserChanged)
      {
         var assignee = GQL.Models.MetricQueryModel.Associations.User13.Assignee;
         var assigneeChange = Change<IMeetingChange>.UpdatedAssociation(Change.Target(m.Id, assignee), m.Assignee.Id, m.Assignee);

        await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), change).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Metric(m.Id), assigneeChange).ConfigureAwait(false);

      }

      // When there is no recurrence associated to the metric, the value of "recurrences" is null, therefore, in this validation we only make sure not to send an update message with null targets.
      if (recurrenceMeasurables.Count() > 0)
      {
        await _eventSender.SendChangeAsync(ResourceNames.Metric(m.Id), change).ConfigureAwait(false);
      }

      foreach (var rm in recurrenceMeasurables)
      {
        /*  NOTE: We need to set the RecurrenceId property on the MetricQueryModel object m, that is embedded in the change so that associated metric dividers can be included in subscription messages.
        **
        */
        var recurrence = rm.L10Recurrence;
        var metric = rm.TransformL10RecurrenceMeasurable();
        var metric_change = Change<IMeetingChange>.Updated(metric.Id, metric, targets.ToArray());

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(rm.L10Recurrence.Id), metric_change).ConfigureAwait(false);

        if (updates.AccountableUserChanged) {
          var assignee = GQL.Models.MetricQueryModel.Associations.User13.Assignee;
          var assigneeChange = Change<IMeetingChange>.UpdatedAssociation(Change.Target(m.Id, assignee), m.Assignee.Id, m.Assignee);

          await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), assigneeChange).ConfigureAwait(false);
        }

        // updated MetricAddExistingLookup
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), changeMetricAddExistingLookup).ConfigureAwait(false);
      }

      foreach (var metricTabId in metricTabIds)
      {
        await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(metricTabId), change).ConfigureAwait(false);
      }
    }
  }
}

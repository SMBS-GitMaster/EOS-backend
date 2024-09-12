using HotChocolate.Subscriptions;
using NHibernate;
using GQL = RadialReview.GraphQL;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Accessors;
using RadialReview.GraphQL.Models;
using log4net.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions {
  public class Subscription_L10_Metrics : IMetricHook //, IMeetingNoteHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Metrics(ITopicEventSender eventSender) {
      _eventSender = eventSender;
    }

    public bool AbsorbErrors() {
      return false;
    }

    public bool CanRunRemotely() {
      return false;
    }

    public HookPriority GetHookPriority() {
      return HookPriority.UI;
    }

    //public async Task CreateMetricTab(ISession s, UserOrganizationModel caller, Metrics metric)
    //{
    //  var m = metricTab.TransformMetricTab();

    //  if (m.MeetingId.HasValue)
    //  {
    //    await _eventSender.SendAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Created(m.Id, m));
    //    //await _eventSender.SendAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Inserted(Change.Target(m.MeetingId.Value, GQL.Models.MeetingModel.Collections.MetricTab.), m.Id, m));
    //  }
    //}

    //public async Task UpdateMetric(ISession s, UserOrganizationModel caller, MetricTabModel metricTab, IMetricTabHookUpdates updates)
    //{
    //  var m = metricTab.TransformMetricTab();

    //  if (m.MeetingId.HasValue)
    //  {
    //    var targets  =
    //        new[] {
    //          new ContainerTarget {
    //            Type = "meeting",
    //            Id = m.MeetingId.Value,
    //            Property = "METRICS"
    //          }
    //        };

    //    await _eventSender.SendAsync(ResourceNames.MetricTab(m.MeetingId.Value), Change<IMeetingChange>.Updated(m.Id, m, targets));
    //  }
    //}

    public async Task CreateMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab) {
      var m = metricTab.TransformMetricTab();

      if (m.MeetingId.HasValue) {
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Created(m.Id, m)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Inserted(Change.Target(m.MeetingId.Value, GQL.Models.MeetingQueryModel.Collections.MetricTab.MetricsTabs), m.Id, m)).ConfigureAwait(false);
      }
    }

    public async Task UpdateMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab, IMetricTabHookUpdates updates) {
      var m = metricTab.TransformMetricTab();

      if (m.MeetingId.HasValue)
      {
/*        var targets =
            new[] {
              new ContainerTarget {
                Type = "meeting",
                Id = m.MeetingId.Value,
                Property = "METRICS"
              }
            };*/
        var targetsMetricTab =
             new[] {
              new ContainerTarget {
                Type = "meeting",
                Id = m.MeetingId.Value,
                Property = "METRICS_TABS"
              }
             };

        await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(m.Id), Change<IMeetingChange>.Updated(m.Id, m, targetsMetricTab)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Updated(m.Id, m, targetsMetricTab)).ConfigureAwait(false);
      }
    }

    public async Task DeleteMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab, IMetricTabHookUpdates updates)
    {
      var m = metricTab.TransformMetricTab();
      if (m.MeetingId.HasValue)
      {
        await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(m.MeetingId.Value), Change<IMeetingChange>.Deleted<GQL.Models.MetricsTabQueryModel>(m.Id)).ConfigureAwait(false); ;
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Deleted<GQL.Models.MetricsTabQueryModel>(m.Id)).ConfigureAwait(false); ;
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Removed(Change.Target(m.MeetingId.Value, GQL.Models.MeetingQueryModel.Collections.MetricTab.MetricsTabs), m.Id, m)).ConfigureAwait(false);
      }
    }

    public async Task PinUnpinMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab, IMetricTabHookUpdates updates)
    {
      var m = metricTab.TransformMetricTab();


      var targetsMetricTab =
          new[] {
            new ContainerTarget {
              Type = "metricsTab",
              Id = m.Id,
              Property = "METRICS_TABS"
            }
          };

      await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(m.Id), Change<IMeetingChange>.Updated(m.Id, m, targetsMetricTab)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(m.MeetingId.Value), Change<IMeetingChange>.Updated(m.Id, m, targetsMetricTab)).ConfigureAwait(false);
    }

    public async Task CreateTrackedMetric(ISession s, UserOrganizationModel caller, TrackedMetricModel trackedMetricTab) {
      var m = trackedMetricTab.TransformTrackedMetric();

      await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(m.MetricTabId), Change<IMeetingChange>.Created(m.Id, m)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(m.MetricTabId), Change<IMeetingChange>.Inserted(Change.Target(m.MetricTabId, GQL.Models.MetricsTabQueryModel.Collections.TrackedMetric.TrackedMetrics), m.Id, m));
    }

    public async Task UpdateTrackedMetric(ISession s, UserOrganizationModel caller, TrackedMetricModel trackedMetricTab, ITrackedMetricHookUpdates updates) {
      var m = trackedMetricTab.TransformTrackedMetric();

      var targets =
          new[] {
            new ContainerTarget {
              Type = "metricTab",
              Id = m.MetricTabId,
              Property = "TRACKED_METRICS"
            }
          };

      await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(m.MetricTabId), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
      if (updates.Deleted) {
        await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(m.MetricTabId), Change<IMeetingChange>.Removed(Change.Target(m.MetricTabId, GQL.Models.MetricsTabQueryModel.Collections.TrackedMetric.TrackedMetrics), m.Id, m)).ConfigureAwait(false);
      }
    }

    public async Task CreateMetricCustomGoal(ISession s, UserOrganizationModel caller, MetricCustomGoal goal) {
      var m = goal.TransformMetricCustomGoal();
      var metricTabIds = await MetricTabAccessor.GetMetricTabIdsByMetricIdUnsafe(m.MeasurableId);

      await _eventSender.SendChangeAsync(ResourceNames.Metric(m.MeasurableId), Change<IMeetingChange>.Created(m.Id, m)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Metric(m.MeasurableId), Change<IMeetingChange>.Inserted(Change.Target(m.MeasurableId, GQL.Models.MetricQueryModel.Collections.MetricCustomGoal.CustomGoals), m.Id, m)).ConfigureAwait(false);

      var recurrences = L10Accessor.GetRecurrenceByCustomGoalId(goal.Id);
      foreach (var recurrence in recurrences)
      {
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Created(m.Id, m)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(Change.Target(m.MeasurableId, GQL.Models.MetricQueryModel.Collections.MetricCustomGoal.CustomGoals), m.Id, m)).ConfigureAwait(false);
      }

      foreach (var metricTabId in metricTabIds)
      {
        await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(metricTabId), Change<IMeetingChange>.Created(m.MeasurableId, m)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(metricTabId), Change<IMeetingChange>.Inserted(Change.Target(m.MeasurableId, GQL.Models.MetricQueryModel.Collections.MetricCustomGoal.CustomGoals), m.Id, m)).ConfigureAwait(false);
      }
    }

    public async Task UpdateMetricCustomGoal(ISession s, UserOrganizationModel caller, MetricCustomGoal goal, IMetricCustomGoalHookUpdates updates)
    {
      var recurrences = L10Accessor.GetRecurrenceByCustomGoalId(goal.Id);
      var m = goal.TransformMetricCustomGoal();

      var metricTabIds = await MetricTabAccessor.GetMetricTabIdsByMetricIdUnsafe(m.MeasurableId);

      var targets =
          new[] {
            new ContainerTarget {
              Type = "metric",
              Id = goal.MeasurableId.Value,
              Property = "CUSTOM_GOALS"
            }
          };

      await _eventSender.SendChangeAsync(ResourceNames.Metric(m.MeasurableId), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);

      if (updates.Deleted)
      {
        await _eventSender.SendChangeAsync(ResourceNames.Metric(m.MeasurableId), Change<IMeetingChange>.Removed(Change.Target(m.MeasurableId, GQL.Models.MetricQueryModel.Collections.MetricCustomGoal.CustomGoals), m.Id, m)).ConfigureAwait(false);
      }

      // metric tabs
      foreach (var metricTabId in metricTabIds)
      {
        if (updates.Deleted)
        {
          await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(metricTabId), Change<IMeetingChange>.Removed(Change.Target(m.MeasurableId, GQL.Models.MetricQueryModel.Collections.MetricCustomGoal.CustomGoals), m.Id, m)).ConfigureAwait(false);
        }
        else {
          await _eventSender.SendChangeAsync(ResourceNames.MetricsTab(metricTabId), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
        }
      }
      // recurrences
      foreach (var rec in recurrences)
      {
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(rec.Id), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);

        if (updates.Deleted)
        {
          await _eventSender.SendChangeAsync(ResourceNames.Meeting(rec.Id), Change<IMeetingChange>.Removed(Change.Target(m.MeasurableId, MetricQueryModel.Collections.MetricCustomGoal.CustomGoals), m.Id, m)).ConfigureAwait(false);
        }
      }
    }

    public async Task CreateMetricDivider(ISession session, UserOrganizationModel caller, L10Recurrence.L10Recurrence_MetricDivider divider, L10Recurrence.L10Recurrence_Measurable measurable, L10Recurrence recurrence)
    {
      await SendMetricDividerNotification(session, caller, divider, measurable, recurrence);
    }

    public async Task EditMetricDivider(ISession session, UserOrganizationModel caller, L10Recurrence.L10Recurrence_MetricDivider divider, L10Recurrence.L10Recurrence_Measurable  measurable, L10Recurrence recurrence)
    {
      await SendMetricDividerNotification(session, caller, divider, measurable, recurrence);
    }

    public async Task DeleteMetricDivider(ISession session, UserOrganizationModel caller, L10Recurrence.L10Recurrence_MetricDivider divider, L10Recurrence.L10Recurrence_Measurable  measurable, L10Recurrence recurrence)
    {
      await SendMetricDividerNotification(session, caller, divider, measurable, recurrence);
    }

    public async Task SortMetricDivider(ISession session, UserOrganizationModel caller, long[] measurableIds, long[] dividerIds, L10Recurrence recurrence)
    {
      var measurables = session.Query<L10Recurrence.L10Recurrence_Measurable>().Where(x => measurableIds.Contains(x.Id)).ToList();
      var dividers = session.Query<L10Recurrence.L10Recurrence_MetricDivider>().Where(x => dividerIds.Contains(x.Id)).ToList();

      foreach (var measurable in measurables)
      {
        var m = measurable.TransformL10RecurrenceMeasurable();

        var targets = new ContainerTarget[]{
          new (){
            Type = "meeting",
            Id = recurrence.Id,
            Property = "METRICS",
          }
        };

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Metric(measurable.Id), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
      }

      foreach (var divider in dividers)
      {
        var md = divider.Transform();

          var targets = new ContainerTarget[]{
            new (){
              Type = "meeting",
              Id = recurrence.Id,
              Property = "METRIC_DIVIDERS",
            }
          };

          await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Updated(md.Id, md, targets)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.MetricDivider(md.Id), Change<IMeetingChange>.Updated(md.Id, md, targets)).ConfigureAwait(false);
      }
    }

    async Task SendMetricDividerNotification(ISession session, UserOrganizationModel caller, L10Recurrence.L10Recurrence_MetricDivider divider, L10Recurrence.L10Recurrence_Measurable measurable, L10Recurrence recurrence)
    {
      var m = measurable.TransformL10RecurrenceMeasurable();
      var md = divider.Transform();

      var measurableTargets = new ContainerTarget[]{
        new (){
          Type = "meeting",
          Id = recurrence.Id,
          Property = "METRICS",
        }
      };

      var dividerTargets = new ContainerTarget[]{
        new (){
          Type = "meeting",
          Id = recurrence.Id,
          Property = "METRIC_DIVIDERS",
        }
      };

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Updated(m.Id, m, measurableTargets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Metric(measurable.Id), Change<IMeetingChange>.Updated(m.Id, m, measurableTargets)).ConfigureAwait(false);

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Updated(md.Id, md, dividerTargets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.MetricDivider(md.Id), Change<IMeetingChange>.Updated(md.Id, md, dividerTargets)).ConfigureAwait(false);
    }

  }
}
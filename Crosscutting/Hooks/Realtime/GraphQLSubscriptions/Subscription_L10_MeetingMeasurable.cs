using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.MetricAddExistingLookup;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;
using GQL = RadialReview.GraphQL;


namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_MeetingMeasurable : IMeetingMeasurableHook
  {

    private readonly ITopicEventSender _eventSender;

    public Subscription_L10_MeetingMeasurable(ITopicEventSender eventSender)
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



    public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable)
    {
      var response = SubscriptionResponse<long>.Updated(measurable.Id);

      await _eventSender.SendAsync(ResourceNames.MeasurableEvents, response);

      var recurrenceMeasurables = L10Accessor.GetMeetingsContainingMeasurable(measurable.Id);

      var m = recurMeasurable.TransformL10RecurrenceMeasurable();

      var targets =
          new[] {
            new ContainerTarget
            {
              Type = "meeting",
              Id = recurMeasurable.L10Recurrence.Id,
              Property = "METRICS", //
            }
          };

      var change = Change<IMeetingChange>.Updated(m.Id, m, targets);

      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), change).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(m.Owner.Id), change).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Metric(m.Id), change).ConfigureAwait(false);

      var target = Change.Target(recurMeasurable.L10Recurrence.Id, GQL.Models.MeetingQueryModel.Collections.Metric.Metrics);

      foreach (var rm in recurrenceMeasurables)
      {
        var recurrence = rm.L10Recurrence;
        var metric = rm.TransformL10RecurrenceMeasurable();
        var metric_change = Change<IMeetingChange>.Updated(metric.Id, metric, targets);

        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), metric_change).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Inserted(target, metric.Id, metric)).ConfigureAwait(false);

        if(rm.L10Recurrence.Id != recurMeasurable.L10Recurrence.Id)
        {
          await _eventSender.SendChangeAsync(
            ResourceNames.Meeting(rm.L10Recurrence.Id),
            Change<IMeetingChange>.Inserted(
                Change.Target(rm.L10Recurrence.Id, MetricAddExistingLookupQueryModel.Collections.Meeting9.Meetings),
                m.Id,
                recurMeasurable.L10Recurrence.TransformMeasurableRecurrenceToMeetingQueryModel()
             )
          ).ConfigureAwait(false);
        }
      }
    }


    public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) { }
    public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId, long l10MeasurableId)
    {
      var response = SubscriptionResponse<long>.Archived(measurable.Id);

      await _eventSender.SendAsync(ResourceNames.MeasurableEvents, response);

      var recurrences = L10Accessor.GetMeetingsByL10measurableId(measurable.Id , l10MeasurableId);
      var recurrenceMeasurables = L10Accessor.GetMeetingsContainingMeasurable(measurable.Id);

      /* NOTE: Workaround for Removed_Meeting_Metric notification not sent in DEV when metric is removed from meeting
      ** This issue only appears in release mode builds,
      ** The call to TransformMeasurableToMetric references a property that is lazy loaded therefore requires an active session.
      ** No active session is present at this invocation when any of the following hooks are enabled in HookConfig.cs:
      **   -- ZapierEventSubscription
      **   -- DepristineHooks
      **   -- AuditLogHooks
      ** The workaround is to get a fresh instance of the MeasurableModel using the passed in session.
      */
      var m = s.Get<MeasurableModel>(measurable.Id).TransformMeasurableToMetric();
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Removed(Change.Target(recurrenceId, GQL.Models.MeetingQueryModel.Collections.Metric.Metrics), m.Id, m)).ConfigureAwait(false);

      var targets =
          recurrenceMeasurables.Select(rm =>
            new ContainerTarget
            {
              Type = "meeting",
              Id = rm.L10Recurrence.Id,
              Property = "METRICS", //
            }
          )
          .ToArray();


      foreach (var rm in recurrenceMeasurables)
      {
        var recurrence = rm.L10Recurrence;
        var metric = rm.TransformL10RecurrenceMeasurable();
        var change = Change<IMeetingChange>.Updated(metric.Id, metric, targets);

        if (recurrenceMeasurables.Count == 0)
        {
          await _eventSender.SendChangeAsync(ResourceNames.Metric(metric.Id), change).ConfigureAwait(false);
        }

        var metricAddExistingLookup = measurable.TransformMeasurableToMetricAddExistingLookup();

        await _eventSender.SendChangeAsync(ResourceNames.Metric(metric.Id), Change<IMeetingChange>.Removed(Change.Target(recurrence.Id, GQL.Models.MeetingQueryModel.Collections.Metric.Metrics), metric.Id, metric)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Removed(Change.Target(recurrence.Id, GQL.Models.MeetingQueryModel.Collections.Metric.Metrics), metric.Id, metric)).ConfigureAwait(false);
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Removed(Change.Target(recurrence.Id, GQL.Models.MeetingQueryModel.Collections.MetricAddExistingLookup.MetricAddExistingLookup), m.Assignee.Id, metricAddExistingLookup)).ConfigureAwait(false);

      }

      foreach (var rm in recurrenceMeasurables)
      {
        var recurrence = rm.L10Recurrence;
        var metric = rm.TransformL10RecurrenceMeasurable();

        var metricDeleted = recurrences[0].TransformMeasurableRecurrenceToMeetingQueryModel();
        await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrence.Id), Change<IMeetingChange>.Removed(Change.Target(recurrence.Id, MetricAddExistingLookupQueryModel.Collections.Meeting9.Meetings), recurrenceId, metricDeleted)).ConfigureAwait(false);
      }

      await _eventSender.SendChangeAsync(ResourceNames.Metric(m.Id), Change<IMeetingChange>.Deleted<IMeetingChange>(m.Id)).ConfigureAwait(false);

      var metricFormula = measurable.TransformMeasurableToMetricFormulaLookup();

      await _eventSender.SendChangeAsync(ResourceNames.User(m.Assignee.Id), Change<IMeetingChange>.Removed(Change.Target(m.Assignee.Id, GQL.Models.UserQueryModel.Collections.MetricFormulaLookup.MetricFormulaLookup), m.Assignee.Id, metricFormula)).ConfigureAwait(false);
    }

  }
}

using NHibernate;
using RadialReview.Models;
using RadialReview.Core.Models.Scorecard;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RadialReview.Utilities.Hooks {

	public class IMetricTabHookUpdates {
	//	public IMetricTabHookUpdates(string updateSource) {
	//		UpdateSource = updateSource;
	//	}

	//	public string UpdateSource { get; set; }

	}

	public class ITrackedMetricHookUpdates {
    public bool Deleted { get; set; }
  }

	public class IMetricCustomGoalHookUpdates {
    public bool Deleted { get; set; }
	}

	public interface IMetricHook : IHook {
		Task CreateMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab);
		Task UpdateMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab, IMetricTabHookUpdates updates);
    Task DeleteMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab, IMetricTabHookUpdates updates);

    Task PinUnpinMetricTab(ISession s, UserOrganizationModel caller, MetricTabModel metricTab, IMetricTabHookUpdates updates);

    Task CreateTrackedMetric(ISession s, UserOrganizationModel caller, TrackedMetricModel trackedMetricTab);
		Task UpdateTrackedMetric(ISession s, UserOrganizationModel caller, TrackedMetricModel trackedMetricTab, ITrackedMetricHookUpdates updates);

		Task CreateMetricCustomGoal(ISession s, UserOrganizationModel caller, MetricCustomGoal goal);
		Task UpdateMetricCustomGoal(ISession s, UserOrganizationModel caller, MetricCustomGoal goal, IMetricCustomGoalHookUpdates updates);

    Task CreateMetricDivider(ISession s, UserOrganizationModel caller, Models.L10.L10Recurrence.L10Recurrence_MetricDivider divider, Models.L10.L10Recurrence.L10Recurrence_Measurable measurable, Models.L10.L10Recurrence recurrence);
    Task EditMetricDivider(ISession s, UserOrganizationModel caller, Models.L10.L10Recurrence.L10Recurrence_MetricDivider divider, Models.L10.L10Recurrence.L10Recurrence_Measurable measurable, Models.L10.L10Recurrence recurrence);
    Task DeleteMetricDivider(ISession s, UserOrganizationModel caller, Models.L10.L10Recurrence.L10Recurrence_MetricDivider divider, Models.L10.L10Recurrence.L10Recurrence_Measurable measurable, Models.L10.L10Recurrence recurrence);
    Task SortMetricDivider(ISession session, UserOrganizationModel caller, long[] measurables, long[] dividers, Models.L10.L10Recurrence recurrence);
  }
}

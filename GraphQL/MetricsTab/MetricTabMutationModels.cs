using HotChocolate.Types;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RadialReview.GraphQL.Models.Mutations
{

  public class CreateTrackedMetricModel
  {

    public long MetricId { get; set; }

    public string Color { get; set; }

  }

  public class MetricTabCreateModel
  {

    public long MeetingId { get; set; }

    public string Name { get; set; }

    public string Units { get; set; }

    public string Frequency { get; set; }

    public bool IsPinnedToTabBar { get; set; }

    public bool IsVisibleForTeam { get; set; }

    public long Creator { get; set; }

    [DefaultValue(null)] public CreateTrackedMetricModel[] TrackedMetrics { get; set; }
  }

  public class MetricTabEditModel
  {

    public long Id { get; set; }

    public long? Creator { get; set; }

    public long? MeetingId { get; set; }

    [DefaultValue(null)] public string Name { get; set; }

    [DefaultValue(null)] public string Units { get; set; }

    [DefaultValue(null)] public string Frequency { get; set; }

    [DefaultValue(null)] public bool? IsVisibleForTeam { get; set; }

  }

  public class MetricTabDeleteModel
  {
    public long Id { get; set; }
  }

  public class MetricRemoveFromTabModel
  {
    public long TrackedMetricId { get; set; }
  }

  public class MetricAddToTabModel
  {
    public long MetricsTabId { get; set; }

    public long MetricId { get; set; }

    public string Color { get; set; }
  }

  public class MetricRemoveAllMetricsFromTabModel
  {
    public long MetricsTabId { get; set; }

  }

  public class PinMetricTabModel
  {
    public long Id { get; set; }
    public bool IsPinnedToTabBar { get; set; }
  }

}
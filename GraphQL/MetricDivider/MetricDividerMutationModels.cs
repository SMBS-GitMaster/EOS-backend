using HotChocolate;
using HotChocolate.Types;
using RadialReview.Core.GraphQL.Enumerations;

namespace RadialReview.GraphQL.Models.Mutations
{
  public class MetricDividerCreateModel {
    public long MeetingId { get; set; }
    public int Height { get; set; }
    public string Title { get; set; }
    public string Frequency { get; set; }
  }

  public class MetricDividerEditModel {
    public long Id {get; set; }
    [DefaultValue(null)] public Optional<int?> Height {get; set;}
    [DefaultValue(null)] public Optional<string> Title { get; set; }
    [DefaultValue(null)] public Optional<long?> MetricId { get; set; }
  }

  public class MetricDividerDeleteModel {
    public long Id { get; set;}
  }

  public class MetricDividerSortModel {
    public long Id { get; set; }
    public long MeetingId { get; set; }
    public int SortOrder { get; set; }
  }

}

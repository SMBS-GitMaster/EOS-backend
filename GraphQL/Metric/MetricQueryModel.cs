using Microsoft.Extensions.Logging;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Types;
using System;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models
{
  public class MetricQueryModel : ILogProperties
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }
    public double LastUpdatedClientTimestemp { get; set; }
    public string Type { get { return "metric"; } }

    #endregion

    #region Properties

    public string Title { get; set; }

    public gqlMetricFrequency Frequency { get; set; }

    public gqlUnitType Units { get; set; }

    public gqlLessGreater Rule { get; set; }

    public string SingleGoalValue { get; set; }

    public string MinGoalValue { get; set; }

    public string MaxGoalValue { get; set; }

    public string NotesId { get; set; }

    public MetricCumulativeDataModel CumulativeData { get; set; }

    public MetricAverageDataModel AverageData { get; set; }

    public MetricProgressiveDataModel ProgressiveData { get; set; }

    public string Formula { get; set; }

    public bool Archived { get; set; }

    public long? RecurrenceId { get; set; }

    public int IndexInTable { get; set; }
    public MetricDividerQueryModel? MetricDivider { get; set; }

    public UserQueryModel Assignee { get; set; }
    public UserQueryModel Owner { get; set; }

    // NOTE: This property is never assigned to.  It is here to force the correct filter type in GraphQL
    public List<MeetingQueryModel> Meetings { get; init; } = null;

    public bool ShowCumulative { get; set; }
    public DateTime? CumulativeRange { get; set; }
    public bool ShowAverage { get; set; }
    public DateTime? AverageRange { get; set; }
    public DateTime? ProgressiveDate { get; set; }
    public DayOfWeek StartOfWeek { get; set; }

    #endregion

    public void Log(ITagCollector collector, string prefix)
    {
      collector.Add($"{prefix}.{nameof(this.Id)}", this.Id);
      collector.Add($"{prefix}.{nameof(this.Version)}", this.Version);
      collector.Add($"{prefix}.{nameof(this.LastUpdatedBy)}", this.LastUpdatedBy);
      collector.Add($"{prefix}.{nameof(this.DateCreated)}", this.DateCreated);
      collector.Add($"{prefix}.{nameof(this.DateLastModified)}", this.DateLastModified);

      collector.Add($"{prefix}.{nameof(this.RecurrenceId)}", this.RecurrenceId);
      collector.Add($"{prefix}.{nameof(this.Title)}", this.Title);
      collector.Add($"{prefix}.{nameof(this.Frequency)}", this.Frequency);
      collector.Add($"{prefix}.{nameof(this.Units)}", this.Units);
      collector.Add($"{prefix}.{nameof(this.Rule)}", this.Rule);
      collector.Add($"{prefix}.{nameof(this.Formula)}", this.Formula);
      collector.Add($"{prefix}.{nameof(this.Archived)}", this.Archived);
    }

    public static class Collections
    {
      public enum MetricScore1
      {
        MetricScores
      }

      public enum MetricCustomGoal
      {
        CustomGoals
      }

      public enum Meeting7
      {
        Meetings
      }
    }

    public static class Associations
    {
      public enum MetricDivider1
      {
        MetricDivider
      }

      public enum MetricCumulativeData
      {
        CumulativeData
      }
      public enum MetricAverageData
      {
        AverageData
      }

      public enum MetricProgressiveData
      {
        ProgressiveData
      }

      public enum User13
      {
        Assignee
      }
    }
  }

    public class MetricQueryModelLookup
    {
      public long Id { get; set; }
      public string Title { get; set; }

      public gqlMetricFrequency Frequency { get; set; }
      public bool Archived { get; set; }
    }
}

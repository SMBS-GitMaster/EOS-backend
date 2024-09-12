using HotChocolate;
using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Utilities.Types;
using RadialReview.Models.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations {
  public class MetricCreateModel {

    public string Title { get; set; }
    public long Assignee { get; set; }
    public string Units { get; set; }
    public string Rule { get; set; }
    public string Frequency { get; set; }
    [DefaultValue(null)] public string NotesId { get; set; }
    [DefaultValue(null)] public long[] Meetings { get; set; }
    [DefaultValue(null)] public string Formula { get; set; }
    [DefaultValue(null)] public string SingleGoalValue { get; set; }
    [DefaultValue(null)] public string MinGoalValue { get; set; }
    [DefaultValue(null)] public string MaxGoalValue { get; set; }
    [DefaultValue(null)] public MetricStartRange AverageData { get; set; }
    [DefaultValue(null)] public MetricStartRange CumulativeData { get; set; }
    [DefaultValue(null)] public MetricTargetRange ProgressiveData { get; set; }
    [DefaultValue(null)] public IList<CustomGoalCreate> CustomGoals { get; set; }
  }

  public class MetricEditModel {
    public long MetricId { get; set; }
    [DefaultValue(null)] public string Title { get; set; }
    public long? Assignee { get; set; }
    public string Units { get; set; }
    public string Rule { get; set; }
    public Optional<string> Frequency { get; set; }
    public string NotesId { get; set; }
    [DefaultValue(null)] public long[] Meetings { get; set; }
    [DefaultValue(null)] public Optional<string> Formula { get; set; }
    [DefaultValue(null)] public bool? Archived { get; set; }
    [DefaultValue(null)] public string SingleGoalValue { get; set; }
    [DefaultValue(null)] public string MinGoalValue { get; set; }
    [DefaultValue(null)] public string MaxGoalValue { get; set; }
    [DefaultValue(null)] public NullableField<MetricStartRange> AverageData { get; set; }
    [DefaultValue(null)] public NullableField<MetricStartRange> CumulativeData { get; set; }
    [DefaultValue(null)] public NullableField<MetricTargetRange> ProgressiveData { get; set; }
    [DefaultValue(null)] public IList<CustomGoalEdit> CustomGoals { get; set; }
  }

  public class MetricSortModel {
    public long Id { get; set; }
    public double OldIndex { get; set; }
    public double NewIndex { get; set; }
    public long MeetingId { get; set; }

  }

  public class MetricAddExistingToMeetingModel
  {
    public long MetricId { get; set; }
    public long MeetingId { get; set; }
  }

  public class MetricStartRange {
    public double? StartDate { get; set; }
  }
  public class MetricTargetRange {
    public double TargetDate { get; set; }
  }

  public class CustomGoalCreate
  {
    public double StartDate { get; set; }
    public double EndDate { get; set; }
    public string Rule { get; set; }
    [DefaultValue(null)]
    public string SingleGoalValue { get; set; }
    [DefaultValue(null)]
    public string MinGoalValue { get; set; }
    [DefaultValue(null)]
    public string MaxGoalValue { get; set; }
  }

  public class CustomGoalEdit
  {
    public long? Id { get; set; }
    [DefaultValue(null)]
    public double? StartDate { get; set; }
    [DefaultValue(null)]
    public double? EndDate { get; set; }
    [DefaultValue(null)]
    public string Rule { get; set; }
    [DefaultValue(null)]
    public string SingleGoalValue { get; set; }
    [DefaultValue(null)]
    public string MinGoalValue { get; set; }
    [DefaultValue(null)]
    public string MaxGoalValue { get; set; }
  }

  public class MetricByMeetingIdModel
  {
    public List<long> MeetingIds { get; set;}
  }

  public class MetricFormulaModel
  {
    public List<MetricEditModel> basicFormula { get; set; }
    public List<MetricEditModel> updateFormula { get; set; }
  }
}

using HotChocolate.Types;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Mutations
{
  public class CustomGoalCreateModel
  {
    public long MetricId { get; set; }
    public double StartDate { get; set; }
    public double EndDate { get; set; }
    public string Rule { get; set; }
    [DefaultValue(null)] public string SingleGoalValue { get; set; }
    [DefaultValue(null)] public string MinGoalValue { get; set; }
    [DefaultValue(null)] public string MaxGoalValue { get; set; }
  }

  public class CustomGoalEditModel
  {
    public long Id { get; set; }
    public double? StartDate { get; set; }
    public double? EndDate { get; set; }
    [DefaultValue(null)] public string Rule { get; set; }
    [DefaultValue(null)] public string SingleGoalValue { get; set; }
    [DefaultValue(null)] public string MinGoalValue { get; set; }
    [DefaultValue(null)] public string MaxGoalValue { get; set; }
  }

  public class CustomGoalDeleteModel
  {
    public long Id { get; set; }
  }
}
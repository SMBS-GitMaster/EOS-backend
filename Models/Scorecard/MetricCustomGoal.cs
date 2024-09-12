using Amazon.EC2.Model;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Scorecard
{
  public class MetricCustomGoal : BaseModel, ILongIdentifiable, IDeletable
  {
    public virtual long Id { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual LessGreater Rule { get; set; }

    public virtual string SingleGoalValue { get; set; }

    public virtual string MinGoalValue { get; set; }
    public virtual string MaxGoalValue { get; set; }
    public virtual double? StartDate { get; set; }

    public virtual double? EndDate { get; set; }

    public virtual long? MeasurableId { get; set; }
    public virtual MeasurableModel Measurable { get; set; }

    public MetricCustomGoal()
    {
      CreateTime = DateTime.UtcNow;
    }

    public class CustomGoalsMap : BaseModelClassMap<MetricCustomGoal>
    {
      public CustomGoalsMap()
      {
        Id(x => x.Id);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.StartDate);
        Map(x => x.EndDate);
        Map(x => x.Rule);
        Map(x => x.SingleGoalValue);
        Map(x => x.MinGoalValue);
        Map(x => x.MaxGoalValue);
        Map(x => x.MeasurableId);
        References(x => x.Measurable).Column("MeasurableId").LazyLoad().ReadOnly();
      }
    }
  }
}

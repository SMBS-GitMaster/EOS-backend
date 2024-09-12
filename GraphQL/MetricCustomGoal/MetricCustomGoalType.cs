namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;

  public class MetricCustomGoalChangeType : MetricCustomGoalType
  {
    public MetricCustomGoalChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricCustomGoalQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricCustomGoalChange");
    }
  }

  public class MetricCustomGoalType : ObjectType<MetricCustomGoalQueryModel>
  {
    protected readonly bool isSubscription;

    public MetricCustomGoalType() 
      : this(false)
    {
    }

    protected MetricCustomGoalType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricCustomGoalQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricCustomGoal");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>();

    }
  }
}

using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types
{
  public class MetricScoreChangeType : MetricScoreType
  {
    public MetricScoreChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricScoreQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricScoreModelChange");
    }
  }

  public class MetricScoreType : ObjectType<MetricScoreQueryModel>
  {
    protected readonly bool isSubscription; 

    public MetricScoreType() 
      : this(false)
    {
    }

    protected MetricScoreType(bool isSubscription) 
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricScoreQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricScore");

      descriptor
          .Field(t => t.Value)
          .Type<StringType>();
    }
  }
}

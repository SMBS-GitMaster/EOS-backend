using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
  public class MetricProgressiveDataChangeType : MetricAverageDataType
  {
    public MetricProgressiveDataChangeType()
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricAverageDataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricAverageDataChange");
    }
  }

  public class MetricProgressiveDataType : ObjectType<MetricProgressiveDataModel>
  {
    protected readonly bool isSubscription;

    public MetricProgressiveDataType()
      : this(isSubscription: false)
    {
    }

    protected MetricProgressiveDataType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricProgressiveDataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricProgressiveData")
      ;

      descriptor
          .Field(t => t.TargetDate)
          .Type<LongType>()
        ;
    }
  }
}
using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
  public class MetricCumulativeDataChangeType : MetricCumulativeDataType
  {
    public MetricCumulativeDataChangeType()
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricCumulativeDataModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("MetricCumulativeChange");
    }
  }

  public class MetricCumulativeDataType : ObjectType<MetricCumulativeDataModel>
  {
    protected readonly bool isSubscription;

    public MetricCumulativeDataType()
      : this(isSubscription: false)
    {
    }

    protected MetricCumulativeDataType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricCumulativeDataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricCumulativeData")
      ;

      descriptor
          .Field(t => t.StartDate)
          .Type<LongType>()
        ;

    }
  }
}
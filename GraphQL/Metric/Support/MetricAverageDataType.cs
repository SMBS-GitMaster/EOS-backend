using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
  public class MetricAverageDataChangeType : MetricAverageDataType
  {
    public MetricAverageDataChangeType()
      : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MetricAverageDataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MetricAverageDataChange");
    }
  }

  public class MetricAverageDataType : ObjectType<MetricAverageDataModel>
  {
    protected readonly bool isSubscription;

    public MetricAverageDataType()
      : this(isSubscription: false)
    {
    }

    protected MetricAverageDataType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MetricAverageDataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricAverageData")
      ;

      descriptor
          .Field(t => t.StartDate)
          .Type<LongType>()
        ;
    }
  }
}
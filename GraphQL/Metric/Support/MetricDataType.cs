using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
  public class MetricDataType : ObjectType<MetricDataModel>
  {

    protected override void Configure(IObjectTypeDescriptor<MetricDataModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricData")
      ;

    }
  }
}

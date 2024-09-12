namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;

  public class MetricsTabPinnedQueryType : ObjectType<MetricsTabPinnedQueryModel>
  {

    protected override void Configure(IObjectTypeDescriptor<MetricsTabPinnedQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "metricsTabPinned")
          ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.MetricsTabId)
          .Type<LongType>()
          .IsProjected(true);

      descriptor
          .Field(t => t.UserId)
          .Type<LongType>()
          .IsProjected(true);

    }

  }
}

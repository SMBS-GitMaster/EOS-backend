namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;

  public class NodeStatDataQueryType : ObjectType<NodeStatDataQueryModel>
  {

    protected override void Configure(IObjectTypeDescriptor<NodeStatDataQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "nodeStatData");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>();

    }

  }
}

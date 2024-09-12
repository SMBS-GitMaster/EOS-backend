namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;


  public class WorkspaceTilePositionQueryType : ObjectType<WorkspaceTilePositionQueryModel>
  {

    protected override void Configure(IObjectTypeDescriptor<WorkspaceTilePositionQueryModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
}
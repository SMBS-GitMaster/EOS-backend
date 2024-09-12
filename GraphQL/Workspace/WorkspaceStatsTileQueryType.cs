namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;


  public class WorkspaceStatsTileQueryType : ObjectType<WorkspaceStatsTileQueryModel>
  {

    protected override void Configure(IObjectTypeDescriptor<WorkspaceStatsTileQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "statsTileSettings")
          .UsePaging<WorkspaceStatsTileQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          ;

    }
  }
}
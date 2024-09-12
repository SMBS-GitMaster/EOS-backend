namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;


  public class WorkspaceChangeType : WorkspaceQueryType
  {
    public WorkspaceChangeType()
        : base(isSubscription: true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<WorkspaceQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("WorkspaceModelChange");
    }
  }

  public class WorkspaceQueryType : ObjectType<WorkspaceQueryModel>
  {
    protected readonly bool isSubscription;

    public WorkspaceQueryType()
        : this(isSubscription: false)
    {
    }

    protected WorkspaceQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<WorkspaceQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "workspace")
          ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();

      if (isSubscription)
      {
        descriptor
          .Field(t => t.Tiles)
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("workspaceNotes")
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetWorkspacePersonalNotes(ctx.Parent<WorkspaceQueryModel>().Id, cancellationToken))
          .UseFiltering()
          .UseSorting()
          ;
      }
      else
      {
        descriptor
          .Field(t => t.Tiles)
          .UsePaging<WorkspaceTileQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("workspaceNotes")
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetWorkspacePersonalNotes(ctx.Parent<WorkspaceQueryModel>().Id, cancellationToken))
          .UsePaging<WorkspaceNoteQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseFiltering()
          .UseSorting()
          ;

      }
    }

  }
}
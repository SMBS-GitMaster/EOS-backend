namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class WorkspaceTileChangeType : WorkspaceTileQueryType
  {
    public WorkspaceTileChangeType() : base(isSubscription: true) { }

    protected override void Configure(IObjectTypeDescriptor<WorkspaceTileQueryModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("WorkspaceTileModelChange");
    }
  }


  public class WorkspaceTileQueryType : ObjectType<WorkspaceTileQueryModel>
  {

    protected readonly bool isSubscription;

    public WorkspaceTileQueryType() : this(isSubscription: false) { }

    protected WorkspaceTileQueryType(bool isSubscription) { this.isSubscription = isSubscription; }

    protected override void Configure(IObjectTypeDescriptor<WorkspaceTileQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "workspaceTile")
          ;

    }

  }
}
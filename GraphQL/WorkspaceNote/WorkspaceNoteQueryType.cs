namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class WorkspaceNoteChangeType : WorkspaceNoteQueryType
  {
    public WorkspaceNoteChangeType() : base(isSubscription: true) { }

    protected override void Configure(IObjectTypeDescriptor<WorkspaceNoteQueryModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("WorkspaceNoteModelChange");
    }
  }


  public class WorkspaceNoteQueryType : ObjectType<WorkspaceNoteQueryModel>
  {

    protected readonly bool isSubscription;

    public WorkspaceNoteQueryType() : this(isSubscription: false) { }

    protected WorkspaceNoteQueryType(bool isSubscription) { this.isSubscription = isSubscription; }


    protected override void Configure(IObjectTypeDescriptor<WorkspaceNoteQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "workspaceNote")
          ;
    }

  }

}
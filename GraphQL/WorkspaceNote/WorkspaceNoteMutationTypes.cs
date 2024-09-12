using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Repositories;

namespace RadialReview.Core.GraphQL.Types.Mutations
{
  public partial class WorkspaceNoteCreateMutationType : InputObjectType<CreateWorkspaceNoteModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<CreateWorkspaceNoteModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class WorkspaceNoteEditMutationType : InputObjectType<EditWorkspaceNoteModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<EditWorkspaceNoteModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL
{

  public partial class MutationType
  {

    public void AddWorkspaceNoteMutations(IObjectTypeDescriptor descriptor)
    {

      descriptor
        .Field("CreateWorkspaceNote")
        .Argument("input", a => a.Type<NonNullType<WorkspaceNoteCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateWorkspaceNote(ctx.ArgumentValue<CreateWorkspaceNoteModel>("input")));

      descriptor
        .Field("EditWorkspaceNote")
        .Argument("input", a => a.Type<NonNullType<WorkspaceNoteEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditWorkspaceNote(ctx.ArgumentValue<EditWorkspaceNoteModel>("input")));

    }

  }

}
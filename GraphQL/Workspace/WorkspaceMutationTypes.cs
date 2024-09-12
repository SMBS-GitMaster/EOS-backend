using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Repositories;

namespace RadialReview.Core.GraphQL.Types.Mutations
{

  public partial class WorkspaceCreateMutationType : InputObjectType<WorkspaceCreateModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<WorkspaceCreateModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class WorkspaceEditMutationType : InputObjectType<WorkspaceEditModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<WorkspaceEditModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class WorkspaceCreateWorkspaceTileNodeMutationType : InputObjectType<WorkspaceTileNodeCreateModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<WorkspaceTileNodeCreateModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class WorkspaceEditWorkspaceTileNodeMutationType : InputObjectType<WorkspaceTileNodeEditModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<WorkspaceTileNodeEditModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }


  public partial class WorkspaceEditWorkspaceTilePositionsMutationType : InputObjectType<WorkspaceEditTilePositionsModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<WorkspaceEditTilePositionsModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

}

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddWorkspaceMutations(IObjectTypeDescriptor descriptor)
    {

      descriptor
        .Field("CreateWorkspace")
        .Argument("input", a => a.Type<NonNullType<WorkspaceCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateWorkspace(ctx.ArgumentValue<WorkspaceCreateModel>("input")));

      descriptor
        .Field("EditWorkspace")
        .Argument("input", a => a.Type<NonNullType<WorkspaceEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditWorkspace(ctx.ArgumentValue<WorkspaceEditModel>("input")));

      descriptor
        .Field("CreateWorkspaceTileNode")
        .Argument("input", a => a.Type<NonNullType<WorkspaceCreateWorkspaceTileNodeMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateWorkspaceTile(ctx.ArgumentValue<WorkspaceTileNodeCreateModel>("input")));

      descriptor
        .Field("EditWorkspaceTileNode")
        .Argument("input", a => a.Type<NonNullType<WorkspaceEditWorkspaceTileNodeMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditWorkspaceTile(ctx.ArgumentValue<WorkspaceTileNodeEditModel>("input")));


      descriptor
        .Field("EditWorkspaceTilePositions")
        .Argument("input", a => a.Type<NonNullType<WorkspaceEditWorkspaceTilePositionsMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditWorkspaceTilePositions(ctx.ArgumentValue<WorkspaceEditTilePositionsModel>("input")));

    }
  }
}
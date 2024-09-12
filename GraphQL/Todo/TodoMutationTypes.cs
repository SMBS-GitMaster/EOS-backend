using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL {

  public partial class TodoCreateMutationType : InputObjectType<TodoCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<TodoCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class TodoEditMutationType : InputObjectType<TodoEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<TodoEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddTodoMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("CreateTodo")
        .Argument("input", a => a.Type<NonNullType<TodoCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateTodo(ctx.ArgumentValue<TodoCreateModel>("input")));

      descriptor
        .Field("EditTodo")
        .Argument("input", a => a.Type<NonNullType<TodoEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditTodo(ctx.ArgumentValue<TodoEditModel>("input")));

    }
  }
}

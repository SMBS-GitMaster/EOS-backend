using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Types.Mutations
{
    public partial class CustomGoalCreateMutationType : InputObjectType<CustomGoalCreateModel>
    {
      protected override void Configure(IInputObjectTypeDescriptor<CustomGoalCreateModel> descriptor)
      {
        base.Configure(descriptor);
        descriptor.Field(f => f.Rule)
        .Type<NonNullType<StringType>>();
      }
    }

    public partial class CustomGoalEditMutationType : InputObjectType<CustomGoalEditModel>
    {
      protected override void Configure(IInputObjectTypeDescriptor<CustomGoalEditModel> descriptor)
      {
        base.Configure(descriptor);
      }
    }
    public partial class CustomGoalDeleteMutationType : InputObjectType<CustomGoalDeleteModel>
    {
      protected override void Configure(IInputObjectTypeDescriptor<CustomGoalDeleteModel> descriptor)
      {
        base.Configure(descriptor);
      }
    }

}

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddCustomGoalMutations(IObjectTypeDescriptor descriptor)
    {
      descriptor
        .Field("CreateCustomGoal")
        .Argument("input", a => a.Type<NonNullType<CustomGoalCreateMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateCustomGoal(ctx.ArgumentValue<CustomGoalCreateModel>("input")));

      descriptor
        .Field("EditCustomGoal")
        .Argument("input", a => a.Type<NonNullType<CustomGoalEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditCustomGoal(ctx.ArgumentValue<CustomGoalEditModel>("input")));

      descriptor
        .Field("DeleteCustomGoal")
        .Argument("input", a => a.Type<NonNullType<CustomGoalDeleteMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().DeleteCustomGoal(ctx.ArgumentValue<CustomGoalDeleteModel>("input")));

    }
  }
} 

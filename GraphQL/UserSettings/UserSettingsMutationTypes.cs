using HotChocolate.Types;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Repositories;

namespace RadialReview.GraphQL
{

  public partial class UserSettingsEditType : InputObjectType<UserSettingsEditModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<UserSettingsEditModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

}

namespace RadialReview.GraphQL
{
  public partial class MutationType
  {
    public void AddUserSettingsMutations(IObjectTypeDescriptor descriptor)
    {

      descriptor
        .Field("EditUserSettings")
        .Argument("input", a => a.Type<NonNullType<UserSettingsEditType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditUserSettings(ctx.ArgumentValue<UserSettingsEditModel>("input")));

    }
  }
}

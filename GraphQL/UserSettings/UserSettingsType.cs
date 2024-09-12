namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.Core.GraphQL.Types;
  using RadialReview.GraphQL.Models;
  using RadialReview.Models;
  using RadialReview.Repositories;

  public class UserChangeSettingsType : UserSettingsType
  {
    public UserChangeSettingsType()
    {
    }

    protected override void Configure(IObjectTypeDescriptor<UserSettingsQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("UserModelChange");
    }
  }

  public class UserSettingsType : ObjectType<UserSettingsQueryModel>
  {
    public UserSettingsType()
    {
    }

    protected override void Configure(IObjectTypeDescriptor<UserSettingsQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "user");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
      .Field(t => t.HasViewedFeedbackModalOnce)
      .Type<BooleanType>();

      descriptor
      .Field(t => t.DoNotShowFeedbackModalAgain)
      .Type<BooleanType>();

      descriptor
        .Field(t => t.TransferredBusinessPlansBannerViewCount)
        .Type<IntType>();
    }
  }
}

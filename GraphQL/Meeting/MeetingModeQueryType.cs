namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;
  using System;
  using System.Linq;

  public class MeetingModeQueryType : ObjectType<MeetingModeModel>
  {
    protected readonly bool isSubscription;

    public MeetingModeQueryType()
      : this(false)
    {
    }

    protected MeetingModeQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingModeModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "meetingMode")
        ;

      descriptor
        .Field(t => t.Id)
        .Type<StringType>();

      descriptor
        .Field(t => t.Name)
        .Type<StringType>();

      descriptor
        .Field(t => t.Enabled)
        .Type<BooleanType>();

      descriptor
        .Field(t => t.Hidden)
        .Type<BooleanType>();

      descriptor
        .Field(t => t.ImportLongTermIssues)
        .Type<BooleanType>();

    }
  }
}
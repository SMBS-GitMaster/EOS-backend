namespace RadialReview.GraphQL
{
  using System.Linq;
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;

  public class PermissionType : ObjectType
  {
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("canViewMeetingsThatSelfDoesNotAttend")
        .Type<BooleanType>()
        .Resolve(ctx => true);

      descriptor
        .Field("permission1")
        .Type<BooleanType>()
        .Resolve(ctx => true);

      descriptor
        .Field("permission2")
        .Type<BooleanType>()
        .Resolve(ctx => false);
    }
  }
}
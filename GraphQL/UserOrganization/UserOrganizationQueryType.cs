namespace RadialReview.GraphQL {
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;

  public class UserOrganizationQueryType : ObjectType<UserOrganizationQueryModel> {
    protected override void Configure(IObjectTypeDescriptor<UserOrganizationQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "userOrganization");
    }
  }
}
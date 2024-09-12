namespace RadialReview.GraphQL
{
  using System.Linq;
  using HotChocolate.Types;
  using RadialReview.Api.Authentication;

  public class TokenResultType : ObjectType<TokenResult>
  {
    protected override void Configure(IObjectTypeDescriptor<TokenResult> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field(t => t.Id)
        .Type<StringType>();

      descriptor
        .Field(t => t.Token)
        .Type<StringType>();

      descriptor
        .Field(t => t.ValidTo)
        .Type<DateTimeType>();
    }
  }
}
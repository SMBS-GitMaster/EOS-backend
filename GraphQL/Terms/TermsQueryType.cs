namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;
  using HotChocolate.Types.Pagination;
  using RadialReview.Repositories;

  public class TermsQueryType : ObjectType<TermsQueryModel>
  {
    protected readonly bool isSubscription;

    public TermsQueryType() : this(false)
    {
    }

    protected TermsQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<TermsQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "terms")
          ;

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          ;
    }
  }
}
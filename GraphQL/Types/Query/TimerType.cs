namespace RadialReview.Core.GraphQL.Types.Query
{
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.Repositories;
  using System.Linq;

  public class TimerType : ObjectType
  {

    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "timer");

    }

  }
}

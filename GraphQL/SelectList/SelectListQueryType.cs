using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.Core.GraphQL.Types;

public class SelectListChangeType : SelectListQueryType
{
  public SelectListChangeType() : base(true)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<SelectListQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor.Name("SelectListModelChange");
  }
}

public class SelectListQueryType : ObjectType<SelectListQueryModel>
{
  protected readonly bool isSubscription;

  public SelectListQueryType()
    : this(false)
  {
  }

  protected SelectListQueryType(bool isSubscription)
  {
    this.isSubscription = isSubscription;
  }

  protected override void Configure(IObjectTypeDescriptor<SelectListQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor
        .Field("type")
        .Resolve(_ => "meetingPermissionLookup");
  }
}
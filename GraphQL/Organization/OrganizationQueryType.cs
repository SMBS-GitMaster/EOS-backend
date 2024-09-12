namespace RadialReview.GraphQL.Types
{
  using HotChocolate.Types;
  using RadialReview.GraphQL.Models;
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;


  public class OrganizationChangeType : OrganizationQueryType
  {
    public OrganizationChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<OrganizationQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("OrganizationModelChange");
    }
  }

  public class OrganizationQueryType : ObjectType<OrganizationQueryModel>
  {
    protected static readonly System.Object dummy = new System.Object();
    protected readonly bool isSubscription;

    public OrganizationQueryType()
      : this(false)
    {
    }

    protected OrganizationQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<OrganizationQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "Organization");
    }
  }
}

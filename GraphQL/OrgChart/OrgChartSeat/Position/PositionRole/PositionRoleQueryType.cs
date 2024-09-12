namespace RadialReview.Core.GraphQL.Types;

using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class PositionRoleChangeType : PositionRoleQueryType
{
  public PositionRoleChangeType() : base(true)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartPositionRoleQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor.Name("PositionRoleModelChange");
  }
}

public class PositionRoleQueryType : ObjectType<OrgChartPositionRoleQueryModel>
{
  protected readonly bool isSubscription;

  protected PositionRoleQueryType(bool isSubscription)
  {
    this.isSubscription = isSubscription;
  }

  public PositionRoleQueryType()
    : this(false)
  {
  }

  protected override void Configure(IObjectTypeDescriptor<OrgChartPositionRoleQueryModel> descriptor)
  {
    base.Configure(descriptor);

    descriptor
      .Field("type")
      .Resolve(_ => "orgChartPositionRole")
      ;

  }
}
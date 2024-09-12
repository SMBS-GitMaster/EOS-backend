using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Types.Subscriptions
{
  public class ContainerTargetQueryType : ObjectType<ContainerTarget>
  {
    protected override void Configure(IObjectTypeDescriptor<ContainerTarget> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Field(t => t.Id)
      .Type<NonNullType<CustomKeyType>>();
    }
  }
}

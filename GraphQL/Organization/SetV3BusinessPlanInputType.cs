using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Organization
{
  public class SetV3BusinessPlanInputType : InputObjectType<SetV3BusinessPlanInput>
  {
    protected override void Configure(IInputObjectTypeDescriptor<SetV3BusinessPlanInput> descriptor)
    {
      descriptor.Name("SetV3BusinessPlanInput");
      base.Configure(descriptor);
    }
  }

  public class SetV3BusinessPlanInput
  {
    public long? BusinessPlanId { get; set; }
  }
}

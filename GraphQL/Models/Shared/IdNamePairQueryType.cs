using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Types
{

  public class IdNamePairQueryType : ObjectType<IdNamePairQueryModel>
  {

    protected override void Configure(IObjectTypeDescriptor<IdNamePairQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "idNamePair");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>()
        .IsProjected(true)
        ;

      descriptor
        .Field(t => t.Name)
        .Type<StringType>();

    }
  }
}

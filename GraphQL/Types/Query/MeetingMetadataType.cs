using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Types{

  public class MeetingMetadataType : ObjectType<MeetingMetadataModel> {

    protected override void Configure(IObjectTypeDescriptor<MeetingMetadataModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meeting");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>()
          .IsProjected(true);

    }
  }
}

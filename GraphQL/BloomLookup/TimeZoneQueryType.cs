using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
  public class TimeZoneQueryType : ObjectType<TimeZoneQueryModel>
  {

    protected override void Configure(IObjectTypeDescriptor<TimeZoneQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
          .Field(t => t.Id)
          .Type<StringType>()
          ;

      descriptor
        .Field(t => t.IANA_Name)
        .Type<StringType>()
        .Name("iANA_Name")
        ;

    }

  }
}

using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types {
  public class MeetingSummaryType : ObjectType<MeetingSummaryModel> {
    protected override void Configure(IObjectTypeDescriptor<MeetingSummaryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meetingSummary");

      descriptor
          .Field(t => t.Id)
          .Type<LongType>();
    }
  }
}
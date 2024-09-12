namespace RadialReview.GraphQL
{
    using HotChocolate.Types;
    using RadialReview.GraphQL.Models;

    public class PageNameType : ObjectType<PageNameModel>
    {
        protected override void Configure(IObjectTypeDescriptor<PageNameModel> descriptor)
        {
            base.Configure(descriptor);

            descriptor
              .Field("type")
              .Type<StringType>()
              .Resolve(ctx => "pageName")
              ;

            descriptor
                .Field(t => t.Id)
                .Type<LongType>();
        }
    }
}
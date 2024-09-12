namespace RadialReview.GraphQL
{
    using HotChocolate.Types;
    using RadialReview.GraphQL.Models;

    public class IssueContextType : ObjectType<ContextModel>
    {
        protected override void Configure(IObjectTypeDescriptor<ContextModel> descriptor)
        {
            base.Configure(descriptor);

            descriptor
                .Field("type")
                .Type<StringType>()
                .Resolve(ctx => "issueContext")
                ;

            descriptor
                .Field(t => t.FromNodeTitle)
                .Type<StringType>()
                ;

            descriptor
                .Field(t => t.FromNodeType)
                .Type<StringType>()
                ;
        }
    }
}
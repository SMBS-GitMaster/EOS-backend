using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;

namespace RadialReview.GraphQL.Types
{
    public class CommentChangeType : CommentQueryType
    {
        public CommentChangeType() : base(true)
        {
        }

        protected override void Configure(IObjectTypeDescriptor<CommentQueryModel> descriptor)
        {
        base.Configure(descriptor);

        descriptor.Name("CommentModelChange");
        }
    }

    public class CommentQueryType : ObjectType<CommentQueryModel>
    {
        protected bool isSubscription;
        public CommentQueryType() 
            : this(false)
        {
        }

        protected CommentQueryType(bool isSubscription)
        {
            this.isSubscription = isSubscription;
        }

        protected override void Configure(IObjectTypeDescriptor<CommentQueryModel> descriptor)
        {
            base.Configure(descriptor);

            descriptor
                .Field("type")
                .Type<StringType>()
                .Resolve(ctx => "comment")
                ;

            descriptor
                .Field(t => t.Id).IsProjected(true)
                .Type<LongType>()
                ;

            descriptor
                .Field(t => t.AuthorId).IsProjected(true)
                .Type<LongType>()
                ;

            if (isSubscription)
            {
              descriptor
                  .Field("author")
                  .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetUserById(ctx.Parent<CommentQueryModel>().AuthorId, ct))
                  .Type<UserChangeType>()
                  ;
            }
            else
            {
              descriptor
                  .Field("author")
                  .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetUserById(ctx.Parent<CommentQueryModel>().AuthorId, ct))
                  .Type<UserQueryType>()
                  ;
            }
          }
    }
}
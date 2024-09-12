using HotChocolate.Types;
using RadialReview.GraphQL.Models;

namespace RadialReview.GraphQL.Types
{
    public class MilestoneChangeType : MilestoneQueryType
    {
        public MilestoneChangeType() : base(true)
        {
        }

        protected override void Configure(IObjectTypeDescriptor<MilestoneQueryModel> descriptor)
        {
        base.Configure(descriptor);
        
        descriptor.Name("MilestoneModelChange");
        }
    }

    public class MilestoneQueryType : ObjectType<MilestoneQueryModel>
    {
        protected readonly bool isSubscription; 

        public MilestoneQueryType() 
          : this(false)
        {
        }

        protected MilestoneQueryType(bool isSubscription)
        {
            this.isSubscription = isSubscription;
        }

        protected override void Configure(IObjectTypeDescriptor<MilestoneQueryModel> descriptor)
        {
            base.Configure(descriptor);

            descriptor
                .Field("type")
                .Type<StringType>()
                .Resolve(ctx => "milestone");

            descriptor
                .Field(t => t.Id)
                .Type<LongType>();
        }
    }
}
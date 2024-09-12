namespace RadialReview.GraphQL
{
    using System.Linq;
    using HotChocolate.Types;
    using RadialReview.GraphQL.Models;
    using RadialReview.Repositories;

    public class TimerByPageType : ObjectType<TimerByPageModel>
    {
        protected override void Configure(IObjectTypeDescriptor<TimerByPageModel> descriptor)
        {
            base.Configure(descriptor);

            descriptor
                .Field("type")
                .Type<StringType>()
                .Resolve(ctx => "timerByPage")
                ;

            descriptor
                .Field(t => t.Id)
                .Type<LongType>()
                ;
        }
    }

}
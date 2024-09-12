using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Types;

namespace RadialReview.Core.GraphQL.BusinessPlan.Types
{
  public class SubscriptionTypeExtension
  {
    public static void BusinessPlanSubscriptions(IObjectTypeDescriptor descriptor)
    {
      descriptor
            .Field("businessPlanCreated")
            .Type<ObjectType<BusinessPlanModel>>()
            .Resolve(context => context.GetEventMessage<BusinessPlanModel>())
            .Subscribe(async ctx =>
            {
              var receiver = ctx.Service<ITopicEventReceiver>();

              ISourceStream stream =
                  await receiver.SubscribeAsync<BusinessPlanModel>("businessPlanCreated");

              return stream;
            });

      descriptor
        .Field("businessPlans")
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.BusinessPlans);
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("businessPlan")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.BusinessPlan(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;
    }
  }
}

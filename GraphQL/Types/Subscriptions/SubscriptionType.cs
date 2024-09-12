using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Subscriptions;
using HotChocolate.Types;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Types;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models.L10;
using MeetingType = RadialReview.GraphQL.Types.MeetingQueryType;
using RadialReview.Core.GraphQL.BusinessPlan.Types;
using RadialReview.Utilities;

namespace RadialReview.Core.GraphQL.Types
{
  public class SubscriptionType : ObjectType
  {
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("bloomLookupNode")
        .Argument("id", a => a.Type<NonNullType<StringType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = long.Parse(ctx.ArgumentValue<string>("id"));
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.BloomlookupNode(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("meeting")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Meeting(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("headline")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Headline(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("workspace")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Workspace(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("attendees")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.MeetingAttendee(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("issue")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id =ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Issue(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("goal")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Goal(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("todo")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Todo(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("metricsTab")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.MetricsTab(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("metric")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Metric(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("milestone")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Milestone(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("user")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.User(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("orgChart")
        .Argument("id", a => a.Type<NonNullType<LongType>>())
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var id = ctx.ArgumentValue<long>("id");
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.OrgChart(id));
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("users")
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Users);
          var modified_stream = stream.ToRedactedStream(ctx);

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("meetings")
        .Type<MeetingSubscriptionChangeType>()
        .Resolve(ctx => ctx.GetEventMessage<IChange<IMeetingChange>>())
        .UseFiltering()
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();
          var stream = await receiver.SubscribeAsync<string>(ResourceNames.Meetings);
          var modified_stream = stream.ToRedactedStream(ctx, x => {
            using var session = HibernateSession.GetCurrentSession();

            var accessor = ctx.Service<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var caller = RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems.GetUser(accessor.HttpContext);
            var perms = PermissionsUtility.Create(session, caller);

            return x switch
            {
              Created<IMeetingChange, MeetingQueryModel, long> created => perms.IsPermitted(x => x.CanView(RadialReview.Models.PermItem.ResourceType.L10Recurrence, created.Id)),
              Updated<IMeetingChange, MeetingQueryModel, long> updated => perms.IsPermitted(x => x.CanView(RadialReview.Models.PermItem.ResourceType.L10Recurrence, updated.Id)),
              Deleted<IMeetingChange, MeetingQueryModel, long> deleted => perms.IsPermitted(x => x.CanView(RadialReview.Models.PermItem.ResourceType.L10Recurrence, deleted.Id)),
              UpdatedAssociation<IMeetingChange, MeetingQueryModel, MeetingQueryModel, long> updatedAssociation => perms.IsPermitted(x => x.CanView(RadialReview.Models.PermItem.ResourceType.L10Recurrence, updatedAssociation.Id)),
              Inserted<IMeetingChange, MeetingQueryModel, MeetingQueryModel, long> Inserted => perms.IsPermitted(x => x.CanView(RadialReview.Models.PermItem.ResourceType.L10Recurrence, Inserted.Id)),

              _ => false
            };
          });

          return modified_stream;
        })
        .Authorize()
        ;

      descriptor
        .Field("setMeetingPage")
        .Resolve(ctx => ctx.GetEventMessage<GraphQLResponseBase>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<GraphQLResponseBase>("setMeetingPage");

          return stream;
        });

      descriptor
        .Field(ResourceNames.StartMeeting)
        .Resolve(ctx => ctx.GetEventMessage<GraphQLResponse<StartMeetingMutationOutputDTO>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<GraphQLResponse<StartMeetingMutationOutputDTO>>(ResourceNames.StartMeeting);

          return stream;
        });

      descriptor
        .Field(ResourceNames.HeadlineEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.HeadlineEvents);

          return stream;
        });


      descriptor
        .Field(ResourceNames.WorkspaceEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.WorkspaceEvents);

          return stream;
        });


      descriptor
        .Field(ResourceNames.GoalsEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.GoalsEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.TodoEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.TodoEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.IssueEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.IssueEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.MeetingAttendeeEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.MeetingAttendeeEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.MeasurableEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.MeasurableEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.RateMeetingEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.RateMeetingEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.WrapUpEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.WrapUpEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.MilestoneEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.MilestoneEvents);

          return stream;
        });

      descriptor
        .Field(ResourceNames.ScoreEvents)
        .Resolve(ctx => ctx.GetEventMessage<SubscriptionResponse<long>>())
        .Subscribe(async ctx =>
        {
          var receiver = ctx.Service<ITopicEventReceiver>();

          ISourceStream stream = await receiver.SubscribeAsync<SubscriptionResponse<long>>(ResourceNames.ScoreEvents);

          return stream;
        });

      SubscriptionTypeExtension.BusinessPlanSubscriptions(descriptor);
    }
  }
}

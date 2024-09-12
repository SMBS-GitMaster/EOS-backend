using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;
using System.Diagnostics;
using System.Linq;
using static HotChocolate.Types.ProjectionObjectFieldDescriptorExtensions;

namespace RadialReview.GraphQL.Types
{
  public class GoalMeetingChangeType : GoalMeetingQueryType
  {
    public GoalMeetingChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<GoalMeetingQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("GoalMeetingModelChange");
    }
  }

  public class GoalMeetingQueryType : ObjectType<GoalMeetingQueryModel>
  {
    protected readonly bool isSubscription;

    public GoalMeetingQueryType()
      : this(false)
    {
    }

    protected GoalMeetingQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<GoalMeetingQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "meeting")
        ;

      descriptor
        .Field(t => t.Id).IsProjected(true)
        .Type<LongType>()
        ;

      descriptor
        .Field("currentMeetingAttendee")
        .Type<MeetingAttendeeQueryType>()
        .Resolve(context => {
          var sw = Stopwatch.StartNew();
          //we don't want the pharent to try to initially load the attendees list (perfomance stuff)
          // so we make use of BatchDataLoader instead 
          var res = context.BatchDataLoader<long, MeetingAttendeeQueryModel>(async (meetingIds, ct) => {
            var sw = Stopwatch.StartNew();
            var dc = context.Service<IDataContext>();
            var a = sw.ElapsedMilliseconds;
            var ids = meetingIds.ToList();
            var b = sw.ElapsedMilliseconds;
            var att = context.Parent<GoalMeetingQueryModel>().Attendees;
            var result = await dc.GetCurrentAttendeesByMeetingIdsAsync(ids,
              context.Parent<GoalMeetingQueryModel>().UserId, ct);

            var f = sw.ElapsedMilliseconds;
            var output = result.ToDictionary(attendee => attendee.MeetingId);
            var e = sw.ElapsedMilliseconds;
            return output;
          }).LoadAsync(context.Parent<GoalMeetingQueryModel>().Id);
          var a = sw.ElapsedMilliseconds;
          return res;
        })
        //.Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetCallerAsMeetingAttendee(ctx.Parent<MeetingModel>().Id, cancellationToken))
        .UseProjection()
        ;

    }
  }
}
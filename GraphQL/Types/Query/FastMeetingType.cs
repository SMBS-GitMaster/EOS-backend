//using HotChocolate.Types;
//using HotChocolate.Types.Pagination;
//using RadialReview.GraphQL.Models;
//using RadialReview.Repositories;
//using System.Linq;
//using System;

//namespace RadialReview.GraphQL.Types {
//  public class FastMeetingType : ObjectType<FastMeetingModel> {
//    protected override void Configure(IObjectTypeDescriptor<FastMeetingModel> descriptor) {
//      base.Configure(descriptor);

//      descriptor
//          .Field("type")
//          .Type<StringType>()
//          .Resolve(ctx => "meeting");

//      descriptor
//          .Field(t => t.Id)
//          .Type<LongType>()
//      .IsProjected(true);

//      descriptor
//          .Field(t => t.CurrentMeetingInstanceId)
//          .Type<LongType>()
//      .IsProjected(true);

//      descriptor
//          .Field("currentMeetingInstance")
//          .Type<MeetingInstanceType>()
//          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetInstanceForMeetingAsync(ctx.Parent<FastMeetingModel>().CurrentMeetingInstanceId, cancellationToken))
//      .UseProjection();

//      descriptor
//          .Field("goals")
//          .Type<ListType<GoalType>>()
//          .Resolve(ctx => ctx.Parent<FastMeetingModel>().Goals)
//          .UsePaging<GoalType>(options: new PagingOptions { IncludeTotalCount = true })
//      .UseProjection()
//      .UseFiltering()
//      .UseSorting();

//      // descriptor
//      //     .Field("ongoing")
//      //     .Type<ListType<OngoingMeetingType>>()
//      //     .Resolve(ctx => ctx.Parent<FastMeetingModel>().Ongoing)
//      //     .UsePaging<OngoingMeetingType>(options: new PagingOptions { IncludeTotalCount = true })
//      //     .UseProjection()
//      //     .UseFiltering()
//      //     .UseSorting()
//      //     ;


//      descriptor
//          .Field("attendees")
//          .Type<ListType<MeetingAttendeeType>>()
//          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<FastMeetingModel>().Id, cancellationToken))
//          .UsePaging<MeetingAttendeeType>(options: new PagingOptions { IncludeTotalCount = true })

//      .UseProjection()
//      .UseFiltering()
//      .UseSorting();

//      descriptor
//          .Field("meetingPages")
//          .Type<ListType<MeetingPageType>>()
//          .Resolve(ctx => ctx.Parent<FastMeetingModel>().MeetingPages)
//          .UsePaging<MeetingPageType>(options: new PagingOptions { IncludeTotalCount = true })
//      .UseProjection()
//      .UseFiltering()
//      .UseSorting();

//      descriptor
//          .Field("attendeeInstances")
//          // .Field(t => t.AttendeeInstances)
//          .Type<ListType<MeetingRatingType>>()
//          /*
//          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingRatingModel>(async (keys, cancellationToken) => {
//            var results = await ctx.Service<IDataContext>().GetMeetingAttendeesForMeetingsAsync(keys, cancellationToken);
//            return results.ToLookup(x => x.MeetingId);
//          }, "meeting_atttendeeInstances").LoadAsync(ctx.Parent<FastMeetingModel>().Id))
//          */
//          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeeInstancesForMeetingsAsync(new[] { ctx.Parent<FastMeetingModel>().Id }, cancellationToken))
//          .UsePaging<MeetingRatingType>(options: new PagingOptions { IncludeTotalCount = true })
//     .UseProjection()
//     .UseFiltering()
//     .UseSorting();

//      descriptor
//          .Field("notes")
//          .Type<ListType<MeetingNoteType>>()
//          .Resolve(ctx => ctx.Parent<FastMeetingModel>().Notes)
//          .UsePaging<MeetingNoteType>(options: new PagingOptions { IncludeTotalCount = true })
//      .UseProjection()
//      .UseFiltering()
//      .UseSorting();

//      descriptor
//          .Field("todos")
//          .Type<ListType<TodoType>>()
//          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetTodosForMeetingsAsync(new[] { ctx.Parent<FastMeetingModel>().Id }, cancellationToken))
//          .UsePaging<TodoType>(options: new PagingOptions { IncludeTotalCount = true })
//     .UseProjection()
//     .UseFiltering()
//     .UseSorting();

//      descriptor
//          .Field("issues")
//          .Type<ListType<IssueType>>()
//          .Resolve(ctx => ctx.Parent<FastMeetingModel>().Issues)
//          .UsePaging<IssueType>(options: new PagingOptions { IncludeTotalCount = true })
//      .UseProjection()
//      .UseFiltering()
//      .UseSorting();


//      descriptor
//        .Field("headlines")
//        .Type<ListType<HeadlineType>>()
//        .Resolve(ctx => ctx.Parent<FastMeetingModel>().Headlines)
//        .UsePaging<HeadlineType>(options: new PagingOptions { IncludeTotalCount = true })
//      .UseProjection()
//      .UseFiltering()
//      .UseSorting();

//      descriptor
//          .Field("meetingInstances")
//          .Type<ListType<MeetingInstanceType>>()
//          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingInstanceModel>(async (keys, cancellationToken) => {
//            var result = await ctx.Service<IDataContext>().GetInstancesForMeetingsAsync(keys, cancellationToken);
//            return result.ToLookup(x => x.RecurrenceId);
//          }, "meeting_meetingInstances").LoadAsync(ctx.Parent<FastMeetingModel>().Id))
//          .UsePaging<MeetingInstanceType>(options: new PagingOptions { IncludeTotalCount = true })
//      .UseProjection()
//      .UseFiltering()
//      .UseSorting();

//      descriptor
//          .Field("metrics")
//          .Type<ListType<MetricType>>()
//          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMetricsForMeetingAsync(ctx.Parent<FastMeetingModel>().Id, cancellationToken))
//          .UsePaging<MetricType>(options: new PagingOptions { IncludeTotalCount = true })
//          .UseProjection()
//          .UseFiltering()
//          .UseSorting();

//      descriptor
//          .Field("issuesSentTo")
//          .Type<ListType<IssueSentToType>>()
//          // .Resolve(ctx =>
//          //     ctx.GroupDataLoader<long, IssueSentToModel>(async (keys, cancellationToken) => {
//          //         var result = await ctx.Service<IDataContext>().GetIssuesSentToForMeetingsAsync(keys, cancellationToken);
//          //         return result.ToLookup(x => x.MeetingId);
//          //     }).LoadAsync(ctx.Parent<FastMeetingModel>().Id))
//          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetIssuesSentToForMeetingAsync(ctx.Parent<FastMeetingModel>().Id, cancellationToken))
//          .UsePaging<IssueSentToType>(options: new PagingOptions { IncludeTotalCount = true })
//          .UseProjection()
//          .UseFiltering()
//          .UseSorting();
//    }
//  }
//}
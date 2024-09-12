namespace RadialReview.GraphQL.Types {
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.GraphQL.Models;
  using RadialReview.GraphQL.Types;
  using RadialReview.Repositories;
  using System;
  using System.Linq;

  public class MeetingInstanceChangeType : MeetingInstanceQueryType
  {
    public MeetingInstanceChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingInstanceQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("MeetingInstanceModelChange");
    }
  } 

  public class MeetingInstanceQueryType : ObjectType<MeetingInstanceQueryModel> {
    protected readonly bool isSubscription;

    public MeetingInstanceQueryType()
      : this(false)
    {
    }

    protected MeetingInstanceQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingInstanceQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "meetingInstance")
        ;

      descriptor
        .Field(t => t.Id)
        .Type<LongType>();

      descriptor
        .Field(t => t.TimerByPage)
        .Type<TimerByPageType>();

      descriptor
        .Field("issuesSolvedCount")
        .Type<IntType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetSolvedIssueCountAsync(ctx.Parent<MeetingInstanceQueryModel>().Id, ctx.Parent<MeetingInstanceQueryModel>().RecurrenceId, cancellationToken));

      descriptor
        .Field(t => t.RecurrenceId)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
        .Field(t => t.LeaderId)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
        .Field("currentPageId")
        .Type<StringType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingCurrentPageAsync(ctx.Parent<MeetingInstanceQueryModel>().Id, cancellationToken));

      if (isSubscription)
      {
        descriptor
          .Field("attendees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingInstanceQueryModel>().RecurrenceId, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("notes")
          .Type<ListType<MeetingNoteChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetNotesForMeetingsAsync(new[] { ctx.Parent<MeetingInstanceQueryModel>().RecurrenceId }, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("attendeeInstances")
          .Type<ListType<MeetingInstanceAttendeeChangeType>>()
          .Resolve(async (ctx, ct) => 
             await ctx.Service<IDataContext>().GetMeetingInstanceAttendees(ctx.Parent<MeetingInstanceQueryModel>().RecurrenceId, ctx.Parent<MeetingInstanceQueryModel>().Id, ct)
          )
          .UseProjection()
          .UseFiltering()
          .UseSorting();
      }
      else
      {
        descriptor
          .Field("attendees")
          .Type<ListType<MeetingAttendeeQueryType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetMeetingAttendeesAsync(ctx.Parent<MeetingInstanceQueryModel>().RecurrenceId, cancellationToken))
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("notes")
          .Type<ListType<MeetingNoteQueryType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetNotesForMeetingsAsync(new[] { ctx.Parent<MeetingInstanceQueryModel>().RecurrenceId }, cancellationToken))
          .UsePaging<MeetingNoteQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("attendeeInstances")
          .Type<ListType<MeetingInstanceAttendeeQueryType>>()
          .Resolve(async (ctx, ct) =>
            await ctx.Service<IDataContext>().GetMeetingInstanceAttendees(ctx.Parent<MeetingInstanceQueryModel>().RecurrenceId, ctx.Parent<MeetingInstanceQueryModel>().Id, ct)
          )
          .UsePaging<MeetingInstanceAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
      }

      descriptor
          .Field("selectedNotes")
          .Type<ListType<LongType>>()
          .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetL10MeetingNotes(ctx.Parent<MeetingInstanceQueryModel>().Id, cancellationToken));
    }
  }
}
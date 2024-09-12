using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.MeetingListLookup;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RadialReview.GraphQL.Types
{
  public class MeetingListLookupChangeType : MeetingListLookupType
  {
    public MeetingListLookupChangeType() : base(true)
    {
    }
    protected override void Configure(IObjectTypeDescriptor<MeetingListLookupModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("MeetingListLookupModelChange");
    }
  }
  public class MeetingListLookupType : ObjectType<MeetingListLookupModel>
  {
    protected readonly bool isSubscription;
    public MeetingListLookupType() : this(false)
    {
    }
    protected MeetingListLookupType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }
    protected override void Configure(IObjectTypeDescriptor<MeetingListLookupModel> descriptor)
    {
      base.Configure(descriptor);
      descriptor
          .Field("type")
          .Type<StringType>()
          .Resolve(ctx => "meetingListLookup");

      if (isSubscription)
      {
        descriptor
          .Field("attendees")
          .Type<ListType<MeetingAttendeeChangeType>>()
          .Resolve(context =>
          {
            return context.GroupDataLoader<long, MeetingAttendeeQueryModel>(async (meetingIds, ct) =>
            {
              var result = await context.Service<IDataContext>().GetAttendeesByMeetingIdsAsync(meetingIds.ToList(), ct);
              return result.ToLookup(attendee => attendee.MeetingId);
            }).LoadAsync(context.Parent<MeetingListLookupModel>().Id);
          })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
      }
      else
      {
        descriptor
          .Field(t => t.AttendeesLookup).IsProjected(true)
          .Name("y")
          .Resolve(ctx => "y");

        descriptor
          .Field("attendeesLookup")
          .Type<ListType<MeetingAttendeeLookupQueryType>>()
          .Resolve(ctx => ctx.Parent<MeetingListLookupModel>().AttendeesLookup)
          .UsePaging<MeetingAttendeeLookupQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
        .Field("attendees")
        .Type<ListType<MeetingAttendeeQueryType>>()
        .Resolve(context =>
        {
          return context.GroupDataLoader<long, MeetingAttendeeQueryModel>(async (meetingIds, ct) =>
          {
            var result = await context.Service<IDataContext>().GetAttendeesByMeetingIdsAsync(meetingIds.ToList(), ct);
            return result.ToLookup(attendee => attendee.MeetingId);
          }).LoadAsync(context.Parent<MeetingListLookupModel>().Id);
        })
        .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
        .UseProjection()
        .UseFiltering()
        .UseSorting();

        descriptor
        .Field("currentMeetingInstance")
        .Type<MeetingInstanceQueryType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IDataContext>().GetInstanceForMeetingByRecurrenceIdAsync(ctx.Parent<MeetingListLookupModel>().Id, cancellationToken))
        .UseProjection()
        .UseFiltering()
        .UseSorting();
      }
    }
  }
}
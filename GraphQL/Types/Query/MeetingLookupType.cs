using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using RadialReview.Accessors;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Types
{
  public class MeetingLookupType : ObjectType<MeetingLookupModel>
  {
    protected readonly bool isSubscription;

    public MeetingLookupType() : this(false)
    {
    }

    protected MeetingLookupType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingLookupModel> descriptor)
    {
      base.Configure(descriptor);

      if (isSubscription)
      {
        descriptor
          .Field(f => f.Attendees)
          .Type<ListType<MeetingAttendeeChangeType>>()
          .UseFiltering()
          .UseSorting()
          .Resolve(context =>
          {
            return context.GroupDataLoader<long, MeetingAttendeeQueryModel>(async (meetingIds, ct) =>
            {
              var result = await context.Service<IDataContext>().GetAttendeesByMeetingIdsAsync(meetingIds.ToList(), ct);
              return result.ToLookup(attendee => attendee.MeetingId);
            }).LoadAsync(context.Parent<MeetingLookupModel>().Id);
          });

        descriptor
          .Field(f => f.MeetingPages)
          .Type<ListType<MeetingPageChangeType>>()
          .UseFiltering()
          .UseSorting()
          .Resolve(async context =>
          {
            var meetingPages = await context.GroupDataLoader<long, MeetingPageQueryModel>(async (meetingIds, ct) =>
            {
              var result = await context.Service<IDataContext>().GetPagesByMeetingIdsAsync(meetingIds.ToList(), ct);
              return result.ToLookup(page => page.MeetingId);
            }).LoadAsync(context.Parent<MeetingLookupModel>().Id);

            return meetingPages.AsQueryable();
          });
      }
      else
      {
        descriptor
          .Field(f => f.Attendees)
          .Type<ListType<MeetingAttendeeQueryType>>()
          .UsePaging<MeetingAttendeeQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseFiltering()
          .UseSorting()
          .Resolve(context =>
          {
            return context.GroupDataLoader<long, MeetingAttendeeQueryModel>(async (meetingIds, ct) =>
            {
              var result = await context.Service<IDataContext>().GetAttendeesByMeetingIdsAsync(meetingIds.ToList(), ct);
              return result.ToLookup(attendee => attendee.MeetingId);
            }).LoadAsync(context.Parent<MeetingLookupModel>().Id);
          });

        descriptor
          .Field(f => f.MeetingPages)
          .Type<ListType<MeetingPageQueryType>>()
          .UsePaging<MeetingPageQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseFiltering()
          .UseSorting()
          .Resolve(async context =>
          {
            var meetingPages = await context.GroupDataLoader<long, MeetingPageQueryModel>(async (meetingIds, ct) =>
            {
              var result = await context.Service<IDataContext>().GetPagesByMeetingIdsAsync(meetingIds.ToList(), ct);
              return result.ToLookup(page => page.MeetingId);
            }).LoadAsync(context.Parent<MeetingLookupModel>().Id);

            return meetingPages.AsQueryable();
          });
      }

    }
  }

  public class MetricMeetingLookupType : MeetingLookupType
  {
    public MetricMeetingLookupType(): base(false)
    {
    }

    protected MetricMeetingLookupType(bool isSubscription) : base(isSubscription)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingLookupModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("metricMeetingLookup");

      descriptor
      .Field("type")
      .Type<StringType>()
      .Resolve(ctx => "metricMeetingLookup");
    }
  }

  public class MetricMeetingLookupChangeType : MetricMeetingLookupType
  {
    public MetricMeetingLookupChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<MeetingLookupModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("metricMeetingLookupModelChange");
    }
  }
}

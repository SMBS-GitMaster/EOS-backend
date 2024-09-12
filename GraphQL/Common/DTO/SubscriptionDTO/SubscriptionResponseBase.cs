using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Common.DTO.Subscription
{
  public class SubscriptionResponseBase
  {
    public SubscriptionResponseBase() { }

    public SubscriptionResponseBase(EventType @event)
    {
      Event = @event;
    }

    public static SubscriptionResponseBase Added() => new SubscriptionResponseBase(@event: EventType.Added);
    public static SubscriptionResponseBase Updated() => new SubscriptionResponseBase(@event: EventType.Updated);
    public static SubscriptionResponseBase Archived() => new SubscriptionResponseBase(@event: EventType.Archived);
    public static SubscriptionResponseBase UnArchived() => new SubscriptionResponseBase(@event: EventType.UnArchived);


    public enum EventType
    {
      Added,
      Updated,
      Archived,
      UnArchived
    }

    public EventType Event { get; set; }
  }
}

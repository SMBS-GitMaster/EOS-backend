namespace RadialReview.Core.GraphQL.Common.DTO.Subscription
{
  public class SubscriptionResponse<T> : SubscriptionResponseBase
  {
    public SubscriptionResponse() { }

    private SubscriptionResponse(T data, EventType @event) : base (@event)
    {
      Data = data;
    }
    
    public static SubscriptionResponse<T> Added(T data) => new SubscriptionResponse<T>(data: data, @event: EventType.Added);
    public static SubscriptionResponse<T> Updated(T data) => new SubscriptionResponse<T>(data: data, @event: EventType.Updated);
    public static SubscriptionResponse<T> Archived(T data) => new SubscriptionResponse<T>(data: data, @event: EventType.Archived);
    public static SubscriptionResponse<T> UnArchived(T data) => new SubscriptionResponse<T>(data: data, @event: EventType.UnArchived);

    public T Data { get; set; }
  }
}

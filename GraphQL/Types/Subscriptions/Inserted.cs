namespace RadialReview.Core.GraphQL.Types
{
  using Microsoft.Extensions.Logging;

  public partial class Inserted<TBearer, TProperty, T, TKey> : Change<TBearer>
    where T : class
  {
    public override ChangeAction Action => ChangeAction.Inserted;

    public Target<TProperty, TKey> Target { get; set; }

    public TKey Id { get; set; }
    public T Value { get; set; }

    public override void Log(ILogger<IChange<TBearer>> logger)
    {
      if (!logger.IsEnabled(LogLevel.Information))
      {
          return;
      }

      LoggerMessageState state = LoggerMessageHelper.ThreadLocalState;
      var change = this;

      _ = state.ReserveTagSpace(2);
      state.TagNamePrefix = nameof(change);
      state.TagArray[0] = new(nameof(change.Id), change.Id);
      state.TagArray[1] = new(nameof(change.Target), change.Target);

      var collector = state as ITagCollector;

      var value = change.Value as ILogProperties;
      if (value == null)
      {
        collector.Add(nameof(change.Value), change?.Value);
      }
      else
      {
        value.Log(collector, nameof(change.Value));
      }

      logger.Log(
        LogLevel.Information,
        new(0, nameof(Inserted<TBearer, TProperty, T, TKey>)),
        state,
        null,
        static (s, _) =>
        {
          return "Sending notification for GraphQL subscription";
        }
      );

      state.Clear();
    }
  }
}

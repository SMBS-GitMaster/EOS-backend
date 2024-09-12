namespace RadialReview.Core.GraphQL.Types
{
  using Microsoft.Extensions.Logging;

  public partial class Emptied<TBearer, TProperty, T, TKey> : Change<TBearer>
    where T : class
  {
    public override ChangeAction Action => ChangeAction.Emptied;

    public Target<TProperty, long> Target { get; set; }

    public override void Log(ILogger<IChange<TBearer>> logger)
    {
      if (!logger.IsEnabled(LogLevel.Information))
      {
          return;
      }

      LoggerMessageState state = LoggerMessageHelper.ThreadLocalState;
      var change = this;

      _ = state.ReserveTagSpace(1);
      state.TagNamePrefix = nameof(change);
      state.TagArray[0] = new(nameof(change.Target), change.Target);

      var collector = state as ITagCollector;

      logger.Log(
        LogLevel.Information,
        new(0, nameof(Emptied<TBearer, TProperty, T, TKey>)),
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

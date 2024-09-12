namespace RadialReview.Core.GraphQL.Types
{
  using Microsoft.Extensions.Logging;

  public partial class Deleted<TBearer, T, TKey> : Change<TBearer>
    where T : class
  {
    public override ChangeAction Action => ChangeAction.Deleted;

    public TKey Id { get; set; }

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

      logger.Log(
        LogLevel.Information,
        new(0, nameof(Deleted<TBearer, T, TKey>)),
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

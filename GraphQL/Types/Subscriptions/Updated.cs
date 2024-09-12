using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace RadialReview.Core.GraphQL.Types
{
  public partial class Updated<TBearer, T, TKey> : Change<TBearer>
    where T : class
  {
    public override ChangeAction Action => ChangeAction.Updated;

    public Updated(TKey id, T value, ContainerTarget[]? targets)
    {
      Id = id;
      Value = value;
      ContainerTargets = targets;
    }

    public TKey Id { get; init; }
    public T Value { get; init; }
    public ContainerTarget[]? ContainerTargets { get; init; }

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
      state.TagArray[1] = new(nameof(change.Action), change.Action);

      var collector = state as ITagCollector;

      if(change.ContainerTargets == null)
      {
        collector.Add(nameof(change.ContainerTargets), change.ContainerTargets);
      }
      else
      {
        for (int i = 0; i < change.ContainerTargets.Length; i++)
        {
          var containerTarget = change.ContainerTargets[i];
          collector.Add($"{nameof(containerTarget)}[{i}].{nameof(containerTarget.Id)}", containerTarget?.Id);
          collector.Add($"{nameof(containerTarget)}[{i}].{nameof(containerTarget.Type)}", containerTarget?.Type);
          collector.Add($"{nameof(containerTarget)}[{i}].{nameof(containerTarget.Property)}", containerTarget?.Property);
        }
      }

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
          new(0, nameof(Updated<TBearer, T, TKey>)),
          state,
          null,
          static (s, _) =>
          {
              return "Sending notification for GraphQL subscription";
          });

      state.Clear();
    }
  }

  // public class Updated2<TBearer, T> : Change<TBearer>
  //   where T : class
  // {
  //   public override ChangeAction Action => ChangeAction.Updated;

  //   public Updated2(Guid id, T value, ContainerTarget[]? targets)
  //   {
  //     Id = id;
  //     Value = value;
  //     ContainerTargets = targets;
  //   }

  //   public Guid Id { get; init; }
  //   public T Value { get; init; }
  //   public ContainerTarget[]? ContainerTargets { get; init; }

  //   public override void Log(ILogger<IChange<TBearer>> logger)
  //   {
  //   }
  // }
}

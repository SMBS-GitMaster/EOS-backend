using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Subscriptions;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using RadialReview.Utilities;
using RadialReview.Models;
using HotChocolate;
using RadialReview.Identity;
using System.Security.Permissions;

namespace RadialReview.Core.GraphQL.Types
{
  public static class StreamExtensions
  {
    static ILoggerFactory factory = LoggerFactory.Create(builder =>
      builder.AddOpenTelemetry(logging => {
        logging.AddOtlpExporter();
        logging.AddConsoleExporter();
      })
    );

    static ILogger<IChange<IMeetingChange>> logger = factory.CreateLogger<IChange<IMeetingChange>>();

    public static string ToJson<T>(this T item)
    {
      var json = JsonConvert.SerializeObject(item, new JsonSerializerSettings
      {
        TypeNameHandling = TypeNameHandling.All
      });

      return json;
    }

    public static T FromJson<T>(this string json)
    {
      var item = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
      {
        TypeNameHandling = TypeNameHandling.All
      });

      return item;
    }

    public static ISourceStream<IChange<IMeetingChange>> ToRedactedStream(this ISourceStream<string> stream, IResolverContext ctx, Func<IChange<IMeetingChange>, bool> securitycheck = null)
    {

      var modified_stream =
          stream
            .Select(x => x.FromJson<IChange<IMeetingChange>>())
            .GetFilteredStream(ctx)
            .WhereIsExplicitlyRequestedNotification(ctx)
            .Redacted(securitycheck);

      return modified_stream;
    }

    public static async Task SendChangeAsync(this ITopicEventSender _eventSender, string topic, IChange<IMeetingChange> change)
    {
      await _eventSender.SendAsync<string>(topic, change.ToJson());
      change.Log(logger);
    }

    public static ISourceStream<TResult> Select<T, TResult>(this ISourceStream<T> stream, Func<T, TResult> project)
    {
      return new FilteredStream<T, TResult>(stream, () => stream.ReadEventsAsync().Select(project));
    }

    public static ISourceStream<T> Where<T>(this ISourceStream<T> stream, System.Func<T, bool> predicate)
    {
      return new FilteredStream<T, T>(stream, () => stream.ReadEventsAsync().Where(predicate));
    }

    public static ISourceStream<T> Redacted<T>(this ISourceStream<T> stream, Func<T, bool> securitycheck)
    {
      return stream.Where(securitycheck ?? (x => true));
    }

    public static ISourceStream<IChange<T>> GetFilteredStream<T>(this ISourceStream<IChange<T>> stream, IResolverContext ctx)
    {
      var filter = ctx.ArgumentLiteral<HotChocolate.Language.IValueNode>("where");
      if (filter == null || filter.Value == null)
      {
        return stream;
      }
      else
      {
        var predicate = filter.ToPredicate<IChange<T>>();
        return stream.Where(predicate);
      }
    }

    public static ISourceStream<IChange<T>> WhereIsExplicitlyRequestedNotification<T>(this ISourceStream<IChange<T>> stream, IResolverContext ctx)
    {
      var notifications = ((HotChocolate.Language.FieldNode)ctx.Operation.Definition.SelectionSet.Selections.First()).SelectionSet.Selections.OfType<HotChocolate.Language.InlineFragmentNode>().Select(x => x.TypeCondition.Name.Value);
      var hashSet = new HashSet<string>(notifications);

      return stream.Where(msg => msg.IsExplicitlyRequestedNotification(hashSet));

    }

    public static bool IsExplicitlyRequestedNotification<T>(this IChange<T> msg, HashSet<string> hashSet)
    {
      var type = msg.GetType().Name;

      if (type == typeof(Created<,,>).Name)
      {
        var modelType = msg.GetType().GetGenericArguments()[1].Name.Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var result = hashSet.Contains($"Created_{modelType}");
        return result;
      }
      else if (type == typeof(Updated<,,>).Name)
      {
        var modelType = msg.GetType().GetGenericArguments()[1].Name.Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var result = hashSet.Contains($"Updated_{modelType}");
        return result;
      }
      else if (type == typeof(Deleted<,,>).Name)
      {
        var modelType = msg.GetType().GetGenericArguments()[1].Name.Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var result = hashSet.Contains($"Deleted_{modelType}");
        return result;
      }
      else if (type == typeof(Inserted<,,,>).Name)
      {
        var modelType = msg.GetType().GetGenericArguments()[1].FullName.Split(".")[3].Split("+")[0].Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var propertyType = msg.GetType().GetGenericArguments()[2].Name.Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var result = hashSet.Contains($"Inserted_{modelType}_{propertyType}");
        return result;
      }
      else if (type == typeof(Removed<,,,>).Name)
      {
        var modelType = msg.GetType().GetGenericArguments()[1].FullName.Split(".")[3].Split("+")[0].Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var propertyType = msg.GetType().GetGenericArguments()[2].Name.Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var result = hashSet.Contains($"Removed_{modelType}_{propertyType}");
        return result;
      }
      else if (type == typeof(Emptied<,,,>).Name)
      {
        var modelType = msg.GetType().GetGenericArguments()[1].FullName.Split(".")[3].Split("+")[0].Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var propertyType = msg.GetType().GetGenericArguments()[2].Name.Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var result = hashSet.Contains($"Emptied_{modelType}_{propertyType}");
        return result;
      }
      else if (type == typeof(UpdatedAssociation<,,,>).Name)
      {
        var modelType = msg.GetType().GetGenericArguments()[1].FullName.Split(".")[3].Split("+")[0].Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var propertyType = msg.GetType().GetGenericArguments()[2].Name.Replace("QueryModel", "").Replace("Model", "").Replace("DTO", "");
        var result = hashSet.Contains($"UpdatedAssociation_{modelType}_{propertyType}");
        return result;
      }
      else
      {
        return false;
      }
    }
  }
}

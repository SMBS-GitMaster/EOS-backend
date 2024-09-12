namespace RadialReview.Core.GraphQL.Types
{
  using System.Collections.Generic;
  using System.Linq;
  using Microsoft.Extensions.Logging;

  public interface IChange<TBearer>
  {
    void Log(ILogger<IChange<TBearer>> logger);
  }

  public interface ILogProperties
  {
    void Log(ITagCollector collector, string prefix);
  }

  public static class LoggingHelper
  {
    public static void Log<T>(this IEnumerable<T> collection, ITagCollector collector, string prefix)
    {
      if (collection == null)
      {
        collector.Add(prefix, null);
      }
      else if (collection.Any() == false)
      {
        collector.Add(prefix, "[]");
      }
      else
      {
        if (typeof(ILogProperties).IsAssignableFrom(typeof(T)))
        {
          foreach (var (item, index) in collection.Cast<ILogProperties>().Select((x, i) => (x, i)))
          {
            item.Log(collector, $"{prefix}.[{index}]");
          }
        }
        else
        {
          collector.Add(prefix, collection);
        }
      }
    }
  }
}

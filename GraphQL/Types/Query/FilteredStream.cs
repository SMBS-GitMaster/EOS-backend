using HotChocolate.Execution;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RadialReview.Core.GraphQL.Types
{
  internal class FilteredStream<T, TResult> : ISourceStream<TResult>
  {
    private readonly ISourceStream<T>? stream;
    private readonly Func<IAsyncEnumerable<TResult>> action;

    public FilteredStream(ISourceStream<T>? stream, Func<IAsyncEnumerable<TResult>> action)
    {
      this.stream = stream;
      this.action = action;
    }

    public ValueTask DisposeAsync()
    {
      return stream == null ? ValueTask.CompletedTask : stream.DisposeAsync();
    }

    public IAsyncEnumerable<TResult> ReadEventsAsync()
    {
      return action();
    }

    IAsyncEnumerable<object> ISourceStream.ReadEventsAsync()
    {
      return (IAsyncEnumerable<object>) ReadEventsAsync();
    }
  }
}

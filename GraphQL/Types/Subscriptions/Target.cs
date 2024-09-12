using System;

namespace RadialReview.Core.GraphQL.Types
{
  public class Target<TProperty, TKey>
  {
    public TKey Id { get; set; }
    public TProperty Property { get; set; }
  }
}

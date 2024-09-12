using System.Diagnostics;
using System.Text;
using HotChocolate.Diagnostics;
using Microsoft.Extensions.ObjectPool;

namespace RadialReview.Core.GraphQL.Obserability;

public sealed class CustomActivityEnricher : ActivityEnricher
{
  public CustomActivityEnricher(ObjectPool<StringBuilder> stringBuilderPool, InstrumentationOptions options)
    : base(stringBuilderPool, options)
  {

  }

  protected override string CreateRootActivityName(Activity activity, Activity root, string displayName)
  {
    return displayName;
  }
}
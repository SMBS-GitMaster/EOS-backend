using HotChocolate.Data.Filters;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using System.Linq.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Data;
using HotChocolate.Utilities;

namespace RadialReview.Core.GraphQL.Types
{
  internal class ExecutorBuilder<T>
  {
    private readonly IFilterInputType _inputType;

    public ExecutorBuilder()
    {
      var type = new FilterInputType<T>();

      /* NOTE: type.EntityType is null at this point. 
       * SetEntityType will update its value! 
       * This value is needed for the Visit call in the Build method to succeed. */
      SetEntityType(type);

      _inputType = type;
    }

    /*
     * NOTE: type.EntityType is updated by side effect upon building a schema!
     */
    protected static void SetEntityType(FilterInputType<T>  type)
    {
      var convention = new FilterConvention(x => x.AddDefaults().BindRuntimeType(typeof(T), type.GetType()));

      var builder =
          SchemaBuilder
            .New()
            .AddConvention<IFilterConvention>(convention)
            .TryAddTypeInterceptor<FilterTypeInterceptor>()
            .AddQueryType(c => c.Name("Query").Field("foo").Type<StringType>().Resolve("bar"))
            .AddType(type);

      builder.Create();
    }

    public Func<T, bool> Build(IValueNode filter)
    {
        var visitorContext = new QueryableFilterContext(_inputType, true);
        var visitor = new FilterVisitor<QueryableFilterContext, Expression>(new QueryableCombinator());

        visitor.Visit(filter, visitorContext);

        if (visitorContext.TryCreateLambda(out Expression<Func<T, bool>>? where))
        {
          return where.Compile();
        }

      throw new InvalidOperationException();
    }
  }

  public static class ExecutorBuilderExtensions
  {
    public static Func<T, bool> ToPredicate<T>(this IValueNode filter) 
    {
        var executorBuilder = new ExecutorBuilder<T>();
        var predicate = executorBuilder.Build(filter);
        
        return predicate;
    }
  }
}

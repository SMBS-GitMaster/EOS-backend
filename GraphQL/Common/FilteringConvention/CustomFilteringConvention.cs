using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Data.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotChocolate.Language;
using HotChocolate.Types;
using System.Reflection;
using HotChocolate.Data;
using System.Linq.Expressions;

namespace RadialReview.Core.GraphQL.Common.FilteringConvention
{
  public class CustomFilteringConvention : FilterConvention
  {
    protected override void Configure(IFilterConventionDescriptor descriptor)
    {
      descriptor.AddDefaults();
      descriptor.AddProviderExtension(
          new QueryableFilterProviderExtension(
              x => x.AddFieldHandler<QueryableStringInvariantContainsHandler>()));
    }
  }

  public class QueryableStringInvariantContainsHandler : QueryableStringOperationHandler
  {
    private static readonly MethodInfo _toLower = typeof(string)
        .GetMethods()
        .Single(
            x => x.Name == nameof(string.ToLower) &&
            x.GetParameters().Length == 0);

    public QueryableStringInvariantContainsHandler(InputParser inputParser) : base(inputParser)
    {
    }

    protected override int Operation => DefaultFilterOperations.Contains;

    public override Expression HandleOperation(
        QueryableFilterContext context,
        IFilterOperationField field,
        IValueNode value,
        object parsedValue)
    {
      Expression property = context.GetInstance();
      if (parsedValue is string str && !string.IsNullOrEmpty(str))
      {
        // Create an Expression that calls ToLower on the property and the input value
        Expression toLowerProperty = Expression.Call(property, _toLower);
        Expression toLowerValue = Expression.Constant(str.ToLower());

        // Check for null values
        Expression nullCheck = Expression.AndAlso(
            Expression.NotEqual(property, Expression.Constant(null)),
            Expression.NotEqual(toLowerValue, Expression.Constant(null)));


        // Use the String.Contains method for case-insensitive 'contains'
        MethodInfo containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var containsExpression = Expression.Condition(
            nullCheck,
            Expression.Call(toLowerProperty, containsMethod, toLowerValue),
            Expression.Constant(false));

        return containsExpression;
      }

      throw new InvalidOperationException();
    }
  }
}
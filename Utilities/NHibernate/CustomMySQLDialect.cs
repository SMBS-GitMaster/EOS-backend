using NHibernate.Dialect.Function;
using NHibernate.Dialect;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate.Linq.Functions;
using NHibernate.Hql.Ast;
using NHibernate.Linq.Visitors;
using System.Collections.ObjectModel;
using System.Reflection;
using FluentNHibernate.Utils.Reflection;
using System.Linq.Expressions;

namespace RadialReview.Core.Utilities.NHibernate
{
  public class CustomMySQLDialect : MySQL5Dialect
  {
    public CustomMySQLDialect()
    {
      RegisterFunction(
          "date_add",
          new SQLFunctionTemplate(
              NHibernateUtil.DateTime,
              "DATE_ADD(?1, INTERVAL ?2 DAY)"
          )
      );
    }
  }

  public class DateAddGenerator : BaseHqlGeneratorForMethod
  {
    public DateAddGenerator()
    {
      var dateAddMethod = typeof(DateTime).GetMethod("AddDays", new[] { typeof(double) });
      SupportedMethods = new[] { dateAddMethod };
    }

    public override HqlTreeNode BuildHql(MethodInfo method,
        Expression targetObject,
        ReadOnlyCollection<Expression> arguments,
        HqlTreeBuilder treeBuilder,
        IHqlExpressionVisitor visitor)
    {
      return treeBuilder.MethodCall("date_add",
            visitor.Visit(targetObject).AsExpression(),
            visitor.Visit(arguments[0]).AsExpression());
    }
  }

  public class CustomLinqToHqlGeneratorsRegistry : DefaultLinqToHqlGeneratorsRegistry
  {
    public CustomLinqToHqlGeneratorsRegistry() : base()
    {
      this.Merge(new DateAddGenerator());
    }
  }

}

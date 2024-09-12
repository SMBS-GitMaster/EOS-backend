using NHibernate;
using NHibernate.Criterion;
using System;
using System.Linq.Expressions;

namespace RadialReview.Utilities.NHibernate {
	public class CriterionUtility {

		public static SimpleExpression Mod<T>(Expression<Func<T, object>> property, int divisor, int remainder) {
			return Restrictions.Eq(Projections.SqlFunction("mod", NHibernateUtil.Int64, Projections.Property<T>(property), Projections.Constant(divisor)), remainder);
		}
	}
}
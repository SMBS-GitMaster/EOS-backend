using Microsoft.AspNetCore.Mvc;
using NHibernate.Criterion;
using RadialReview.Utilities.NHibernate;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RadialReview.Controllers.AbstractController {
	public abstract class BaseExpensiveController : BaseController {
    public BaseExpensiveController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
    }

		// GET: BaseExpensive
		public class Divisor {
			public int divisor { get; set; }

			public int remainder { get; set; }

			public double? duration { get; set; }

			public int? updates { get; set; }

			public TimeSpan GetDuration() {
				return TimeSpan.FromMilliseconds(duration ?? 0);
			}

			public double GetDurationSeconds() {
				return GetDuration().TotalSeconds;
			}

			public Divisor() {
				divisor = 1447;
				remainder = 0;
				duration = 0;
				updates = 0;
			}
		}

		protected SimpleExpression Mod<T>(Expression<Func<T, object>> property, int divisor, int remainder) {
			return CriterionUtility.Mod(property, divisor, remainder);
		}

		protected SimpleExpression Mod<T>(Expression<Func<T, object>> property, Divisor dd) {
			return this.Mod(property, dd.divisor, dd.remainder);
		}
	}
}

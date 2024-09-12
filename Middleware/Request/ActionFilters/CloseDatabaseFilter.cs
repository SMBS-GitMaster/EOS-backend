using RadialReview.Utilities;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class CloseDatabaseFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			try {
				await next();
			} finally {
				HibernateSession.CloseCurrentSession();
			}
		}
	}
}

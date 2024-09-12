using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class ModelStateMergeFilter : IAsyncActionFilter {
		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			await next();

			//Not really sure if this is used anywhere?
			//Seems like it can be used to copy errors between subsequent requests.
			if (context.Controller is Controller ctrl) {
				if (ctrl.TempData["ModelState"] != null && !ctrl.ModelState.Equals(ctrl.TempData["ModelState"])) {
					ctrl.ModelState.Merge((ModelStateDictionary)ctrl.TempData["ModelState"]);
				}
			}
		}
	}
}

using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System.Threading.Tasks;
using RadialReview.Middleware.Request.HttpContextExtensions.FormValidation;
using System.Linq;
using RadialReview.Exceptions;

namespace RadialReview.Middleware.Request.ActionFilters {
	public class ValidationCollectionFilter : IAsyncActionFilter {


		public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
			var toValidate = new List<string>();
			if (context.HttpContext.Request.HasFormContentType) {
				var validationCollection = context.HttpContext.Request.Form;
				foreach (var f in validationCollection.Keys) {
					if (f != null && f.EndsWith(SecuredValueFieldNameComputer.NameSuffix)) {
						toValidate.Add(f.Substring(0, f.Length - SecuredValueFieldNameComputer.NameSuffix.Length));
					}
				}
				context.HttpContext.InitializeFormValdiationData(toValidate, validationCollection);
			}
			await next();

			if (toValidate.Any()) {
				var err = "Didn't validate: " + string.Join(",", toValidate);
				throw new PermissionsException(err);
			}
		}
	}
}

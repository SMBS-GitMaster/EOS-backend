using System;
using RadialReview.Exceptions;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using RadialReview.Middleware.Request.HttpContextExtensions.FormValidation;

namespace RadialReview.Controllers {
	public partial class BaseController : Controller {
		private bool SkipValidation = false;
		protected void ValidateValues<T>(T model, params Expression<Func<T, object>>[] selectors) {
			if (SkipValidation) {
				return;
			}

			var toValidate = HttpContext.GetFormFieldsToValidate();
			var validationCollection = HttpContext.GetFormCollectionToValidate();

			foreach (var e in selectors) {
				var name = e.GetMvcName();
				if (!toValidate.Remove(name)) {
					throw new PermissionsException("Validation item does not exist.");
				}
				SecuredValueValidator.ValidateValue(validationCollection, name);
			}
		}
	}
}

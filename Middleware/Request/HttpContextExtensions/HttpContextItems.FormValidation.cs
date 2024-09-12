using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using static RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems;

namespace RadialReview.Middleware.Request.HttpContextExtensions.FormValidation {
	public static class HttpContextItemsFormValidation {

		public static HttpContextItemKey SHA_FORM_VALIDATION_COLLECTION = new HttpContextItemKey("ShaFormValidationCollection");
		public static HttpContextItemKey SHA_FORM_TO_VALIDATE = new HttpContextItemKey("ShaFormToValidate");

		public static void InitializeFormValdiationData(this HttpContext ctx, List<string> toValidate, IFormCollection validationCollection) {
			ctx.SetRequestItem(SHA_FORM_TO_VALIDATE, toValidate);
			ctx.SetRequestItem(SHA_FORM_VALIDATION_COLLECTION, validationCollection);
		}

		public static List<string> GetFormFieldsToValidate(this HttpContext ctx) {
			return ctx.GetRequestItem<List<string>>(SHA_FORM_TO_VALIDATE);
		}

		public static IFormCollection GetFormCollectionToValidate(this HttpContext ctx) {
			return ctx.GetRequestItem<IFormCollection>(SHA_FORM_VALIDATION_COLLECTION);
		}

	}
}

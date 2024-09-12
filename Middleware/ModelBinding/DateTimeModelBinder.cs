using log4net;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Request.ModelBinders {
	public class DateTimeModelBinderProvider : IModelBinderProvider {
		public IModelBinder GetBinder(ModelBinderProviderContext context) {
			if (context == null) {
				throw new ArgumentNullException(nameof(context));
			}

			if (context.Metadata.ModelType == typeof(DateTime)) {
				return new DateTimeModelBinder(false);
			}
			if (context.Metadata.ModelType == typeof(DateTime?)) {
				return new DateTimeModelBinder(true);
			}
			return null;
		}
	}

	public class DateTimeModelBinder : IModelBinder {
		public bool AllowNull { get; private set; }
		public DateTimeModelBinder(bool allowNull) {
			AllowNull = allowNull;

		}

		private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


		/// https://stackoverflow.com/questions/528545/mvc-datetime-binding-with-incorrect-date-format
		public async Task BindModelAsync(ModelBindingContext bindingContext) {
			var vpr = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
			var date = vpr.FirstValue;
			if (String.IsNullOrEmpty(date)) {
				bindingContext.Result = AllowNull ? ModelBindingResult.Success(null) : ModelBindingResult.Failed();
				return;
			}

			// Set the ModelState to the first attempted value before we have converted the date. This is to ensure that the ModelState has
			// a value. When we have converted it, we will override it with a full universal date.
			bindingContext.ModelState.SetModelValue(bindingContext.ModelName, bindingContext.ValueProvider.GetValue(bindingContext.ModelName));
			try {
				var realDate = DateTime.Parse(date, System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en-us"));
				// Now set the ModelState value to a full value so that it can always be parsed using InvarianCulture, which is the
				// default for QueryStringValueProvider.
				var sv = new StringValues(new[] { date, realDate.ToString("yyyy-MM-dd hh:mm:ss") });
				bindingContext.ModelState.SetModelValue(bindingContext.ModelName, new ValueProviderResult(sv, System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en-us")));
				bindingContext.Result = ModelBindingResult.Success(DateTime.SpecifyKind(realDate, DateTimeKind.Utc));
				return;
			} catch (Exception) {
				logger.ErrorFormat("Error parsing bound date '{0}' as US format.", date);
				bindingContext.ModelState.AddModelError(bindingContext.ModelName, String.Format("\"{0}\" is invalid.", bindingContext.ModelName));
				bindingContext.Result = ModelBindingResult.Failed();
				return;
			}
		}
	}
}
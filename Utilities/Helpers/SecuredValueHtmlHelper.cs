using System.Linq.Expressions;
using RadialReview.Exceptions;
using RadialReview.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using RadialReview.Html;

namespace RadialReview {
	//From http://blog.slatner.com/2010/01/20/SecuringFormValuesInASPNETMVC.aspx
	public interface IHashComputer {
		string GetBase64HashString(string value, string secret);
	}

	public static class SecuredValueHtmlHelper {
		public static IHtmlContent SecuredHiddenFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression) {
			var html = new StringBuilder();
			html.Append(htmlHelper.HiddenFor(expression).ConvertToString());
			var name = htmlHelper.NameFor(expression).ToString();
			var value = GetValueAsString(htmlHelper.ValueFor(expression));
			html.Append(GetHashFieldHtml(htmlHelper, name, value).ConvertToString());
			return new HtmlString(html.ToString());
		}

		public static IHtmlContent SecuredHiddenField(this IHtmlHelper htmlHelper, string name, object value) {
			var html = new StringBuilder();
			html.Append(htmlHelper.Hidden(name, value));
			html.Append(GetHashFieldHtml(htmlHelper, name, GetValueAsString(value)));
			return new HtmlString(html.ToString());
		}

		public static IHtmlContent HashField(this IHtmlHelper htmlHelper, string name, object value) {
			return GetHashFieldHtml(htmlHelper, name, GetValueAsString(value));
		}

		public static IHtmlContent MultipleFieldHashField(this IHtmlHelper htmlHelper, string name, IEnumerable values) {
			var valueToHash = new StringBuilder();
			foreach (var v in values) {
				valueToHash.Append(v);
			}

			return HashField(htmlHelper, name, valueToHash);
		}

		private static string GetValueAsString(object value) {
			return Convert.ToString(value, CultureInfo.CurrentCulture);
		}

		private static IHtmlContent GetHashFieldHtml(IHtmlHelper htmlHelper, string name, string value) {
			return htmlHelper.Hidden(SecuredValueFieldNameComputer.GetSecuredValueFieldName(name), SecuredValueHashComputer.GetHash(value));
		}
	}

	public class SHA1HashComputer : IHashComputer {
		public string GetBase64HashString(string value, string secret) {
			// create an array of bytes that contains the value and the secret
			var bytes = Encoding.UTF8.GetBytes(value + secret);
			var sha1 = System.Security.Cryptography.SHA1.Create();
			var hash = sha1.ComputeHash(bytes);
			return Convert.ToBase64String(hash);
		}
	}

	public static class SecuredValueHashComputer {
		public static string Secret { get; set; }

		public static IHashComputer HashComputer { get; set; }

		static SecuredValueHashComputer() {
			Secret = Config.GetSecret();
			HashComputer = new SHA1HashComputer();
		}

		public static string GetHash(string value) {
			return HashComputer.GetBase64HashString(value, Secret);
		}
	}

	public static class SecuredValueFieldNameComputer {
		public const string NameSuffix = "_sha1";
		public static string GetSecuredValueFieldName(string name) {
			return name + NameSuffix;
		}
	}

	public static class SecuredValueValidator {
		public static void ValidateValue(string value, string hash) {
			var computedHash = SecuredValueHashComputer.GetHash(value);
			if (computedHash != hash) {
				throw new PermissionsException("Unexpected field was edited.");
			}
		}

		public static void ValidateValue(IFormCollection formValues, string name) {
			var value = formValues[name];
			var hash = formValues[SecuredValueFieldNameComputer.GetSecuredValueFieldName(name)];
			ValidateValue(value, hash);
		}

		public static void ValidateMultipleValues(NameValueCollection formValues, string name, IEnumerable<string> names) {
			var valueToHash = new StringBuilder();
			foreach (var n in names) {
				valueToHash.Append(formValues[n]);
			}
			ValidateValue(valueToHash.ToString(), formValues[SecuredValueFieldNameComputer.GetSecuredValueFieldName(name)]);
		}
	}
}

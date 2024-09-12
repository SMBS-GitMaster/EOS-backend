using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RadialReview {
	public static class ExpressionExtensions {
		public static Expression<Func<TInput, object>> AddBox<TInput, TOutput>(this Expression<Func<TInput, TOutput>> expression) {
			// Add the boxing operation, but get a weakly typed expression
			Expression converted = Expression.Convert(expression.Body, typeof(object));
			// Use Expression.Lambda to get back to strong typing
			return Expression.Lambda<Func<TInput, object>>(converted, expression.Parameters);
		}

		public static String GetMvcName<T>(this Expression<Func<T, object>> selector) {
			string p;
			if (selector.Body is UnaryExpression) {
				p = ((UnaryExpression)selector.Body).Operand.ToString();
				return p.Substring(p.IndexOf(".") + 1);
			}
			if (selector.Body is MemberExpression) {
				p = ((MemberExpression)selector.Body).ToString();
				return p.Substring(p.IndexOf(".") + 1);
			}
			throw new Exception("Unhandled");
		}

		public static Type GetMemberType(this LambdaExpression memberSelector) {

			return memberSelector.Body.Type;

		}

		public static string GetMemberName(this LambdaExpression memberSelector) {
			Func<Expression, string> nameSelector = null;  //recursive func
			nameSelector = e => //or move the entire thing to a separate recursive method
			{
				switch (e.NodeType) {
					case ExpressionType.Parameter:
						return ((ParameterExpression)e).Name;
					case ExpressionType.MemberAccess:
						return ((MemberExpression)e).Member.Name;
					case ExpressionType.Call:
						return ((MethodCallExpression)e).Method.Name;
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
						return nameSelector(((UnaryExpression)e).Operand);
					case ExpressionType.Invoke:
						return nameSelector(((InvocationExpression)e).Expression);
					case ExpressionType.ArrayLength:
						return "Length";
					default:
						throw new Exception("not a proper member selector");
				}
			};

			return nameSelector(memberSelector.Body);
		}


		public static string GetPropertyDisplayName(this LambdaExpression propertyExpression) {
			var memberInfo = GetPropertyInformation(propertyExpression.Body);
			if (memberInfo == null) {
				throw new ArgumentException("No property reference expression was found.", "propertyExpression");
			}

			var attr = GetAttribute<DisplayAttribute>(memberInfo, false);
			if (attr == null || attr.GetName() == null) {
				return SplitCamelCase(memberInfo.Name);
			}
			return attr.GetName();
		}

		public static string GetPropertyDisplayPrompt(this LambdaExpression propertyExpression) {
			var memberInfo = GetPropertyInformation(propertyExpression.Body);
			if (memberInfo == null) {
				throw new ArgumentException("No property reference expression was found.", "propertyExpression");
			}
			var attr = GetAttribute<DisplayAttribute>(memberInfo, false);
			if (attr == null) {
				return "";
			}
			return attr.GetPrompt();
		}

		private static Type GetMemberUnderlyingType(MemberInfo member) {
			switch (member.MemberType) {
				case MemberTypes.Field:
					return ((FieldInfo)member).FieldType;
				case MemberTypes.Property:
					return ((PropertyInfo)member).PropertyType;
				case MemberTypes.Event:
					return ((EventInfo)member).EventHandlerType;
				default:
					throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", "member");
			}
		}

		public static Type GetPropertyType(this LambdaExpression propertyExpression) {
			return GetMemberUnderlyingType(GetPropertyInformation(propertyExpression.Body));
		}


		public static string GetPropertyDisplayDescription(this LambdaExpression propertyExpression) {
			var memberInfo = GetPropertyInformation(propertyExpression.Body);
			if (memberInfo == null) {
				throw new ArgumentException("No property reference expression was found.", "propertyExpression");
			}
			var attr = GetAttribute<DisplayAttribute>(memberInfo, false);
			if (attr == null) {
				return "";
			}
			return attr.GetDescription();
		}

		#region helpers
		public static string SplitCamelCase(string input) {
			return System.Text.RegularExpressions.Regex.Replace(input, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
		}

		public static T GetAttribute<T>(MemberInfo member, bool isRequired) where T : Attribute {
			var attribute = member.GetCustomAttributes(typeof(T), false).SingleOrDefault();

			if (attribute == null && isRequired) {
				throw new ArgumentException(
					string.Format(CultureInfo.InvariantCulture, "The {0} attribute must be defined on member {1}", typeof(T).Name, member.Name));
			}

			return (T)attribute;
		}

		public static MemberInfo GetPropertyInformation(Expression propertyExpression) {
			MemberExpression memberExpr = propertyExpression as MemberExpression;
			if (memberExpr == null) {
				UnaryExpression unaryExpr = propertyExpression as UnaryExpression;
				if (unaryExpr != null && unaryExpr.NodeType == ExpressionType.Convert) {
					memberExpr = unaryExpr.Operand as MemberExpression;
				}
			}

			if (memberExpr != null && memberExpr.Member.MemberType == MemberTypes.Property) {
				return memberExpr.Member;
			}

			return null;
		}
		#endregion

	}
}

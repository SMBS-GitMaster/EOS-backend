using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using System.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Html;
using System;
using System.ComponentModel;

namespace RadialReview.Html {
  public static class SelectExtensions {
    public static string GetInputName<TModel, TProperty>(Expression<Func<TModel, TProperty>> expression) {
      if (expression.Body.NodeType == ExpressionType.Call) {
        MethodCallExpression methodCallExpression = (MethodCallExpression)expression.Body;
        string name = GetInputName(methodCallExpression);
        return name.Substring(expression.Parameters[0].Name.Length + 1);
      }
      return expression.Body.ToString().Substring(expression.Parameters[0].Name.Length + 1);
    }

    private static string GetInputName(MethodCallExpression expression) {
      MethodCallExpression methodCallExpression = expression.Object as MethodCallExpression;
      if (methodCallExpression != null) {
        return GetInputName(methodCallExpression);
      }
      return expression.Object.ToString();
    }

    public static IHtmlContent EnumDropDownListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes = null)
      where TModel : class {
      if (htmlAttributes == null)
        htmlAttributes = new { };
      string inputName = GetInputName(expression);
      var value = htmlHelper.ViewData.Model == null ? default(TProperty) : expression.Compile()(htmlHelper.ViewData.Model);
      return htmlHelper.DropDownList(inputName, ToSelectList(typeof(TProperty), value.ToString()), htmlAttributes);
    }

    public static IHtmlContent EnumDropDownList<TModel, TEnum>(this IHtmlHelper<TModel> htmlHelper, string name, TEnum selected, object htmlAttributes = null, bool? useDescription = null)
      where TModel : class where TEnum : struct, IConvertible {
      return htmlHelper.DropDownList(name, ToSelectList(typeof(TEnum), selected.ToString(), useDescription), htmlAttributes);
    }

    public static List<SelectListItem> ToSelectList<TEnum>(TEnum selectedItem, Func<TEnum, string> overrideTitle, bool? useDescription = null) {
      return ToSelectList(typeof(TEnum), selectedItem.ToString(), useDescription, o => overrideTitle((TEnum)o));
    }


    public static List<SelectListItem> ToSelectList(Type enumType, string selectedItem, bool? useDescription = null, Func<object, string> overrideTitle = null) {
      var items = new List<SelectListItem>();
      var allowNull = false;
      if (Nullable.GetUnderlyingType(enumType) != null) {
        allowNull = true;
        enumType = Nullable.GetUnderlyingType(enumType);
      }

      foreach (var item in Enum.GetValues(enumType)) {
        var fi = enumType.GetField(item.ToString());
        var attribute = fi.GetCustomAttributes(typeof(DisplayAttribute), true).FirstOrDefault() as DisplayAttribute;
        var description = fi.GetCustomAttributes(typeof(DescriptionAttribute), true).FirstOrDefault() as DescriptionAttribute;
        var doNotDisplay = fi.GetCustomAttributes(typeof(DoNotDisplay), true).FirstOrDefault() as DoNotDisplay;
        string title;
        if (doNotDisplay != null)
          continue;
        if (attribute != null) {
          if (useDescription == true && description != null)
            title = description.Description;
          else
            title = attribute.ResourceType == null ? attribute.Name : new ResourceManager(attribute.ResourceType).GetString(attribute.Name);
        } else {
          title = item.ToString();
        }

        if (item.ToString().ToLower() == "invalid")
          continue;

        if (overrideTitle!=null) {
          title = overrideTitle(item) ?? title;
        }

        var listItem = new SelectListItem { Value = item.ToString(), Text = title, Selected = selectedItem == (item).ToString() };
        items.Add(listItem);
      }

      if (allowNull && selectedItem == null) {
        items.Insert(0, new SelectListItem { Value = null, Text = "Unset", Selected = selectedItem == null });
      }
      return items;
    }
  }
}

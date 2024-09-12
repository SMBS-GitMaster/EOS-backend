using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using RadialReview.Models;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Html {
	public enum TimeOfDay {
		Beginning,
		End
	}

	public static partial class HtmlExtensions {

		public static SettingsViewModel Settings(this RazorPageBase page) {
			SettingsViewModel settings = null;
			if (page != null && page.ViewBag != null && page.ViewBag.Settings is SettingsViewModel) {
				settings = (SettingsViewModel)page.ViewBag.Settings;
			}
			return settings ?? new SettingsViewModel();
		}


		public static IHtmlContent GenerateEmailButton(this IHtmlHelper html, string url, string contents, string hexFillColor, string hexBackgroundColor, int width) {
			return new HtmlString(HtmlUtility.GenerateHtmlButton(url, contents, hexFillColor, hexBackgroundColor, width));
		}

		public static string ConvertToString(this IHtmlContent content) {
			var sb = new StringBuilder();
			var stringWriter = new StringWriter(sb);
			content.WriteTo(stringWriter, System.Text.Encodings.Web.HtmlEncoder.Default);
			return sb.ToString();
		}

		public static IHtmlContent DisableToReadonly(this IHtmlContent helper) {
			if (helper == null) {
				throw new ArgumentNullException();
			}

			var html = helper.ConvertToString();
			html = html.Replace("disabled=\"disabled\"", "readonly=\"readonly\"");
			return new HtmlString(html);
		}

		public static IHtmlContent Disable(this IHtmlContent helper, bool disabled) {
			if (helper == null) {
				throw new ArgumentNullException();
			}

			if (disabled) {
				string html = helper.ConvertToString();
				int startIndex = html.IndexOf('>');
				html = html.Insert(startIndex, " readonly=\"readonly\"");
				return new HtmlString(html);
			}

			return helper;
		}

		public static string VideoConferenceUrl(this IHtmlHelper html, string resource = null) {
			return Config.VideoConferenceUrl(resource);
		}

		public static string GetBaseUrl(this IHtmlHelper html, string resource = null) {
			var server = Config.BaseUrl((OrganizationModel)html.ViewBag.Organization).TrimEnd('/');
			if (resource != null) {
				server = server + "/" + resource.TrimStart('/');
			}

			return server;
		}

		public static UserOrganizationModel UserOrganization(this IHtmlHelper html) {
			return (UserOrganizationModel)html.ViewBag.UserOrganization;
		}

		public static OrganizationModel Organization(this IHtmlHelper html) {
			return (OrganizationModel)html.ViewBag.Organization;
		}

		public static DateTime ConvertFromUtc(this IHtmlHelper html, DateTime time) {
			var user = (UserOrganizationModel)html.ViewBag.UserOrganization;
			if (user != null) {
				return time.AddMinutes(user.GetTimezoneOffset());
			}

			var org = (OrganizationModel)html.ViewBag.Organization;
			if (org != null) {
				return org.ConvertFromUTC(time);
			}

			return time;
		}

		public static IHtmlContent ConvertFromUtcLocal(this IHtmlHelper html, DateTime utcDate, string format = null) {
			var guid = "date_" + Guid.NewGuid().ToString().Replace("-", "");
			var formatArg = format == null ? "" : ",\"" + format + "\"";
			var str = $@"<span class=""display-date local {guid}"" id=""{guid}""><script>document.getElementById(""{guid}"").innerHTML=getFormattedDate(ConvertFromServerTime({utcDate.ToJavascriptMilliseconds()}){formatArg});</script></span>";
			return new HtmlString(str);
		}

		public static string ProductName(this IHtmlHelper html) {
			return Config.ProductName(html.Organization());
		}

		public static string ReviewName(this IHtmlHelper html) {
			return Config.ReviewName(html.Organization());
		}

		public static async Task<IHtmlContent> CollapseSection(this IHtmlHelper html, String title, String viewName, object model, string checkboxClass = null) {
			html.ViewData["PartialViewName"] = viewName;
			html.ViewData["SectionTitle"] = title;
			html.ViewData["CheckboxClass"] = checkboxClass;
			return await html.PartialAsync("Partial/Collapsable", model, html.ViewData);
		}

		public static IHtmlContent ViewOrEdit(this IHtmlHelper html, bool edit, bool icon = true) {
			if (icon) {
				return new HtmlString(edit ? "<span class='glyphicon glyphicon-pencil viewEdit edit'></span>" : "<span class='glyphicon glyphicon-eye-open viewEdit view'></span>");
			}

			return new HtmlString(edit ? "Edit" : "View");
		}

		public static IHtmlContent GrayScale(this IHtmlHelper html, double value, double neg, double pos, double alpha) {
			double scale = 0;
			if (pos - neg != 0) {
				scale = (value - neg) / (pos - neg) * 255.0;
			}

			int coerced = (int)(255 - Math.Max(0, Math.Min(scale, 255.0)));
			return new HtmlString(String.Format("rgba({0},{0},{0},{1})", coerced, alpha));
		}

		public static IHtmlContent Color(this IHtmlHelper html, double value, double neg, double zero, double pos, double alpha) {
			double v = 0;
			var redValue = 0.0;
			var greenValue = 0.0;
			// value is a value between 0 and 511;
			// 0 = red, 255 = yellow, 511 = green.
			if (value > zero) {
				if (pos - zero == 0) {
					v = 255;
				} else {
					v = (int)((value - zero) / (pos - zero) * 255.0 + 255.0);
				}
			} else {
				if (zero - neg == 0) {
					v = 0;
				} else {
					v = (int)((value - neg) / (zero - neg) * 255.0);
				}
			}

			v = Math.Max(0, Math.Min(511, v));
			if (v < 255) {
				redValue = 255;
				greenValue = Math.Sqrt(v) * 16;
				greenValue = Math.Round(greenValue);
			} else {
				greenValue = 255;
				v = v - 255;
				redValue = 256 - (v * v / 255);
				redValue = Math.Round(redValue);
			}

			int red = Math.Min(255, Math.Max(0, (int)redValue));
			int green = Math.Min(255, Math.Max(0, (int)greenValue));
			var hexColor = String.Format("rgba({0},{1},{2},{3})", red, green, 0, alpha);
			return new HtmlString(hexColor);
		}

		public static IHtmlContent ShowNew(this IHtmlHelper html, DateTime showUntil) {
			if (DateTime.UtcNow < showUntil) {
				return new HtmlString("<span class='show-new-marker' style='color:red;font-size:70%;opacity:0.7;pointer-events:none;width:0px;display:inline-block;'>New!</span>");
			}

			return new HtmlString("");
		}

		public static IHtmlContent EditFirstButton(this IHtmlHelper html, List<string> items, bool edit = true) {
			var count = items.Count();
			var name = "" + count;
			var after = "";
			var joined = String.Join(", ", items);
			if (count == 1) {
				name = items.First();
			} else if (count == 0) {
				name = "<i>None</i>";
				joined = "None";
			} else {
				name = items.First() + "<span class='hidden'>" + String.Join(",", items.Skip(1)) + "</span>";
				after = "(+" + (count - 1) + ")";
				joined = String.Join(",", items);
			}

			return new HtmlString("<span class='editFirst'><span title='" + joined + "' class='text'><span class='uncollapsable'>" + after + "</span><span class='collapsable'>" + name + "</span></span></span>");
		}

		public static IHtmlContent Badge<T>(this IHtmlHelper<T> html, Func<T, int> count) {
			var c = count(html.ViewData.Model);
			if (c != 0) {
				return new HtmlString(@"<span class=""badge"">" + c + "</span>");
			}

			return new HtmlString("");
		}

		public static IHtmlContent ShowModal(this IHtmlHelper html, String title, String pullUrl, String pushUrl, String callbackFunction = null, String preSubmitCheck = null, String onComplete = null, String onCompleteFunction = null) {
			title = title.Replace("'", "\\'");
			if (onComplete != null || onCompleteFunction != null) {
				var c = "";
				if (onComplete != null) {
					c = "'" + onComplete + "'";
				} else {
					c = onCompleteFunction;
				}

				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "','" + preSubmitCheck + "'," + c + ")");
			}

			if (preSubmitCheck != null) {
				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "','" + preSubmitCheck + "')");
			} else if (callbackFunction != null) {
				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "','" + callbackFunction + "')");
			} else {
				return new HtmlString(@"showModal('" + title + @"','" + pullUrl + @"','" + pushUrl + "')");
			}
		}

		public static IHtmlContent AlertBoxDismissableJavascript(this IHtmlHelper html, String messageVariableName, String alertType = "alert-danger") {
			return new HtmlString("\"<div class=\\\"alert " + alertType + " alert-dismissable\\\"><button type=\\\"button\\\" class=\\\"close\\\" data-dismiss=\\\"alert\\\" aria-hidden=\\\"true\\\">&times;</button><strong>" + MessageStrings.Warning + "</strong> <span class=\\\"message\\\">\" + " + messageVariableName + " + \"</span></div>\"");
		}

		public static IHtmlContent AlertBoxDismissable(this IHtmlHelper html, String message, String alertType = null, String alertMessage = null) {
			if (String.IsNullOrWhiteSpace(alertType)) {
				alertType = "alert-danger";
			}

			if (String.IsNullOrWhiteSpace(alertMessage)) {
				alertMessage = MessageStrings.Warning;
			}

			if (!String.IsNullOrWhiteSpace(message)) {
				return new HtmlString("<div class=\"alert " + alertType + " alert-dismissable\"><button type=\"button\" class=\"close\" data-dismiss=\"alert\" aria-hidden=\"true\">&times;</button><strong>" + alertMessage + "</strong> <span class=\"message\">" + message + "</span></div>");
			}

			return new HtmlString("");
		}

		public static IHtmlContent ValidationSummaryMin(this IHtmlHelper html, Boolean excludePropertyErrors = false) {
			/*
             *
             * <div class="validation-summary-errors" data-valmsg-summary="true">
             * <ul><li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                <li>Value must be between 1 and 10.</li>
                </ul></div>
             *
             */
			var errors = html.ViewData.ModelState.Values.SelectMany(x => x.Errors);
			var output = @"<div class=""validation-summary-" + (errors.Any() ? "errors" : "valid") + @" alert alert-error"" data-valmsg-summary=""true"">";
			output += "<ul>";
			if (errors.Any()) {
				foreach (var li in errors.GroupBy(x => x.ErrorMessage)) {
					output += @"<li>" + li.First().ErrorMessage + "</li>";
				}
			} else {
				output += @"<li style=""display:none""></li>";
			}

			output += "</ul>" +  "</div>";
			return new HtmlString(output);
		}

		public static IHtmlContent ArrayToString<T>(this IHtmlHelper html, IEnumerable<T> items, bool format = false, bool stringEnums = false) {
			var settings = new JsonSerializerSettings() { Formatting = format ? Formatting.Indented : Formatting.None, };
			if (stringEnums) {
				settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
			}

			return new HtmlString(JsonConvert.SerializeObject(items, settings).Replace("</script>", "<\\/script>"));
		}



















		private static string GenerateJsonField<T>(Expression<Func<T, object>> field) {
			var name = field.GetMemberName();
			var builder = "{";
			builder += "name:\"" + name + "\",";
			builder += "text:\"" + field.GetPropertyDisplayName() + "\",";
			var type = field.GetPropertyType();
			if (type.IsEnum) {
				builder += "type:\"select\",";
				builder += "options:[";
				foreach (var e in Enum.GetValues(type)) {
					builder += $@"{{text:""{ExpressionExtensions.SplitCamelCase(Enum.GetName(type, e))}"", value: ""{Enum.GetName(type, e)}""}},";
				}

				builder += "],";
			}

			if (field.GetMemberName().ToLower() == "id") {
				builder += "type:\"hidden\"";
			} else if (type == typeof(bool)) {
				builder += $@"	type:""radio"",
	options:[
		{{text:""yes"",value:true,  checked:function(x){{ return x.value==""true"";}} }},
		{{text:""no"", value:false, checked:function(x){{ return x.value!=""true"";}} }}
	],";
			} else if (type == typeof(long) || type == typeof(int) || type == typeof(long?) || type == typeof(int?)) {
				builder += "type:\"number\",";
			} else if (type == typeof(DateTime) || type == typeof(DateTime?)) {
				builder += "type:\"datetime\",";
			}

			builder += "}";
			return builder;
		}

		private static IHtmlContent ToJsonFields<T>(this IHtmlHelper<List<T>> html, params Expression<Func<T, object>>[] fields) {
			var builder = "[";
			foreach (var field in fields) {
				builder += GenerateJsonField(field) + ",";
			}

			builder += "]";
			return new HtmlString(builder);
		}

		public static IHtmlContent FieldToRow<T>(this IHtmlHelper<List<T>> html, params Expression<Func<T, object>>[] fields) {
			var unusedProps = typeof(T).GetProperties().Select(x => x.Name).ToList();
			var builder = "[";
			foreach (var field in fields) {
				//Remove and Edit switchs.
				if (field.Body is ConstantExpression) {
					var obj = ((ConstantExpression)field.Body).Value;
					if (obj is string) {
						var str = ((string)obj).ToLower();
						builder += "";
						switch (str) {
							case "edit":
								builder += "{edit:true,classes:\"noWidth\"},";
								break;
							case "remove":
								builder += "{remove:true,classes:\"noWidth\"},";
								break;
							default:
								break;
						}
					}
				} else {
					var name = field.GetMemberName();
					//remove from unused list
					unusedProps.Remove(name);
					//build the column
					builder += $@"{{
	id:""{name}"",
	name:""{field.GetPropertyDisplayName()}"",
";
					var type = field.GetPropertyType();
					if (type == typeof(DateTime) || type == typeof(DateTime?)) {
						builder += "	contents:function(x){ return getFormattedDate(x." + name + "); },";
					} else if (type.IsEnum) {
						var enumLookup1 = "";
						var enumLookup2 = "";
						foreach (var e in Enum.GetValues(type)) {
							enumLookup1 += "enumLookup[\"" + e + "\"]=\"" + ExpressionExtensions.SplitCamelCase(Enum.GetName(type, e)) + "\";\n";
							try {
								enumLookup2 += "enumLookup[" + (int)e + "]=\"" + ExpressionExtensions.SplitCamelCase(Enum.GetName(type, e)) + "\";\n";
							} catch (Exception) {
							}
						}

						builder += $@"	contents:function(x){{
		var enumLookup = {{}};
		{enumLookup1}
		{enumLookup2}
		if (x.{field.GetMemberName()} in enumLookup){{
			return enumLookup[x.{field.GetMemberName()}];
		}}
		return x.{field.GetMemberName()};
	}},";
					} else if (type == typeof(bool) || type == typeof(bool?)) {
						builder += "	contents:function(x){ return \"\"+x." + name + ";},";
					} else {
						builder += "	contents:function(x){ return x." + name + ";},";
					}

					builder += $@"	input: {GenerateJsonField(field)} ,";
					builder += "},\n";
				}
			}

			foreach (var name in unusedProps) {
				builder += $@"{{
	id:""{name}"",
	contents:function(x){{return x.{name};}},
	hideColumn:true,
	input:{{
		type:""hidden"",
		name:""{name}"",
	}}
}},";
			}

			builder += "]";
			return new HtmlString(builder);
		}

		public static IHtmlContent ToJson<T>(this IHtmlHelper html, T item) {
			if (item == null)
				return new HtmlString("null");
			return new HtmlString(JsonConvert.SerializeObject(item));
		}

		public static IEnumerable<object> AdaptArray<T>(this IHtmlHelper html, IEnumerable<T> items, Func<T, object> converter) {
			return items.Select(x => converter(x));
		}

		public static IHtmlContent ArrayToString<T>(this IHtmlHelper html, IEnumerable<T> items, Func<T, object> converter) {
			var convert = AdaptArray(html, items, converter);
			return new HtmlString(JsonConvert.SerializeObject(convert));
		}

		public static string NewGuid(this IHtmlHelper html) {
			return "g" + Guid.NewGuid().ToString().Replace("-", "");
    }
    public static IHtmlContent ClientDateFor<T>(this IHtmlHelper<T> html, Expression<Func<T, DateTime?>> serverDateSelector, TimeOfDay timeOfDay, string classes = null) {
      var model = (T)html.ViewData.Model;
      var name = html.NameFor(serverDateSelector);
      var id = html.IdFor(serverDateSelector);
      var serverDate = serverDateSelector.Compile();
      var guid = html.NewGuid();
      return BuildClientDate(guid, id, name, serverDate(model), timeOfDay, classes);
    }

    public static IHtmlContent ClientDateFor<T>(this IHtmlHelper<T> html, Expression<Func<T, DateTime>> serverDateSelector, TimeOfDay timeOfDay, string classes = null) {
			var model = (T)html.ViewData.Model;
			var name = html.NameFor(serverDateSelector);
			var id = html.IdFor(serverDateSelector);
			var serverDate = serverDateSelector.Compile();
			var guid = html.NewGuid();
			return BuildClientDate(guid, id, name, serverDate(model), timeOfDay, classes);
		}

    public static IHtmlContent ClientDate(this IHtmlHelper html, string name, DateTime serverDate, TimeOfDay timeOfDay, string classes = null) {
      var guid = html.NewGuid();
      return BuildClientDate(guid, name, name, serverDate, timeOfDay, classes);
    }


    public static IHtmlContent ClientDate(this IHtmlHelper html, string name, DateTime? serverDate,  TimeOfDay timeOfDay, string classes = null) {
      var guid = html.NewGuid();
      return BuildClientDate(guid, name, name, serverDate, timeOfDay, classes);
    }

    private static IHtmlContent BuildClientDate(string guid, string id, string name, DateTime serverDate, TimeOfDay timeOfDay, string classes) {
			var date = "";
			var builder = $@"
<div class='{guid} client-datepicker {classes}'></div>
<script>
	let clock_{guid} = setInterval(function(){{
    if (typeof(Time)!==""undefined""){{
      clearInterval(clock_{guid})
		  var options = {{
			  selector : $("".{guid}.client-datepicker""),
			  serverTime : new Date(""{serverDate.ToString("yyyy/MM/dd HH:mm:ss")}""),
			  displayAsLocal : true,
			  name:""{name}"",
			  id:""{id}"",
			  datePickerOptions:undefined,
			  endOfDay: {(timeOfDay == TimeOfDay.End ? "true" : "false")},
		  }};
		  Time.createClientDatepicker(options);
    }}
	}},50);
 </script>
";
			return new HtmlString(builder);
    }
    private static IHtmlContent BuildClientDate(string guid, string id, string name, DateTime? serverDate, TimeOfDay timeOfDay, string classes) {
      var date = "";
      var builder = $@"
<div class='{guid} client-datepicker {classes}'></div>
<script>
	  let clock_{guid} = setInterval(function(){{
    if (typeof(Time)!==""undefined""){{
        clearInterval(clock_{guid})
		    var options = {{
			    selector : $("".{guid}.client-datepicker""),";
          if (serverDate!=null) {
            builder+=$@"serverTime : new Date(""{serverDate.Value.ToString("yyyy/MM/dd HH: mm: ss")}""),";
          }
          builder+=$@"
			    displayAsLocal : true,
			    name:""{name}"",
			    id:""{id}"",
			    datePickerOptions:undefined,
			    endOfDay: {(timeOfDay == TimeOfDay.End ? "true" : "false")},
		    }};
		    Time.createClientDatepicker(options);
    }}
	}},50);
 </script>
";
      return new HtmlString(builder);
    }
  }
}

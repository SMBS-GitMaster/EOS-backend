using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RadialReview.Utilities
{
    public class CellInfo<T>
    {
        public int Row { get; set; }

        public int Col { get; set; }

        public T Cell { get; set; }
    }

    public class TableOptions<T>
    {
        public bool Responsive { get; set; }

        public Func<CellInfo<T>, string> CellClass { get; set; }

        public Func<CellInfo<T>, Dictionary<string, string>> CellProperties { get; set; }

        public string TableClass { get; set; }

        public Func<CellInfo<T>, string> CellText { get; set; }
    }

    public static class HtmlUtility
    {
        public static String Table<T>(List<List<T>> rowData, TableOptions<T> options = null)
        {
            var sb = new StringBuilder();
            //Defaults
            options = options ?? new TableOptions<T>();
            options.CellClass = options.CellClass ?? new Func<CellInfo<T>, string>(x => "");
            options.CellProperties = options.CellProperties ?? new Func<CellInfo<T>, Dictionary<string, string>>(x => new Dictionary<string, string>());
            options.TableClass = options.TableClass ?? "";
            options.CellText = options.CellText ?? new Func<CellInfo<T>, string>(x => x.Cell.ToString());
            if (options.Responsive)
                sb.Append("<div class='table-responsive'>");
            sb.Append("<table class=\"").Append(options.TableClass).Append("\" >");
            var i = 0;
            foreach (var row in rowData)
            {
                sb.Append("<tr>");
                var j = 0;
                foreach (var cell in row)
                {
                    var c = new CellInfo<T>()
                    {Cell = cell, Row = i, Col = j};
                    sb.Append("<td class=\"").Append(options.CellClass(c)).Append("\" ");
                    var props = options.CellProperties(c);
                    foreach (var prop in props)
                    {
                        sb.Append(prop.Key).Append("=\"").Append(prop.Value).Append("\" ");
                    }

                    sb.Append(">").Append(options.CellText(c)).Append("</td>");
                    j++;
                }

                sb.Append("</tr>");
                i++;
            }

            sb.Append("</table>");
            if (options.Responsive)
                sb.Append("</div>");
            return sb.ToString();
        }

        /// <summary>
        /// Remove all the tags in a string and unescape string
        /// </summary>
        /// <param name = "html"></param>
        /// <returns></returns>
        public static string StripHtml(this string html)
        {
            var output = Regex.Replace(html, "<.*?>", string.Empty);
            output = WebUtility.HtmlDecode(output);
            return output;
        }

        public static string ReplaceNewLineWithBr(this string text)
        {
            return Regex.Replace(text, @"\r\n?|\n", "<br />");
        }


        public static string GenerateHtmlButton(string url, string contents, string hexFillColor, string hexBackgroundColor, int width) {
            return $@"<div><!--[if mso]>
  <v:roundrect xmlns:v=""urn:schemas-microsoft-com:vml"" xmlns:w=""urn:schemas-microsoft-com:office:word"" href=""{url}"" style=""height:40px;v-text-anchor:middle;width:{width}px;"" arcsize=""100%"" stroke=""f"" fillcolor=""{hexBackgroundColor}"">
    <w:anchorlock/>
    <center>
  <![endif]-->
      <a href=""{url}""
style=""background-color:{hexBackgroundColor};border-radius:40px;color:{hexFillColor};display:inline-block;font-family:sans-serif;font-size:13px;font-weight:bold;line-height:40px;text-align:center;text-decoration:none;width:{width}px;-webkit-text-size-adjust:none;"">{contents}</a>
  <!--[if mso]>
    </center>
  </v:roundrect>
<![endif]--></div>";
        }
    }
}

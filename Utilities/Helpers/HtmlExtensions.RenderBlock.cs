using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;

namespace RadialReview.Html {
	public static partial class HtmlExtensions {

		private const string SCRIPTS_PAGEBLOCK = "SCRIPTS_PAGEBLOCK";
		private const string STYLES_PAGEBLOCK = "STYLES_PAGEBLOCK";

    public static IDisposable BeginScripts(this IHtmlHelper helper) {
			return new RenderBlock(SCRIPTS_PAGEBLOCK, helper.ViewContext);
		}

		public static HtmlString PageScripts(this IHtmlHelper helper) {
			return new HtmlString(string.Join(Environment.NewLine, RenderBlock.GetPageScriptsList(SCRIPTS_PAGEBLOCK, helper.ViewContext.HttpContext)));
		}
		public static IDisposable BeginStyles(this IHtmlHelper helper) {
			return new RenderBlock(STYLES_PAGEBLOCK, helper.ViewContext);
		}

		public static HtmlString PageStyles(this IHtmlHelper helper) {
			return new HtmlString(string.Join(Environment.NewLine, RenderBlock.GetPageScriptsList(STYLES_PAGEBLOCK, helper.ViewContext.HttpContext)));
		}

		private class RenderBlock : IDisposable {
			private readonly TextWriter _originalWriter;
			private readonly StringWriter _scriptsWriter;
			private readonly ViewContext _viewContext;
      public string BlockType { get; private set; }

			public RenderBlock(string blockType, ViewContext viewContext) {
				_viewContext = viewContext;
				_originalWriter = _viewContext.Writer;
				_viewContext.Writer = _scriptsWriter = new StringWriter();
				BlockType = blockType;
			}

			public void Dispose() {
				_viewContext.Writer = _originalWriter;
				var pageScripts = GetPageScriptsList(BlockType, _viewContext.HttpContext);
				pageScripts.Add(_scriptsWriter.ToString());
			}

			public static List<string> GetPageScriptsList(string key, HttpContext httpContext) {
          var pageScripts = (List<string>)httpContext.Items[key];

          if (pageScripts == null)
          {
            pageScripts = new List<string>();
            lock(httpContext)
            {
              httpContext.Items[key] = pageScripts;

            }
          }

          return pageScripts;
        }
		}

	}
}

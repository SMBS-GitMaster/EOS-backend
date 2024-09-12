using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace RadialReview.Utilities {
  public partial class ViewUtility {
    public class ViewRenderer {
      private HttpContext HttpContext { get; set; }

      protected ViewEngineResult ViewEngineMain { get; set; }
      protected ViewEngineResult ViewEnginePartial { get; set; }

      public bool UsePartialRenderer { get; set; }

      private IServiceScopeFactory _scopeFactory;




      public ViewDataDictionary ViewData { get; private set; }
      private ActionContext ActionContext { get; set; }

      public ViewRenderer(IServiceScopeFactory scopeFactory, bool partial, ViewEngineResult viewEngineMain, ViewEngineResult viewEnginePartial, HttpContext httpContext, ActionContext actionContext, object model = null) {
        UsePartialRenderer = partial;
        ViewEngineMain = viewEngineMain;
        ViewEnginePartial = viewEnginePartial;
        HttpContext = httpContext;
        ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
        ViewData.Model = model;
        ActionContext = actionContext;
        _scopeFactory = scopeFactory;
      }

      public async Task<string> ExecuteAsync() {
        IServiceScope scope = null;
        if (HttpContext.RequestServices == null) {
          scope = _scopeFactory.CreateScope();
          HttpContext.RequestServices = scope.ServiceProvider;
        }

        try {
          string result = null;
          var sb = new StringBuilder();
          using (var sw = new StringWriter(sb)) {
            var actionContext = new ActionContext(ActionContext);
            var tempDataProvider = new FakeTempDataProvider();
            var helperOptions = new HtmlHelperOptions();

            var viewRenderer = (UsePartialRenderer ? ViewEnginePartial : ViewEngineMain);

            var viewContext = new ViewContext(actionContext, viewRenderer.View, ViewData, new TempDataDictionary(HttpContext, tempDataProvider), sw, helperOptions);

            await viewRenderer.View.RenderAsync(viewContext);
            result = sb.ToString();
            return result;
          }
        } finally {
          if (scope != null)
            scope.Dispose();
        }

      }

      public override string ToString() {
        return "View Renderer";
      }

      public HttpContext GetHttpContext() {
        return HttpContext;
      }

      public void SetViewBag<T>(string key, T value) {
        ViewData[key]=value;
      }

      public class FakeTempDataProvider : ITempDataProvider {
        public IDictionary<string, object> LoadTempData(HttpContext context) {
          throw new NotImplementedException();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values) {
          throw new NotImplementedException();
        }
      }
    }
  }
}

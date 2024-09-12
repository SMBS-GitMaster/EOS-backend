using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using RadialReview.Accessors.PDF;
using RadialReview.Engines;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.Files;
using RadialReview.Utilities.Serializers;
using SpreadsheetLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Controllers {
  public partial class BaseController : Controller {

    #region Angular Json Overrides
    private bool TransformAngular = true;
    protected new JsonResult Json(object data) {
      if (data is IAngular && TransformAngular && (Request == null || string.IsNullOrEmpty(Request.Query["transform"].ToString()))) {
        return base.Json(AngularSerializer.Serialize((IAngular)data));
      }

      if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || string.IsNullOrEmpty(Request.Query["transform"].ToString()))) {
        return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray());
      }

      return base.Json(data);
    }

    protected new JsonResult Json(object data, object serialerSettings) {
      if (data is IAngular && TransformAngular && (Request == null || string.IsNullOrEmpty(Request.Query["transform"].ToString()))) {
        return base.Json(AngularSerializer.Serialize((IAngular)data));
      }

      if (data is IEnumerable<IAngular> && TransformAngular && (Request == null || string.IsNullOrEmpty(Request.Query["transform"].ToString()))) {
        return base.Json(((IEnumerable<IAngular>)data).Select(x => AngularSerializer.Serialize(x)).ToArray());
      }

      return base.Json(data, serialerSettings);
    }
    #endregion


    protected async Task<ActionResult> Xls(SLDocument document, string name = null) {
      name = name ?? ("export_" + DateTime.UtcNow.ToJavascriptMilliseconds());
      Response.Clear();
      Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
      Response.Headers.Add("Content-Disposition", "attachment; filename=" + name + ".xlsx");
      using (var ms = new MemoryStream()) {
        document.SaveAs(ms);
        ms.Position = 0;
        await ms.CopyToAsync(Response.Body);
      }

      return new EmptyResult();
    }

    protected async Task<ActionResult> DownloadAsync(byte[] contents, string fileName, string fileType) {
      MemoryStream stream = new MemoryStream(contents);
      Response.Clear();
      Response.ContentType = MimeTypeMap.GetMimeType(fileType);
      var name = fileName;
      if (!name.ToLower().EndsWith(fileType.ToLower())) {
        if (!fileType.StartsWith(".")) {
          name = name + ".";
        }

        name = name + fileType;
      }

      Response.Headers["Content-Disposition"] = "filename=\"" + name + "\"";
      Response.Headers["content-length"] = stream.Length.ToString();
      await Response.Body.WriteAsync(stream.ToArray());
      stream.Close();
      return new EmptyResult();
    }

    protected async Task<ActionResult> Pdf(StreamAndMetaCollection documentCollection, string name) {
      using (var stream = new MemoryStream()) {
        await PdfEngine.Merge(documentCollection, stream);
        using (var ms = new MemoryStream(stream.ReadBytes())) {
          ms.Position = 0;
          return await PdfFromStreamAsync(name, ms);
        }
      }
    }

    protected async Task<ActionResult> Pdf(PdfDocument document, string name = null, bool inline = true) {
      name = name ?? document.Info.Title;
      MemoryStream stream = new MemoryStream();
      document.Save(stream, false);
      return await PdfFromStreamAsync(name, stream);
    }

    private async Task<ActionResult> PdfFromStreamAsync(string name, MemoryStream stream) {
      name = name ?? ((DateTime.UtcNow.ToJsMs()) + ".pdf");
      Response.Clear();
      Response.ContentType = "application/pdf";
      if (name != null) {
        if (!name.ToLower().EndsWith(".pdf")) {
          name += ".pdf";
        }

        name = CleanFileName(name);
        Response.Headers["Content-Disposition"] = "filename=\"" + name + "\"";
      }

      Response.Headers["content-length"] = stream.Length.ToString();
      await Response.Body.WriteAsync(stream.ToArray());
      stream.Close();
      return new EmptyResult();
    }

    protected FileResult Pdf(Document document, string name = null, bool inline = true) {

      var pdfRenderer = new PdfDocumentRenderer(true);
      pdfRenderer.Document = document;
      pdfRenderer.RenderDocument();
      if (pdfRenderer.PageCount == 0) {
        var doc = new Document();
        var s = doc.AddSection();
        s.AddParagraph("No pages.");
        return Pdf(doc, name, inline);
      }

      var stream = new MemoryStream();
      pdfRenderer.Save(stream, false);
      name = name ?? (Guid.NewGuid() + ".pdf");
      if (inline) {
      }

      return new FileStreamResult(stream, System.Net.Mime.MediaTypeNames.Application.Pdf);

    }


    protected ActionResult RedirectToLocal(string returnUrl) {
      if (Url.IsLocalUrl(returnUrl)) {
        return Redirect(returnUrl);
      } else {
        throw new RedirectException("Return URL is invalid.");
      }
    }

    protected async Task RequestAsLogStream(Func<LogResponseWriter,Task> actions) {
      using (var writer = new LogResponseWriter(HttpContext)) {
        await actions(writer);
      }
    }

  }
}

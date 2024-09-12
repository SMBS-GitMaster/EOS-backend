using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfToSvg;
using RadialReview.Models.Documents;
using System.Xml;
using RadialReview.Models.Downloads;
using NHibernate;
using RadialReview.Utilities;
using RadialReview.Models.Documents.Interceptors.Data;
using RadialReview.Utilities.FileTypes;
using RadialReview.Exceptions;

namespace RadialReview.Accessors {
  public partial class DocumentsAccessor {

    public static async Task<DocumentItemVM> UploadInteractive(UserOrganizationModel caller, IBlobStorageProvider bsp, string name, string type, Stream stream, string folderId, bool isTemplate) {
      FileTypeExtensionUtility.FileType fileType = FileTypeExtensionUtility.GetFileTypeFromExtension(type);
      Stream svg;
      switch (fileType.Extension) {
        case "pdf": svg = await ConvertPdfToSvg(stream); break;
        case "svg": svg = stream;break;
        default: throw new PermissionsException("Live Files only works with PDFs and SVG file formats. It is often possible to convert a file to a pdf.");
      }


      DocumentsFolder folder;
      string tagName;
      var now = DateTime.UtcNow;

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.CreateFileUnderDocumentsFolder(folderId);
          folder = GetDocumentFolder_Unsafe(s, folderId);
          tagName = folder.GetInterceptorProperty(InterceptConstants.TAG_HINTS, (string)null);

        }
      }
      var tags = new List<TagModel>();
      if (tagName != null) {
        tags = tagName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => TagModel.Create(x)).ToList();
      }

      tags.Add(TagModel.Create("interactive"));
      if (fileType.Known) {
        tags.Add(TagModel.Create("interactive-"+fileType.Extension));
      }

      //Save the whiteboard which is the active document
      var wb = await DocumentsAccessor.CreateWhiteboard(caller, name, folderId);

      //Save the SVG
      var permtypes = new PermTiny[]{
            PermTiny.Creator(),
            PermTiny.InheritedFrom(PermItem.ResourceType.Whiteboard,wb.Id)
      };
      var fileId = await FileAccessor.Save_Unsafe(bsp, caller.Id, svg, name,"svg",
        "Live "+fileType.Kind+" File uploaded by " + caller.GetName(),
        FileOrigin.Uploaded, FileOutputMethod.Save, ForModel.Create<DocumentsFolder>(folder.Id),
        FileNotification.DoNotNotify(), permtypes, tags.ToArray()
      );

      //Save Background..
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var url = "/documents/open/"+fileId;
          WhiteboardAccessor.SetBackgroundSvg(s, perms, wb.LookupId, url);
          WhiteboardAccessor.SetIsTemplate(s, perms, wb.LookupId, isTemplate);
          tx.Commit();
          s.Flush();
        }
      }

      var res = DocumentItemVM.Create(wb, new DocumentItemSettings(folder));
      res.Menu.AddRange(MenuItems.ConstructMenu(res));


      return res;
    }


    private static async Task<Stream> ConvertPdfToSvg(Stream stream) {
      var memoryStream = new MemoryStream();
      await stream.CopyToAsync(memoryStream);

      var maxW = 0;
      var maxH = 0;

      var totalH = 0;
      var pageCount = 0;

      var pages = new List<XmlDocument>();
      var widths = new List<int>();
      var heights = new List<int>();
      var pagePad = 10;

      using (var pdfDoc = await PdfDocument.OpenAsync(memoryStream, true)) {

        foreach (var page in pdfDoc.Pages) {
          var contents = await page.ToSvgStringAsync();
          var inner = new XmlDocument();
          inner.LoadXml(contents);

          var ww = int.Parse(inner.FirstChild.Attributes.GetNamedItem("width").Value);
          var hh = int.Parse(inner.FirstChild.Attributes.GetNamedItem("height").Value);
          widths.Add(ww);
          heights.Add(hh);

          totalH += hh;
          pageCount+=1;
          maxW = Math.Max(ww, maxW);
          maxH = Math.Max(hh, maxH);

          pages.Add(inner);
        }
      }

      maxW += pagePad*2;
      maxH += pagePad*2;

      //Build outer document <svg>
      var doc = new XmlDocument();
      {
        var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        var root = doc.DocumentElement;
        doc.InsertBefore(xmlDeclaration, root);

        var svgNode = doc.CreateElement(string.Empty, "svg", string.Empty);
        var w = ""+maxW;
        var h = ""+(totalH + (pageCount-1)*pagePad + 2*pagePad);
        svgNode.SetAttribute("width", w);
        svgNode.SetAttribute("height", h);
        svgNode.SetAttribute("preserveAspectRatio", "xMidYMid meet");
        svgNode.SetAttribute("viewBox", $@"0 0 {w} {h}");
        svgNode.SetAttribute("xmlns", $@"http://www.w3.org/2000/svg");

        doc.AppendChild(svgNode);
      }

      //Append each page...
      var hOffset = pagePad;
      for (var i = 0; i<pages.Count; i++) {
        var w = widths[i];
        var h = heights[i];
        var wOffset = (maxW - w)/w +2+ pagePad;

        var gNode = doc.CreateElement(string.Empty, "g", string.Empty);
        gNode.SetAttribute("transform", $@"translate({wOffset} {hOffset})");
        gNode.InnerXml += $@"<rect width=""{w}"" height=""{h}"" style=""fill: white;stroke-width: 1px;stroke:#e1e1e1;""/>";

        var p = pages[i];

        for (var ci = 0; ci<p.FirstChild.ChildNodes.Count; ci++) {
          var c = p.FirstChild.ChildNodes[ci];
          //var clonedChild = doc.ImportNode(c,true);
          gNode.InnerXml +=c.OuterXml;/* .AppendChild(clonedChild);*/
        }

        //Add rectangle for page

        doc.DocumentElement.AppendChild(gNode);

        hOffset += pagePad + heights[i];
      }

      var res = doc.OuterXml;

      return res.ToStream();
    }
  }
}

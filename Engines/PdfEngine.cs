using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using RadialReview.Accessors.PDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Engines {
	public class PdfEngine {

		public static int GetPageCount(Stream stream) {
			try {
				var mode = PdfDocumentOpenMode.Import;
				var inputDocument = PdfReader.Open(stream, mode);
				return inputDocument.PageCount;
			} catch (Exception) {
				return 0;
			}
		}

		private static StreamAndMeta EmptyDocument() {
			var doc = new PdfDocument();
			var page = doc.AddPage();

			XGraphics gfx = XGraphics.FromPdfPage(page);
			XFont font = new XFont("Times New Roman", 10, XFontStyle.Bold);
			XTextFormatter tf = new XTextFormatter(gfx);

			var rect = new XRect(0, 40, page.Width.Point, 100);
			tf.Alignment = XParagraphAlignment.Center;
			tf.DrawString("Page intentionally left blank.", font, XBrushes.Black, rect);

			var output = new MemoryStream();
			doc.Save(output, false);
			output.Position = 0;
			return new StreamAndMeta() {
				Content = output,
				Name = "",
				Pages = 1,
				DrawPageNumber = false
			};
		}


		public static async Task Merge(StreamAndMetaCollection pages, Stream destination) {
			// we only have Pdfsharp as our working merger for now
			var outputDocument = new PdfDocument();
			var curPage = 1;
			IEnumerable<StreamAndMeta> pdfs = pages.ToList();

			//Cannot save empty document
			if (!pdfs.Any()) {
				pdfs = pdfs.Union(EmptyDocument().AsList());
			}

			foreach (var sam in pdfs) {
				var stream = sam.Content;
				// Attention: must be in Import mode
				var mode = PdfDocumentOpenMode.Import;
				var inputDocument = PdfReader.Open(stream, mode);

				int totalPages = inputDocument.PageCount;
				for (int pageNo = 0; pageNo < totalPages; pageNo++) {
					// Get the page from the input document...
					var page = inputDocument.Pages[pageNo];


					// ...and copy it to the output document.
					var newPage = outputDocument.AddPage(page);

					if (sam.DrawPageNumber) {
						// Get an XGraphics object for drawing
						XGraphics gfx = XGraphics.FromPdfPage(newPage);
						XFont font = new XFont("Verdana", 8, XFontStyle.Regular);
						try {
							var topCorner = 28;
							var leftCorner = 36;
							gfx.DrawString("" + curPage, font, XBrushes.DarkGray, new XRect(newPage.Width - leftCorner, newPage.Height - topCorner, leftCorner, topCorner), XStringFormats.TopLeft);
						} catch (Exception e) {
							throw;
						}
					}

					curPage += 1;


				}
			}

			// Save the document
			outputDocument.Save(destination, false);
			destination.Position = 0;
		}

	}
}

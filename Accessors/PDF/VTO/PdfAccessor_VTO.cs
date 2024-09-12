using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using RadialReview.Models.Angular.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Table = MigraDoc.DocumentObjectModel.Tables.Table;
using VerticalAlignment = MigraDoc.DocumentObjectModel.Tables.VerticalAlignment;

namespace RadialReview.Accessors {


	public partial class PdfAccessor {
		public static bool DEBUGGER = false && Config.IsLocal();



		public static async Task AddVTO(Document doc, AngularVTO vto, string dateformat, VtoPdfSettings settings) {
			if (vto.IncludeVision) {
				await AddVtoVision(doc, vto, dateformat, settings);
			}
			if (vto.IncludeTraction) {
				await AddVtoTraction(doc, vto, dateformat, settings);
			}
		}

		public static string HtmlMeasSection(string title, IVtoSectionHeader header, string listTitle, IEnumerable<string> list, bool number, string dateformat) {
			try {
				var outer = "";
				{
					var builder = "";
					builder += "<b>Future Date:</b>" + header.FutureDate.NotNull(x => x.Value.ToString(dateformat)) + "<br/>";
					foreach (var h in header.Headers) {
						builder += "<b>" + h.K + ":</b>" + h.V + "<br/>";
					}
					outer += HtmlSection(title, builder);
				}
				{
					outer += HtmlList(listTitle, list, number, "h4");
				}
				return outer;
			} catch (Exception e) {
				return "<i>Failed to generate section</i>";
			}
		}

		public static string HtmlList(string title, IEnumerable<string> items, bool numbered, string heading = "h3") {
			try {
				var builder = (numbered ? "<ol>" : "<ul>");
				foreach (var item in items) {
					builder += "<li>" + item + "</li>";
				}
				builder += (numbered ? "</ol>" : "</ul>");
				return HtmlSection(title, builder, heading);

			} catch (Exception e) {
				return "<i>Failed to generate section</i>";
			}
		}

		private static string HtmlSection(string title, string body, string heading = "h3") {
			try {
				var builder = "";
				builder += "<" + heading + ">" + title + "</" + heading + ">";
				builder += "<p>" + body + "</p>";
				return builder;
			} catch (Exception e) {
				return "<i>Failed to generate section</i>";
			}
		}

		private static async Task<Section> AddVtoPage(Document doc, string docName, string pageName, VtoPdfSettings settings) {
			Section section;

			section = doc.AddSection();
			await AddVtoPageToSection(section, docName, pageName, settings);

			return section;
		}

		private static async Task AddVtoPageToSection(Section section, string docName, string pageName, VtoPdfSettings settings) {
			section.PageSetup.Orientation = Orientation.Landscape;
			section.PageSetup.PageFormat = PageFormat.Letter;


			var paragraph = new Paragraph();
			var p = section.Footers.Primary.AddParagraph("© 2022 - " + DateTime.UtcNow.Year + " Bloom Growth. All Rights Reserved.");
			p.Format.LeftIndent = Unit.FromPoint(14);

			section.Footers.Primary.Format.Font.Size = 10;
			section.Footers.Primary.Format.Font.Name = "Arial Narrow";
			section.Footers.Primary.Format.Font.Size = 8;
			section.Footers.Primary.Format.Font.Color = settings.LightTextColor;

			section.PageSetup.LeftMargin = Unit.FromInch(.3);
			section.PageSetup.RightMargin = Unit.FromInch(.3);
			section.PageSetup.TopMargin = Unit.FromInch(.2);
			section.PageSetup.BottomMargin = Unit.FromInch(.5);

			var title = section.AddTable();
			title.AddColumn(Unit.FromInch(0.05));
			title.AddColumn(Unit.FromInch(2.22));
			title.AddColumn(Unit.FromInch(10.07 - 2.22));

			var titleRow = title.AddRow();
			try {
				var image = await settings.GetImage();

				var img = titleRow.Cells[1].AddImage(image.Base64);
				if (image.Width > image.Height) {
					img.Width = Unit.FromInch(1.95);
				} else {
					img.Height = Unit.FromInch(1.75);
				}

			} catch (Exception) {
			}
			titleRow.Cells[1].VerticalAlignment = VerticalAlignment.Center;
			titleRow.Cells[1].Format.Alignment = ParagraphAlignment.Left;
			titleRow.Height = Unit.FromInch(1.787);

			var titleTable = titleRow.Cells[2].Elements.AddTable();
			titleTable.AddColumn(Unit.FromInch(10.07 - 2.22));
			var trow = titleTable.AddRow();
			trow.TopPadding = Unit.FromInch(.1);
			trow.BottomPadding = Unit.FromInch(.14);

			paragraph = trow.Cells[0].AddParagraph("THE BUSINESS PLAN");
			paragraph.Format.Font.Size = 32;
			paragraph.Format.Alignment = ParagraphAlignment.Center;
			paragraph.Format.Font.Name = "Arial Narrow";

			trow = titleTable.AddRow();

			var frame = trow.Cells[0].AddTextFrame();
			frame.Height = Unit.FromInch(0.38);
			frame.Width = Unit.FromInch(5.63);

			frame.MarginRight = Unit.FromInch(1);
			frame.MarginLeft = Unit.FromInch(1.15);
			frame.MarginTop = Unit.FromInch(.05);


			var box = frame.AddTable();
			box.Borders.Color = settings.BorderColor;
			box.Borders.Width = Unit.FromPoint(.75);
			box.LeftPadding = Unit.FromInch(.1);

			var size = Unit.FromInch(5.63);
			var c = box.AddColumn(size);
			c.Format.Alignment = ParagraphAlignment.Left;
			var rr = box.AddRow();
			rr.Cells[0].AddParagraph(docName);
			rr.Format.Font.Size = 16;
			rr.Format.Font.Bold = true;
			rr.Format.Font.Name = "Arial Narrow";
			rr.HeightRule = RowHeightRule.Exactly;
			rr.VerticalAlignment = VerticalAlignment.Center;
			rr.Height = Unit.FromInch(0.38);

			frame = trow.Cells[0].AddTextFrame();
			frame.Height = Unit.FromInch(0.38);
			frame.Width = Unit.FromInch(5.63);

			frame.MarginTop = Unit.FromInch(.05);

			p = frame.AddParagraph();
			p.Format.Alignment = ParagraphAlignment.Center;
			p.Format.LeftIndent = Unit.FromInch(2);
			p.Format.SpaceBefore = Unit.FromInch(.11);
			var ft = p.AddFormattedText(pageName, TextFormat.Bold | TextFormat.Underline);
			ft.Font.Size = 20;
			ft.Font.Name = "Arial Narrow";
		}

		private static Cell FormatParagraph(string title, ResizeContext ctx, VtoPdfSettings settings) {
			var container = ctx.Container;
			var vars = ctx.Variables;
			var table = new Table();
			container.Add(table);
			table.AddColumn(VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);
			table.AddColumn(VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH);
			table.Rows.LeftIndent = 0;
			table.LeftPadding = 0;
			table.RightPadding = 0;
			table.Borders.Bottom.Width = 1;
			table.Borders.Bottom.Color = settings.BorderColor;

			var row = table.AddRow();
			var titleCell = row.Cells[0];
			titleCell.Borders.Bottom.Color = settings.BorderColor;
			titleCell.Borders.Right.Color = settings.BorderColor;
			titleCell.Borders.Bottom.Width = 1;
			titleCell.Format.Font.Bold = true;
			titleCell.Format.Font.Size = 14;
			titleCell.Shading.Color = settings.FillColor;
			titleCell.Format.Font.Color = settings.FillTextColor;

			titleCell.AddParagraph(title);

			titleCell.VerticalAlignment = VerticalAlignment.Center;
			titleCell.Format.Alignment = ParagraphAlignment.Center;


			var contentsTable = new Table();


			contentsTable.AddColumn(VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH - VtoVisionDocumentGenerator.CELL_PADDING);
			var contentCell = contentsTable.AddRow().Cells[0];
			row.Cells[1].Elements.Add(contentsTable);
			AddPadding(vars, contentCell);

			return contentCell;
		}

		private static void AddPadding(ResizeContext ctx, bool left) {
			AddPadding(ctx.Variables, ctx.Container, false, false);
		}

		private static void AddPadding(RangedVariables vars, Cell contentCell, bool top = true, bool bottom = true) {
			contentCell.Format.Font.Size = vars.Get("FontSize");

			contentCell.Borders.Left.Width = VtoVisionDocumentGenerator.CELL_PADDING;
			contentCell.Borders.DistanceFromRight = VtoVisionDocumentGenerator.CELL_PADDING;
			contentCell.Borders.Right.Width = VtoVisionDocumentGenerator.CELL_PADDING;


			if (top) {
				contentCell.Borders.Top.Width = vars.Get("Spacer");
			}

			if (bottom) {
				contentCell.Borders.Bottom.Width = vars.Get("Spacer");
			}

			contentCell.Borders.Color = Colors.Transparent;

			if (PdfAccessor.DEBUGGER) {
				contentCell.Borders.Color = Colors.Blue;
			}
		}

		private static void AppendCoreValues(Cell cell, AngularVTO vto) {
			var values = vto.Values.Select(x => x.CompanyValue).ToList();
			foreach (var l in OrderedList(values, ListType.NumberList1)) {
				cell.Add(l);
			}
		}

		private static void AppendCoreFocus(Cell cell, AngularVTO vto) {
			var paragraphs = new List<Paragraph>();
			var purpose = new Paragraph();
			var text = (vto.NotNull(x => x.CoreFocus.PurposeTitle) ?? "Purpose/Cause/Passion").Trim().TrimEnd(':') + ": ";
			purpose.AddFormattedText(text, TextFormat.Bold);
			purpose.Format.Font.Name = "Arial Narrow";
			purpose.AddText(vto.NotNull(x => x.CoreFocus.Purpose) ?? "");
			paragraphs.Add(purpose);

			purpose.Format.SpaceAfter = 7 * 1.5;

			var niche = new Paragraph();
			niche.AddFormattedText((vto.NotNull(x => x.CoreFocus.NicheTitle) ??"Our Niche").Trim().TrimEnd(':') + ": ", TextFormat.Bold);
			niche.AddText(vto.NotNull(x => x.CoreFocus.Niche) ?? "");
			niche.Format.Font.Name = "Arial Narrow";
			paragraphs.Add(niche);

			foreach (var p in paragraphs) {
				cell.Add(p);
			}
		}

		private static void AppendTenYear(Cell cell, AngularVTO vto) {
			var tenYear = new Paragraph();
			tenYear.Format.Font.Name = "Arial Narrow";
			tenYear.AddText(vto.NotNull(x => x.TenYearTarget) ?? "");
			cell.Add(tenYear);
		}

		private static void AppendMarketStrategy(ResizeContext ctx, Cell cell, AngularStrategy strat, VtoPdfSettings setting, bool isFirst, bool isLast, bool isOnly, ref bool addSpaceBefore) {
			var fs = ctx.Variables.Get("FontSize");

			var paragraphs = new List<Paragraph>();
			//Add spacer
			if (!isFirst) {
				var spacer = new Paragraph();
				spacer.Format.Borders.Top.Color = setting.LightBorderColor;
				paragraphs.Add(spacer);
			}

			//Add strategy title
			if (!isOnly && !string.IsNullOrWhiteSpace(strat.Title)) {
				var title = new Paragraph();
				if (!isFirst && addSpaceBefore) {
					title.Format.SpaceBefore = fs * 1.5;
				}
				title.Format.SpaceAfter = fs * 1;
				title.AddFormattedText(strat.Title ?? "", TextFormat.Bold | TextFormat.Underline);
				title.Format.Font.Name = "Arial Narrow";
				paragraphs.Add(title);
			}

			//Target Market List
			if (!string.IsNullOrWhiteSpace(strat.TargetMarket)) {
				var theList = new Paragraph();
				theList.Format.Font.Name = "Arial Narrow";
				theList.AddFormattedText( "Who is your ideal client?: ", TextFormat.Bold);
				theList.AddText(strat.TargetMarket ?? "");
				paragraphs.Add(theList);
			}

			//Three Uniques
			var uniques = strat.NotNull(x => x.Uniques.ToList()) ?? new List<AngularVtoString>();
			if (uniques.Any(x => !string.IsNullOrWhiteSpace(x.Data))) {
				var uniquePara = new Paragraph();
				uniquePara.Format.SpaceBefore = fs * 1.25;
				var uniquesTitle = "Differentiator: ";
				if (uniques.Count == 3) {
					uniquesTitle = "Differentiators: ";
				}

				uniquePara.AddFormattedText(uniquesTitle, TextFormat.Bold);
				uniquePara.Format.Font.Name = "Arial Narrow";
				paragraphs.Add(uniquePara);
				paragraphs.AddRange(OrderedList(uniques.Select(x => x.Data), ListType.NumberList1, Unit.FromInch(.44), setFontSize: false));
			}

			//Proven Process
			if (!string.IsNullOrEmpty(strat.ProvenProcess)) {
				var provenProcess = new Paragraph();
				provenProcess.Format.SpaceBefore = fs * 1.25;

				if (!isOnly && string.IsNullOrEmpty(strat.Guarantee)) {
					provenProcess.Format.SpaceAfter = fs * 1.25;
				}
				provenProcess.AddFormattedText( "Sharing your process: ", TextFormat.Bold);
				provenProcess.Format.Font.Name = "Arial Narrow";
				provenProcess.AddText(strat.ProvenProcess ?? "");
				paragraphs.Add(provenProcess);
			}

			//Guarantee
			if (!string.IsNullOrEmpty(strat.Guarantee)) {
				var guarantee = new Paragraph();
				guarantee.Format.SpaceBefore = fs * 1.25;

				if (!isOnly) {
					guarantee.Format.SpaceAfter = fs * 1.25;
				}

				guarantee.AddFormattedText("Your product promise: ", TextFormat.Bold);
				guarantee.Format.Font.Name = "Arial Narrow";
				guarantee.AddText(strat.Guarantee ?? "");
				paragraphs.Add(guarantee);
			}

			addSpaceBefore = false;
			if (string.IsNullOrEmpty(strat.ProvenProcess) && string.IsNullOrEmpty(strat.ProvenProcess)) {
				addSpaceBefore = true;
			}

			foreach (var p in paragraphs) {
				cell.Add(p);
			}
		}

		private static IEnumerable<IHint> GenerateThreeYearHints(AngularVTO vto, string dateFormat) {

			var header = new ResizableElement((ctx) => {
				var c = ctx.Container;
				var v = ctx.Variables;
				c.Borders.Left.Width = VtoVisionDocumentGenerator.CELL_PADDING;
				c.Borders.Right.Width = VtoVisionDocumentGenerator.CELL_PADDING;
				c.Borders.Color = Colors.Transparent;

				var fs = v.Get("FontSize");
				var paragraphs = AddVtoSectionHeader(vto.ThreeYearPicture, fs, dateFormat);
				foreach (var a in paragraphs) {
					c.Add(a);
				}

				var p = new Paragraph();
				p.AddFormattedText("What does it look like?", TextFormat.Bold | TextFormat.Underline);
				p.Format.Font.Name = "Arial Narrow";
				p.Format.Font.Size = fs;
				c.Add(p);
			});

			yield return new Hint(VtoVisionDocumentGenerator.RIGHT_COLUMN, header);

			var looksLike = vto.ThreeYearPicture.LooksLike.Where(x => !string.IsNullOrWhiteSpace(x.Data)).Select(x => x.Data).ToList();
			var listing = OrderedList(looksLike, ListType.BulletList1);

			var looksLikeHints = listing.Select((ll, i) => {
				return new StaticElement(c => {
					c.Container.Add(ll.Clone());
				});
			}).Select(x => {
				return new Hint(VtoVisionDocumentGenerator.RIGHT_COLUMN, x);
			});

			foreach (var h in looksLikeHints) {
				yield return h;
			}
		}

		private static void AppendRowTitle(Row row, string title, VtoPdfSettings settings) {
			var cvTitle = row.Cells[0];
			row.Borders.Bottom.Color = settings.BorderColor;
			row.Borders.Right.Color = settings.BorderColor;
			cvTitle.Shading.Color = settings.FillColor;
			cvTitle.Format.Font.Bold = true;
			cvTitle.Format.Font.Size = 14;
			cvTitle.Format.Font.Name = "Arial Narrow";
			cvTitle.AddParagraph(title ?? "");
			cvTitle.Format.Alignment = ParagraphAlignment.Center;
			row.VerticalAlignment = VerticalAlignment.Center;
		}

		public static async Task AddVtoVision(Document doc, AngularVTO vto, string dateFormat, VtoPdfSettings settings) {
			var timeout = new TimeoutCheck(TimeSpan.FromSeconds(settings.MaxSeconds ?? 20));
			settings = settings ?? new VtoPdfSettings();
			var visionLayoutGenerator = new VtoVisionDocumentGenerator(vto, settings);

			var vars = new RangedVariables();
			vars.Add("FontSize", 10, 6, 10);
			vars.Add("Spacer", Unit.FromInch(.25), Unit.FromInch(.05), Unit.FromInch(8));

			var coreValueTitle = vto.NotNull(x => x.CoreValueTitle) ?? "CORE VALUES";
			var coreFocusTitle = vto.NotNull(x => x.CoreFocus.CoreFocusTitle) ?? "FOCUS";
			var tenYearTitle = vto.NotNull(x => x.TenYearTargetTitle) ?? "BHAG";
			var marketingStrategyTitle = vto.NotNull(x => x.Strategy.MarketingStrategyTitle) ?? "MARKETING STRATEGY";

			var coreValuesPanel = new ResizableElement((c) => { AppendCoreValues(FormatParagraph(coreValueTitle, c, settings), vto); });
			var coreFocusPanel = new ResizableElement((c) => { AppendCoreFocus(FormatParagraph(coreFocusTitle, c, settings), vto); });
			var tenYearPanel = new ResizableElement((c) => { AppendTenYear(FormatParagraph(tenYearTitle, c, settings), vto); });
			var marketingStrategyPanel = GenerateMarketingStrategyHints_Old(VtoVisionDocumentGenerator.LEFT_COLUMN, marketingStrategyTitle, vto, settings);

			var hints = new List<IHint>();
			hints.Add(new Hint(VtoVisionDocumentGenerator.LEFT_COLUMN, coreValuesPanel));
			hints.Add(new Hint(VtoVisionDocumentGenerator.LEFT_COLUMN, coreFocusPanel));
			hints.Add(new Hint(VtoVisionDocumentGenerator.LEFT_COLUMN, tenYearPanel));
			hints.AddRange(marketingStrategyPanel);

			hints.AddRange(GenerateThreeYearHints(vto, dateFormat));
			var result = LayoutOptimizer.Optimize(visionLayoutGenerator, hints, vars, timeout);

			LayoutOptimizer.Draw(doc, result);
		}

		#region ignore

		public static async Task AddVtoTraction(Document doc, AngularVTO vto, string dateformat, VtoPdfSettings settings) {
			settings = settings ?? new VtoPdfSettings();
			Unit baseHeight = Unit.FromInch(5.0);
			var vt = await AddPage_VtoTraction(doc, vto, settings, baseHeight);
			Cell oneYear = vt.oneYear, quarterlyRocks = vt.quarterlyRocks, issuesList = vt.issuesList;
			Table issueTable = vt.issueTable, rockTable = vt.rockTable, goalTable = vt.goalTable;


			#region One Year Plan
			Unit fs = 10;
			var goalObjects = new List<DocumentObject>();
			var goalsSplits = new List<Page>();
			var goalRows = new List<Row>();
			var goalParagraphs = new List<Paragraph>();
			{
				var oneYearPlan = vto.OneYearPlan ?? new AngularOneYearPlan();
				oneYearPlan.GoalsForYear = oneYearPlan.GoalsForYear ?? new List<AngularVtoString>();

				var goals = oneYearPlan.GoalsForYear.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();



				goalObjects.AddRange(AddVtoSectionHeader(oneYearPlan, fs, dateformat).ToList());
				var gfy = new Paragraph();

				gfy.Format.Font.Size = fs;
				gfy.Format.Font.Name = "Arial Narrow";
				gfy.AddFormattedText("Goals for the Year:", TextFormat.Bold);
				goalObjects.Add(gfy);


				var headerHeight = TestHeight(Unit.FromInch(3.47), AddVtoSectionHeader(oneYearPlan, fs, dateformat));








				var minRowHeight = Unit.FromInch(0.2444 * fs.Point / 10);

				for (var i = 0; i < goals.Count; i++) {
					var r = new Row();
					r.Height = minRowHeight;
					r.HeightRule = RowHeightRule.AtLeast;
					var p = r.Cells[0].AddParagraph("" + (i + 1) + ".");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					p.Format.Alignment = ParagraphAlignment.Right;
					p = r.Cells[1].AddParagraph(goals[i] ?? "");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					goalRows.Add(r);
					goalParagraphs.Add(p);
				}

				var headerSize = headerHeight;
				Unit pg1Height = baseHeight - headerSize;
				goalsSplits = SplitHeights(Unit.FromInch(3), new[] { pg1Height, (baseHeight) }, goalParagraphs, elementAtLeast: minRowHeight);


				goalObjects.Add(goalTable);
			}
			#endregion
			#region Rocks
			var rockObjects = new List<DocumentObject>();
			var rockSplits = new List<Page>();
			var rockRows = new List<Row>();
			var rockParagraphs = new List<Paragraph>();
			{
				var vtoQuarterlyRocks = vto.QuarterlyRocks ?? new AngularQuarterlyRocks();
				vtoQuarterlyRocks.Rocks = vtoQuarterlyRocks.Rocks ?? new List<AngularVtoRock>();

				var rocks = vtoQuarterlyRocks.Rocks.Where(x => !String.IsNullOrWhiteSpace(x.Rock.Name)).ToList();
				quarterlyRocks.Format.LeftIndent = Unit.FromInch(.095);
				rockObjects.AddRange(AddVtoSectionHeader(vtoQuarterlyRocks, fs, dateformat));
				var gfy = new Paragraph();

				gfy.Format.Font.Size = fs;
				gfy.Format.Font.Name = "Arial Narrow";
				gfy.AddFormattedText("Goals for the Quarter:", TextFormat.Bold);
				rockObjects.Add(gfy);


				var headerHeight = TestHeight(Unit.FromInch(3.47), AddVtoSectionHeader(vtoQuarterlyRocks, fs, dateformat));



				var minRowHeight = Unit.FromInch(0.2444 * fs.Point / 10);

				for (var i = 0; i < rocks.Count; i++) {
					var r = new Row();
					r.Height = minRowHeight;
					r.HeightRule = RowHeightRule.AtLeast;
					var p = r.Cells[0].AddParagraph("" + (i + 1) + ".");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					p.Format.Alignment = ParagraphAlignment.Right;
					p = r.Cells[1].AddParagraph(rocks[i].Rock.Name ?? "");
					rockParagraphs.Add(p);
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					p = r.Cells[2].AddParagraph(rocks[i].Rock.Owner.NotNull(X => X.Initials) ?? "");
					p.Format.SpaceBefore = Unit.FromPoint(2);
					p.Format.Alignment = ParagraphAlignment.Center;
					p.Format.Font.Size = fs;
					p.Format.Font.Name = "Arial Narrow";
					rockRows.Add(r);

				}

				var headerSize = headerHeight;
				Unit pg1Height = baseHeight - headerSize;
				rockSplits = SplitHeights(Unit.FromInch(2.6), new[] { pg1Height, (baseHeight) }, rockParagraphs, elementAtLeast: minRowHeight);
				rockObjects.Add(rockTable);

			}
			#endregion
			#region Issues
			var issuesObjects = new List<DocumentObject>();
			var issueSplits = new List<Page>();
			var issueRows = new List<Row>();
			var issueParagraph = new List<Paragraph>();
			{
				var vtoIssues = vto.Issues ?? new List<AngularVtoString>();

				var issues = vtoIssues.Select(x => x.Data).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();


				if (issues.Any()) {


					var rspace = issueTable.AddRow();
					rspace.Height = Unit.FromInch(0.095);
					rspace.HeightRule = RowHeightRule.Exactly;
					rspace.Borders.Left.Visible = false;
					rspace.Borders.Right.Visible = false;
					rspace.Borders.Top.Visible = false;

					var minRowHeight = Unit.FromInch(0.2444 * fs.Point / 10);
					for (var i = 0; i < issues.Count; i++) {
						var r = new Row();
						r.Height = minRowHeight;
						r.HeightRule = RowHeightRule.AtLeast;
						var p = r.Cells[0].AddParagraph("" + (i + 1) + ".");
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						p.Format.Alignment = ParagraphAlignment.Right;
						p = r.Cells[1].AddParagraph(issues[i] ?? "");
						issueParagraph.Add(p);
						p.Format.Font.Size = fs;
						p.Format.Font.Name = "Arial Narrow";
						issueRows.Add(r);
					}

					var extraHeight = 0.51;
					Unit issueHeight = baseHeight - Unit.FromInch(0.095 * 2);

					issueSplits = SplitHeights(Unit.FromInch(3.0), new[] { (issueHeight), (issueHeight) }, issueParagraph, null , extraHeight, elementAtLeast: minRowHeight);
					issuesObjects.Add(issueTable);
				}

			}
			#endregion

			AppendAll(oneYear, goalObjects);
			AppendAll(quarterlyRocks, rockObjects);
			AppendAll(issuesList, issuesObjects);

			var maxPage = Math.Max(Math.Max(issueSplits.Count(), goalsSplits.Count()), rockSplits.Count());

			var curGoalI = 0;
			var curRockI = 0;
			var curIssueI = 0;

			for (var p = 0; p < maxPage; p++) {

				if (p < goalsSplits.Count()) {
					foreach (var r in goalsSplits[p]) {
						goalTable.Rows.Add(goalRows[curGoalI]);
						curGoalI++;
					}
				}

				if (p < rockSplits.Count()) {
					foreach (var r in rockSplits[p]) {
						rockTable.Rows.Add(rockRows[curRockI]);
						curRockI++;
					}
				}

				if (p < issueSplits.Count()) {
					foreach (var r in issueSplits[p]) {
						issueTable.Rows.Add(issueRows[curIssueI]);
						curIssueI++;
					}
				}

				if (p + 1 < maxPage) {
					var vt2 = await AddPage_VtoTraction(doc, vto, settings, baseHeight);
					oneYear = vt2.oneYear;
					quarterlyRocks = vt2.quarterlyRocks;
					issuesList = vt2.issuesList;
					issueTable = vt2.issueTable;
					rockTable = vt2.rockTable;
					goalTable = vt2.goalTable;
					AppendAll(oneYear, new DocumentObject[] { goalTable }.ToList());
					AppendAll(quarterlyRocks, new DocumentObject[] { rockTable }.ToList());
					AppendAll(issuesList, new DocumentObject[] { issueTable }.ToList());

				}
			}
		}

		private static Unit TestHeight(Unit width, List<Paragraph> cloneOfParagraphs) {
			var tempDoc = new Document();
			tempDoc.AddSection();
			tempDoc.LastSection.PageSetup.PageHeight = Unit.FromInch(1000);
			tempDoc.LastSection.PageSetup.PageWidth = width;
			tempDoc.LastSection.PageSetup.TopMargin = Unit.FromInch(0);
			tempDoc.LastSection.PageSetup.LeftMargin = Unit.FromInch(0);
			tempDoc.LastSection.PageSetup.RightMargin = Unit.FromInch(0);
			foreach (var p in cloneOfParagraphs) {
				tempDoc.LastSection.Add(p);
			}
			var tempRender = new DocumentRenderer(tempDoc);
			tempRender.PrepareDocument();
			var renderInfo = tempRender.GetRenderInfoFromPage(1);
			RenderInfo rr = renderInfo[renderInfo.Count() - 1];
			var headerHeight = rr.LayoutInfo.ContentArea.Y + rr.LayoutInfo.ContentArea.Height;
			return Unit.FromPoint(headerHeight);
		}

		public class VtoTractionFrame {
			public Cell oneYear { get; set; }
			public Cell quarterlyRocks { get; set; }
			public Cell issuesList { get; set; }
			public Table issueTable { get; set; }
			public Table rockTable { get; set; }
			public Table goalTable { get; set; }
		}

		private static async Task<VtoTractionFrame> AddPage_VtoTraction(Document doc, AngularVTO vto, VtoPdfSettings settings, Unit height) {
			var o = new VtoTractionFrame();

			var section = await AddVtoPage(doc, vto._TractionPageName ?? vto.Name ?? "", "SHORT-TERM FOCUS", settings);


			var oneYearPlan = vto.OneYearPlan ?? new AngularOneYearPlan();
			var quarterlyRocks = vto.QuarterlyRocks ?? new AngularQuarterlyRocks();

			var table = section.AddTable();
			table.AddColumn(Unit.FromInch(3.47));
			table.AddColumn(Unit.FromInch(3.47));
			table.AddColumn(Unit.FromInch(3.47));
			table.Borders.Color = settings.BorderColor;

			var tractionHeader = table.AddRow();
			tractionHeader.KeepWith = 1;
			tractionHeader.Shading.Color = settings.FillColor;
			tractionHeader.Height = Unit.FromInch(0.55);
			var paragraph = tractionHeader.Cells[0].AddParagraph(oneYearPlan.OneYearPlanTitle ?? "1-YEAR GOALS");
			paragraph.Format.Font.Name = "Arial Narrow";
			paragraph.Format.Font.Size = 14;
			paragraph.Format.Font.Bold = true;
			paragraph.Format.Alignment = ParagraphAlignment.Center;

			tractionHeader.Cells[0].VerticalAlignment = VerticalAlignment.Center;


			paragraph = tractionHeader.Cells[1].AddParagraph(quarterlyRocks.RocksTitle ?? "GOALS");
			paragraph.Format.Font.Name = "Arial Narrow";
			paragraph.Format.Font.Size = 14;
			paragraph.Format.Font.Bold = true;
			paragraph.Format.Alignment = ParagraphAlignment.Center;
			tractionHeader.Cells[1].VerticalAlignment = VerticalAlignment.Center;

			paragraph = tractionHeader.Cells[2].AddParagraph(vto.IssuesListTitle ?? "LONG-TERM ISSUES");
			paragraph.Format.Font.Name = "Arial Narrow";
			paragraph.Format.Font.Size = 14;
			paragraph.Format.Font.Bold = true;
			paragraph.Format.Alignment = ParagraphAlignment.Center;
			tractionHeader.Cells[2].VerticalAlignment = VerticalAlignment.Center;

			var tractionData = table.AddRow();

			tractionData.Height = height;

			o.oneYear = tractionData.Cells[0];
			o.quarterlyRocks = tractionData.Cells[1];
			o.issuesList = tractionData.Cells[2];
			o.issueTable = new Table();
			o.issueTable.Borders.Color = settings.LightBorderColor;
			o.issueTable.AddColumn(Unit.FromInch(.28));
			o.issueTable.AddColumn(Unit.FromInch(3));

			o.rockTable = new Table();
			o.rockTable.Borders.Color = settings.LightBorderColor;
			o.rockTable.AddColumn(Unit.FromInch(.28));
			o.rockTable.AddColumn(Unit.FromInch(2.6));
			o.rockTable.AddColumn(Unit.FromInch(.4));

			o.goalTable = new Table();
			o.goalTable.Borders.Color = settings.LightBorderColor;
			o.goalTable.AddColumn(Unit.FromInch(.28));
			o.goalTable.AddColumn(Unit.FromInch(3));

			return o;
		}


		private static List<Paragraph> OrderedList(IEnumerable<string> items, ListType type, Unit? leftIndent = null, bool setFontSize = true) {
			var o = new List<Paragraph>();
			var res = items.Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
			for (int idx = 0; idx < res.Count(); ++idx) {
				ListInfo listinfo = new ListInfo();
				listinfo.ContinuePreviousList = idx > 0;
				listinfo.ListType = type;
				var paragraph = new Paragraph();
				paragraph.AddText((res[idx] ?? "").Trim());
				paragraph.Format.Font.Name = "Arial Narrow";
				if (setFontSize) {
					paragraph.Format.Font.Size = 10;
				}
				paragraph.Style = "" + type;
				paragraph.Format.ListInfo = listinfo;

				paragraph.Format.SpaceAfter = 0;
				paragraph.Format.SpaceBefore = 0;

				leftIndent = leftIndent ?? Unit.FromInch(0.05);

				if (leftIndent != null) {
					var tabStopDist = Unit.FromInch(.15);
					paragraph.Format.TabStops.ClearAll();
					paragraph.Format.TabStops.AddTabStop(Unit.FromInch(leftIndent.Value.Inch + tabStopDist));
					paragraph.Format.FirstLineIndent = -1 * tabStopDist;
					paragraph.Format.LeftIndent = leftIndent.Value + tabStopDist;
				}
				o.Add(paragraph);
			}
			return o;
		}

		#endregion
		#region Old

























































		private class MarketingStrategyHint : Hint {

			public class MSHintGroups {
				public string Title { get; set; }
				public DefaultDictionary<Cell, MSHintGroup> Groups { get; set; }

				public MSHintGroups(string title) {
					Title = title;
					Groups = new DefaultDictionary<Cell, MSHintGroup>(x => new MSHintGroup(Title));

				}
			}

			public class MSHintGroup {
				public Table OuterContainer { get; set; }
				public Table InnerContainer { get; set; }
				public Cell TitleCell { get; set; }
				public string Title { get; set; }
				public int Rows = 0;

				public MSHintGroup(string title) {
					Title = title;
				}
			}

			public MarketingStrategyHint(string viewBox, MSHintGroups groups, VtoPdfSettings settings, params IElement[] elements) : base(viewBox, elements) {
				Groups = groups;
				Settings = settings;
			}

			public MSHintGroups Groups { get; set; }
			public VtoPdfSettings Settings { get; set; }


			public override void DrawElement(Container elementContents, Cell viewBoxContainer, int page) {

				var G = Groups.Groups[viewBoxContainer];

				Column resizeableColumn1 = null;
				Column resizeableColumn2 = null;


				if (G.OuterContainer == null) {
					G.OuterContainer = new Table();
					var t = G.OuterContainer;
					t.Rows.LeftIndent = 0;
					t.LeftPadding = 0;
					t.RightPadding = 0;

					t.AddColumn(VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);
					resizeableColumn1 = t.AddColumn(viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);
					viewBoxContainer.Elements.Add(t);
					var row = t.AddRow();
					AppendRowTitle(row, G.Title, Settings);
					row.Borders.Bottom.Width = 0;
					G.TitleCell = row.Cells[0];
					G.TitleCell.Format.Font.Color = Settings.FillTextColor;

					G.InnerContainer = new Table();
					G.InnerContainer.Rows.LeftIndent = 0;
					G.InnerContainer.LeftPadding = 0;
					G.InnerContainer.RightPadding = 0;
					resizeableColumn2 = G.InnerContainer.AddColumn(viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH);

					row.Cells[1].Elements.Add(G.InnerContainer);


					row.Cells[1].VerticalAlignment = VerticalAlignment.Center;

				} 

				G.Rows += 1;
				var r = G.InnerContainer.AddRow();

				if (resizeableColumn1 != null && resizeableColumn2 != null && (viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH) != resizeableColumn1.Width) {
					resizeableColumn1.Width = viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH;
					resizeableColumn2.Width = viewBoxContainer.Column.Width - VtoVisionDocumentGenerator.LEFT_TITLE_WIDTH;
				}

				base.DrawElement(elementContents, r.Cells[0], page);
				if (HasError) {
					try {
						elementContents.Rows[0].Cells[0].Format.Font.Size = 6;
					} catch (Exception) {
						int i = 0;
					}
				}
			}

		}
		private static IEnumerable<IHint> GenerateMarketingStrategyHints_Old(string viewBoxName, string title, AngularVTO vto, VtoPdfSettings setting) {
			if (vto.NotNull(x => x.Strategies) != null) {
				var stratCount = vto.Strategies.Count();
				var spaceBefore = true;
				var group = new MarketingStrategyHint.MSHintGroups(title);
				for (var i = 0; i < stratCount; i++) {
					var strat = vto.Strategies.ElementAt(i);
					var isOnly = stratCount == 1;
					var paddOnlyOnce = i == 0;


					yield return new MarketingStrategyHint(viewBoxName, group, setting, new ResizableElement((ctx) => {
						var c = ctx.Container;
						var v = ctx.Variables;




						var isFirst = true;
						var isLast = true;

						AddPadding(v, c, isFirst, isLast);
						AppendMarketStrategy(ctx, c, strat, setting, isFirst, isLast, isOnly, ref spaceBefore);
					}, widthOverride: VtoVisionDocumentGenerator.LEFT_CONTENT_WIDTH));
				}
			}
		}
		#endregion
	}
}

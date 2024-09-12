using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Pdf;
using RadialReview.Accessors.PDF.AC;
using RadialReview.Accessors.PDF.Generators;
using RadialReview.Accessors.PDF.Generators.ViewModels;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Accessors.PDF;
using RadialReview.Controllers;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;
using RadialReview.Engines;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Organization;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Downloads;
using RadialReview.Models.L10;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.RealTime;
using QuarterlyController = RadialReview.Core.Controllers.QuarterlyController;

namespace RadialReview.Accessors.PDF.Partial {

  public class FooterData {
    public string Generated { get; set; }
    public string YearEnd { get; set; }
    public string CompanyName { get; set; }
  }

  public class QuarterlyPdf {
    private const string VIEW = "~/Views/Quarterly/PdfHtml.cshtml";

    public static QuarterlyController.PrintoutPdfOptions GetDefaultPdfOptions() {
      return new QuarterlyController.PrintoutPdfOptions {
        coverPage = true,
        issues = true,
        scorecard = true,
        rocks = true,
        vto = true,
        l10 = true,
        acc = true,
        pa = true,
        todos = false,
        headlines = false,
      };
    }

    [AutomaticRetry(Attempts = 0)]
    [Queue(HangfireQueues.Immediate.GENERATE_QUARTERLY_PRINTOUT)]
    public static async Task GeneratePdfStreamForRecurrence_Hangfire(HangfireCaller hangfire, long recurrenceId,
      QuarterlyController.PrintoutPdfOptions printoutOptions, PdfPageSettings pdfPageSettings,
      FileOutputMethod outputMethod, bool showNotifications, bool debug,
      PerformContext pc,
      [ActivateParameter] IHtmlRenderService renderService,
      [ActivateParameter] IBlobStorageProvider blobProvider
    ) {
      UserOrganizationModel caller;
      string recurName = "";
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          caller = s.Get<UserOrganizationModel>(hangfire.UserOrganizationId);
          recurName = s.Get<L10Recurrence>(recurrenceId).Name;
        }
      }
      using (var stream = new MemoryStream()) {
        await GeneratePdfStreamForRecurrence(caller, stream, renderService, recurrenceId, showNotifications, printoutOptions, status => pc.WriteLine(status));
        var tags = new List<TagModel>();
        tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, TagModel.Constants.QUARTERLY_PRINTOUT));

        if (printoutOptions.rocks)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Goals"));
        if (printoutOptions.headlines)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Headlines"));
        if (printoutOptions.vto)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Business Plan"));
        if (printoutOptions.acc)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Organizational Chart"));
        if (printoutOptions.l10)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Weekly Meeting Agenda"));
        if (printoutOptions.scorecard)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Metrics"));
        if (printoutOptions.todos)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Todos"));
        if (printoutOptions.issues)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "Issues"));
        if (printoutOptions.pa)
          tags.Add(TagModel.Create<L10Recurrence>(recurrenceId, "People Analyzer"));


        await FileAccessor.Save_Unsafe(blobProvider, hangfire.UserOrganizationId, stream,
          "Quarterly Printout - " + recurName, "pdf",
          "Quarterly Printout generated " + hangfire.GetCallerLocalTime().ToShortDateString(),
          FileOrigin.UserGenerate, outputMethod, ForModel.Create<L10Recurrence>(recurrenceId),
          FileNotification.NotifyCaller(hangfire.ConnectionId), null, tags.ToArray());
      }
    }


    private static IEnumerable<IPdfPageGenerator> GetGenerators(UserOrganizationModel caller, AngularOrganization organization, OrganizationModel orgModel, AngularRecurrence angRecur, AngularVTO angVto, TermsCollection terms, Chart<AngularAccountabilityChart> chart, List<string> values, QuarterlyController.PrintoutPdfOptions printoutOptions) {

      if (printoutOptions.coverPage) {
        var date = caller.GetTimeSettings().ConvertFromServerTime(DateTime.UtcNow).ToString(caller.GetTimeSettings().DateFormat);
        yield return new CoverPagePdfPageGenerator("Quarterly Report", organization.Name, date, organization.ImageUrl, organization.HasLogo, values);
      }

      if (printoutOptions.rocks) {
        yield return new RockPdfPageGenerator(organization, angRecur, angVto, angRecur.PrintOutRockStatus, terms);
      }

      if (printoutOptions.headlines) {
        yield return new HeadlinePdfPageGenerator(angRecur, organization, terms);
      }

      if (printoutOptions.vto) {
        yield return new BusinessPlanPdfPageGenerator(angVto, orgModel, caller.GetTimeSettings(), terms);
      }

      if (printoutOptions.acc) {
        var userIds = angRecur.Attendees.Select(x => x.Id);
        var userNodeIds = new List<long>();
        chart.data.Dive(x => {
          if (userIds.Any(uid => x.HasUser(uid))) {
            userNodeIds.Add(x.Id);
          }
        });

        var settings = new List<AccountabilityPrintoutSettings>() {
          new AccountabilityPrintoutSettings().ExpandAll().Compactify().Highlight(userNodeIds).PreparePdfViewport()
        };



        var additionalPages = AccountabilityChartPdfUtility.GetAllChildrenCharts(chart, n => userIds.Any(uid => n.HasUser(uid)), 2, 6);
        settings.AddRange(additionalPages);

        yield return new AccountabilityChartPdfPageGenerator(chart, settings, organization.Id, terms);
      }

      if (printoutOptions.todos) {
        yield return new TodoPdfPageGenerator(angRecur, organization, terms);
      }

      if (printoutOptions.issues) {
        yield return new IssuesPdfPageGenerator(angRecur, organization, terms);
      }

      if (printoutOptions.scorecard) {
        if (angRecur.Scorecard.Measurables.Any()) {
          yield return new ScorecardPdfPageGenerator(angRecur, organization, terms);
        }
      }

      if (printoutOptions.pa) {
        var angPeopleAnalyzer = QuarterlyConversationAccessor.GetVisiblePeopleAnalyzers(caller, caller.Id, angRecur.Id);
        yield return new PeopleAnalyzerPdfPageGenerator(caller.GetTimeSettings(), angPeopleAnalyzer, terms);
      }
    }



    public static async Task CreatePdfFromGenerators(IEnumerable<IPdfPageGenerator> generators, IHtmlRenderService renderService, Stream destination, StatusUpdater statusUpdater) {
      //Resolve once.
      var openStreams = new List<Stream>();
      try {
        generators = generators.ToList();

        var pageNums = new List<PageNumber>();
        var streams = new StreamAndMetaCollection();
        var coverGenerator = generators.FirstOrDefault(x => x is CoverPagePdfPageGenerator) as CoverPagePdfPageGenerator;
        var hasCover = coverGenerator != null;
        var curPage = !hasCover ? 1 : 2;
        var total = generators.Count() + 1;
        var i = 0;

        //Render non-cover pages
        foreach (var g in generators) {
          i += 1;
          var name = "unknown page";
          try {
            if (g is CoverPagePdfPageGenerator)
              continue;
            name = g.GetPageName();
            pageNums.Add(new PageNumber() {
              Page = curPage,
              Title = name
            });
            statusUpdater?.UpdateStatus(x => x.SetMessage("Adding " + name + "...").SetPercentage(i, total).SetTimeout(TimeSpan.FromMinutes(2)));
            var stream = new MemoryStream();
            openStreams.Add(stream);
            await g.GeneratePdf(renderService, stream);
            var pageCount = PdfEngine.GetPageCount(stream);
            streams.Add(new StreamAndMeta {
              Content = stream,
              DrawPageNumber = true,
              Name = name,
              Pages = pageCount
            });
            curPage += pageCount;
          } catch (Exception e) {
            statusUpdater?.UpdateStatus(x => x.SetMessage("Failed to add " + name).SetPercentage(i, total).SetTimeout(TimeSpan.FromMinutes(.1)));
          }
        }

        //Render cover page
        if (hasCover) {
          coverGenerator.SetPageNumbers(pageNums);
          var stream = new MemoryStream();
          openStreams.Add(stream);
          await coverGenerator.GeneratePdf(renderService, stream);
          streams.Insert(0, new StreamAndMeta {
            Content = stream,
            DrawPageNumber = false,
            Name = coverGenerator.GetPageName(),
            Pages = 1
          });
        }

        //Combine together
        await PdfEngine.Merge(streams, destination);
      } finally {
        foreach (var s in openStreams) {
          try {
            s.Dispose();
          } catch (Exception e) {
          }
        }
      }

    }


    public static async Task GeneratePdfStreamForRecurrence(UserOrganizationModel caller, Stream destination, IHtmlRenderService renderService, long recurrenceId, bool showNotifications,
      QuarterlyController.PrintoutPdfOptions printoutOptions, Action<string> messageLogger = null) {


      await using (var rt = RealTimeUtility.Create(showNotifications)) {
        var statusUpdater = rt.UpdateUsers(caller.UserName).GetStatusUpdater();

        if (messageLogger != null) {
          statusUpdater.OnStatusUpdate(status => {
            if (status.ShowMessage != false) {
              messageLogger(status.Message);
            }
          });
        }

        await statusUpdater.UpdateStatus(x => x.SetMessage("Gathering data").SetPercentage(0, 1).SetTimeout(TimeSpan.FromMinutes(2)));

        //Get data..
        var angRecur = await L10Accessor.GetOrGenerateAngularRecurrence(caller, recurrenceId, includeHistorical: false, forceIncludeTodoCompletion: true);
        var angVto = VtoAccessor.GetAngularVTO(caller, angRecur.VtoId.Value);
        var angOrg = new AngularOrganization(caller);
        var values = OrganizationAccessor.GetCompanyValues(caller, caller.Organization.Id).Select(v => v.CompanyValue).Where(cv => !string.IsNullOrEmpty(cv)).ToList();

        var tree = AccountabilityAccessor.GetTree(caller, caller.Organization.AccountabilityChartId);
        var chart = new Chart<AngularAccountabilityChart>(tree.Id) { height = "100%", width = "100%", data = tree };

        //Adjust data
        AdjustCompanyImage(angOrg);

        var terms = TermsAccessor.GetTermsCollection(caller, caller.Organization.Id);

        //Get generators
        var pageGenerators = GetGenerators(caller, angOrg, caller.Organization, angRecur, angVto, terms, chart, values, printoutOptions);

        //Run generators
        await CreatePdfFromGenerators(pageGenerators, renderService, destination, statusUpdater);

        statusUpdater?.UpdateStatus(x => x.SetMessage("Done.").SetPercentage(1, 1).SetTimeout(TimeSpan.FromMinutes(.1)));
      }

    }

    private static void AdjustCompanyImage(AngularOrganization angOrg) {
      angOrg.ImageUrl = GetImage(angOrg);
    }

    private static string GetImage(AngularOrganization organization) {
      if (string.IsNullOrEmpty(organization.ImageUrl))
        return CompanyLogoUrl();
      if (organization.ImageUrl.Contains("placeholder"))
        return CompanyLogoUrl();
      return organization.ImageUrl;
    }

    private static Stream DocumentToStream(object pdfDocument) {
      var pdfStream = new MemoryStream();
      // save to Stream
      if (pdfDocument is PdfDocument pdfDoc) {
        pdfDoc.Save(pdfStream, false);
      } else {
        // Migra Doc, use pdfRenderer
        PdfDocumentRenderer renderer = renderer = new PdfDocumentRenderer(true);
        if (pdfDocument is PdfDocumentRenderer docRenderer) {
          // override current renderer
          renderer = docRenderer;
        }
        // type of document to convert
        else if (pdfDocument is Document migraDoc) {
          renderer.Document = migraDoc;
          renderer.RenderDocument();
        } else {
          throw new ArgumentException();
        }
        renderer.Save(pdfStream, false);
      }
      return pdfStream;
    }



    private static string CompanyLogoUrl() {
      return "~/Content/img/BloomGrowth_Logo.png";
    }

  }

}

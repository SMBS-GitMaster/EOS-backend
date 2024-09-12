using Hangfire;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;
using RadialReview.Engines;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Middleware.Services.HeadlessBrower;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using RadialReview.Models.Downloads;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static RadialReview.Core.Controllers.AccountabilityController;

namespace RadialReview.Accessors.PDF {


  public class AccountabilityChartPDF : BaseAccessor {




    public static async Task Generate(UserOrganizationModel caller, StreamAndMetaCollection destination, TermsCollection terms, IHtmlRenderService renderService, long chartId, List<AccountabilityPrintoutSettings> settings, StatusUpdater statusUpdater) {
      statusUpdater = statusUpdater ?? StatusUpdater.NoOp();
      try {
        var tree = AccountabilityAccessor.GetTree(caller, chartId);
        var c = new Chart<AngularAccountabilityChart>(tree.Id) { height = "100%", width = "100%", data = tree, };

        Action<GenerationStatus> status = (async x => await statusUpdater.UpdateStatus(y =>
          y.SetPercentage(x.CurrentStep, x.TotalSteps)
          .SetMessage("Generating page: " + x.CurrentPageNumber + "/" + x.TotalPages)
          .SetTimeout(TimeSpan.FromMinutes(2))));

        await RenderPages(destination, terms, renderService, c, caller.Organization.Id, settings, status);
      } catch (Exception e) {
        await statusUpdater.UpdateStatus(x => x.Failed());
        throw;
      }
    }

    public static async Task RenderPages(StreamAndMetaCollection destination, TermsCollection terms, IHtmlRenderService renderService, Chart<AngularAccountabilityChart> chart, long orgId, List<AccountabilityPrintoutSettings> settings, Action<GenerationStatus> status) {
      var json = AngularSerializer.Serialize(chart).SafeJsonSerializeString();
      var view = ViewUtility.RenderView("~/Views/Accountability/Chart.cshtml", new AccountabilityChartVM() {
        OrganizationId = orgId,
        ChartId = chart.Id,
        Json = json,
        CanEditHierarchy = true,
        IsVerified = true
      });
      view.ViewData["HasBaseController"] = true;
      var viewStr = await view.ExecuteAsync();
      await RenderPages(destination, terms, renderService, viewStr, settings, status);
    }

    private static string PRINT_READY_CLASS = "PrintReady";

    private static string CommandAsFunction(IEnumerator<AccountabilityChartAction> actionEnumerator) {
      if (!actionEnumerator.MoveNext()) {
        return $@"$('body').addClass('{PRINT_READY_CLASS}');";
      }

      string builder = "";
      var guid = "g" + Guid.NewGuid().ToString().Replace("-", "");
      var action = actionEnumerator.Current;
      builder += action.ExecuteScript;

      if (action.WaitForSelector != null) {
        //Wait for selector
        builder +=
$@"let {guid} = setInterval(function(){{
	if ($('{action.WaitForSelector}').length!=0){{
		console.log(""Found '{action.WaitForSelector}'. Continuing."");
		clearInterval({guid});
		/*Next command*/
";
      }
      //Add next command.
      builder += CommandAsFunction(actionEnumerator);


      if (action.WaitForSelector != null) {
        builder += $@"
	}}
}},50);";
      }


      return builder;
    }

    private static string BuildCommandScripts(List<AccountabilityChartAction> actions) {
      var builder = "<script> setTimeout(function(){";
      builder += $@"$('body').removeClass('{PRINT_READY_CLASS}');";
      builder += CommandAsFunction(actions.GetEnumerator());
      builder += "},10); </script>";
      return builder;
    }




    public static async Task RenderPages(StreamAndMetaCollection destination, TermsCollection terms, IHtmlRenderService renderService, string html, List<AccountabilityPrintoutSettings> settingsByPage, Action<GenerationStatus> status) {
      var pidx = 0;
      var totalPages = settingsByPage.Count;
      foreach (var page in settingsByPage) {
        pidx += 1;
        var copyHtml = html;
        var commandScripts = BuildCommandScripts(page.Actions);
        copyHtml += commandScripts;

        status?.Invoke(new GenerationStatus(GenerationStatusStep.PageStarted, pidx, totalPages));

        var fake_endpoint = "http://localhost/" + Guid.NewGuid();

        var offlineProvider = renderService.GetOfflineFileProvider();
        await offlineProvider.SetFile(uri => uri.ToString() == fake_endpoint ? FileResponse.FromHtml(copyHtml) : null);


        var pdf = new MemoryStream();
        await renderService.GeneratePdfFromOfflineUrl(pdf, fake_endpoint, new PdfGenerationSettings() {
          Orientation = PdfOrientation.Landscape,
          WaitForCssSelector = "." + PRINT_READY_CLASS

        });

        destination.Add(terms.GetTerm(TermKey.OrganizationalChart), pdf, PdfEngine.GetPageCount(pdf), false);
      }
    }


    [AutomaticRetry(Attempts = 0)]
    [Queue(HangfireQueues.Immediate.GENERATE_AC_PRINTOUT)]
    public static async Task GeneratePdf_Hangfire(
      HangfireCaller hangfire,
      long chartId,
      List<AccountabilityPrintoutSettings> settings,
      FileOutputMethod outputMethod,
      [ActivateParameter] IHtmlRenderService renderService,
      [ActivateParameter] IBlobStorageProvider bsp,
      bool showNotifications) {


      UserOrganizationModel caller;
      string orgName;
      TermsCollection terms;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          caller = s.Get<UserOrganizationModel>(hangfire.UserOrganizationId);
          var chart = s.Get<AccountabilityChart>(chartId);
          orgName = s.Get<OrganizationModel>(chart.OrganizationId).GetName();
          terms = TermsAccessor.GetTermsCollection_Unsafe(s, chart.OrganizationId);
        }
      }
      await using (var rt = RealTimeUtility.Create(showNotifications)) {
        var statusUpdater = rt.UpdateUsers(caller.Id).GetStatusUpdater();
        try {
          await statusUpdater.UpdateStatus(x => x.SetPercentage(0, 1).SetMessage("Gathering data.").SetTimeout(TimeSpan.FromMinutes(2)));

          //Heavy lifting
          using (var pages = new StreamAndMetaCollection()) {
            await Generate(caller, pages, terms, renderService, chartId, settings, statusUpdater);

            var tags = new List<TagModel>();
            tags.Add(TagModel.Create<AccountabilityChart>(chartId, "Organizational Chart"));

            await statusUpdater.UpdateStatus(x => x.SetMessage("Formatting").SetTimeout(TimeSpan.FromMinutes(2)));

            using (var pdfStream = new MemoryStream()) {
              await PdfEngine.Merge(pages, pdfStream);
              pdfStream.Position = 0;

              await statusUpdater.UpdateStatus(x => x.SetMessage("Sending...").SetTimeout(TimeSpan.FromMinutes(2)).SetPercentage(995, 1000));

              await FileAccessor.Save_Unsafe(bsp, hangfire.UserOrganizationId, pdfStream,
                terms.GetTerm(TermKey.OrganizationalChart) + " - " + orgName, "pdf",
                terms.GetTerm(TermKey.OrganizationalChart) +" generated " + hangfire.GetCallerLocalTime().ToShortDateString(),
                FileOrigin.UserGenerate, outputMethod, ForModel.Create<AccountabilityChart>(chartId),
                FileNotification.NotifyCaller(hangfire.ConnectionId), null, tags.ToArray());

              await statusUpdater.UpdateStatus(x => x.SetMessage("Sent.").SetTimeout(4000).SetPercentage(1, 1));
            }
          }
        } catch (Exception e) {
          await statusUpdater.UpdateStatus(x => x.Failed());
          throw;
        }
      }
    }
  }
}

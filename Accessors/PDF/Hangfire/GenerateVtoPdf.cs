using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Downloads;
using RadialReview.Models.L10;
using RadialReview.Models.UserModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static RadialReview.Accessors.PdfAccessor;
using Hangfire;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Hangfire.Activator;
using RadialReview.Accessors.PDF.Generators;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Accessors.PDF.Hangfire {
  public class GenerateVtoPdf {

    public class VtoSettings {
      public string fill { get; set; }
      public string border { get; set; }
      public string image { get; set; }
      public string filltext { get; set; }
      public string lighttext { get; set; }
      public string lightborder { get; set; }
      public string textColor { get; set; }
    }

    [Queue(HangfireQueues.Immediate.GENERATE_VTO)]
    [AutomaticRetry(Attempts = 0)]
    public async static Task GenerateVTO(HangfireCaller hangfire, long vtoId, FileOutputMethod method, VtoPdfSettings settings,
      [ActivateParameter] IBlobStorageProvider bsp,
      [ActivateParameter] IHtmlRenderService renderer,
      string CompanyImage = null
    ) {
      UserOrganizationModel caller;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          caller = s.Get<UserOrganizationModel>(hangfire.UserOrganizationId);
        }
      }



      var vto = VtoAccessor.GetAngularVTO(caller, vtoId);
      vto.CompanyImg = CompanyImage;

      var terms = TermsAccessor.GetTermsCollection(caller, caller.Organization.Id);
      var generator = new BusinessPlanPdfPageGenerator(vto, caller.Organization, caller.GetTimeSettings(), terms);

      using (var output = new MemoryStream()) {
        await generator.GeneratePdf(renderer, output);


        var tags = new List<TagModel>();
        if (vto.L10Recurrence.HasValue) {
          tags.Add(TagModel.Create<L10Recurrence>(vto.L10Recurrence.Value, "Business Plan"));
        }
        tags.Add(TagModel.Create("Business Plan"));
        output.Seek(0, SeekOrigin.Begin);


        await FileAccessor.Save_Unsafe(
          bsp,
          hangfire.UserOrganizationId,
          output,
          vto.Name, "pdf",
          terms.GetTerm(TermKey.BusinessPlan)+" generated " + hangfire.GetCallerLocalTime().ToShortDateString(),
          FileOrigin.UserGenerate,
          method,
          ForModel.Create<VtoModel>(vtoId),
          FileNotification.NotifyCaller(hangfire.ConnectionId),
          null, tags.ToArray());
        output.Close();
      }
    }
  }
}

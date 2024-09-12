using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Models.L10.L10Recurrence;
using RadialReview.Variables;
using RadialReview.Utilities.Encrypt;
using RadialReview.Models.VTO;
using RadialReview.Utilities.Synchronize;
using RadialReview.Utilities.NHibernate;
using RadialReview.Models.Issues;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.Models.Terms;
using RadialReview.Utilities.Hooks;

namespace RadialReview.Accessors {

  public class ModeList {
    public List<Mode> Modes { get; set; }
  }

  public class Mode {
    [Obsolete("use other constructor")]
    public Mode() {
    }

    public Mode(string id, string name, bool importLongTermIssues, bool hidden) {
      Id = id;
      Name = name;
      Enabled = true;
      ImportLongTermIssues = importLongTermIssues;
      Hidden = hidden;
    }

    public string Id { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public bool Hidden { get; set; }
    public bool ImportLongTermIssues { get; set; }
    public string InjectOnAllPages { get; set; }
    //public string ConcludePage { get; set; }
    public List<ModePageFormat> PageFormats { get; set; }

  }


  public class ModePageFormat {
    [Obsolete("use other constructor")]
    public ModePageFormat() { }
    public ModePageFormat(string title, L10PageType? pageType, decimal? minutes, string subheading, string html, string url) {
      PageType = pageType;
      Url = url;
      Title = title;
      Subheading = subheading;
      Html = html;
      Minutes = minutes;
    }

    public static ModePageFormat CreatePage(string title, L10PageType pageType, decimal minutes, string subheading = null) {
      return new ModePageFormat(title, pageType, minutes, subheading, null, null);
    }
    public static ModePageFormat CreateHtmlPage(string title, decimal minutes, string html, params SummaryNotes[] notes) {
      return new ModePageFormat(title, L10PageType.Html, minutes, null, html, null) {
        SummaryNotes = notes.NotNull(x => x.ToList())
      };
    }
    public static ModePageFormat CreateTitlePage(string title, decimal minutes, string subheading = null) {
      return new ModePageFormat(title, L10PageType.Empty, minutes, subheading, null, null);
    }

    public L10PageType? PageType { get; set; }
    public string Url { get; set; }
    public string Title { get; set; }
    public string Subheading { get; set; }
    public string Html { get; set; }
    public decimal? Minutes { get; set; }
    public List<SummaryNotes> SummaryNotes { get; set; }
  }

  public partial class L10Accessor : BaseAccessor {
    public static List<Mode> GetMeetingModes(TermsCollection terms, bool enabledOnly = true) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          return GetMeetingModes(s, terms, enabledOnly);
        }
      }
    }

    public static List<Mode> GetMeetingModes(ISession s, TermsCollection terms, bool enabledOnly) {
      var res = s.GetSettingOrDefault<ModeList>(Variable.Names.MEETING_MODE_FORMATS, () => ModeHelpers.GetDefaultModeList())
        .NotNull(x => x.Modes.Where(y => !enabledOnly || y.Enabled).ToList());

      if (res!=null) {
        foreach (var r in res) {
          foreach (var t in terms) {
            var name = r.Name;
            name = name.Replace(t.Default, t.Value ?? t.Default);
            name = name.Replace(t.KeyString, t.Value ?? t.Default);
            r.Name = name;
          }
        }
      }
      return res;

    }

    [Obsolete("Do not call within a session")]
    public static async Task<bool> RevertMode(UserOrganizationModel caller, long recurrenceId, bool revertIssues = false) {
      await SyncUtil.Lock(SyncAction.UpdateRecurrenceMode(recurrenceId).ToString(), caller._ClientTimestamp, async (s, lk) => {
        var perms = PermissionsUtility.Create(s, caller);
        perms.ViewL10Recurrence(recurrenceId);

        var r = s.Get<L10Recurrence>(recurrenceId);

        if (r.MeetingMode == null) {
          throw new PermissionsException("Already reverted.");
        }
        Mode oldMode = null;
        var oldModeName = r.MeetingMode;
        try {
          oldMode = GetMode(s, TermsCollection.DEFAULT, r.MeetingMode);
        } catch (Exception e) {
        }

        var now = DateTime.UtcNow;

        var pages = s.QueryOver<L10Recurrence.L10Recurrence_Page>()
          .Where(x => (x.DeleteTime == r.RestoreTime || x.DeleteTime == null) && x.L10RecurrenceId == recurrenceId)
          .List();

        foreach (var p in pages) {
          if (p.DeleteTime == null) {
            p.DeleteTime = now;
            s.Update(p);
          } else if (p.DeleteTime == r.RestoreTime) {
            p.DeleteTime = null;
            s.Update(p);
          } else {
            //huh?
          }
        }

        if (revertIssues) {
          try {
            var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                      .Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null)
                      .List();

            var prefix = ModeHelpers.LONGTERM_PREFIX;
            foreach (var issue in issues) {
              if (issue.Issue != null && issue.Issue.Message != null && issue.Issue.Message.StartsWith(prefix)) {
                var builder = IssuesAccessor.BuildEditIssueExecutor(issue.Id, issue.Issue.Message.SubstringAfter(prefix));
                await SyncUtil.ExecuteNonAtomically(s, perms, builder);
                //await IssuesAccessor.EditIssue(OrderedSession.Indifferent(s), perms, );
                await IssuesAccessor.MoveIssueToVto(s, perms, issue.Id, null);
              }
            }
          } catch (Exception e) {
            log.Error("Error reverting issues.", e);
          }
        }

        r.MeetingMode = null;
        r.RestoreTime = null;
        s.Update(r);
        await HooksRegistry.Each<IMeetingModeHook>((ss, x) => x.RevertL10Mode(ss, recurrenceId, oldMode, oldModeName));
      });
      return true;
    }

    public static async Task<bool> SetMode(UserOrganizationModel caller, TermsCollection terms, long recurrenceId, string mode, bool moveIssues = true) {
      await SyncUtil.Lock(SyncAction.UpdateRecurrenceMode(recurrenceId).ToString(), caller._ClientTimestamp, async (s, lk) => {

        var perms = PermissionsUtility.Create(s, caller);
        perms.ViewL10Recurrence(recurrenceId);

        var r = s.Get<L10Recurrence>(recurrenceId);

        if (string.IsNullOrWhiteSpace(mode))
          throw new PermissionsException("Cannot set to mode: null");

        if (r.MeetingInProgress != null)
          throw new PermissionsException("Cannot change a meeting in progress.");

        var now = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(r.MeetingMode)) {
          //Old mode does not exist. Mark existing pages as "deleted" and set restore time.
          r.RestoreTime = now;
        } else {
          //DO NOT ALTER THE RESTORE TIME.
          //Old mode exists. We must have changed from one mode to another. Delete current pages.
        }

        var pages = s.QueryOver<L10Recurrence.L10Recurrence_Page>()
            .Where(x => x.DeleteTime == null && x.L10RecurrenceId == recurrenceId)
            .List();
        foreach (var p in pages) {
          p.DeleteTime = now;
          s.Update(p);
        }
        r.MeetingMode = mode;

        Mode usingMode = GetMode(s, terms, mode);

        ModeHelpers.SaveModePages(s, perms, r.OrganizationId, recurrenceId, usingMode, now);

        if (usingMode.ImportLongTermIssues && moveIssues) {
          var vtoId = r.VtoId;
          var issueIds = s.QueryOver<VtoItem_String>()
                  .Where(x => x.Type == VtoItemType.List_Issues && x.Vto.Id == vtoId && x.DeleteTime == null)
                  .Select(x => x.Id)
                  .List<long>();

          var prefix = ModeHelpers.LONGTERM_PREFIX;
          foreach (var issueId in issueIds) {
            var issue = await IssuesAccessor.MoveIssueFromVto(s, perms, issueId);

            var name = issue.Issue.Message;
            if (name!=null && !name.StartsWith(prefix)) {
              name = prefix + name;
              var builder = IssuesAccessor.BuildEditIssueExecutor(issue.Id, name);
              await SyncUtil.ExecuteNonAtomically(s, perms, builder);
              //await IssuesAccessor.EditIssue(OrderedSession.Indifferent(s), perms, issue.Id, name);
            }
          }
        }

        s.Update(r);
        await HooksRegistry.Each<IMeetingModeHook>((ss, x) => x.StartL10Mode(ss, recurrenceId, usingMode));
        await HooksRegistry.Each<IMeetingEvents>((ss, x) => x.UpdateRecurrence(ss, caller, r));
        s.Flush();
      });
      return true;
    }

    public static Mode TryGetMode(TermsCollection terms, string mode) {
      if (mode == null)
        return null;

      try {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var m = GetMode(s, terms, mode);
            tx.Commit();
            s.Flush();
            return m;
          }
        }
      } catch (Exception) {
        return null;
      }
    }

    public static Mode GetMode(ISession s, TermsCollection terms, string mode) {

      var modeList = GetMeetingModes(s, terms, false);

      if (modeList == null)
        throw new PermissionsException("Mode list was null");

      var usingMode = modeList.FirstOrDefault(x => x.Id.NotNull(y => y.ToLower()) == mode.ToLower());
      if (usingMode == null) {
        throw new PermissionsException("Unknown mode:" + mode);
      }

      return usingMode;
    }

    public class ModeHelpers {
      public const string LONGTERM_PREFIX = "[Long–Term] ";

      public static bool SaveModePages(ISession s, PermissionsUtility perms, long orgId, long recurId, Mode mode, DateTime createTime) {
        if (mode == null) {
          throw new PermissionsException("Mode is null");
        }
        if (mode.PageFormats == null) {
          throw new PermissionsException("mode.PageFormats should be an array.");
        }
        foreach (var format in mode.PageFormats) {
          var page = ConstructModePage(format, orgId, recurId, createTime);
          L10Accessor.EditOrCreatePage(s, perms, page, true);
        }
        return true;

      }


      public class TemplateTransformer {
        private static string _modeSalt = "28382282-1B2D-4A3C-BE86-9DB0D25CD5AC";

        public TemplateTransformer(string pageTitle, DateTime createTime, long orgId, long recurId) {
          OrgGuid = "o" + Crypto.UniqueHash("" + orgId, _modeSalt);
          TeamGuid = "t" + Crypto.UniqueHash("" + recurId, _modeSalt);
          MeetingGuid = "m" + Crypto.UniqueHash(recurId + "_" + createTime.Ticks, _modeSalt);
          PageGuid = "p" + Crypto.UniqueHash(recurId + "_" + createTime.Ticks + "_" + pageTitle, _modeSalt);
          NotesUrl = Config.NotesUrl("p/");
          RecurId = "" + recurId;
          OrgId = "" + orgId;
        }

        private string OrgGuid { get; set; }
        private string TeamGuid { get; set; }
        private string MeetingGuid { get; set; }
        private string PageGuid { get; set; }
        private string NotesUrl { get; set; }
        private string RecurId { get; set; }
        private string OrgId { get; set; }

        public string Transform(string template) {
          if (template == null)
            return null;

          return template.Replace("{{ORG}}", OrgGuid)
                   .Replace("{{TEAM}}", TeamGuid)
                   .Replace("{{MEETING}}", MeetingGuid)
                   .Replace("{{PAGE}}", PageGuid)
                   .Replace("{{NOTES}}", NotesUrl)
                   .Replace("{{ID}}", RecurId)
                   .Replace("{{ORGID}}", OrgId);
        }
      }



      public static L10Recurrence_Page ConstructModePage(ModePageFormat pageFormat, long orgId, long recurId, DateTime createTime) {

        var transformer = new TemplateTransformer(pageFormat.Title, createTime, orgId, recurId);
        var subheading = (pageFormat.Subheading ?? "") + (pageFormat.Html ?? "");

        var res = new L10Recurrence_Page() {
          AutoGen = true,
          CreateTime = createTime,
          L10RecurrenceId = recurId,
          Minutes = Math.Max(0, pageFormat.Minutes ?? 5),
          PageType = pageFormat.PageType ?? L10PageType.Html,
          Subheading = transformer.Transform(subheading),
          Title = pageFormat.Title ?? "--",
          Url = transformer.Transform(pageFormat.Url),
        };

        if (pageFormat.SummaryNotes != null) {
          var summary = res.GetSummary();
          summary.SummaryNotes = pageFormat.SummaryNotes.Select(x => new SummaryNotes(transformer.Transform(x.Title), transformer.Transform(x.PadId))).ToList();
          res.SetSummary(summary);
        }

        return res;
      }



      /*	L10 = 0,
	FocusDay = 1,
	VisionBuilding1 = 2,
	VisionBuilding2 = 3,
	AnnualPlanning = 4
	*/

      public static ModeList GetDefaultModeList() {

        //QUARTERLY
        var quarterly = new Mode("quarterly", "Quarterly Mapping", true, false) {
          PageFormats = new List<ModePageFormat>() {
            ModePageFormat.CreateHtmlPage("Objectives", 5, "<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 60%;margin: auto;font-size:21px;font-family: 'Open Sans',sans-serif;\">\n\t\t\t<h2>Objectives for the day</h2>\n\t\t\t<ul>\n\t\t\t\t<li>We share the same vision and are on the same page.</li>\n\t\t\t\t<li>We have a clear plan for the next 90 days.</li>\n\t\t\t\t<li>We have solved the most important issues.</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n\t<br/>\n\t<br/>\n\t<div class=\"btn btn-success\" onclick=\"showPrintPdf()\">Print your quarterly data</div>\n</center>"),
            ModePageFormat.CreateHtmlPage("Check-in", 15, "<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 80%;margin: auto;font-size:21px;font-family: 'Open Sans',sans-serif;\">\n\t\t\t<h2>Check-in</h2>\n\t\t\t<ul>\n\t\t\t\t<li>hat are your personal and professional bests from the past 90 days?</li>\n\t\t\t\t<li>What has been working and not working these past 90 days?</li>\n\t\t\t\t<li>What are your expectations for this session today?</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">QP pg 1</span>"),
            ModePageFormat.CreatePage("Review Prior Quarter", L10PageType.Rocks, 30, "<div class='btn btn-success' onclick='$(\".vto-button\").click()'>View Business Plan</div>"),
            ModePageFormat.CreateHtmlPage("Business Plan Review", 60, "<iframe src=\"/l10/EditVto/{{ID}}?noheading=true\" frameborder=\"0\"  style=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;\"></iframe>"),
                        ModePageFormat.CreateHtmlPage("Quarterly Learning Opportunity", 60, "<style>\t\n\t.flex-eos-tools{\n\t\tdisplay: flex;   \n\t\tflex-wrap: wrap;\n\t    justify-content: flex-start;\n\t    align-content: stretch;\n\t    align-items: flex-start;\n\t\tmargin:auto;\n\t\twidth:90%;\n\t\tmargin:auto;\n\t\ttext-align:left;\n\t\tfont-size:20px;\n\t\tpadding-top:30px;\n\t\tfont-family:'Open Sans', sans-serif;\n\t}\t\n\t.flex-eos-tool{\n\t\twidth:380px;\n\t\tpadding:10px 20px;\n\t}\n</style>\n\n<div class='flex-eos-tools' style=\"\">\n\t<div class='flex-eos-tool'>The EOS Toolbox</div>\n\t<div class='flex-eos-tool'>The EOS Model</div> \n\t<div class='flex-eos-tool'>The Five Leadership Abilities</div>\n\t<div class='flex-eos-tool'>The Business Plan</div>\n\t<div class='flex-eos-tool'>The Accountability Chart</div>\n\t<div class='flex-eos-tool'>The Meeting Pulse</div>\n\t<div class='flex-eos-tool'>The Level 10 Meeting</div>\n\t<div class='flex-eos-tool'>The Issues Solving Track</div>\n\t<div class='flex-eos-tool'>Quarterly Goals</div>\n\t<div class='flex-eos-tool'>Goals – First Step Page</div>\n\t<div class='flex-eos-tool'>Company Metrics</div>\n\t<div class='flex-eos-tool'>The 8 Cash Flow Drivers</div>\n\t<div class='flex-eos-tool'>LMA Leadership Questionnaire</div>\n\t<div class='flex-eos-tool'>LMA Management Questionnaire</div>\n\t<div class='flex-eos-tool'>The People Analyzer</div>\n\t<div class='flex-eos-tool'>GWC</div>\n\t<div class='flex-eos-tool'>The 5-5-5</div>\n\t<div class='flex-eos-tool'>Clarity Break</div>\n\t<div class='flex-eos-tool'>Delegate and Elevate (4 Quadrants)</div>\n\t<div class='flex-eos-tool'>The Assistance Track</div>\n\t<div class='flex-eos-tool'>The Trust Builders</div>\n\t<div class='flex-eos-tool'>Kolbe Profiling</div>\n\t<div class='flex-eos-tool'>The 3-Step Process Documenter</div>\n\t<div class='flex-eos-tool'>Followed By All (FBA) Checklist</div>\n\t<div class='flex-eos-tool'>The H/R Process</div>\n\t<div class='flex-eos-tool'>“Back to the Basics” Checklist</div>\n\t<div class='flex-eos-tool'>“Off-line” Meeting Track</div>\n\t<div class='flex-eos-tool'>Partnership Rules of the Game</div>\n\t<div class='flex-eos-tool'>Sales Department Checkup</div>\n\t<div class='flex-eos-tool'>Merger / Acquisition “Fit”</div>\n\t<div class='flex-eos-tool'>Getting What You Want</div>\n\t<div class='flex-eos-tool'>Compartmentalizing</div>\t\n</div>"),
                        ModePageFormat.CreateHtmlPage("Establish Quarterly Goals", 120, "<iframe src=\"/l10/wizard/{{ID}}?noheading=true&nosidebar=true#/Rocks\"\n\t\tframeborder=\"0\"  \n\t\tstyle=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;\"\n></iframe>\t\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">QP pg 3-5</span>"),
            ModePageFormat.CreatePage("Issues List ", L10PageType.IDS, 180),
            ModePageFormat.CreateTitlePage("To-dos", 7),
            ModePageFormat.CreatePage("Wrap-up", L10PageType.Conclude, 8),
          },
          InjectOnAllPages = "<script>\n\t$('body').append('<style> .page-time.over{ color:#444!important; } .meeting-stats .row,.cascading-messages { display: none; } .meeting-stats:after { content: 'Thank you!';font-size: 20px;text-align: center;width:100%;display:block;padding-top: 60px;color: #666;font-family: 'Raleway', sans-serif;}</style>\")</script>",


        };

        var annual = new Mode("annual", "Annual Mapping", true, false) {
          PageFormats = new List<ModePageFormat>() {
            ModePageFormat.CreateHtmlPage("Objectives: Day 1",5,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:450px;padding:40px 10px 35px;\">\n\t\t<h2>Objectives</h2>\n\t\t<div style=\"text-align: left;width: 90%;margin: auto;\">\n\t\t\t<ul>\n\t\t\t\t<li>Increase team health</li>\n\t\t\t\t<li>Clear company vision</li>\n\t\t\t\t<li>Issues list clear</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n\n<style>\n\t.l10-page-title{\n\t\tvisibility:hidden;\n\t}\n</style>"),
            ModePageFormat.CreateHtmlPage("Check-in",30,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 80%;margin: auto;\">\n\t\t\t<h2>Check-in</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Share personal and professional bests</li>\n\t\t\t\t<li>Share an update on what’s working/ not working?</li>\n\t\t\t\t<li>Share your expectations</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n\n<style>\n\t.l10-page-title{\n\t\tvisibility:hidden;\n\t}\n</style>"),
            ModePageFormat.CreateHtmlPage("Review Prior Quarter",120,"<div class='btn btn-success pull-right' onclick='$(\".vto-button\").click()'>View Business Plan</div> <center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 40px 35px;\">\n\t\t<h2>Goals</h2>\n\t\t<h4>What are the 3-7 most important things to accomplish in the next 90 days?</h4>\n\t</div>\t\n</center>\n<br/>\n<br/>\n<center>\n\t<iframe src=\"/l10/wizard/{{ID}}?noheading=true&amp;nosidebar=true#/Rocks\" frameborder=\"0\" style=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;max-width: 1200px;\"></iframe>\n</center>\n\n\n<!--span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg 7-10</span-->"),
            ModePageFormat.CreateHtmlPage("Team Health",60,"<iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style>.meeting-page .centered{max-width:90%;width:90%;}</style>",
              new SummaryNotes("Team Health","{{PAGE}}")
            ),
            ModePageFormat.CreateHtmlPage("Organizational Check-Up",15,"<iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style>.meeting-page .centered{max-width:90%;width:90%;}</style>",
              new SummaryNotes("Organizational Check-Up","{{PAGE}}")
            ),
            ModePageFormat.CreateHtmlPage("S.W.O.T Analysis/Issues List",60,"<table class=\"table\">\n\t<tr><th>STRENGTHS</th><th>WEAKNESSES</th></tr>\n\t<tr>\n\t\t<td><iframe style='width:100%; height:calc(50vh - 130px);' src='{{NOTES}}{{PAGE}}_strengths'/></td>\n\t\t<td><iframe style='width:100%; height:calc(50vh - 130px);' src='{{NOTES}}{{PAGE}}_weakness'/></td>\n\t</tr>\n\t<tr><th>OPPORTUNITIES</th><th>THREATS</th></tr>\n\t<tr>\n\t\t<td><iframe style='width:100%; height:calc(50vh - 130px);' src='{{NOTES}}{{PAGE}}_opportunities'/></td>\n\t\t<td><iframe style='width:100%; height:calc(50vh - 130px);' src='{{NOTES}}{{PAGE}}_threats'/></td>\n\t</tr>\n</table>\n\n<style>\n\t.htmlpage th{\n\t\ttext-align:center;\n\t    padding-bottom: 0px !important;\n\t    color: #666;\n\t    text-decoration: underline;\n\t}\n\t\n\t.htmlpage td{\n\t    padding-top: 0px !important;\n\t}\n</style>",
              new SummaryNotes("Strengths","{{PAGE}}_strengths"),
              new SummaryNotes("Weaknesses","{{PAGE}}_weakness"),
              new SummaryNotes("Opportunities","{{PAGE}}_opportunities"),
              new SummaryNotes("Threats","{{PAGE}}_threats")
            ),
            ModePageFormat.CreateHtmlPage("Business Plan",30,"<iframe src=\"/l10/EditVto/{{ID}}?noheading=true\" frameborder=\"0\"  style=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;\"></iframe>"),
            ModePageFormat.CreateHtmlPage("Objectives: Day 2",5,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:450px;padding:40px 10px 35px;\">\n\t\t<h2>Objectives</h2>\n\t\t<div style=\"text-align: left;width: 90%;margin: auto;\">\n\t\t\t<ul>\n\t\t\t\t<li>Clear plan to achieve vision</li>\n\t\t\t\t<li>Clear plan for next quarter</li>\n\t\t\t\t<li>Resolve all key issues</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n\n<style>\n\t.l10-page-title{\n\t\tvisibility:hidden;\n\t}\n</style>"),
            ModePageFormat.CreateHtmlPage("3-Year Goals™",30,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 3-Year Goals on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/> <style> .meeting-page .centered{ max-width:90%; width:90%; }</style>",
              new SummaryNotes("3-Year Goals™","{{PAGE}}")
            ),
            ModePageFormat.CreateHtmlPage("1-Year Goals",30,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 1-Year Plan on VTO</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style> .meeting-page .centered{ max-width:90%; width:90%;}</style>",
              new SummaryNotes("1-Year Goals","{{PAGE}}")
            ),
            ModePageFormat.CreateHtmlPage("Establish Quarterly Goals", 120, "<div class='btn btn-success' onclick='currentPageType=\"Rocks\"; $(\".editMeeting-button\").click()'>Edit Goals</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style>.meeting-page .centered{ max-width:90%;width:90%;}</style><!--span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">QP pg 3-5</span-->",
              new SummaryNotes("Quarterly Goals","{{PAGE}}")
            ),
            ModePageFormat.CreatePage("IDS", L10PageType.IDS, 60),
            ModePageFormat.CreateHtmlPage("Next Steps",7,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:850px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 90%;margin: auto;\">\n\t\t\t<h2>Next Steps</h2>\n\t\t\t<ul style=\"text-align:left\" class=\"x-nextstep\">\n\t\t\t\t<li> Listen twice to Vision audio: EOS app</li>\n\t\t\t\t<li> Complete the Accountability Chart\t</li>\n\t\t\t\t<li> Complete your Rocks\t</li>\n\t\t\t\t<li> Do your weekly Level 10 Meetings\t</li>\n\t\t\t\t<li>Watch the Compartmentalize whiteboard video:\n\t\t\t\t\t<ul>\n\t\t\t\t\t\t<li><a href=\"https://www.eosworldwide.com/compartmentalizing\">https://www.eosworldwide.com/compartmentalizing</a></li>\n\t\t\t\t\t</ul>\n\t\t\t\t</li>\n\t\t\t\t<li>Prepare to share your Core Values</li>\n\t\t\t\t<li> Finish Good to Great</li>\n\t\t\t\t<li> Update your Business Plan\t</li>\n\t\t\t\t<li> Schedule your VB2 Session</li>\n\t\t\t\t<li> I will check in with you two times between now and the next session.</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n\n<style>\n\t.l10-page-title{\n\t\tvisibility:hidden;\n\t}\n</style>"),
            ModePageFormat.CreatePage("Wrap-up", L10PageType.Conclude, 8),

          }
        };


        var focusDay = new Mode("focusday", "Focus Day", false, false) {
          PageFormats = new List<ModePageFormat>() {
            ModePageFormat.CreateHtmlPage("Objectives", 5, "<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 90%;margin: auto;\">\n\t\t\t<h2>Objectives</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Have Fun</li>\n\t\t\t\t<li>Get you thinking and working “on” your business</li>\n\t\t\t\t<li>Understand “healthy <u>and</u> smart”</li>\n\t\t\t\t<li>Implement practical tools – increase traction, accountability, communication, team health, and results</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n\t<br/>\n\t<br/>\n\t<br/>\n\t<div class='component' style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<h2>My Goals</h2>\t\t\t\n\t\t<div style='text-align: left;width: 90%;margin: auto;'>\n\t\t\t<ul>\n\t\t\t\t<li>Put you in more control of your business</li>\n\t\t\t\t<li>Increase the value of your business</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>"),
            ModePageFormat.CreateHtmlPage("Check-in", 30, "<center>\n\t<div class='component' style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 80%;margin: auto;\">\n\t\t\t<h2>Check-in</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Name and Role (not title) – what you do</li>\n\t\t\t\t<li>Good News: Personal and Professional</li>\n\t\t\t\t<li>Share your expectations</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg 1</span>"),
            ModePageFormat.CreateHtmlPage("Hitting the Ceiling", 30, "<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 90%;margin: auto;\">\n\t\t\t<h2>The Journey</h2>\n\t\t\t<h4><b>Are you willing to become your best?</b></h4>\n\t\t</div>\n\t</div>\n\t<br/>\n\t<br/>\n\t<br/>\n\t<div class='centered'>\n\t\t<div class='component' style=\"display:inline-block;min-width:650px;padding:40px 10px 35px;\">\n\t\t\t<h2>The Five Leadership Abilities™</h2>\n\t\t\t<h4>\n\t\t\t\t<div style='text-align: left;width: 90%;margin: auto;'>\n\t\t\t\t\t<ol>\n\t\t\t\t\t\t<li>Simplify</li>\n\t\t\t\t\t\t<li>Delegate</li>\n\t\t\t\t\t\t<li>Predict</li>\n\t\t\t\t\t\t<li>Systemize</li>\n\t\t\t\t\t\t<li>Structure</li>\n\t\t\t\t\t</ol>\n\t\t\t\t</div>\n\t\t\t</h4>\n\t\t</div>\n\t</div>\n</center>\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg 2-3</span>"),
            ModePageFormat.CreateHtmlPage("The Organizational Chart", 180, "<iframe src=\"/accountability/chart?noheading=true\" frameborder=\"0\" style=\"left:0;right:0px;position:relative;width:100%;height: calc(100vh - 160px);border: none;border-radius: 23px;\"></iframe>\n\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg 4-5</span>\n\n<style>\n.htmlpage-contents{\n\tbox-shadow: inset 0px 2px 4px 1px #0000002e;\n    padding: 1px;\n    border-radius: 23px;\n    background: #e9e9e9;\n}\n</style>"),
            ModePageFormat.CreateHtmlPage("Goals", 120, "<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 40px 35px;\">\n\t\t<h2>Goals</h2>\n\t\t<h4>What are the 3-7 most important things to accomplish in the next 90 days?</h4>\n\t</div>\t\n</center>\n<br/>\n<br/>\n<center>\n\t<iframe src=\"/l10/wizard/{{ID}}?noheading=true&amp;nosidebar=true#/Rocks\" frameborder=\"0\" style=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;max-width: 1200px;\"></iframe>\n</center>\n\n\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg 7-10</span>"),
            ModePageFormat.CreateHtmlPage("The Meeting Pulse™", 45, "<script> $(\".meeting-page .component\").hide();   $(\".meeting-page\").append(\"<br/><center><img src=\\\"https://s3.amazonaws.com/Radial/base/Pictures/L10+Agenda.png\\\" width=824 height=1068 style=\\\"border:1px solid rgba(0,0,0,10%);box-shadow: 0 10px 10px 2px rgba(0,0,0,10%)\\\"/></center><span style=\\\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\\\">FD pg 11-12</span>\"); </script>"),
            ModePageFormat.CreateHtmlPage("Scorecard", 60, "<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 40px 35px;\">\n\t\t<h2>Scorecard</h2>\n\t\t<h4>What are our leading indicators?</h4>\n\t</div>\n</center>\n<br/>\n<br/>\n<center>\n<iframe src=\"/l10/wizard/{{ID}}?noheading=true&nosidebar=true#/Scorecard\"\nframeborder=\"0\"  \nstyle=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;max-width:1200px\"></iframe>\n</center>\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg  13-16</span>"),
            ModePageFormat.CreateHtmlPage("Next Steps", 7, "<center>\n\t<div class=\"component centered\" style=\"display:inline-block;min-width:550px;padding:40px 60px 40px;text-align:left\">\n\t\t<h2>Next Steps</h2>\n\t\t<ul style=\"text-align:left\" class=\"x-nextstep\"> \t\n\t\t\t<li> Add your company rocks and issues to the Business Plan\t</li>\n\t\t\t<li> Build Rock Sheet\t</li>\n\t\t \t<li> Focus on completing your Rocks\t</li>\n\t\t \t<li> Complete your Accountability Chart\t</li>\n\t\t \t<li> Listen to Focus audio twice: EOS app (app.eosworldwide.com)\t</li>\n\t\t \t<li> Watch the L10 whiteboard video: http://eosworldwide.com/level-10/\t</li>\n\t\t \t<li> Run Weekly Level 10 Meetings\t</li>\n\t\t\t<li> Create and use your Metrics\t</li>\n\t\t \t<li> Read the first five chapters of Good to Great with an emphasis on the 5th chapter, “Hedgehog Concept,” and the Harvard Business Press articles on Core Values.\t</li>\n\t\t</ul> \n\t</div>\n</center>\n\n<style>    \n.x-nextstep li{\n\tmargin-bottom: 12px; \t\n} \t \t\n.centered{\n\twidth:85% !important;\n\tmax-width:800px !important;\n} \n.l10-page-title{\n\tvisibility:hidden;\n}\n</style>"),
            ModePageFormat.CreatePage("Wrap-up", L10PageType.Conclude, 5),

          },
          InjectOnAllPages = "<script>\n$('body').append('<style> .page-time.over{ color:#444!important; } </style>');\n $(\\\".agenda\\\").append(\\\"<div class='component additional-pages x-eosprocess'><div><div class='alignRight'><div class='clickable gray' onclick='window.open(\\\\\"https://www.eosworldwide.com/eos-process\\\\\", \\\\\"_blank\\\\\");'>The EOS Process<sup>®</sup><span class='glyphicon glyphicon-chevron-right'/></div></div></div>\n</div>\\\");</script>"
        };

        var visionBuilding1 = new Mode("vb1", "Future Focus Day", false, false) {
          PageFormats = new List<ModePageFormat>() {
            ModePageFormat.CreateHtmlPage("Objectives",5,"<iframe style=\"width:calc(100% - 40px); height:30vh;min-height:350px;margin:20px\" src=\"{{NOTES}}{{PAGE}}\"></iframe>"),
            ModePageFormat.CreateHtmlPage("Check-in",30,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 80%;margin: auto;\">\n\t\t\t<h2>Check-in</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Share personal and professional bests</li>\n\t\t\t\t<li>Share an update on what’s working/ not working?</li>\n\t\t\t\t<li>Share your expectations</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>"),
            ModePageFormat.CreateHtmlPage("Launch Day Tools",210,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 80%;margin: auto;\">\n\t\t\t<h2 style=\"text-align:center\">Check-in</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Share personal and professional bests</li>\n\t\t\t\t<li> Share an update on what’s working/ not working?</li>\n\t\t\t\t<li>Share your expectations</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>"),
            ModePageFormat.CreateHtmlPage("Core Values",120,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update Core Values on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/> <style> .meeting-page .centered{ max-width:90%; width:90%;}"),
            ModePageFormat.CreateHtmlPage("Focus",70,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update Focus on Business Plan</div> <br/> <br/> <iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/> <style> .meeting-page .centered{ max-width:90%; width:90%;}"),
            ModePageFormat.CreateHtmlPage("BHAG",30,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 10-Year Goal on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/> <style> .meeting-page .centered{ max-width:90%; width:90%;}"),
            ModePageFormat.CreateHtmlPage("Marketing Strategy",5,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update Marketing & Strategy on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'><style> .meeting-page .centered{ max-width:90%; width:90%; }"),
            ModePageFormat.CreateHtmlPage("3-Year Vision",5,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 3-Year Goals on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style> 	.meeting-page .centered{ max-width:90%; width:90%; }"),
            ModePageFormat.CreateHtmlPage("1-Year Goals",5,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 1-Year Goals on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style> .meeting-page .centered{ max-width:90%; width:90%;}"),
            ModePageFormat.CreateHtmlPage("Quarterly Goals",120,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 40px 35px;\">\n\t\t<h2>Goals</h2>\n\t\t<h4>What are the 3-7 most important things to accomplish in the next 90 days?</h4>\n\t</div>\t\n</center>\n<br/>\n<br/>\n<center>\n\t<iframe src=\"/l10/wizard/{{ID}}?noheading=true&amp;nosidebar=true#/Rocks\" frameborder=\"0\" style=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;max-width: 1200px;\"></iframe>\n</center>\n\n\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg 7-10</span>"),
            ModePageFormat.CreatePage("Issues List", L10PageType.IDS, 60),
						/*Diffferent from VB2*/
						ModePageFormat.CreateHtmlPage("Next Steps",5,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:850px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 90%;margin: auto;\">\n\t\t\t<h2 style=\"text-align: center\">Next Steps</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Complete the Org Chart</li>\n\t\t\t\t<li>Complete your Goals</li>\n\t\t\t\t<li>Do your Weekly Meetings</li>\n\t\t\t\t<li>Prepare to share your Core Values</li>\n\t\t\t\t<li>Update your Business Plan</li>\n\t\t\t\t<li>Schedule your Short-Term Focus Day session</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n\n<style>\n.page-header .l10-page-title{\n\tvisibility:hidden;\n}\n</style>"),
            ModePageFormat.CreatePage("Wrap-up", L10PageType.Conclude, 8),
          },
          InjectOnAllPages = "<script>\n$('body').append('<style> .page-time.over{ color:#444!important; } </style>');</script>"
        };


        var visionBuilding2 = new Mode("vb2", "Short-Term Focus Day", false, false) {
          PageFormats = new List<ModePageFormat>() {
            ModePageFormat.CreateHtmlPage("Objectives",5,"<iframe style=\"width:calc(100% - 40px); height:30vh;min-height:350px;margin:20px\" src=\"{{NOTES}}{{PAGE}}\"></iframe>"),
            ModePageFormat.CreateHtmlPage("Check-in",30,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 80%;margin: auto;\">\n\t\t\t<h2>Check-in</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Share personal and professional bests</li>\n\t\t\t\t<li>Share an update on what’s working/ not working?</li>\n\t\t\t\t<li>Share your expectations</li>\n\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>"),
            ModePageFormat.CreateHtmlPage("Launch Day Tools",210,"<center>\n\t<div class='component' style=\"display:inline-block;min-width:600px;padding:40px 10px 35px;\">\n\t\t\t<div style='text-align: left;width: 90%;margin: auto;'>\n\t\t\t\t<h2>Building a Foundation</h2>\n\t\t\t\t<ul id='x-second-li'>\n\t\t\t\t\t<li>Accountability Chart™ <span onclick='$(\".ac-button\").click()' class='btn btn-success btn-xs' style='cursor:pointer;color:white;margin: 3px 20px 0px;float: right;'>Review <span class='glyphicon glyphicon-chevron-right'/></span></li>\n\t\t\t\t\t<li>Goals <span onclick='showAltPageX(\"Rockarchive\",\"/l10/details/{{ID}}?noheading=true#/Rocks\",\"0px\",1000,50);' class='btn btn-success btn-xs' style='cursor:pointer;color:white;margin: 3px 20px 0px;float: right;'>Review <span class='glyphicon glyphicon-chevron-right'/></span></li>\n\t\t\t\t\t<li>Meeting Pulse™ <span onclick='showAltPageX(\"L10Agenda\",\"data:text/html;charset=utf-8,&lt;html style=&apos;text-align:center;padding:40px;&apos;&gt;&lt;img src=\\\\&apos;https://s3.amazonaws.com/Radial/base/Pictures/L10+Agenda.png\\\\&apos;&gt;&lt;/html&gt;\",\"0px\",1000,50);' class='btn btn-success btn-xs' style='cursor:pointer;color:white;margin: 3px 20px 0px;float: right;'>Review <span class='glyphicon glyphicon-chevron-right'/></span></li>\n\t\t\t\t\t<li>Metrics <span onclick='showAltPageX(\"SCarchive\",\"/l10/details/{{ID}}?noheading=true#/scorecard\",\"0px\",1000,50);'  class='btn btn-success btn-xs' style='cursor:pointer;color:white;margin: 3px 20px 0px;float: right;'>Review <span class='glyphicon glyphicon-chevron-right'/></span></li>\n\t\t\t\t</ul>\n\t\t\t</div>\n\t\t</div>\n\t</div>\n</center>\n\n<script>\n\tfunction showAltPageX(pageName,pageUrl,backButtonTop,wait,headerHeight){ \n\t\tvar timer = 0; \n\t\tif (typeof(wait)===\"undefined\"){\n\t\t\twait =  500;\n\t\t}\n\t\tif(wait<5){\n\t\t\twait = 5;\n\t\t} \t\t\n\t\t$(\".right-panel-header\").css(\"height\", typeof (headerHeight) === \"undefined\" ? 48 : headerHeight);\n\t\tif (typeof($(\".vto-frame\").attr(\"src\"))===\"undefined\"|| $(\".vto-frame\").attr(\"data-page\")!=pageName){\n\t\t\t$(\"body\").addClass(\"loading\"); $(\".vto-frame\").attr(\"src\",\"about:blank\"); \n\t\t\tsetTimeout(function(){ \n\t\t\t\t$(\".vto-frame\").attr(\"src\",pageUrl); \n\t\t\t\t$(\".vto-frame\").attr(\"data-page\",pageName); \n\t\t\t},0);\n\t\t\ttimer=wait; \n\t\t} \n\t\tsetTimeout(function(){ \n\t\t\tsetTimeout(function(){ \n\t\t\t\t$(\"body\").removeClass(\"loading\");\n\t\t\t },100); \n\t\t\t $(\".slider\").css({ marginLeft: \"-100%\" });\n\t\t\t $(\".slider-container\").css(\"overflow-y\",\"hidden\"); \n\t\t\t $(\".back-button\").css(\"top\",backButtonTop).show();\n\t\t\t page=pageName;\n\t\t\t vtoLastScroll = $(\".slider-container\").scrollTop(); \n\t\t\t $(\".slider-container\").scrollTop(0); \n\t\t},timer);\n\t}\n</script>\n\n<style>\n\t#x-second-li li{\n\t\tpadding: 0px 0px 8px;   \n\t}\n</style>"),
            ModePageFormat.CreateHtmlPage("Core Values",120,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update Core Values on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/> <style> .meeting-page .centered{ max-width:90%; width:90%;}</style>"),
            ModePageFormat.CreateHtmlPage("Focus",70,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update Focus on Business Plan</div> <br/> <br/> <iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/> <style> .meeting-page .centered{ max-width:90%; width:90%;}</style>"),
            ModePageFormat.CreateHtmlPage("BHAG",30,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 10 Year Goal on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/> <style> .meeting-page .centered{ max-width:90%; width:90%;}</style>"),
            ModePageFormat.CreateHtmlPage("Marketing Strategy",5,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update Marketing & Strategy on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'><style> .meeting-page .centered{ max-width:90%; width:90%; }</style>"),
            ModePageFormat.CreateHtmlPage("3-Year Vision",5,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 3-Year Goals on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style> 	.meeting-page .centered{ max-width:90%; width:90%; }</style>"),
            ModePageFormat.CreateHtmlPage("1-Year Goals",5,"<div class='btn btn-success' onclick='$(\".vto-button\").click()'>Update 1-Year Goals on Business Plan</div><br/><br/><iframe style='width:100%; height:50vh;' src='{{NOTES}}{{PAGE}}'/><style> .meeting-page .centered{ max-width:90%; width:90%;}</style>"),
            ModePageFormat.CreateHtmlPage("Quarterly Goals",120,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:550px;padding:40px 40px 35px;\">\n\t\t<h2>Goals</h2>\n\t\t<h4>What are the 3-7 most important things to accomplish in the next 90 days?</h4>\n\t</div>\t\n</center>\n<br/>\n<br/>\n<center>\n\t<iframe src=\"/l10/wizard/{{ID}}?noheading=true&amp;nosidebar=true#/Rocks\" frameborder=\"0\" style=\"left:0;right:0px;position:relative;width:100%;height:calc(100vh - 130px);border:none;max-width: 1200px;\"></iframe>\n</center>\n\n\n<span style=\"position: fixed;left: 43px;bottom: 40px;background: #cccccc;padding: 4px 12px;border-radius: 12px;font-size: 14px;color: #333;\">FD pg 7-10</span>"),
            ModePageFormat.CreatePage("Issues List", L10PageType.IDS, 60),

						/*Different from VB1*/
						ModePageFormat.CreateHtmlPage("Next Steps",5,"<center>\n\t<div class=\"component\" style=\"display:inline-block;min-width:900px;padding:40px 10px 35px;\">\n\t\t<div style=\"text-align: left;width: 90%;margin: auto;\">\n\t\t\t<h2>Next Steps</h2>\n\t\t\t<ul>\n\t\t\t\t<li>Roll out any tools as agreed upon in the Goals.</li>\n\t\t\t\t<li>Update the Business Plan and any other tools</li>\n\t\t\t\t<li>Schedule your Quarterly Mapping Session</li>\n\t\t\t\t</ul>\n\t\t</div>\n\t</div>\n</center>\n\n<style>\n.page-header .l10-page-title{\n\tvisibility:hidden;\n}\n</style>"),
            ModePageFormat.CreatePage("Wrap-up", L10PageType.Conclude, 8),
          },
          InjectOnAllPages = "<script>\n$('body').append('<style> .page-time.over{ color:#444!important; } </style>');</script>"
        };



        return new ModeList() {
          Modes = new List<Mode>() { quarterly, annual, focusDay, visionBuilding1, visionBuilding2 }
        };
      }
    }
  }
}

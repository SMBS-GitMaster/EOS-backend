using Microsoft.AspNetCore.Html;
using Pluralize.NET;
using RadialReview.Core.Models.Terms;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview {
  public class SettingsViewModel  {
    public SettingsViewModel() {
      user = new User();
      organization = new Org();
      notifications = new List<Notification>();
      signalr = new SignalR();
      service = new Service();
      flags = new Flags();
      ui = new UI();
      localization = new Localization();
    }
    public class User {
      public long userId { get; set; }
      public string userName { get; set; }
      public string name { get; set; }
      public bool isOrganizationAdmin { get; set; }
      public bool isSupervisor { get; set; }
      public bool isSuperAdmin { get; set; }
    }
    public class Org {
      public string name { get; set; }
      public string accountType { get; set; }
      public long organizationId { get; set; }
      public bool eosw { get; set; }
    }
    public class Notification {
      public long id { get; set; }
      public bool canMarkSeen { get; set; }
    }
    public class SignalR {
      public string endpoint_pattern { get; set; }
      public string endpoint_count { get; set; }
      public string settings { get; set; }
    }
    public class Service {
      public string knowledgebase_url { get; set; }
    }

    public class Localization {
      public Localization() {
        terms = new List<Term>();
      }
      public string lang { get; set; }
      public List<Term> terms { get; set; }

      public int timezone_offset { get; set; }

      public string this[TermKey key] {
        get { return terms.FirstOrDefault(x => x.Key == key).NotNull(x => x.Value ?? x.Default) ?? ("" + key); }
      }
      public string GetDefault(TermKey key) {
        return terms.FirstOrDefault(x => x.Key == key).NotNull(x => x.Default) ?? ("" + key);
      }
    }

    public class UI {
      public UI() {
        show_title_bar = true;
        outer_padding = null;
      }

      public bool show_title_bar { get; set; }
      public string outer_padding { get; set; }


      public bool hideV1SideNavAndTopNav { get; set; }


      public void ToStylesAndScripts(StringBuilder sb) {
        if (hideV1SideNavAndTopNav == true) {
          sb.Append($@"<script> hideNavBar();</script>");
        }
      
        if (outer_padding != null) {
          sb.Append($@"<style>html {{padding: {outer_padding};}}</style>");
        }
        if (!show_title_bar) {
          sb.Append($@"<script>(function(){{ $(""html"").addClass(""body-notitlebar"");}})()</script>");
        }
      }
    }

    public class Flags {
      public bool enable_shotclock { get; set; }
    }

    public SignalR signalr { get; set; }
    public User user { get; set; }
    public Org organization { get; set; }
    public List<Notification> notifications { get; set; }
    public Service service { get; set; }
    public Flags flags { get; set; }
    public UI ui { get; set; }
    public Localization localization { get; set; }


    public string GetTerm(TermKey key) {
      return localization[key];
    }

    public string GetDefault(TermKey key)
    {
      return localization.GetDefault(key);
    }

    public string GetTermSingular(TermKey key) {
      return TermsPluralizer.Singularize(localization[key]);
    }
    public string GetTermPlural(TermKey key) {
      return TermsPluralizer.Pluralize(localization[key]);
    }

    public HtmlString ToJson() {
      return this.SafeJsonSerialize();
    }

    public DateTime ConvertFromServerTime(DateTime serverTime) {
      return TimeData.ConvertFromServerTime(serverTime,localization.NotNull(x=>x.timezone_offset));
    }
    public DateTime ConvertToServerTime(DateTime localTime) {
      return TimeData.ConvertToServerTime(localTime, localization.NotNull(x => x.timezone_offset));
    }

    public HtmlString ToStylesAndScripts() {
      var sb = new StringBuilder();
      try {
        sb.Append($@"<script>try {{ window.settings = {ToJson()};}} catch (e){{console.error(""failed to load settings"")}}</script>");
        ui?.ToStylesAndScripts(sb);
      } catch (Exception e) {
        sb.Append(@"<script>console.error(""Fatal: failed to load settings"");</script>");
      }
      return new HtmlString(sb.ToString());
    }
  }
}

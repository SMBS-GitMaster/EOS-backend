using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RadialReview.Identity;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.ClientSuccess;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using static RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems;
using ISession = NHibernate.ISession;
using Microsoft.AspNetCore.Http.Extensions;
using RadialReview.Models.UserModels;
using System.Text;
using RadialReview.Core.Models.Terms;
using NHibernate.Criterion;
using RadialReview.Models.Synchronize;

namespace RadialReview.Middleware.Request.HttpContextExtensions.Prefetch {
  public class PrefetchData {
    public bool ReadOnlyMode { get; set; }
    public string InjectedScript { get; set; }
    public string UserStyles { get; set; }
    public UserModel User { get; set; }
    public List<TooltipViewModel> Tooltips { get; set; }
    public List<FeatureSwitch> FeatureSwitch { get; set; }
    public string KnowledgeBaseUrl { get; set; }
    public int UserOrganizationCount { get; set; }
    public bool TooltipsEnabled { get; set; }
    public List<Variable> Variables { get; set; }
    public T GetSettingOrDefault<T>(string name, T deflt) {
      var f = Variables.SingleOrDefault(x => x.K == name);
      if (f == null)
        return deflt;
      if (typeof(T) == typeof(string))
        return (T)((object)(f.V));
      return JsonConvert.DeserializeObject<T>(f.V);
    }
  }

  public static class HttpContextItemsPrefetch {

    private static HttpContextItemKey PREFETCH = new HttpContextItemKey("Prefetch");
    public static PrefetchData GetPrefetchData(this HttpContext context) {
      return context.GetOrCreateRequestItem(PREFETCH, ctx => {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var userId = ctx.User.GetUserId();
            string userStyles = "";
            IEnumerable<TooltipViewModel> tooltipsF = new List<TooltipViewModel>();

            IEnumerable<UserModel> userF = null;
            var variablesF = s.QueryOver<Variable>().Future();
            var featureSwitchF = s.QueryOver<FeatureSwitch>().Where(x => x.DeleteTime == null).Future();
            if (userId != null) {
              userF = s.QueryOver<UserModel>().Where(x => x.Id == userId).Future();
              tooltipsF = GetTooltipsFuture(s, userId, context.Request.GetEncodedPathAndQuery());
              userStyles = GetStylesExecute(s, userId);
            }


            var featureSwitch = featureSwitchF.ToList();
            var variables = variablesF.ToList();
            var user = userF.NotNull(x => x.ToList().FirstOrDefault());
            var userOrgCount = user.NotNull(x => x.UserOrganizationCount);

            if (tooltipsF == null || user == null || user.DisableTips) {
              tooltipsF = new List<TooltipViewModel>();
            }

            try {
              var maxSyncKeys = variables.ToList().FirstOrDefault(x => x.K == Variable.Names.MAX_SYNC_KEYS).NotNull(x => int.Parse(x.V));
              if (SyncLock.MAX_SYNC_KEYS != maxSyncKeys && maxSyncKeys>0) {
                SyncLock.MAX_SYNC_KEYS = maxSyncKeys;
              }
            } catch (Exception e) {
              //humm
            }

            return new PrefetchData() {
              ReadOnlyMode = Config.ReadonlyMode() || variables.ToList().FirstOrDefault(x => x.K == Variable.Names.READ_ONLY_MODE).NotNull(x => x.V.ToLower() == "true"),
              InjectedScript = variables.ToList().FirstOrDefault(x => x.K == Variable.Names.INJECTED_SCRIPTS).NotNull(x => x.V),
              KnowledgeBaseUrl = variables.ToList().FirstOrDefault(x => x.K == Variable.Names.KB_URL).NotNull(x => x.V),
              FeatureSwitch = featureSwitch.ToList(),
              Tooltips = tooltipsF.ToList(),
              UserOrganizationCount = userOrgCount,
              UserStyles = userStyles,
              User = user,
              TooltipsEnabled = user.NotNull(x => !x.DisableTips),
              Variables = variables,
              
            };
          }
        }
      });
    }

    #region helper

   


    private static IEnumerable<TooltipViewModel> GetTooltipsFuture(ISession s, string userId, string path) {
      var now = DateTime.UtcNow;

      var seenQ = s.QueryOver<TooltipSeen>()
              .Where(x => x.UserId == userId)
              .Select(x => x.TipId)
              .Future<long>();
      var tooltipsQ = s.CreateQuery("from TooltipTemplate t where :path like t.UrlSelector")
              .SetParameter("path", path)
              .Future<TooltipTemplate>();

      return tooltipsQ.Where(x => (x.DeleteTime == null || x.DeleteTime > now) && x.IsEnabled == true)
              .Where(x => !seenQ.Contains(x.Id))
              .Select(x => new TooltipViewModel(x));
    }
    private static string GetStylesExecute(ISession s, string userModel) {
      var builder = new StringBuilder();
      UserStyleSettings styles = null;
      styles = s.Get<UserStyleSettings>(userModel);
      if (styles != null) {
        if (styles.ShowScorecardColors == false) {
          builder.AppendLine(".scorecard-table .score .success, .scorecard-table .score.success input, .scorecard-table .score input.success, .scorecard-table .score.success{color: inherit !important;background-color: inherit !important;}");
          builder.AppendLine(".scorecard-table .score .danger,.scorecard-table .score.danger input, .scorecard-table .score input.danger, .scorecard-table .score.danger{color: inherit !important;background-color: inherit !important;}");
        }
      }

      return builder.ToString();
    }

    #endregion
  }
}

using Hangfire;
using NHibernate;
using RadialReview.Accessors.Whiteboard;
using RadialReview.Crosscutting.Interceptor.Whiteboard;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Hubs;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Middleware.Services.HtmlRender;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadialReview.Accessors {

  public class WhiteboardAccessor {

    public static Regex GuidRegex = new Regex("^[gG]?[a-fA-F0-9]{8}(?:-[a-fA-F0-9]{4}){3}-[a-fA-F0-9]{12}$");

    public static List<IWhiteboardDiffInterceptor> DiffInterceptors = new List<IWhiteboardDiffInterceptor> {
      new WhiteboardDiffClear()
    };

    public static async Task<WhiteboardModel> CreateWhiteboard(UserOrganizationModel caller, string name, long orgId, params PermTiny[] permissions) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var wb = await CreateWhiteboard(s, perms, name, orgId, permissions);
          tx.Commit();
          s.Flush();
          return wb;
        }
      }
    }

    public static async void SetBackgroundSvg(ISession s, PermissionsUtility perms, string whiteboardId, string svgUrl) {
      perms.TryWithAlternateUsers(x => x.EditWhiteboard(whiteboardId));
      var wb = GetWhiteboard_Unsafe(s, whiteboardId, false);
      wb.SvgUrl = svgUrl;
      s.Update(wb);
    }
    public static async void SetIsTemplate(ISession s, PermissionsUtility perms, string whiteboardId, bool isTemplate) {
      perms.TryWithAlternateUsers(x => x.EditWhiteboard(whiteboardId));
      var wb = GetWhiteboard_Unsafe(s, whiteboardId, false);
      wb.IsTemplate = isTemplate;
      s.Update(wb);
    }

    public static async Task<WhiteboardModel> CreateWhiteboard(ISession s, PermissionsUtility perms, string name, long orgId, params PermTiny[] permissions) {
      perms.TryWithAlternateUsers(x => x.ViewOrganization(orgId));
      var wb = new WhiteboardModel() {
        Name = name,
        CreatedBy = perms.GetCaller().Id,
        OrgId = orgId,
        LookupId = RandomUtil.SecureRandomGuid().ToString()
      };
      s.Save(wb);
      if (name == null) {
        wb.Name = wb.LookupId;
        s.Update(wb);
      }


      var permItems = (permissions ?? new PermTiny[0]).ToList();
      permItems.Add(PermTiny.Creator(true, true, true));
      PermissionsAccessor.InitializePermItems_Unsafe(s, perms.GetCaller(), PermItem.ResourceType.Whiteboard, wb.Id, permItems.ToArray());
      return wb;
    }

    public static async Task<bool> Unlock(UserOrganizationModel caller, string whiteboardId, string[] guids) {
      if (whiteboardId == null || guids == null || !guids.Any()) {
        return false;
      }
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);


          var g = string.Join(",", guids.Where(x => GuidRegex.IsMatch(x)).Select(x => "\"" + x.ToString() + "\"").ToList());

          await SaveDiff(s, perms, caller.Id, caller.Organization.Id, new Diff() {
            Id = whiteboardId,
            ConnectionId = null,
            Delta = "{\"command\": \"unlock\",\"ids\": [" + g + "], \"unload\":true}",
            ElementId = "project",
            Type = "wb",
            Version = -1,
          });

          tx.Commit();
          s.Flush();
        }
      }
      return true;
    }

    public static void SaveAsSvg(UserOrganizationModel caller, string whiteboardId, string svg) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditWhiteboard(whiteboardId);
        }
      }



    }

    public static List<Diff> GetDiffsAfter(UserOrganizationModel caller, string whiteboardId, long after) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewWhiteboard(whiteboardId);
          var wb = GetWhiteboard_Unsafe(s, whiteboardId, false);

          if (wb.LastDiff <= after)
            return new List<Diff>();

          return s.QueryOver<WhiteboardDiff>()
            .Where(x => x.DeleteTime == null && x.WhiteboardId == whiteboardId && x.Id > after)
            .List()
            .Select(x => new Diff(x))
            .ToList();
        }
      }
    }

    public class DiffData {
      public long DiffId { get; set; }
      public long Created { get; set; }
      public long Time { get; set; }
      public bool Valid { get; set; }
    }


    public static DiffData GetCurrentDiff(UserOrganizationModel caller, string whiteboardId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetCurrentDiff(s, perms, whiteboardId);
        }
      }
    }

    public static DiffData GetCurrentDiff(ISession s, PermissionsUtility perms, string whiteboardId) {
      perms.ViewWhiteboard(whiteboardId);
      var wb = GetWhiteboard_Unsafe(s, whiteboardId, false);
      return new DiffData() {
        Created = wb.LastDiffTime.NotNull(x => x.Value.ToJsMs()),
        DiffId = wb.LastDiff,
        Valid = wb.LastDiffTime != null && wb.LastDiff > 0,
        Time = DateTime.UtcNow.ToJsMs()
      };
    }

    public static WhiteboardModel GetWhiteboard(UserOrganizationModel caller, string whiteboardId, bool includeDiffs) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewWhiteboard(whiteboardId);
          return GetWhiteboard_Unsafe(s, whiteboardId, includeDiffs);
        }
      }
    }

    public static async Task JoinWhiteboard(IOuterSession s, PermissionsUtility perms, string whiteboardId, string connectionId) {
      await using (var rt = RealTimeUtility.Create()) {
        perms.ViewWhiteboard(whiteboardId);
        await rt.AddToGroup(connectionId, RealTimeHub.Keys.GenerateWhiteboardGroupId(whiteboardId));
      }
    }

    private static WhiteboardModel GetWhiteboard_Unsafe(ISession s, string whiteboardId, bool includeDiffs) {
      var wb = s.QueryOver<WhiteboardModel>().Where(x => x.LookupId == whiteboardId).Take(1).SingleOrDefault();
      if (includeDiffs) {
        wb.SetDiffs(GetWhiteboardDiffs_Unsafe(s, whiteboardId));
      }

      return wb;
    }

    public static async Task SaveDiff(ISession s, PermissionsUtility perms, long callerId, long orgId, Diff diff) {
      var whiteboardId = diff.GetModelId();
      if (diff == null || diff.Delta == null)
        return;
      var wb = GetWhiteboard_Unsafe(s, whiteboardId, false);
      wb.Version += 1;
      perms.EditWhiteboard(whiteboardId);
      var applicableIntercept = DiffInterceptors.Where(x => {
        try { return x.ShouldApplyInterceptor(s, perms, callerId, orgId, diff); } catch (NotImplementedException) { return false; }
      }).ToList();

      foreach (var a in applicableIntercept) {
        try {
          await a.ApplyBefore(s, perms, callerId, orgId, diff);
        } catch (NotImplementedException) {
        }
      }

      var model = new WhiteboardDiff {
        ByUserId = callerId,
        Delta = diff.Delta,
        OrgId = orgId,
        WhiteboardId = diff.Id,
        ElementId = diff.ElementId,
        Version = wb.Version
      };
      s.Save(model);
      wb.LastDiff = model.Id;
      s.Update(wb);

      foreach (var a in applicableIntercept) {
        try {
          await a.ApplyAfter(s, perms, callerId, orgId, diff);
        } catch (NotImplementedException) {
        }
      }
      await using (var rt = RealTimeUtility.Create(diff.ConnectionId)) {
        var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateWhiteboardGroupId(diff.Id));
        var clone = diff.Clone();
        clone.O = model.Id;
        clone.ConnectionId = null;
        group.Call("receiveDiff", clone);
      }
    }


    private static string GetWhiteboardDiffs_Unsafe(ISession s, string whiteboardId) {
      var diffs = s.QueryOver<WhiteboardDiff>()
                  .Where(x => x.WhiteboardId == whiteboardId && x.DeleteTime == null)
                  .OrderBy(x => x.CreateTime).Asc
                  .List()
                  .Select(x => new Diff(x))
                  .ToList();
      return SafeJsonUtil.SafeJsonSerializeString(diffs);
    }

    public static async Task<int> DeleteAfter(UserOrganizationModel caller, string whiteboardId, long after) {
      int res;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditWhiteboard(whiteboardId);

          var toDelete = s.QueryOver<WhiteboardDiff>()
            .Where(x => x.WhiteboardId == whiteboardId && x.DeleteTime == null && x.Id > after && !x.Permanent)
            .List().ToList();

          var now = DateTime.UtcNow;
          foreach (var t in toDelete) {
            t.DeleteTime = now;
            s.Update(t);
          }

          var last = s.QueryOver<WhiteboardDiff>()
            .Where(x => x.WhiteboardId == whiteboardId && x.DeleteTime == null)
            .OrderBy(x => x.Id).Desc.Take(1).SingleOrDefault();

          var wb = GetWhiteboard_Unsafe(s, whiteboardId, false);

          if (last != null) {
            wb.LastDiff = last.Id;
            wb.LastDiffTime = last.CreateTime;
          } else {
            wb.LastDiff = -1;
          }
          s.Update(wb);

          tx.Commit();
          s.Flush();
          res = toDelete.Count;

        }
      }
      await using (var rt = RealTimeUtility.Create()) {
        var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateWhiteboardGroupId(whiteboardId));
        group.Call("forceRefreshImmediate");
      }
      return res;
    }

    [Queue(HangfireQueues.Immediate.SAVE_WHITEBOARD)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
    [AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public static async Task<bool> SaveWhiteboard_Hangfire(long callerId, long fileId, string whiteboardId, [ActivateParameter] IHtmlRenderService htmlRenderer, [ActivateParameter] IBlobStorageProvider bsp) {
      UserOrganizationModel caller;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          caller = s.Get<UserOrganizationModel>(callerId);
        }
      }
      using (var image = new MemoryStream()) {
        await WhiteboardImageAccessor.GenerateImageAsync(caller, image, htmlRenderer, whiteboardId);


        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            await FileAccessor.Save_Unsafe(s, bsp, fileId, image, FileNotification.DoNotNotify());
            tx.Commit();
            s.Flush();
          }
        }
        return true;
      }
    }
  }
}

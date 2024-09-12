using NHibernate;
using NHibernate.Criterion;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Queries {
  public class WhiteboardQueryMethods_Unsafe : IDocumentItemQueryMethods_Unsafe {
    public DocumentItemType ForItemType() {
      return DocumentItemType.Whiteboard;
    }

    public IEnumerable<DocumentItemVM> GetDocumentItemsById_Unsafe(ISession s, List<long> ids, DocumentItemSettings settings) {
      return s.QueryOver<WhiteboardModel>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.Id)
        .IsIn(ids)
        .Future()
        .Select(c => DocumentItemVM.Create(c, settings));
    }

    public IEnumerable<DocumentItemVM> GetDocumentItemsWhereIdInQuery_Unsafe<U>(ISession s, QueryOver<U> itemIdQuery, DocumentItemSettings settings) {
      return s.QueryOver<WhiteboardModel>()
          .Where(x => x.DeleteTime == null)
          .WithSubquery.WhereProperty(x => x.Id)
          .In(itemIdQuery)
          .Future()
          .Select(c => DocumentItemVM.Create(c, settings));

    }


    public Dictionary<long, bool> BulkAdmin(ISession s, PermissionsUtility perms, long[] ids) {
      return perms.BulkCanAdmin(PermItem.ResourceType.Whiteboard, ids, true);
    }

    public Dictionary<long, bool> BulkEdit(ISession s, PermissionsUtility perms, long[] ids) {
      return perms.BulkCanEdit(PermItem.ResourceType.Whiteboard, ids, true);
    }

    public Dictionary<long, bool> BulkView(ISession s, PermissionsUtility perms, long[] ids) {
      return perms.BulkCanView(PermItem.ResourceType.Whiteboard, ids, true);
    }

    public bool CanDelete(ISession s, PermissionsUtility perms, long id) {
      return perms.IsPermitted(x => x.TryWithAlternateUsers(y => y.AdminWhiteboard(id)));
    }

    public async Task<bool> Rename(ISession s, long id, string name) {
      var wb = s.Get<WhiteboardModel>(id);
      wb.Name = name;
      s.Update(wb);
      return true;
    }
    public long? GetOrgId_Unsafe(ISession s, long itemId) {
      return s.Get<WhiteboardModel>(itemId).OrgId;
    }
    public async Task<bool> PossibleToClone(ISession s, long itemId) {
      return true;
    }

    public async Task<long> Clone_Unsafe(ISession s, long itemId, long orgId,long clonedByUserId) {
      var wb = s.Get<WhiteboardModel>(itemId);
      var oldWbId = wb.LookupId;
      s.Evict(wb);
      var now = DateTime.UtcNow;
      wb.Id = 0;
      wb.OrgId = orgId;
      wb.IsTemplate = false;
      wb.Name = (wb.Name +" (copy)").Trim();
      wb.CreatedBy = clonedByUserId;
      wb.CreateTime = now;
      wb.LookupId = RandomUtil.SecureRandomGuid().ToString();
      s.Save(wb);

      var diffs = s.QueryOver<WhiteboardDiff>().Where(x => x.DeleteTime == null && x.WhiteboardId == oldWbId).List().ToList();

      foreach (var d in diffs) {
        s.Evict(d);
        d.Id = 0;
        //d.CreateTime //CreateTime is used to order the diffs, probably best to not change.
        //d.ByUserId = clonedByUserId;
        d.OrgId = orgId;
        d.WhiteboardId = wb.LookupId;
        s.Save(d);
      }


      return wb.Id;
    }

  }
}

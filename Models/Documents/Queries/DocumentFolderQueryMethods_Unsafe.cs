using NHibernate;
using NHibernate.Criterion;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Queries {
  public class DocumentFolderQueryMethods_Unsafe : IDocumentItemQueryMethods_Unsafe {
    public DocumentItemType ForItemType() {
      return DocumentItemType.DocumentFolder;
    }

    public IEnumerable<DocumentItemVM> GetDocumentItemsById_Unsafe(ISession s, List<long> ids, DocumentItemSettings settings) {
      return s.QueryOver<DocumentsFolder>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.Id)
        .IsIn(ids)
        .Future()
        .Select(c => DocumentItemVM.Create(c, c.Root, settings));
    }

    public IEnumerable<DocumentItemVM> GetDocumentItemsWhereIdInQuery_Unsafe<U>(ISession s, QueryOver<U> itemIdQuery, DocumentItemSettings settings) {
      return s.QueryOver<DocumentsFolder>()
          .Where(x => x.DeleteTime == null)
          .WithSubquery.WhereProperty(x => x.Id)
          .In(itemIdQuery)
          .Future()
          .Select(c => DocumentItemVM.Create(c, c.Root, settings));
    }

    public Dictionary<long, bool> BulkAdmin(ISession s, PermissionsUtility perms, long[] ids) {
      return perms.BulkCanAdmin(PermItem.ResourceType.DocumentsFolder, ids, true);
    }

    public Dictionary<long, bool> BulkEdit(ISession s, PermissionsUtility perms, long[] ids) {
      return perms.BulkCanEdit(PermItem.ResourceType.DocumentsFolder, ids, true);
    }

    public Dictionary<long, bool> BulkView(ISession s, PermissionsUtility perms, long[] ids) {
      return perms.BulkCanView(PermItem.ResourceType.DocumentsFolder, ids, true);
    }

    public bool CanDelete(ISession s, PermissionsUtility perms, long id) {
      if (!perms.IsPermitted(x => x.TryWithAlternateUsers(y => y.AdminDocumentsFolder(id))))
        return false;
      var folder = s.Get<DocumentsFolder>(id);
      return folder.CanDelete;
    }

    public async Task<bool> Rename(ISession s, long id, string name) {
      var model = s.Get<DocumentsFolder>(id);
      model.Name = name;
      s.Update(model);
      return true;
    }

    public long? GetOrgId_Unsafe(ISession s, long itemId) {
      return s.Get<DocumentsFolder>(itemId).OrgId;
    }

    public async Task<bool> PossibleToClone(ISession s, long itemId) {
      return false;
    }

    public async Task<long> Clone_Unsafe(ISession s, long itemId, long orgId, long clonedByUserId) {
      throw new System.NotImplementedException();
    }
  }
}

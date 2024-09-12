using System.Collections.Generic;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Utilities;

namespace RadialReview.Models.Documents.Queries {
  public interface IDocumentItemQueryMethods_Unsafe {

    DocumentItemType ForItemType();
    IEnumerable<DocumentItemVM> GetDocumentItemsWhereIdInQuery_Unsafe<U>(ISession s, QueryOver<U> itemIdQuery, DocumentItemSettings settings);
    IEnumerable<DocumentItemVM> GetDocumentItemsById_Unsafe(ISession s, List<long> ids, DocumentItemSettings settings);
    Dictionary<long, bool> BulkView(ISession s, PermissionsUtility perms, long[] ids);
    Dictionary<long, bool> BulkEdit(ISession s, PermissionsUtility perms, long[] ids);
    Dictionary<long, bool> BulkAdmin(ISession s, PermissionsUtility perms, long[] ids);
    bool CanDelete(ISession s, PermissionsUtility perms, long id);
    Task<bool> Rename(ISession s, long id, string name);
    long? GetOrgId_Unsafe(ISession s, long itemId);
    Task<bool> PossibleToClone(ISession s, long itemId);
    Task<long> Clone_Unsafe(ISession s, long itemId, long orgId, long clonedByUserId);


  }
}

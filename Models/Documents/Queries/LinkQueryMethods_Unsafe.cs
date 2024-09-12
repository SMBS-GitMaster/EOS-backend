using NHibernate;
using NHibernate.Criterion;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Queries {
  public class LinkQueryMethods_Unsafe : IDocumentItemQueryMethods_Unsafe {

    /*
		 * THIS WHOLE CLASS IS A NOOP FOR LINKS.
		 */

    public DocumentItemType ForItemType() {
      return DocumentItemType.Link;
    }
    public Dictionary<long, bool> BulkAdmin(ISession s, PermissionsUtility perms, long[] ids) {
      return ids.Distinct().ToDictionary(x => x, x => false);
    }

    public Dictionary<long, bool> BulkEdit(ISession s, PermissionsUtility perms, long[] ids) {
      return ids.Distinct().ToDictionary(x => x, x => false);
    }

    public Dictionary<long, bool> BulkView(ISession s, PermissionsUtility perms, long[] ids) {
      return ids.Distinct().ToDictionary(x => x, x => false);
    }


    public IEnumerable<DocumentItemVM> GetDocumentItemsById_Unsafe(ISession s, List<long> ids, DocumentItemSettings settings) {
      return new List<DocumentItemVM>();
    }

    public IEnumerable<DocumentItemVM> GetDocumentItemsWhereIdInQuery_Unsafe<U>(ISession s, QueryOver<U> itemIdQuery, DocumentItemSettings settings) {
      return new List<DocumentItemVM>();
    }

    public bool CanDelete(ISession s, PermissionsUtility perms, long id) {
      return false;
    }

    public async Task<bool> Rename(ISession s, long id, string name) {
      return false;
    }

    public long? GetOrgId_Unsafe(ISession s, long itemId) {
      return null;
    }

    public async Task<bool> PossibleToClone(ISession s, long itemId) {
      return false;
    }

    public async Task<long> Clone_Unsafe(ISession s, long itemId, long orgId, long clonedByUserId) {
      throw new System.NotImplementedException();
    }
  }
}
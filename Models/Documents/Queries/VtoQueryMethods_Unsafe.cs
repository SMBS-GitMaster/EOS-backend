using NHibernate;
using NHibernate.Criterion;
using RadialReview.Models.Downloads;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents.Queries {
  public class VtoQueryMethods_Unsafe : IDocumentItemQueryMethods_Unsafe {
    public DocumentItemType ForItemType() {
      return DocumentItemType.VTO;
    }

    public IEnumerable<DocumentItemVM> GetDocumentItemsById_Unsafe(ISession s, List<long> ids, DocumentItemSettings settings) {
      return s.QueryOver<VtoModel>()
        .Where(x => x.DeleteTime == null)
        .WhereRestrictionOn(x => x.Id)
        .IsIn(ids)
        .Future()
        .Select(c => DocumentItemVM.Create(c, settings));
    }

    public IEnumerable<DocumentItemVM> GetDocumentItemsWhereIdInQuery_Unsafe<U>(ISession s, QueryOver<U> itemIdQuery, DocumentItemSettings settings) {
      return s.QueryOver<EncryptedFileModel>()
          .Where(x => x.DeleteTime == null)
          .WithSubquery.WhereProperty(x => x.Id)
          .In(itemIdQuery)
          .Future()
          .Select(c => DocumentItemVM.Create(c, settings));

    }

    public Dictionary<long, bool> BulkAdmin(ISession s, PermissionsUtility perms, long[] ids) {
      return ids.ToDictionary(x => x, x => false);
    }

    public Dictionary<long, bool> BulkEdit(ISession s, PermissionsUtility perms, long[] ids) {
      return ids.ToDictionary(x => x, id => perms.IsPermitted(p => p.TryWithAlternateUsers(x => x.EditVTO(id))));
    }

    public Dictionary<long, bool> BulkView(ISession s, PermissionsUtility perms, long[] ids) {
      return ids.ToDictionary(x => x,
        id => perms.IsPermitted(p =>
          p.TryWithAlternateUsers(y => y.Or(x => x.ViewVTOTraction(id), x => x.ViewVTOVision(id)))
        )
      );
    }

    public bool CanDelete(ISession s, PermissionsUtility perms, long id) {
      return false;
    }
    public async Task<bool> Rename(ISession s, long id, string name) {
      var model = s.Get<VtoModel>(id);
      model.Name = name;
      s.Update(model);
      return true;
    }

    public long? GetOrgId_Unsafe(ISession s, long itemId) {
      return s.Get<VtoModel>(itemId).Organization.Id;
    }
    public async Task<bool> PossibleToClone(ISession s, long itemId) {
      return false;
    }

    public async Task<long> Clone_Unsafe(ISession s, long itemId, long orgId, long clonedByUserId) {
      throw new System.NotImplementedException();
    }
  }
}

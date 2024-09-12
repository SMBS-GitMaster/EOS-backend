using NHibernate;
using RadialReview.Utilities;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Synchronize {
  public struct StrictlyAfterBehavior {
    public bool HasAfterUpdateFunction;
    public StrictlyAfterBehavior(bool hasAfterUpdateFunction) {
      HasAfterUpdateFunction=hasAfterUpdateFunction;
    }
  }
  public interface IStrictlyAfter {

    public StrictlyAfterBehavior Behavior { get; }
    public Task EnsurePermitted(ISession s, PermissionsUtility perms);
    public Task AtomicUpdate(IOrderedSession s);
    public Task AfterAtomicUpdate(ISession s, PermissionsUtility perms);

  }
}

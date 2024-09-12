using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors.Payments {
  public class GroupCalculator : IEnumerable<UQ> {
    public IEnumerable<UQ> Users { get; set; }
    public ChargeProduct Product { get; private set; }

    public GroupCalculator(IEnumerable<UQ> users, ChargeProduct product) {
      Users=users.ToList();
      Product = product;
    }

    public IEnumerable<UQ> WhereNonFree() {
      return this.Where(x => !x.ChargeDescription.Any(y => y.Type == ChargeType.ForceFree)).ToList();
    }

    public int GetNumberOfUsersChargedFor() {
      var count = 0;

      foreach (var u in Users) {
        if (u.ChargeDescription.Where(x => x.Product==Product).Any(x => x.Type ==ChargeType.ForceCharge)) {
          count++;
          continue;
        }
        if (u.ChargeDescription.Where(x => x.Product==Product).Any(x => x.Type ==ChargeType.ForceFree)) {
          continue;
        }
        count++;
      }
      return count;
    }

    public IEnumerator<UQ> GetEnumerator() {
      return Users.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return Users.GetEnumerator();
    }
  }

  //public class GroupCalculator {
  //  public UQ User { get; set; }
  //  public List<ChargeDescription> Tags { get; set; }
  //  public GroupCalculator(UQ user) {
  //    User = user;
  //    Tags=new List<ChargeDescription>();
  //  }
  //}


}

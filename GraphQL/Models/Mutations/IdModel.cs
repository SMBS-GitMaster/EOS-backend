using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Mutations {
  public class IdModel {
    public IdModel() {}
    public IdModel(long id) {
      if (id==0)
        throw new ArgumentOutOfRangeException(nameof(id), "Id should be non-zero");
      Id=id;
    }

    public long Id { get; set; }
  }

  public class VoidModel {
    public string Result => "Success";
  }
}

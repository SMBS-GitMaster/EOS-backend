using RadialReview.Core.Models.Terms;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Angular.Terms {



  public class AngularTerms : BaseAngular {
    public Dictionary<string, Term> LookupTable { get; set; }

  }
}

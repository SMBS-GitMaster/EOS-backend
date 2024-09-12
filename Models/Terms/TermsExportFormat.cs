using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Terms {


  public class TermsExportFormat {
    public class TermExportFormat {
      public string key { get; set; }
      public string value { get; set; }
    }
    public int version { get; set; }
    public long createdAt { get; set; }
    public string lang { get; set; }

    public List<TermExportFormat> terms { get; set; }


  }
}

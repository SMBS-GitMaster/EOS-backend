using RadialReview.Models.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Terms {

  public class TermsPageVM {
    public TermsCollection TermCollection { get; set; }
    public List<NameId> Meetings { get; set; }
    public Dictionary<TermKey, string> ImportValues { get; set; }
    public bool ImportMode { get; set; }
    public TermsPageVM() {
      ImportValues = new Dictionary<TermKey, string>();
    }
    public string GetOverrideValue(TermKey key) {
      if (ImportValues.ContainsKey(key)) {
        return ImportValues[key];
      }
      return "";
    }
  }
}

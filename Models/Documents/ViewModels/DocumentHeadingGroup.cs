using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models.Documents {
  public class DocumentHeadingGroup {
    public string HeadingName { get; set; }
    public List<DocumentItemVM> Contents { get; set; }
    public DocumentHeadingGroup() {
      Contents = new List<DocumentItemVM>();
    }
  }
}

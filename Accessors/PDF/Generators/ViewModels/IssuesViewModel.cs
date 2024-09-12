using RadialReview.Core.Models.Terms;
using System.Collections.Generic;

namespace RadialReview.Accessors.PDF.Generators.ViewModels {


  public class IssuesPdfViewModel {
    public string Company { get; set; }
    public string Image { get; set; }
    public List<IssuesPdfDto> Issues { get; set; }
    public TermsCollection Terms { get; set; }

    public class IssuesPdfDto {
      public string Number { get; set; }
      public string Issues { get; set; }
      public string Owner { get; set; }
    }
  }

}
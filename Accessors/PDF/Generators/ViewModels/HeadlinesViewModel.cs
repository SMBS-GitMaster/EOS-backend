using RadialReview.Core.Models.Terms;
using System.Collections.Generic;

namespace RadialReview.Accessors.PDF.Generators.ViewModels {

  public class HeadlinesPdfViewModel {
    public string Company { get; set; }
    public string Image { get; set; }
    public List<HeadlinesPdfDto> Headlines { get; set; }
    public class HeadlinesPdfDto {
      public string Headline { get; set; }
      public string Owner { get; set; }
    }
    public TermsCollection Terms { get; set; }
  }

}
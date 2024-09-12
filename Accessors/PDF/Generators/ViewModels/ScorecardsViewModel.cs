using RadialReview.Core.Models.Terms;
using RadialReview.Models.Angular.Scorecard;

namespace RadialReview.Accessors.PDF.Generators.ViewModels {

  public class ScorecardsPdfViewModel {
    public string Company { get; set; }
    public string Image { get; set; }
    public string DateFormat { get; set; }
    public ScorecardsPdfDto Scorecards { get; set; }

    public TermsCollection Terms { get; set; }
    public class ScorecardsPdfDto {
      public AngularScorecard Scorecard { get; set; }
      //public string Owner { get; set; }
      //public string DueDate { get; set; }
    }
  }
}
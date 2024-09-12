using RadialReview.Core.Models.Terms;
using System.Collections.Generic;

namespace RadialReview.Accessors.PDF.Generators.ViewModels {

  public class TodosPdfViewModel {
    public string Company { get; set; }
    public string Image { get; set; }
    public List<TodosPdfDto> Todos { get; set; }
    public TermsCollection Terms { get; set; }
    public class TodosPdfDto {
      public string Number { get; set; }
      public string Todo { get; set; }
      public string Owner { get; set; }
      public string DueDate { get; set; }
    }
  }
}
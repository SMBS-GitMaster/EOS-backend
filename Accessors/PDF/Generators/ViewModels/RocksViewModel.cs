using System;
using System.Collections.Generic;
using RadialReview.Core.Models.Terms;
using RadialReview.Models.Enums;

namespace RadialReview.Accessors.PDF.Generators.ViewModels {

  public class RocksPdfViewModel {

    public string Company { get; set; }
    public string Image { get; set; }
    public string FutureDate { get; set; }
    public List<Tuple<string, string>> Headers { get; set; }

    public List<RocksPdfDto> CompanyRocks { get; set; }
    public List<RocksPdfDto> IndividualRocks { get; set; }

    public RockGroupDto CompanyRockGroup { get; set; }
    public List<RockGroupDto> IndvidualRockGroups { get; set; }
    public bool? PrintOutRockStatus { get; set; }

    public TermsCollection Terms { get; set; }


    public class RocksPdfDto {
      public string Description { get; set; }
      public string Type { get; set; }
      public string Owner { get; set; }
      public RockState? Status { get; set; }
    }

    public class RockGroupDto {
      public string Owner { get; set; }
      public int Completed { get; set; }
      public int Total { get; set; }

      public string PercentCompletion {
        get { return Total <= 0 ? "0%" : Convert.ToDouble((Convert.ToDouble(Completed) / Convert.ToDouble(Total))).ToString("P0"); }
      }

      public string CompletionDisplay {
        get { return $"{Completed}/{Total} = {PercentCompletion.Replace(" ", "")}"; }
      }
      public List<RocksPdfDto> Rocks { get; set; }
    }

  }

}
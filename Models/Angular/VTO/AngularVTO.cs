using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.L10;
using Newtonsoft.Json;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Models.Angular.VTO {
  public interface IVtoSectionHeader {
    DateTime? FutureDate { get; set; }
    IEnumerable<AngularVtoKV> Headers { get; set; }

  }

  public class AngularVTO : Base.BaseAngular {
    public AngularVTO(long id) : base(id) {
    }

    public AngularVTO() {
    }

    public DateTime? CreateTime { get; set; }
    public long? CopiedFrom { get; set; }
    public String Name { get; set; }

    public String CompanyImg { get; set; }

    public string _TractionPageName { get; set; }
    [JsonIgnore]
    public long _OrganizationId { get; set; }
    [JsonIgnore]
    public long _VisionId { get; set; }
    public bool IncludeVision { get; set; }
    public bool IncludeTraction { get; set; }

    public AngularCoreFocus CoreFocus { get; set; }
    public AngularStrategy Strategy { get; set; }
    public IEnumerable<AngularStrategy> Strategies { get; set; }

    public AngularQuarterlyRocks QuarterlyRocks { get; set; }
    public AngularThreeYearPicture ThreeYearPicture { get; set; }
    public AngularOneYearPlan OneYearPlan { get; set; }
    public IEnumerable<AngularCompanyValue> Values { get; set; }
    public IEnumerable<AngularVtoString> Issues { get; set; }

    public Dictionary<string, string> Terms { get; set; }

    public bool IssuesDisabled { get; set; }

    public String TenYearTarget { get; set; }
    public String TenYearTargetTitle { get; set; }
    public String CoreValueTitle { get; set; }
    public String IssuesListTitle { get; set; }
    public static AngularVTO Create(VtoModel vto, TermsCollection terms) {
      return new AngularVTO() {
        Id = vto.Id,
        L10Recurrence = vto.L10Recurrence,
        _OrganizationId = vto.Organization.Id,
        CreateTime = vto.CreateTime,
        CopiedFrom = vto.CopiedFrom,
        TenYearTarget = vto.TenYearTarget,
        Name = vto.Name,
        Values = AngularCompanyValue.Create(vto._Values),
        CoreFocus = AngularCoreFocus.Create(vto.CoreFocus, terms),
        Strategy = AngularStrategy.Create(vto.MarketingStrategy, terms),
        Strategies = vto._MarketingStrategyModel.Select(x => AngularStrategy.Create(x, terms)).ToList(),

        OneYearPlan = AngularOneYearPlan.Create(vto.OneYearPlan, terms),
        QuarterlyRocks = AngularQuarterlyRocks.Create(vto.QuarterlyRocks, terms),
        ThreeYearPicture = AngularThreeYearPicture.Create(vto.ThreeYearPicture, terms),
        Issues = AngularVtoString.Create(vto._Issues),
        TenYearTargetTitle = vto.TenYearTargetTitle ?? terms.GetTerm(TermKey.BHAG).ToUpper(),
        CoreValueTitle = vto.CoreValueTitle ?? terms.GetTerm(TermKey.CoreValues).ToUpper(),
        IssuesListTitle = vto.IssuesListTitle ?? terms.GetTerm(TermKey.LongTermIssues).ToUpper(),
        IncludeVision = true,
        IncludeTraction = true,
        Terms = terms?.GetTermsDictionary() ?? new Dictionary<string, string>()
      };
    }

    public long? L10Recurrence { get; set; }

    public void ReplaceVision(AngularVTO vto, TermsCollection terms) {
      _TractionPageName = Name ?? "";
      Name = vto.Name;

      TenYearTarget = vto.TenYearTarget;

      Values = vto.Values;
      CoreFocus = vto.CoreFocus;
      Strategy = vto.Strategy;
      Strategies = vto.Strategies;

      ThreeYearPicture = vto.ThreeYearPicture;
      TenYearTargetTitle = vto.TenYearTargetTitle ?? terms.GetTerm(TermKey.BHAG).ToUpper();
      CoreValueTitle = vto.CoreValueTitle ?? terms.GetTerm(TermKey.CoreValues).ToUpper();
      IncludeVision = true;
      _VisionId = vto.Id;
    }
  }
  #region DataTypes

  public class AngularVtoKV : Base.BaseAngular {
    public AngularVtoKV() {
    }

    public AngularVtoKV(long id) : base(id) {
    }
    public String K { get; set; }
    public String V { get; set; }

    public bool Deleted { get; set; }

    public static AngularVtoKV Create(VtoItem_KV strs) {
      return new AngularVtoKV() {
        K = strs.K,
        V = strs.V,
        Id = strs.Id,
        Deleted = strs.DeleteTime != null,
        _ExtraProperties = strs._Extras
      };
    }
    public static List<AngularVtoKV> Create(IEnumerable<VtoItem_KV> strs) {
      return strs.Select(AngularVtoKV.Create).ToList();
    }
  }

  public class AngularVtoString : Base.BaseAngular {
    public AngularVtoString() {
    }

    public AngularVtoString(long id) : base(id) {
    }
    public String Data { get; set; }

    public bool Deleted { get; set; }

    public static AngularVtoString Create(VtoItem_String strs) {
      return new AngularVtoString() {
        Data = strs.Data,
        Id = strs.Id,
        Deleted = strs.DeleteTime != null,
        _ExtraProperties = strs._Extras
      };
    }
    public static List<AngularVtoString> Create(IEnumerable<VtoItem_String> strs) {
      return strs.Select(AngularVtoString.Create).ToList();
    }
  }
  public class AngularVtoDateTime : Base.BaseAngular {
    public AngularVtoDateTime() {
    }

    public AngularVtoDateTime(long id)
  : base(id) {
    }

    public DateTime? Data { get; set; }

    public static AngularVtoDateTime Create(VtoItem_DateTime futureDate) {
      return new AngularVtoDateTime() {
        Id = futureDate.Id,
        Data = futureDate.Data,
      };
    }
  }
  public class AngularVtoDecimal : Base.BaseAngular {
    public AngularVtoDecimal() {
    }

    public AngularVtoDecimal(long id) : base(id) {
    }
    public decimal? Data { get; set; }

    public static AngularVtoDecimal Create(VtoItem_Decimal value) {
      return new AngularVtoDecimal() {
        Id = value.Id,
        Data = value.Data
      };
    }
  }
  #endregion
  public class AngularCoreFocus : Base.BaseAngular {
    public AngularCoreFocus() {
    }

    public AngularCoreFocus(long id) : base(id) {
    }

    public String Purpose { get; set; }
    public String Niche { get; set; }
    public string PurposeTitle { get; set; }
    public string NicheTitle { get; set; }
    public string CoreFocusTitle { get; set; }

    public static AngularCoreFocus Create(CoreFocusModel coreFocus, TermsCollection terms) {
      return new AngularCoreFocus() {
        Id = coreFocus.Id,
        Niche = coreFocus.Niche,
        Purpose = (coreFocus.Purpose),
        PurposeTitle = coreFocus.PurposeTitle ?? terms.GetTerm(TermKey.PurposeCausePassion),
        NicheTitle = coreFocus.NicheTitle ?? terms.GetTerm(TermKey.Niche),
        CoreFocusTitle = coreFocus.CoreFocusTitle ?? terms.GetTerm(TermKey.Focus).ToUpper()

      };
    }
  }
  public class AngularStrategy : Base.BaseAngular {
    public AngularStrategy() {
    }

    public AngularStrategy(long id) : base(id) {
    }
    public String TargetMarket { get; set; }
    public String ProvenProcess { get; set; }
    public String Guarantee { get; set; }
    public String MarketingStrategyTitle { get; set; }
    public String Title { get; set; }
    public IEnumerable<AngularVtoString> Uniques { get; set; }

    internal static AngularStrategy Create(MarketingStrategyModel marketingStrategyModel, TermsCollection terms) {
      return new AngularStrategy() {
        Id = marketingStrategyModel.Id,
        Guarantee = (marketingStrategyModel.Guarantee),
        ProvenProcess = (marketingStrategyModel.ProvenProcess),
        TargetMarket = (marketingStrategyModel.TargetMarket),
        Uniques = AngularVtoString.Create(marketingStrategyModel._Uniques),
        MarketingStrategyTitle = marketingStrategyModel.MarketingStrategyTitle ?? terms.GetTerm(TermKey.MarketingStrategy).ToUpper(),
        Title = (marketingStrategyModel.Title),
      };
    }
  }
  public class AngularThreeYearPicture : Base.BaseAngular, IVtoSectionHeader {
    public AngularThreeYearPicture() {
    }

    public AngularThreeYearPicture(long id)
  : base(id) {
    }
    public DateTime? FutureDate { get; set; }
    public String ThreeYearPictureTitle { get; set; }
    public IEnumerable<AngularVtoString> LooksLike { get; set; }
    public IEnumerable<AngularVtoKV> Headers { get; set; }

    public static AngularThreeYearPicture Create(ThreeYearPictureModel threeYearPicture, TermsCollection terms) {
      return new AngularThreeYearPicture() {
        FutureDate = (threeYearPicture.FutureDate),
        LooksLike = AngularVtoString.Create(threeYearPicture._LooksLike),
        Headers = AngularVtoKV.Create(threeYearPicture._Headers),
        Id = threeYearPicture.Id,
        ThreeYearPictureTitle = threeYearPicture.ThreeYearPictureTitle ?? terms.GetTerm(TermKey.ThreeYearVision).ToUpper()
      };
    }
  }

  public class AngularOneYearPlan : Base.BaseAngular, IVtoSectionHeader {
    public AngularOneYearPlan() {
    }

    public AngularOneYearPlan(long id) : base(id) {
    }
    public DateTime? FutureDate { get; set; }

    public String OneYearPlanTitle { get; set; }
    public IEnumerable<AngularVtoString> GoalsForYear { get; set; }
    public IEnumerable<AngularVtoKV> Headers { get; set; }

    public static AngularOneYearPlan Create(OneYearPlanModel oneYearPlan, TermsCollection terms) {
      return new AngularOneYearPlan() {
        Id = oneYearPlan.Id,
        FutureDate = (oneYearPlan.FutureDate),
        GoalsForYear = AngularVtoString.Create(oneYearPlan._GoalsForYear),
        Headers = AngularVtoKV.Create(oneYearPlan._Headers),
        OneYearPlanTitle = oneYearPlan.OneYearPlanTitle ?? terms.GetTerm(TermKey.OneYearGoals).ToUpper()
      };
    }
  }

  public class AngularQuarterlyRocks : Base.BaseAngular, IVtoSectionHeader {
    public AngularQuarterlyRocks() {
    }

    public AngularQuarterlyRocks(long id) : base(id) {
    }
    public DateTime? FutureDate { get; set; }
    public String Revenue { get; set; }
    public String Profit { get; set; }
    public String Measurables { get; set; }
    public String RocksTitle { get; set; }
    public IEnumerable<AngularVtoRock> Rocks { get; set; }
    public IEnumerable<AngularVtoKV> Headers { get; set; }

    public static AngularQuarterlyRocks Create(QuarterlyRocksModel quarterlyRocksModel, TermsCollection terms) {
      return new AngularQuarterlyRocks() {
        Id = quarterlyRocksModel.Id,
        FutureDate = (quarterlyRocksModel.FutureDate),
        Rocks = quarterlyRocksModel._Rocks,
        Headers = AngularVtoKV.Create(quarterlyRocksModel._Headers),
        RocksTitle = quarterlyRocksModel.RocksTitle ?? terms.GetTerm(TermKey.QuarterlyGoals).ToUpper()
      };
    }

    public bool IsEmpty() {
      return (string.IsNullOrEmpty(Measurables)
        && string.IsNullOrEmpty(Profit)
        && string.IsNullOrEmpty(Revenue));
    }
  }

  public class AngularVtoRock : Base.BaseAngular {
    public AngularVtoRock(long recurRockId) : base(recurRockId) {
    }
#pragma warning disable CS0618
    public AngularVtoRock() {
    }
#pragma warning restore CS0618

    public AngularRock Rock { get; set; }

    public bool Deleted { get; set; }

    public static AngularVtoRock Create(L10Recurrence.L10Recurrence_Rocks recurRock) {
      return new AngularVtoRock() {
        Rock = new AngularRock(recurRock),
        Deleted = recurRock.DeleteTime != null,
        Id = recurRock.Id,
      };
    }
    public static List<AngularVtoRock> Create(IEnumerable<L10Recurrence.L10Recurrence_Rocks> recurRocks) {
      return recurRocks.Select(Create).ToList();
    }


  }
}

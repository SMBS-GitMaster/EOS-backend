using FluentNHibernate.Mapping;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Issues;
using System;
using System.Collections.Generic;

namespace RadialReview.Models.VTO {
  public enum VtoItemType : int {
    Field = 0,
    List_Uniques,
    List_LookLike,
    List_YearGoals,
    List_Issues,
    Header_ThreeYearPicture,
    Header_OneYearPlan,
    Header_QuarterlyRocks,


  }
  #region Core Focus
  public class CoreFocusModel : ILongIdentifiable {
    public virtual long Id { get; set; }
    public virtual long Vto { get; set; }
    public virtual string Purpose { get; set; }
    public virtual String Niche { get; set; }
    public virtual String PurposeTitle { get; set; }
    public virtual String NicheTitle { get; set; }
    public virtual string CoreFocusTitle { get; set; }
    public CoreFocusModel() {
    }

    public class CoreFocusMap : ClassMap<CoreFocusModel> {
      public CoreFocusMap() {
        Id(x => x.Id);
        Map(x => x.Vto).Column("Vto_id");
        Map(x => x.Purpose);
        Map(x => x.PurposeTitle);
        Map(x => x.NicheTitle);
        Map(x => x.CoreFocusTitle);
        Map(x => x.Niche);
        Table("VTO_CoreFocus");
      }
    }


  }

  #endregion
  #region Marketing Strategy

  public class MarketingStrategyModel : ILongIdentifiable {
    public virtual long Id { get; set; }
    public virtual long Vto { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual string TargetMarket { get; set; }
    public virtual string ProvenProcess { get; set; }
    public virtual string Guarantee { get; set; }
    public virtual List<VtoItem_String> _Uniques { get; set; }
    public virtual string MarketingStrategyTitle { get; set; }
    public virtual string Title { get; set; }

    public MarketingStrategyModel() {
      CreateTime = DateTime.UtcNow;
      _Uniques = new List<VtoItem_String>();
    }
    public class MarketingStrategyMap : ClassMap<MarketingStrategyModel> {
      public MarketingStrategyMap() {
        Id(x => x.Id);
        Map(x => x.Vto).Column("Vto_id");
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.MarketingStrategyTitle);
        Map(x => x.Title);
        Map(x => x.ProvenProcess);
        Map(x => x.TargetMarket);
        Map(x => x.Guarantee);
        Table("VTO_MarketingStrategy");
      }
    }

  }

  #endregion
  #region 3 Year Picture
  public class ThreeYearPictureModel : ILongIdentifiable {
    public virtual long Id { get; set; }
    public virtual long Vto { get; set; }
    public virtual DateTime? FutureDate { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual decimal? Revenue { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual decimal? Profit { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string RevenueStr { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string ProfitStr { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string Measurables { get; set; }
    public virtual string ThreeYearPictureTitle { get; set; }
    public virtual List<VtoItem_KV> _Headers { get; set; }
    public virtual List<VtoItem_String> _LooksLike { get; set; }

    public ThreeYearPictureModel() {
      _LooksLike = new List<VtoItem_String>();
      _Headers = new List<VtoItem_KV>();
    }

    public class ThreeYearPictureMap : ClassMap<ThreeYearPictureModel> {
      public ThreeYearPictureMap() {
        Id(x => x.Id);
        Map(x => x.Vto).Column("Vto_id");
        Map(x => x.FutureDate);
        Map(x => x.Revenue);
        Map(x => x.Profit);
        Map(x => x.RevenueStr);
        Map(x => x.ProfitStr);
        Map(x => x.Measurables);
        Map(x => x.ThreeYearPictureTitle);
        Table("VTO_ThreeYearPicture");
      }
    }


  }
  #endregion
  #region 1 Year Plan
  public class OneYearPlanModel : ILongIdentifiable {

    public virtual long Id { get; set; }
    public virtual long Vto { get; set; }
    public virtual DateTime? FutureDate { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual decimal? Revenue { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual decimal? Profit { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string RevenueStr { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string ProfitStr { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string Measurables { get; set; }
    public virtual string OneYearPlanTitle { get; set; }
    public virtual List<VtoItem_String> _GoalsForYear { get; set; }
    public virtual List<VtoItem_KV> _Headers { get; set; }

    public OneYearPlanModel() {
      _GoalsForYear = new List<VtoItem_String>();
      _Headers = new List<VtoItem_KV>();
    }

    public class OneYearPlanMap : ClassMap<OneYearPlanModel> {
      public OneYearPlanMap() {
        Id(x => x.Id);
        Map(x => x.Vto).Column("Vto_id");
        Map(x => x.Revenue);
        Map(x => x.Profit);
        Map(x => x.RevenueStr);
        Map(x => x.ProfitStr);
        Map(x => x.Measurables);
        Map(x => x.FutureDate);
        Map(x => x.OneYearPlanTitle);
        Table("VTO_OneYearPlan");
      }
    }

  }
  #endregion
  #region Quarterly Goals
  public class QuarterlyRocksModel : ILongIdentifiable {
    public virtual long Id { get; set; }
    public virtual long Vto { get; set; }
    public virtual DateTime? FutureDate { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual decimal? Revenue { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual decimal? Profit { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string RevenueStr { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string ProfitStr { get; set; }
    [Obsolete("Do not use. Use VtoItem_KV instead.")]
    public virtual string Measurables { get; set; }
    public virtual string RocksTitle { get; set; }
    public virtual List<AngularVtoRock> _Rocks { get; set; }
    public virtual List<VtoItem_KV> _Headers { get; set; }

    public QuarterlyRocksModel() {
      _Rocks = new List<AngularVtoRock>();
      _Headers = new List<VtoItem_KV>();
    }
    public class QuarterlyRocksMap : ClassMap<QuarterlyRocksModel> {
      public QuarterlyRocksMap() {
        Id(x => x.Id);
        Map(x => x.Vto).Column("Vto_id");
        Map(x => x.Revenue);
        Map(x => x.Profit);
        Map(x => x.RevenueStr);
        Map(x => x.ProfitStr);
        Map(x => x.Measurables);
        Map(x => x.FutureDate);
        Map(x => x.RocksTitle);
        Table("VTO_QuarterlyRocks");
      }
    }


  }
  #endregion
  #region VtoItems
  public abstract class VtoItem : ILongIdentifiable, IHistorical {
    public virtual long Id { get; set; }
    public virtual long BaseId { get; set; }
    public virtual VtoModel Vto { get; set; }
    //public virtual long? VtoId { get; set; }
    public virtual long? CopiedFrom { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual VtoItemType Type { get; set; }
    public virtual int Ordering { get; set; }
    public virtual ForModel ForModel { get; set; }
    public virtual Dictionary<string, object> _Extras { get; set; }

    protected VtoItem() {
      CreateTime = DateTime.UtcNow;
      _Extras = new Dictionary<string, object>();
    }

    public class VtoItemMap : ClassMap<VtoItem> {
      public VtoItemMap() {
        Id(x => x.Id);
        Map(x => x.BaseId);
        References(x => x.Vto).Nullable().LazyLoad();
        //Map(x => x.VtoId).Column("Vto_id");
        Map(x => x.CopiedFrom);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.Type).CustomType<VtoItemType>();
        Map(x => x.Ordering);
        Component(x => x.ForModel).ColumnPrefix("ForModel_");
      }
    }
  }

  public class VtoItem_String : VtoItem {
    public virtual String Data { get; set; }
    public virtual long? MarketingStrategyId { get; set; }

    public class VtoItem_StringMap : SubclassMap<VtoItem_String> {
      public VtoItem_StringMap() {
        Map(x => x.Data);
        Map(x => x.MarketingStrategyId);
      }
    }
    public override string ToString() {
      return Data ?? "";
    }

    [Obsolete("Use VtoAccessor.Addstring()", false)]
    public VtoItem_String() {
    }
  }


  public class VtoItem_KV : VtoItem {
    public virtual String K { get; set; }
    public virtual String V { get; set; }

    public class Map : SubclassMap<VtoItem_KV> {
      public Map() {
        Map(x => x.K);
        Map(x => x.V);
      }
    }
    public override string ToString() {
      var o = K ?? "";
      if (!string.IsNullOrWhiteSpace(K)) {
        o += " : ";
      }
      o += V ?? "";
      return o;
    }

    [Obsolete("Use VtoAccessor.AddKV()", false)]
    public VtoItem_KV() {
    }
  }

  public class VtoItem_Decimal : VtoItem {
    public virtual decimal? Data { get; set; }
    public class VtoItem_DecimalMap : SubclassMap<VtoItem_Decimal> {
      public VtoItem_DecimalMap() {
        Map(x => x.Data);
      }
    }
    public override string ToString() {
      return Data.NotNull(x => String.Format("{0.00##}", x)) ?? "";
    }
  }
  public class VtoItem_DateTime : VtoItem {
    public virtual DateTime? Data { get; set; }
    public class VtoItem_DateTimeMap : SubclassMap<VtoItem_DateTime> {
      public VtoItem_DateTimeMap() {
        Map(x => x.Data);
      }
    }
    public override string ToString() {
      return Data.NotNull(x => x.Value.ToShortDateString()) ?? "";
    }
  }
  public class VtoItem_Bool : VtoItem {
    public VtoItem_Bool() {

    }

    public virtual bool Data { get; set; }
    public class VtoItem_BoolMap : SubclassMap<VtoItem_Bool> {
      public VtoItem_BoolMap() {
        Map(x => x.Data);
      }
    }
    public override string ToString() {
      return Data ? "Yes" : "No";
    }
  }

  #endregion
  #region Goals




  #endregion


  public class VtoIssue : VtoItem_String {

    public IssueModel.IssueModel_Recurrence Issue { get; set; }

  }

  public class VtoModel : ILongIdentifiable, IHistorical {
    public virtual long Id { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual long? CopiedFrom { get; set; }
    public virtual OrganizationModel Organization { get; set; }
    public virtual bool OrganizationWide { get; set; }
    public virtual String Name { get; set; }
    public virtual List<CompanyValueModel> _Values { get; set; }

    public virtual long? PeriodId { get; set; }
    public virtual long? L10Recurrence { get; set; }
    public virtual CoreFocusModel CoreFocus { get; set; }

    [Obsolete("Instead use _MarketingStrategyModel list property.")]
    public virtual MarketingStrategyModel MarketingStrategy { get; set; }

    public virtual ThreeYearPictureModel ThreeYearPicture { get; set; }
    public virtual OneYearPlanModel OneYearPlan { get; set; }
    public virtual QuarterlyRocksModel QuarterlyRocks { get; set; }
    public virtual string TenYearTarget { get; set; }
    public virtual string TenYearTargetTitle { get; set; }
    public virtual string CoreValueTitle { get; set; }

    public virtual string IssuesListTitle { get; set; }
    public virtual List<VtoIssue> _Issues { get; set; }

    public virtual DateTime? LastModified { get; set; }

    public virtual List<MarketingStrategyModel> _MarketingStrategyModel { get; set; }

    public VtoModel() {
      CreateTime = DateTime.UtcNow;
      CoreFocus = new CoreFocusModel();
      MarketingStrategy = new MarketingStrategyModel();
      _Values = new List<CompanyValueModel>();
      _Issues = new List<VtoIssue>();
      ThreeYearPicture = new ThreeYearPictureModel();
      OneYearPlan = new OneYearPlanModel();
      QuarterlyRocks = new QuarterlyRocksModel();
      LastModified = DateTime.UtcNow;
    }

    public class VtoModelMap : ClassMap<VtoModel> {
      public VtoModelMap() {
        Id(x => x.Id);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.CopiedFrom);
        Map(x => x.PeriodId);
        References(x => x.Organization).Not.Nullable().LazyLoad();
        Map(x => x.OrganizationWide);
        Map(x => x.TenYearTarget);
        Map(x => x.TenYearTargetTitle);
        Map(x => x.IssuesListTitle);
        Map(x => x.CoreValueTitle);

        Map(x => x.L10Recurrence);
        Map(x => x.LastModified);

        Map(x => x.Name);

        References(x => x.CoreFocus).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
        References(x => x.MarketingStrategy).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
        References(x => x.ThreeYearPicture).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
        References(x => x.OneYearPlan).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();
        References(x => x.QuarterlyRocks).Not.Nullable().Not.LazyLoad().Cascade.SaveUpdate();

        Table("VTO_Diagram");
      }
    }
  }


}

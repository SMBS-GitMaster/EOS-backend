namespace RadialReview.Models
{
  using FluentNHibernate.Mapping;
  using RadialReview.Models.Interfaces;
  using System;

  #region Model Class

  public class MetricsTabPinnedModel : ILongIdentifiable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual DateTime CreatedTimestamp { get; set; }

    public virtual DateTime DateLastModified { get; set; }

    public virtual long UserId { get; set; }

    public virtual bool IsPinnedToTabBar { get; set; }

    public virtual long MetricsTabId { get; set; }

    #endregion

  }

  #endregion

  #region Mapping Class

  public class MetricsTabPinnedModelMap : ClassMap<MetricsTabPinnedModel>
  {

    #region Constructor

    public MetricsTabPinnedModelMap()
    {
      Id(x => x.Id);
      Map(x => x.Version).Generated.Always();
      Map(x => x.CreatedTimestamp);
      Map(x => x.DateLastModified);
      Map(x => x.UserId);
      Map(x => x.MetricsTabId);
      Map(x => x.IsPinnedToTabBar);
    }

    #endregion

  }

  #endregion

}

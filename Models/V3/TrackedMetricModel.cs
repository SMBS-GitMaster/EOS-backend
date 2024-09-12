using System;
using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models
{

  #region Model Class
  public class TrackedMetricModel : BaseModel, ILongIdentifiable, IDeletable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual string LastUpdatedBy { get; set; }

    public virtual DateTime CreatedTimestamp { get; set; }

    public virtual DateTime DateLastModified { get; set; }

    public virtual DateTime? DeleteTime { get; set; }

    public virtual long UserId { get; set; }

    public virtual TrackedMetricColor Color { get; set; }

    public virtual long ScoreId { get; set; }

    public virtual long MetricTabId { get; set; }

    #endregion

  }

  #endregion

  #region Mapping Class

  public class TrackedMetricModelMap : ClassMap<TrackedMetricModel>
  {

    #region Constructor

    public TrackedMetricModelMap()
    {
      Id(x => x.Id);
      Map(x => x.Version).Generated.Always();
      Map(x => x.LastUpdatedBy);
      Map(x => x.CreatedTimestamp);
      Map(x => x.DateLastModified);
      Map(x => x.DeleteTime);
      Map(x => x.UserId);
      Map(x => x.Color);
      Map(x => x.ScoreId);
      Map(x => x.MetricTabId);
    }

    #endregion

  }

  #endregion

}
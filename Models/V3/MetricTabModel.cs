using System;
using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models
{

  #region Model Class
  public class MetricTabModel : BaseModel, ILongIdentifiable, IDeletable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual string LastUpdatedBy { get; set; }

    public virtual DateTime CreatedTimestamp { get; set; }

    public virtual DateTime DateLastModified { get; set; }

    public virtual DateTime? DeleteTime { get; set; }

    public virtual long UserId { get; set; }

    public virtual string Title { get; set; }

    public virtual UnitType Units { get; set; }

    public virtual Frequency Frequency { get; set; }

    public virtual bool ShareToMeeting { get; set; }

    public virtual long? MeetingId { get; set; }

    #endregion

  }

  #endregion

  #region Mapping Class

  public class MetricTabModelMap : ClassMap<MetricTabModel>
  {

    #region Constructor

    public MetricTabModelMap()
    {
      Id(x => x.Id);
      Map(x => x.Version).Generated.Always();
      Map(x => x.LastUpdatedBy);
      Map(x => x.CreatedTimestamp);
      Map(x => x.DateLastModified);
      Map(x => x.DeleteTime);
      Map(x => x.UserId);
      Map(x => x.Title);
      Map(x => x.Units);
      Map(x => x.Frequency);
      Map(x => x.ShareToMeeting);
      Map(x => x.MeetingId);
    }

    #endregion

  }

  #endregion

}
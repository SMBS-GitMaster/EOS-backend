using System;
using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models
{

  #region Model Class

  public class MeetingSettingsModel : ILongIdentifiable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual long UserId { get; set; }

    public virtual long RecurrenceId { get; set; }

    public virtual double? LastViewedTimestamp { get; set; }

    public virtual int MetricTableWidthDragScrollPct { get; set; }

    public virtual bool GoalVisible { get; set; }

    public virtual bool CumulativeVisible { get; set; }

    public virtual bool AverageVisible { get; set; }

    #endregion

  }

  #endregion

  #region Mapping Class

  public class MeetingSettingsModelMap : ClassMap<MeetingSettingsModel>
  {

    #region Constructor

    public MeetingSettingsModelMap()
    {
      Id(x => x.Id);
      Map(x => x.UserId);
      Map(x => x.RecurrenceId);
      Map(x => x.LastViewedTimestamp);
      Map(x => x.MetricTableWidthDragScrollPct);
      Map(x => x.GoalVisible);
      Map(x => x.CumulativeVisible);
      Map(x => x.AverageVisible);
    }

    #endregion

  }

  #endregion

}
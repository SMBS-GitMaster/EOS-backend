using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Flurl.Util;

namespace RadialReview.Models.Dashboard
{
  public class MeetingTileModel : ILongIdentifiable
  {

    #region Fields

    public virtual long Id { get; set; }

    public virtual bool Hidden { get; set; }

    public virtual DateTime? DeleteTime { get; set; }

    public virtual DateTime CreateTime { get; set; }

    public virtual string Title { get; set; }

    public virtual TileType Type { get; set; }

    [JsonIgnore]
    [IgnoreDataMember]
    public virtual UserModel ForUser { get; set; }

    public virtual string KeyId { get; set; }

    public virtual string V3Positioning { get; set; }

    public virtual string V3StatsFiltering { get; set; }

    #endregion

    #region Constructors

    public MeetingTileModel()
    {
      CreateTime = DateTime.UtcNow;
    }

    public MeetingTileModel(TileType type, UserModel user, string v3Positioning, string v3StatsFiltering, DateTime? createTime)
    {
      Type = type;
      ForUser = user;
      V3Positioning = v3Positioning;
      V3StatsFiltering = v3StatsFiltering;
      if (createTime.HasValue)
      {
        CreateTime = createTime.Value;
      }
      else
      {
        CreateTime = DateTime.UtcNow;
      }
    }

    #endregion

    #region Mapping Class

    public class MeetingTileMap : ClassMap<MeetingTileModel>
    {

      public MeetingTileMap()
      {
        Id(x => x.Id);
        Map(x => x.KeyId);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.Hidden);
        Map(x => x.Title);
        Map(x => x.V3Positioning);
        Map(x => x.V3StatsFiltering);
        Map(x => x.Type).CustomType<TileType>();
        References(x => x.ForUser).LazyLoad();
      }

    }

    #endregion

  }
}
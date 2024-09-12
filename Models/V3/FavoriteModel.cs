using System;
using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models
{

  #region Enumerations

  public enum FavoriteType
  {
    [Display(Name = "MEETING")]
    Meeting,
    [Display(Name = "WORKSPACE")]
    Workspace,
  }

  #endregion

  #region Model Class

  public class FavoriteModel : ILongIdentifiable, IDeletable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual DateTime CreatedDateTime { get; set; }

    public virtual DateTime DateLastModified { get; set; }

    public virtual DateTime? DeleteTime { get; set; }

    public virtual int Position { get; set; }

    public virtual FavoriteType ParentType { get; set; }

    public virtual long ParentId { get; set; }

    public virtual long UserId { get; set; }

    #endregion

  }

  #endregion

  #region Mapping Class

  public class FavoriteModelMap : ClassMap<FavoriteModel>
  {

    #region Constructor

    public FavoriteModelMap()
    {
      Id(x => x.Id);
      Map(x => x.Version).Generated.Always();
      Map(x => x.CreatedDateTime);
      Map(x => x.DateLastModified);
      Map(x => x.DeleteTime);
      Map(x => x.Position);
      Map(x => x.ParentType);
      Map(x => x.ParentId).Index("idx_favorite_parent_id");
      Map(x => x.UserId).Index("idx_favorite_user_id");
    }

    #endregion

  }

  #endregion

}
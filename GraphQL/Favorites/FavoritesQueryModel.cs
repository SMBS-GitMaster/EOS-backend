using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models
{

  public class FavoriteQueryModel : ILongIdentifiable, IDeletable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual DateTime CreatedDateTime { get; set; }

    public virtual DateTime DateLastModified { get; set; }

    public virtual DateTime? DeleteTime { get; set; }

    public virtual int Position { get; set; }

    public virtual gqlFavoriteType ParentType { get; set; }

    public virtual long ParentId { get; set; }

    public virtual long UserId { get; set; }

    #endregion

  }

}
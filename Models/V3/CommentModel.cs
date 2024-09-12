using System;
using System.ComponentModel.DataAnnotations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models
{

  #region Enumerations

  public enum ParentType
  {
    [Display(Name = "ISSUE")]
    Issue,
    [Display(Name = "HEADLINE")]
    PeopleHeadline,
    [Display(Name = "TODO")]
    Todo,
  }

  #endregion

  #region Model Class
  public class CommentModel : ILongIdentifiable, IDeletable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual DateTime PostedDateTime { get; set; }

    public virtual DateTime DateLastModified { get; set; }

    public virtual DateTime? DeleteTime { get; set; }

    public virtual string Body { get; set; }

    public virtual ParentType CommentParentType { get; set; }

    public virtual long ParentId { get; set; }

    public virtual long AuthorId { get; set; }

    #endregion

  }

  #endregion

  #region Mapping Class

  public class CommentModelMap : ClassMap<CommentModel>
  {

    #region Constructor

    public CommentModelMap()
    {
      Id(x => x.Id);
      Map(x => x.Version).Generated.Always();
      Map(x => x.PostedDateTime);
      Map(x => x.DateLastModified);
      Map(x => x.DeleteTime);
      Map(x => x.Body);
      Map(x => x.CommentParentType);
      Map(x => x.ParentId);
      Map(x => x.AuthorId);
    }

    #endregion

  }

  #endregion

}
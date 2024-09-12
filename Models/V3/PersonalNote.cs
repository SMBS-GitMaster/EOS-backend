using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models
{
  public class PersonalNote : ILongIdentifiable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual string LastUpdatedBy { get; set; }

    public virtual DateTime? DateCreated { get; set; }

    public virtual DateTime? DateLastModified { get; set; }

    public virtual DateTime? DeleteTime { get; set; }

    public virtual string Title { get; set; }

    public virtual string PadId { get; set; }

    public virtual long WorkspaceId { get; set; }

    #endregion

    #region Constructor

    public PersonalNote()
    {
      PadId = Guid.NewGuid().ToString();
    }

    #endregion
  }

  public class PersonalNoteMap : ClassMap<PersonalNote>
  {

    public PersonalNoteMap()
    {
      Id(x => x.Id);
      Map(x => x.Version);
      Map(x => x.LastUpdatedBy);
      Map(x => x.DateCreated);
      Map(x => x.DateLastModified);
      Map(x => x.DeleteTime);
      Map(x => x.Title);
      Map(x => x.PadId);
      Map(x => x.WorkspaceId);
    }

  }

}
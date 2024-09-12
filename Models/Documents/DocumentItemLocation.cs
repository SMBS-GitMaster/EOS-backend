using FluentNHibernate.Mapping;
using NHibernate;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models.Documents {


  public class DocumentItemLocation : ILongIdentifiable, IHistorical {
    public virtual long Id { get; set; }
    public virtual long DocumentFolderId { get; set; }
    public virtual long ItemId { get; set; }

    public virtual DocumentItemType ItemType { get; set; }

    public virtual long SourceOrganizationId { get; set; }
    public virtual DateTime CreateTime { get;set; }
    public virtual DateTime? DeleteTime { get; [Obsolete("Use DocumentAccessor._DeleteLink_Unsafe instead")] set; }
    public virtual bool IsShortcut { get; set; }


    public class Map : ClassMap<DocumentItemLocation> {
      public Map() {
        Id(x => x.Id);
        Map(x => x.ItemId);
        Map(x => x.DocumentFolderId);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.ItemType).CustomType<DocumentItemType>();
        Map(x => x.SourceOrganizationId);
        Map(x => x.IsShortcut);
      }
    }

    [Obsolete("use DocumentAccessor._SaveLink_Unsafe")]
    public DocumentItemLocation() {
    }

    //[Obsolete("do not use", true)]
    //public static DocumentItemLocation CreateFrom(ISession s, DocumentsFolder parent, DocumentsFolder child, bool shortcut = false) {
    //  var res =  new DocumentItemLocation {
    //    CreateTime = DateTime.UtcNow,
    //    DocumentFolderId = parent.Id,
    //    IsShortcut = shortcut,
    //    ItemId = child.Id,
    //    ItemType = DocumentItemType.DocumentFolder,
    //    SourceOrganizationId = parent.OrgId,
    //  };

    //  s.Save(res);

    //  return res;
    //}
  }
}

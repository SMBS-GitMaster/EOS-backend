using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models {
  public class WhiteboardModel : ILongIdentifiable, IHistorical {
    public virtual long Id { get; set; }
    public virtual string Name { get; set; }
    public virtual string Description { get; set; }
    public virtual string IconHint { get; set; }
    public virtual string LookupId { get; set; }
    public virtual long OrgId { get; set; }
    public virtual long CreatedBy { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual int Version { get; set; }
    public virtual long LastDiff { get; set; }
    public virtual DateTime? LastDiffTime { get; set; }
    public virtual string SvgUrl { get; set; }
    public virtual bool IsTemplate { get; set; }
    private string _diffs { get; set; }

    public virtual void SetDiffs(string diffs) {
      _diffs = diffs;
    }
    public virtual string GetDiffs() {
      return _diffs;
    }

    public WhiteboardModel() {
      CreateTime = DateTime.UtcNow;
    }


    public class Map : ClassMap<WhiteboardModel> {
      public Map() {
        Id(x => x.Id);
        Map(x => x.LookupId).Index("IDX_WHITEBOARD_LOOKUPID");
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.OrgId);
        Map(x => x.CreatedBy);
        Map(x => x.Version);
        Map(x => x.LastDiff);
        Map(x => x.LastDiffTime);
        Map(x => x.Name);
        Map(x => x.Description);
        Map(x => x.IconHint);
        Map(x => x.SvgUrl);
        Map(x => x.IsTemplate);
      }
    }
  }
}

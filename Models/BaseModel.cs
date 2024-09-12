using System;
using FluentNHibernate.Mapping;

namespace RadialReview.Models
{
  public class BaseModel 
  {
    public virtual  int Version { get; set; }
    public virtual  string LastUpdatedBy { get; set; }
    // public double DateCreated { get; set; }
    public virtual  double DateLastModified { get; set; }
  }

  public class BaseModelClassMap<T> : ClassMap<T> 
    where T : BaseModel 
  {
    public BaseModelClassMap() 
    {
      /*
      Version(x => x.Version).Generated.Always(); // TODO: Enable versioning
      */

      Map(x => x.Version).Generated.Always();
      Map(x => x.LastUpdatedBy);
      Map(x => x.DateLastModified);
    }
  }
}
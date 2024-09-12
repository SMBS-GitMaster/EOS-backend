using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models {
  public class JointOrganizationModel {
    public virtual long Id { get; set; }
    public virtual long ParentOrgId { get; set; }
    public virtual long ChildOrgId { get; set; }

    public virtual OrganizationModel ParentOrg { get; set; }
    public virtual OrganizationModel ChildOrg { get; set; }

    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }

    public virtual bool ParentPays { get; set; }

    public JointOrganizationModel() {
      CreateTime = DateTime.UtcNow;
      ParentPays = true;
    }

    public class Map : ClassMap<JointOrganizationModel> {
      public Map() {
        Id(x => x.Id);
        Map(x => x.ParentOrgId).Column("ParentOrgId");
        Map(x => x.ChildOrgId).Column("ChildOrgId");
        Map(x => x.ParentPays);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        References(x => x.ParentOrg).Column("ParentOrgId").ReadOnly().LazyLoad();
        References(x => x.ChildOrg).Column("ChildOrgId").ReadOnly().LazyLoad();


      }
    }

  }
}

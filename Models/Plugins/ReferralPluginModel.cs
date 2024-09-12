using FluentNHibernate.Mapping;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PuppeteerSharp;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Terms
{


  public class ReferralPluginModel
  {
    public ReferralPluginModel()
    {
      CreateTime = DateTime.UtcNow;
    }
    public virtual ReferralPluginIdentifier ReferralPluginIdentifier { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual string ReferralCode { get; set; }
    public virtual string ReferralSource { get; set; }
    public virtual long TermsPluginId { get; set; }

    public class Map : ClassMap<ReferralPluginModel>
    {
      public Map()
      {
        CompositeId(x => x.ReferralPluginIdentifier).KeyProperty(x => x.ReferralSource).KeyProperty(x => x.ReferralCode);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);

        //Map(x => x.ReferralCode);
        //Map(x => x.ReferralSource);
        Map(x => x.TermsPluginId);
      }

    }
  }
  public class ReferralPluginIdentifier
  {
    public virtual string ReferralSource { get; set; }
    public virtual string ReferralCode { get; set; }

    public override bool Equals(object obj)
    {
      if (obj == null)
        return false;
      var t = obj as ReferralPluginIdentifier;
      if (t == null)
        return false;
      if (ReferralSource == t.ReferralSource && ReferralSource == t.ReferralSource)
        return true;
      return false;
    }

    public override int GetHashCode()
    {
      return (ReferralSource + "|" + ReferralCode).GetHashCode();
    }
  }
}

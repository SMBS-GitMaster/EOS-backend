using FluentNHibernate.Mapping;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Application {
  public class EncryptedKeyValueModel {

    public const string OUTER_ENCRYPT = "2YbXmjsAuxlEstrheg5WM1YPv8yOFUIrUJ2UiR72";

    public virtual string K { get; set; }
    public virtual string V { get; set; }
    public virtual DateTime ValidUntil { get; set; }

    public EncryptedKeyValueModel() {
      K = RandomUtil.SecureRandomString(32);
    }

    public class Map : ClassMap<EncryptedKeyValueModel> {
      public Map() {
        Id(x => x.K).GeneratedBy.Assigned();
        Map(x => x.V);
        Map(x => x.ValidUntil);
      }

    }
  }
}

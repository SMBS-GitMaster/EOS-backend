using FluentNHibernate.Mapping;
using Microsoft.AspNetCore.Identity;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview.Models
{
    public class UserLogin : IdentityUserLogin<string>, ILongIdentifiable
    {
        public virtual long Id { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
  }

    public class UserLoginModelMap : ClassMap<UserLogin>
    {
      public UserLoginModelMap()
      {
        Id(x => x.Id);
        Map(x => x.ProviderKey);
        Map(x => x.LoginProvider);
        Map(x => x.ProviderDisplayName);
        Map(x => x.DeleteTime);
        Map(x => x.UserId).Column("UserId");

      }
    }
}

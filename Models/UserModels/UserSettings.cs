using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FluentNHibernate.Mapping;
using Microsoft.AspNetCore.Identity;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.UserModels;
using System.Security.Claims;
using RadialReview.Core.GraphQL.Enumerations;

namespace RadialReview.Models
{
  public class UserSettings : ILongIdentifiable
  {
    public virtual long Id { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual long UserId { get; set; }
    public virtual string Timezone { get; set; }
    public virtual gqlDrawerView DrawerView { get; set; }
    public virtual bool HasViewedFeedbackModalOnce { get; set; }
    public virtual bool DoNotShowFeedbackModalAgain { get; set; }
    public virtual int TransferredBusinessPlansBannerViewCount { get; set; }

    public virtual long? WorkspaceHomeId { get; set; }

    public virtual DashboardType? WorkspaceHomeType { get; set; }

    public class UserSettingsMap : ClassMap<UserSettings>
    {
      public UserSettingsMap()
      {
        Id(x => x.Id);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.UserId);
        Map(x => x.Timezone);
        Map(x => x.HasViewedFeedbackModalOnce);
        Map(x => x.DoNotShowFeedbackModalAgain);
        Map(x => x.DrawerView);
        Map(x => x.TransferredBusinessPlansBannerViewCount);
        Map(x => x.WorkspaceHomeId);
        Map(x => x.WorkspaceHomeType);
      }
    }
  } 
}

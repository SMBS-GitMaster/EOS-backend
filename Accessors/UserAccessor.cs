using DocumentFormat.OpenXml.ExtendedProperties;
using NHibernate;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.Exceptions;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public partial class UserAccessor : BaseAccessor {
    public static Task EditUser(UserOrganizationModel caller, UserEditModel userEditModel)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditUserModel(userEditModel.UserId);

          var userOrganizationModel = s.Get<UserOrganizationModel>(userEditModel.UserId);
          var user = s.Get<UserModel>(userOrganizationModel.UserModelId);
          var settings = s.QueryOver<UserSettings>().Where(s => s.UserId == userEditModel.UserId).List().FirstOrDefault();

          if (!string.IsNullOrEmpty(userEditModel.FirstName))
          {
            if (user.FirstName != userEditModel.FirstName)
            {
              user.FirstName = userEditModel.FirstName;
            }
          }

          if (!string.IsNullOrEmpty(userEditModel.LastName))
          {
            if (user.LastName != userEditModel.LastName)
            {
              user.LastName = userEditModel.LastName;
            }
          }

          if (settings == null)
          {
            settings = new UserSettings();
            settings.UserId = userEditModel.UserId;
            settings.CreateTime = DateTime.Now;
            s.Save(settings);
          }

          if (settings.Timezone != userEditModel.Timezone)
          {
            settings.Timezone = userEditModel.Timezone;
          }

          if (settings.HasViewedFeedbackModalOnce != userEditModel.Settings.HasViewedFeedbackModalOnce)
          {
            settings.HasViewedFeedbackModalOnce = userEditModel.Settings.HasViewedFeedbackModalOnce;
          }

          if (settings.DoNotShowFeedbackModalAgain != userEditModel.Settings.DoNotShowFeedbackModalAgain)
          {
            settings.DoNotShowFeedbackModalAgain = userEditModel.Settings.DoNotShowFeedbackModalAgain;
          }

          if(settings.TransferredBusinessPlansBannerViewCount != userEditModel.Settings.TransferredBusinessPlansBannerViewCount)
          {
            settings.TransferredBusinessPlansBannerViewCount = userEditModel.Settings.TransferredBusinessPlansBannerViewCount;
          }

          if (userEditModel.Settings.HomePageWorkspaceType == "NONE")
          {
            // Special delete case
            settings.WorkspaceHomeType = null;
            settings.WorkspaceHomeId = null;
          }
          else
          {
            if (userEditModel.Settings.HomePageWorkspaceType != null)
            {
              settings.WorkspaceHomeType = (Models.Enums.DashboardType)Enum.Parse(typeof(Models.Enums.DashboardType), userEditModel.Settings.HomePageWorkspaceType);
            }
            if (userEditModel.Settings.HomePageMeetingOrWorkspaceId != null)
            {
              settings.WorkspaceHomeId = userEditModel.Settings.HomePageMeetingOrWorkspaceId;
            }
          }

          s.Update(user);
          s.Update(settings);
          tx.Commit();
          s.Flush();
        }
      }

      return Task.CompletedTask;
    }

    public static UserSettingsQueryModel GetSettings(UserOrganizationModel caller, long userId)
    {
      using(var s = HibernateSession.GetCurrentSession())
      {
        using(var tx = s.BeginTransaction())
        {
          var result = s.QueryOver<UserSettings>().Where(s => s.UserId == userId).List().FirstOrDefault();
          if(result != null)
          {
            return UserSettingsTransformer.TransformTimeZone(caller, result);
          }
          return new UserSettingsQueryModel();
        }
      }
    }

    public static List<UserOrganizationModel> GetUsersByIdsUnsafe(List<long> userIds, ISession s)
    {
      return s.QueryOver<UserOrganizationModel>()
            .Where(x => x.DeleteTime == null)
            .WhereRestrictionOn(x => x.Id).IsIn(userIds)
            .List().ToList();
    }

    public static List<UserOrganizationModel> GetUserOrganizationsForUser(UserModel user, ISession s)
    {
      var userOrgs = new List<UserOrganizationModel>();
      foreach (var userOrg in user.UserOrganization.ToListAlive())
      {
        userOrgs.Add(GetUserOrganizationModelForRole(s, userOrg.Id));
      }
      return userOrgs;
    }

    private static UserOrganizationModel GetUserOrganizationModelForRole(ISession session, long id)
    {
      var result = session.Get<UserOrganizationModel>(id);
      return result;
    }
  }
}

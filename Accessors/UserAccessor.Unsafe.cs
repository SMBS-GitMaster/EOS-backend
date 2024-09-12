using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;


namespace RadialReview.Accessors {
  public partial class UserAccessor {
    [Obsolete("Unsafe")]
    public partial class Unsafe {
      [Obsolete("Dont use this elsewhere")]
      public static UserModel GetUserByEmail(ISession s, string email) {
        if (email == null) {
          throw new LoginException();
        }

        var lower = email.ToLower();
        return s.QueryOver<UserModel>().Where(x => x.UserName == lower).SingleOrDefault();
      }

      [Obsolete("Dont use this elsewhere")]
      public static UserModel GetUserByEmail(string email) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return GetUserByEmail(s, email);
          }
        }
      }

      public static UserModel GetUserModelById(string userId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return GetUserModelById(s, userId);
          }
        }
      }

      public static UserModel GetUserModelById(ISession s, string userId) {
        if (userId == null) {
          throw new LoginException();
        }
        return s.Get<UserModel>(userId);
      }

      public static UserOrganizationModel GetUserOrganizationById(long userOrganizationId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return s.Get<UserOrganizationModel>(userOrganizationId);
          }
        }
      }

      [Obsolete("Very expensive for many users")]
      public static List<UserOrganizationModel> GetUserOrganizationsForUser(string userId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return GetUserOrganizationsForUser(s, userId);
          }
        }
      }

      public class UserOrgIdAndGuid {
        public long UserOrgId { get; set; }
        public string UserGuid { get; set; }
      }

      public static IEnumerable<UserOrgIdAndGuid> GetUserOrganizationIdsForUser(ISession s, string userId) {
        if (string.IsNullOrWhiteSpace(userId)) {
          throw new LoginException();
        }
        //var user = s.Get<UserModel>(userId);
        return s.QueryOver<UserOrganizationModel>()
          .Where(x => x.DeleteTime == null && x.User.Id == userId)
          .Select(x => x.Id)
          .Future<long>()
          .Select(x => new UserOrgIdAndGuid {
            UserOrgId = x,
            UserGuid = userId
          });
      }
      public static IEnumerable<UserOrgIdAndGuid> GetUserOrganizationIdsForUserByEmail(ISession s, string email)
      {
        if (string.IsNullOrWhiteSpace(email))
        {
          throw new LoginException();
        }
        //var user = s.Get<UserModel>(userId);
        var userId = s.QueryOver<UserModel>().Where(x => x.UserName == email).Select(x => x.Id).Future<string>().First();
        return s.QueryOver<UserOrganizationModel>()
          .Where(x => x.DeleteTime == null && x.User.Id == userId)
          .Select(x => x.Id)
          .Future<long>()
          .Select(x => new UserOrgIdAndGuid
          {
            UserOrgId = x,
            UserGuid = userId
          });
        //.Select(x => new UserOrgIdAndGuid { UserOrgId = x.Id, UserGuid = x.User.Id })
        //.Future<UserOrgIdAndGuid>();
        //.Select(x => new UserOrgIdAndGuid { UserOrgId = x.Id, UserGuid = x.User.Id });
        //.Future<UserOrgIdAndGuid>();
      }

      [Obsolete("Very expensive for many users")]
      public static List<UserOrganizationModel> GetUserOrganizationsForUser(ISession s, string userId) {
        if (userId == null) {
          throw new LoginException();
        }
        var user = s.Get<UserModel>(userId);
        if (user == null) {
          throw new LoginException();
        }
        var userOrgs = new List<UserOrganizationModel>();
        foreach (var userOrg in user.UserOrganization.ToListAlive()) {
          userOrgs.Add(GetUserOrganizationModelForRole(s, userOrg.Id));
        }
        return userOrgs;
      }

      public static UserOrganizationModel GetUserOrganizationForUserIdAndRole(string userId, long userOrganizationId) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            return GetUserOrganizationForUserIdAndRole(s, userId, userOrganizationId);
          }
        }
      }

      public static UserOrganizationModel GetUserOrganizationForUserIdAndRole(ISession s, string userId, long userOrganizationId) {
        if (userId == null) {
          throw new LoginException();
        }

        var userModel = s.Get<UserModel>(userId);
        long matchId;
        if (userModel.DeleteTime != null)
          userModel = null;

        if (userModel == null || !userModel.IsRadialAdmin) {
          if (userModel == null) {
            throw new LoginException();
          }
          var match = s.Get<UserOrganizationModel>(userOrganizationId);
          if (match.DetachTime != null || match.DeleteTime != null || match.User == null || match.User.Id != userId) {
            throw new OrganizationIdException();
          }
          matchId = match.Id;
        } else {
          matchId = userOrganizationId;
        }

        return GetUserOrganizationModelForRole(s, matchId);
      }

      private static UserOrganizationModel GetUserOrganizationModelForRole(ISession session, long id) {
        var result = session.Get<UserOrganizationModel>(id);
        if (Config.IsTest() && result.IsRadialAdmin) {
          result._IsTestAdmin = true;
        }
        return result;
      }
    }




  }
}

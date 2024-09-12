using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;


namespace RadialReview.Accessors {
  public partial class UserAccessor : BaseAccessor {


    public static List<TinyUser> Search(UserOrganizationModel caller, long orgId, string search, int take = int.MaxValue, long[] exclude = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller).ViewOrganization(orgId);
          var users = TinyUserAccessor.GetOrganizationMembers(s, perms, orgId);
          exclude = exclude ?? new long[0];
          users = users.Where(x => !exclude.Any(y => y == x.UserOrgId)).ToList();
          var splits = search.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          var dist = new DiscreteDistribution<TinyUser>(0, 7, true);
          foreach (var u in users) {
            var fname = false;
            var lname = false;
            var ordered = false;
            var fnameStart = false;
            var lnameStart = false;
            var wasFirst = false;
            var exactFirst = false;
            var exactLast = false;
            var f = u.FirstName.ToLower();
            var l = u.LastName.ToLower();
            foreach (var t in splits) {
              if (f.Contains(t)) {
                fname = true;
              }

              if (f == t) {
                exactFirst = true;
              }

              if (f.StartsWith(t)) {
                fnameStart = true;
              }

              if (l.Contains(t)) {
                lname = true;
              }

              if (l.StartsWith(t)) {
                lnameStart = true;
              }

              if (fname && !wasFirst && lname) {
                ordered = true;
              }

              if (l == t) {
                exactLast = true;
              }

              wasFirst = true;
            }

            var score = fname.ToInt() + lname.ToInt() + ordered.ToInt() + fnameStart.ToInt() + lnameStart.ToInt() + exactFirst.ToInt() + exactLast.ToInt();
            if (score > 0) {
              dist.Add(u, score);
            }
          }

          return dist.GetProbabilities().OrderByDescending(x => x.Value).Select(x => x.Key).Take(take).ToList();
        }
      }
    }




    public static UserOrganizationModel GetUserOrganization(ISession s, PermissionsUtility perms, long userOrganizationId, bool asManager, bool sensitive) {
      return GetUserOrganization(s.ToQueryProvider(true), perms, perms.GetCaller(), userOrganizationId, asManager, sensitive);
    }

    public static UserOrganizationModel GetUserOrganization(UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetUserOrganization(s.ToQueryProvider(true), perms, caller, userOrganizationId, asManager, sensitive);
        }
      }
    }

    public static UserOrganizationModel GetUserOrganization(AbstractQuery s, PermissionsUtility perms, UserOrganizationModel caller, long userOrganizationId, bool asManager, bool sensitive) {
      perms.ViewUserOrganization(userOrganizationId, sensitive);
      if (asManager) {
        perms.ManagesUserOrganization(userOrganizationId, false);
      }
      return s.Get<UserOrganizationModel>(userOrganizationId);
    }


    public static int GetUserOrganizationCounts(ISession s, string userId, string redirectUrl, Boolean full = false) {
      if (userId == null) {
        throw new LoginException(null, redirectUrl);
      }
      var user = s.Get<UserModel>(userId);
      if (user == null) {
        throw new LoginException(null, redirectUrl);
      }
      return user.UserOrganizationCount;
    }




  }
}

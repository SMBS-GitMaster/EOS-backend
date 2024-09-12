using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Documents;
using RadialReview.Models.Enums;
using RadialReview.Models.Integrations;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Prereview;
using RadialReview.Models.Process;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Survey;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.VTO;
using RadialReview.Reflection;
using RadialReview.Utilities.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace RadialReview.Utilities {
  public partial class PermissionsUtility {


    public bool IsPermitted(Action<PermissionsUtility> ensurePermitted) {
      try {
        ensurePermitted(this);
        return true;
      } catch (Exception) {
        return false;
      }
    }



    private bool InTryWithAll = false;
    public PermissionsUtility TryWithAlternateUsers(Func<PermissionsUtility, PermissionsUtility> p) {
      try {
        return p(this);
      } catch (PermissionsException e) {
        if (!InTryWithAll) {
          var originalCaller = caller;
          if (caller.User != null && caller.User.UserOrganizationIds != null) {
            foreach (var id in caller.User.UserOrganizationIds) {
              if (id != caller.Id) {
                try {
                  var tempcaller = session.Get<UserOrganizationModel>(id);
                  if (tempcaller == null || tempcaller.DeleteTime != null) {
                    continue;
                  }

                  var perm = new PermissionsUtility(session, tempcaller) { InTryWithAll = true };
                  caller = tempcaller;
                  return p(perm);
                } catch (PermissionsException) {
                } finally {
                  caller = originalCaller;
                }
              }
            }
          }
          caller = originalCaller;
        } else {
          int a = 0;
        }

        throw e;
      }
    }

    public PermissionsUtility TryWithOverrides(Func<PermissionsUtility, PermissionsUtility> p) {
      return p(this);
    }

    public PermissionsUtility Or(params Func<PermissionsUtility>[] or) {
      foreach (var o in or) {
        try {
          return o();
        } catch (PermissionsException) { } catch (Exception) { }
      }
      throw new PermissionsException();
    }

    public PermissionsUtility Or(params Func<PermissionsUtility, PermissionsUtility>[] or) {
      foreach (var o in or) {
        try {
          return o(this);
        } catch (PermissionsException) { } catch (Exception) { }
      }
      throw new PermissionsException();
    }

  }
}

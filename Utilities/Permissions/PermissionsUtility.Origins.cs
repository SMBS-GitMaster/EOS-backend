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
    public PermissionsUtility EditOrigin(Origin origin, bool manageOnly = false) {
      return EditOrigin(origin.OriginType, origin.OriginId, manageOnly);
    }

    public PermissionsUtility EditOrigin(OriginType origin, long originId, bool manageOnly = false) {
      switch (origin) {
        case OriginType.User:
          if (manageOnly) {
            return ManagesUserOrganization(originId, true);
          }

          return EditUserOrganization(originId);
        case OriginType.Organization:
          return EditOrganization(originId);
        case OriginType.Industry:
          return EditIndustry(originId);
        case OriginType.Application:
          return EditApplication(originId);
        case OriginType.Invalid:
          throw new PermissionsException();
        default:
          throw new PermissionsException();
      }
    }
    public PermissionsUtility ViewOrigin(OriginType originType, long originId) {
      switch (originType) {
        case OriginType.User:
          return ViewUserOrganization(originId, false);
        case OriginType.Organization:
          return ViewOrganization(originId);
        case OriginType.Industry:
          return ViewIndustry(originId);
        case OriginType.Application:
          return ViewApplication(originId);
        case OriginType.Invalid:
          throw new PermissionsException();
        default:
          throw new PermissionsException();
      }
    }
  }
}

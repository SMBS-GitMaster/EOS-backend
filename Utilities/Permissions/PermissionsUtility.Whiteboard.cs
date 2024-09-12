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
    public PermissionsUtility ViewWhiteboard(string whiteboardId) {
      var wb = session.QueryOver<WhiteboardModel>().Where(x => x.LookupId == whiteboardId).Take(1).SingleOrDefault();
      if (wb == null)
        throw new PermissionsException("Whiteboard does not exist");

      return CanView(PermItem.ResourceType.Whiteboard, wb.Id, exceptionMessage: "Cannot view this whiteboard", includeAlternateUsers: true);
    }
    public PermissionsUtility EditWhiteboard(string whiteboardId) {
      var wb = session.QueryOver<WhiteboardModel>().Where(x => x.LookupId == whiteboardId).Take(1).SingleOrDefault();
      if (wb == null)
        throw new PermissionsException("Whiteboard does not exist");
      return CanEdit(PermItem.ResourceType.Whiteboard, wb.Id, exceptionMessage: "Cannot edit this whiteboard", includeAlternateUsers: true);
    }

    public PermissionsUtility AdminWhiteboard(long id) {
      var wb = session.Get<WhiteboardModel>(id);
      if (wb == null)
        throw new PermissionsException("Whiteboard does not exist");
      return CanAdmin(PermItem.ResourceType.Whiteboard, wb.Id, exceptionMessage: "Cannot admin this whiteboard", includeAlternateUsers: true);
    }
  }
}

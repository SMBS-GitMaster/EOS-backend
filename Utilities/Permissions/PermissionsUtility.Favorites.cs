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
    public PermissionsUtility CreateFavorite(long forUserId, FavoriteType parentType, long parentId) {
      switch (parentType) {
        case FavoriteType.Meeting:
          return ViewL10Recurrence(parentId).Self(forUserId);
        case FavoriteType.Workspace:
          return Self(forUserId);
        default:
          throw new PermissionsException("unhandled Favorite Type");
      }
    }

    public PermissionsUtility ViewFavorite(long favoriteId) {
      var found = session.Get<FavoriteModel>(favoriteId);
      if (found==null)
        throw new PermissionsException("Cannot edit favorite");
      return CreateFavorite(found.UserId, found.ParentType, found.ParentId);
    }

    public PermissionsUtility ViewFavorite(long forUserId, FavoriteType parentType, long parentId) {
      return CreateFavorite(forUserId, parentType, parentId);
    }


    public PermissionsUtility EditFavorite(long favoriteId) {
      var found = session.Get<FavoriteModel>(favoriteId);
      if (found==null)
        throw new PermissionsException("Cannot edit favorite");
      return CreateFavorite(found.UserId, found.ParentType, found.ParentId);
    }
  }
}

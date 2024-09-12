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

    public PermissionsUtility CanViewParentType(ParentType parentType, long parentId) {
      switch (parentType) {
        case ParentType.Issue:
          return ViewIssueRecurrence(parentId);
        case ParentType.PeopleHeadline:
          return ViewHeadline(parentId);
        case ParentType.Todo:
          return ViewTodo(parentId);
        default:
          throw new PermissionsException("Unhandled Parent Type"+parentType);
      }
    }

    public PermissionsUtility EditForModel(ForModel model) {
      if (model.ModelType == ForModel.GetModelType<L10Recurrence>()) {
        return EditL10Recurrence(model.ModelId);
      }

      if (model.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
        return EditUserOrganization(model.ModelId);
      }

      throw new PermissionsException("ModelType unhandled");
    }
    public PermissionsUtility ViewForModel(IForModel model) {
      if (model.ModelType == ForModel.GetModelType<L10Recurrence>()) {
        return ViewL10Recurrence(model.ModelId);
      }

      if (model.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
        return ViewUserOrganization(model.ModelId, false);
      }

      if (model.ModelType == ForModel.GetModelType<OrganizationModel>()) {
        return ViewOrganization(model.ModelId);
      }

      if (model.Is<TodoModel>()) {
        return ViewTodo(model.ModelId);
      }

      if (model.Is<RockModel>()) {
        return ViewRock(model.ModelId);
      }

      if (model.Is<IssueModel.IssueModel_Recurrence>()) {
        return ViewIssueRecurrence(model.ModelId);
      }

      if (model.Is<PeopleHeadline>()) {
        return ViewHeadline(model.ModelId);
      }

      throw new PermissionsException("ModelType unhandled");
    }
  }
}

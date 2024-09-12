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

    public PermissionsUtility CreatedSurvey(long surveyContainerId) {
      var surveyContainer = session.Get<SurveyContainer>(surveyContainerId);
      if (caller.ToKey() != surveyContainer.GetCreator().ToKey()) {
        throw new PermissionsException();
      }

      return this;
    }

    public PermissionsUtility CreateQuarterlyConversation(long orgId) {
      return ManagerAtOrganization(caller.Id, orgId);
    }
    public PermissionsUtility ViewSurvey(long surveyId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var survey = session.Get<Survey>(surveyId);
      ViewSurveyContainer(survey.SurveyContainerId);
      return CanView(PermItem.ResourceType.Survey, surveyId);
    }
    public PermissionsUtility ViewSurveyResultsAbout(long surveyContainerId, IForModel about) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var surveyContainer = session.Get<SurveyContainer>(surveyContainerId);
      if (surveyContainer.CreatedBy.ToKey() == caller.ToKey()) {
        return this;
      }

      throw new PermissionsException("Cannot view this");
    }

    public PermissionsUtility ViewSurveyContainer(long surveyContainerId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      return CanView(PermItem.ResourceType.SurveyContainer, surveyContainerId);
    }

    public PermissionsUtility EditSurveyResponse(long surveyResponseId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var response = session.Get<SurveyResponse>(surveyResponseId);


      if (response == null || response.DeleteTime != null) {
        throw new PermissionsException("Response does not exist.");
      }

      var container = session.Get<SurveyContainer>(response.SurveyContainerId);

      if (container.DueDate < DateTime.UtcNow) {
        throw new PermissionsException("Cannot edit: Already wrapped up.");
      }

      if (response.By.Is<UserOrganizationModel>()) {
        if (caller.Id == response.By.ModelId) {
          return this;
        } else {
          throw new PermissionsException("Cannot edit this response");
        }
      }
      throw new PermissionsException("Unknown 'by' type");
    }
  }
}

﻿using log4net;
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
    public PermissionsUtility ViewOldSurveyContainer(long surveyId) {

      if (IsRadialAdmin(caller)) {
        return this;
      }

      var survey = session.Get<SurveyContainerModel>(surveyId);

      if (IsManagingOrganization(survey.OrgId)) {
        return this;
      }

      if (survey.CreatorId == caller.Id) {
        return this;
      }

      throw new PermissionsException("Cannot view this survey");
    }

    public PermissionsUtility CreateOldSurvey() {
      if (IsManagingOrganization(caller.Organization.Id)) {
        return this;
      }

      if (caller.Organization.Settings.EmployeesCanCreateSurvey) {
        return this;
      }

      if (caller.Organization.Settings.ManagersCanCreateSurvey && IsManager(caller.Organization.Id)) {
        return this;
      }

      throw new PermissionsException("Cannot create survey");

    }


    public PermissionsUtility EditOldSurvey(long surveyId) {
      var survey = session.Get<SurveyContainerModel>(surveyId);

      if (survey != null) {
        session.Evict(survey);
        if (survey.QuestionGroup != null) {
          session.Evict(survey.QuestionGroup);
        }

        if (survey.RespondentGroup != null) {
          session.Evict(survey.RespondentGroup);
        }
      }
      if (surveyId == 0) {
        return CreateOldSurvey();
      }

      if (IsRadialAdmin(caller)) {
        return this;
      }

      if (survey.IssueDate != null) {
        throw new PermissionsException("Cannot edit survey.");
      }

      if (IsManagingOrganization(survey.OrgId)) {
        return this;
      }

      if (survey.CreatorId == caller.Id) {
        return this;
      }

      throw new PermissionsException("Cannot view this survey");
    }
  }
}

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

    public PermissionsUtility CreateProcessUnderProcessFolder(long processFolderId) {
      CanAdmin(PermItem.ResourceType.ProcessFolder, processFolderId, exceptionMessage: "You're not an admin for this folder.");
      var folder = session.Get<ProcessFolder>(processFolderId);
      if (folder.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }

    public PermissionsUtility AdminProcessFolder(long folderId) {
      CanAdmin(PermItem.ResourceType.ProcessFolder, folderId, exceptionMessage: "You're not an admin for this folder.");
      var folder = session.Get<ProcessFolder>(folderId);
      if (folder.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }
    public PermissionsUtility EditProcessFolder(long folderId) {
      CanEdit(PermItem.ResourceType.ProcessFolder, folderId, exceptionMessage: "You're not an admin for this folder.");
      var folder = session.Get<ProcessFolder>(folderId);
      if (folder.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }

    public PermissionsUtility ViewProcessFolder(long folderId) {
      CanView(PermItem.ResourceType.ProcessFolder, folderId, exceptionMessage: "Cannot view this folder");
      var folder = session.Get<ProcessFolder>(folderId);
      if (folder.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }

    public PermissionsUtility ViewProcess(long processId) {
      CanView(PermItem.ResourceType.Process, processId, exceptionMessage: "Cannot view this process");

      var process = session.Get<ProcessModel>(processId);
      if (process.DeleteTime != null)
        throw new PermissionsException("Process was deleted.");
      return this;
    }
    public PermissionsUtility EditProcess(long processId) {
      CanEdit(PermItem.ResourceType.Process, processId, exceptionMessage: "Cannot edit this process.");
      var process = session.Get<ProcessModel>(processId);
      if (process.DeleteTime != null)
        throw new PermissionsException("Process was deleted.");
      return this;
    }

    public PermissionsUtility ViewProcessStep(long processStepId) {
      var step = session.Get<ProcessStep>(processStepId);
      return ViewProcess(step.ProcessId);
    }
  }
}
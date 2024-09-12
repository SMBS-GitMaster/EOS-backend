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

    public PermissionsUtility CanViewFile(long fileId) {
      return CanView(PermItem.ResourceType.File, fileId, includeAlternateUsers: true);
    }
    public PermissionsUtility CanEditFile(long fileId) {
      return CanEdit(PermItem.ResourceType.File, fileId, includeAlternateUsers: true);
    }
    public PermissionsUtility CanAdminFile(long fileId) {
      return CanAdmin(PermItem.ResourceType.File, fileId, includeAlternateUsers: true);
    }

    public PermissionsUtility AdminDocumentsFolder(string folderId) {
      var folder = session.QueryOver<DocumentsFolder>().Where(x => x.LookupId == folderId).Take(1).SingleOrDefault();
      if (folder == null)
        throw new PermissionsException("Folder does not exist");
      var fId = folder.Id;
      return AdminDocumentsFolder(fId);
    }
    public PermissionsUtility EditDocumentsFolder(string folderId) {
      var folder = session.QueryOver<DocumentsFolder>().Where(x => x.LookupId == folderId).Take(1).SingleOrDefault();
      if (folder == null)
        throw new PermissionsException("Folder does not exist");
      var fId = folder.Id;
      return EditDocumentsFolder(fId);
    }
    public PermissionsUtility ViewDocumentsFolder(string folderId) {
      var folder = session.QueryOver<DocumentsFolder>().Where(x => x.LookupId == folderId).Take(1).SingleOrDefault();
      if (folder == null)
        throw new PermissionsException("Folder does not exist");
      var fId = folder.Id;
      return ViewDocumentsFolder(fId);
    }
    public PermissionsUtility CreateFileUnderDocumentsFolder(string folderId) {
      var folder = session.QueryOver<DocumentsFolder>().Where(x => x.LookupId == folderId).Take(1).SingleOrDefault();
      if (folder == null)
        throw new PermissionsException("Folder does not exist");
      var fId = folder.Id;
      return CreateFileUnderDocumentsFolder(fId);
    }
    public PermissionsUtility AdminDocumentsFolder(long folderId, string errorMessage = null) {
      CanAdmin(PermItem.ResourceType.DocumentsFolder, folderId, exceptionMessage: errorMessage ?? "You're not an admin for this folder.", includeAlternateUsers: true);
      var f = session.Get<DocumentsFolder>(folderId);
      if (f.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }
    public PermissionsUtility CreateFileUnderDocumentsFolder(long folderId, string errorMessage = null) {
      CanEdit(PermItem.ResourceType.DocumentsFolder, folderId, exceptionMessage: errorMessage ?? "You cannot edit this folder.", includeAlternateUsers: true);
      var f = session.Get<DocumentsFolder>(folderId);
      if (f.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }

    public PermissionsUtility EditDocumentsFolder(long folderId, string errorMessage = null) {
      CanEdit(PermItem.ResourceType.DocumentsFolder, folderId, exceptionMessage: errorMessage ?? "You cannot edit this folder.", includeAlternateUsers: true);
      var f = session.Get<DocumentsFolder>(folderId);
      if (f.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }

    public PermissionsUtility ViewDocumentsFolder(long folderId, string errorMessage = null) {
      CanView(PermItem.ResourceType.DocumentsFolder, folderId, exceptionMessage: errorMessage ?? "You cannot view this folder.", includeAlternateUsers: true);
      var f = session.Get<DocumentsFolder>(folderId);
      if (f.DeleteTime != null)
        throw new PermissionsException("Folder was deleted.");
      return this;
    }
  }
}

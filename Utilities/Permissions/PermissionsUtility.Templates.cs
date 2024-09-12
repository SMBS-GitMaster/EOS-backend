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
    [Obsolete("Deprecated")]
    public PermissionsUtility CreateTemplates(long organizationId) {
      return ManagerAtOrganization(caller.Id, organizationId);
    }

    [Obsolete("Deprecated")]
    public PermissionsUtility ViewTemplate(long templateId) {
      return ViewOrganization(session.Get<UserTemplate_Deprecated>(templateId).OrganizationId);
    }
    [Obsolete("Deprecated")]
    public PermissionsUtility EditTemplate(long templateId) {
      return CreateTemplates(session.Get<UserTemplate_Deprecated>(templateId).OrganizationId);
    }


    private PermissionsUtility _ConfirmPermissions<T, M>(T model, bool fixRefs, Expression<Func<T, long?>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
      var id = model.Get(idSelector);
      var m = model.Get(modelSelector);
      if (id == null) {
        if (m == null) {
          return this; //No error.. looks like its optional
        }

        if (m.Id == 0) {
          throw new PermissionsException("Model uninitialized [1]");
        }

        if (fixRefs) {
          model.Set(idSelector, m.Id);
        }

        return permissionsSelector(this)(m.Id);
      } else if (m == null) {
        if (id == 0) {
          throw new PermissionsException();
        }

        if (fixRefs) {
          var mLoaded = session.Get<M>(id.Value);
          model.Set(modelSelector, mLoaded);
        }

        return permissionsSelector(this)(id.Value);
      } else {
        if (id == 0) {
          if (m.Id == 0) {
            throw new PermissionsException("Model uninitialized [2]");
          }

          if (fixRefs) {
            model.Set(idSelector, m.Id);
          }

          return permissionsSelector(this)(m.Id);
        } else {
          if (m.Id == 0) {
            throw new PermissionsException("Model uninitialized [3]");
          }

          if (id != m.Id) {
            throw new PermissionsException("Model Id != Id");
          }

          return permissionsSelector(this)(id.Value);
        }
      }
    }

    private PermissionsUtility _ConfirmPermissions<T, M>(T model, bool fixRefs, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
      var id = model.Get(idSelector);
      var m = model.Get(modelSelector);
      if (m == null) {
        if (id == 0) {
          throw new PermissionsException();
        }

        if (fixRefs) {
          var mLoaded = session.Load<M>(id);
          model.Set(modelSelector, mLoaded);
        }

        return permissionsSelector(this)(id);
      } else {
        if (id == 0) {
          if (m.Id == 0) {
            throw new PermissionsException("Model uninitialized [2]");
          }

          if (fixRefs) {
            model.Set(idSelector, m.Id);
          }

          return permissionsSelector(this)(m.Id);
        } else {
          if (m.Id == 0) {
            throw new PermissionsException("Model uninitialized [3]");
          }

          if (id != m.Id) {
            throw new PermissionsException("Model Id != Id");
          }

          return permissionsSelector(this)(id);
        }
      }
    }

    public delegate PermissionsUtility LongFunc(long id);

    public PermissionsUtility Confirm<T, M>(T model, Expression<Func<T, long?>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
      return _ConfirmPermissions(model, false, idSelector, modelSelector, permissionsSelector);
    }
    public PermissionsUtility Confirm<T, M>(T model, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
      return _ConfirmPermissions(model, false, idSelector, modelSelector, permissionsSelector);
    }
    public PermissionsUtility ConfirmAndFix<T, M>(T model, Expression<Func<T, long>> idSelector, Expression<Func<T, M>> modelSelector, Func<PermissionsUtility, LongFunc> permissionsSelector) where M : ILongIdentifiable {
      return _ConfirmPermissions(model, true, idSelector, modelSelector, permissionsSelector);
    }
  }
}

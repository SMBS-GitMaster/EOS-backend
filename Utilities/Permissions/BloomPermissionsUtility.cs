using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using System;

namespace RadialReview.Core.Utilities.Permissions
{
  public abstract class BloomPermissionsUtility<T> : PermissionsUtility where T : BaseModel
  {
    public T ResourceModel { get; }
    public BloomPermissionsUtility(ISession session, UserOrganizationModel caller, object resourceId) : base(session, caller)
    {
      ResourceModel = GetResorceModel(resourceId);
    }

    public BloomPermissionsUtility(ISession session, UserOrganizationModel caller) : base(session, caller)
    {
    }

    protected virtual T GetResorceModel(object resourceId)
    {
      return session.Get<T>(resourceId) ?? throw new PermissionsException($"{typeof(T).Name} does not exist.");
    }

    protected static UserOrganizationModel AttachCaller(ISession session, UserOrganizationModel caller)
    {
      var attached = caller;
      if (!session.Contains(caller) && caller.Id != UserOrganizationModel.ADMIN_ID)
      {
        attached = session.Load<UserOrganizationModel>(caller.Id);
        attached._ClientTimestamp = caller._ClientTimestamp;
        attached._PermissionsOverrides = caller._PermissionsOverrides;
        attached._IsTestAdmin = caller._IsTestAdmin;
      }
      if (caller.DeleteTime != null && caller.DeleteTime < DateTime.UtcNow)
      {
        throw new PermissionsException("User has been deleted")
        {
          NoErrorReport = true
        };
      }
      if (caller.Organization != null && caller.Organization.DeleteTime != null && caller.Organization.DeleteTime < DateTime.UtcNow && caller.Organization.DeleteTime != new DateTime(1, 1, 1))
      {
        LockoutUtility.ProcessLockout(caller);
      }

      return attached;
    }

    public bool IsAdminL10Recurrence(long recurrenceId)
    {
      try
      {
        AdminL10Recurrence(recurrenceId);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public bool CanEditL10Recurrence(long recurrenceId)
    {
      try
      {
        EditL10Recurrence(recurrenceId);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }

    public bool CanViewL10Recurrence(long recurrenceId)
    {
      try
      {
        ViewL10Recurrence(recurrenceId);
        return true;
      }
      catch (Exception)
      {
        return false;
      }
    }
  }
}

using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.L10;
using System;

namespace RadialReview.Core.Utilities.Permissions.Models
{
  public class MetricTabPermissions : BloomPermissionsUtility<MetricTabModel>
  {
    public MetricTabPermissions(ISession session, UserOrganizationModel caller) : base(session, caller)
    {
    }

    public MetricTabPermissions(ISession session, UserOrganizationModel caller, long resourceId) : base(session, caller, resourceId)
    {
    }

    public static new MetricTabPermissions Create(ISession session, UserOrganizationModel caller)
    {
      var attached = AttachCaller(session, caller);
      return new MetricTabPermissions(session, attached);
    }

    public static MetricTabPermissions Create(ISession session, UserOrganizationModel caller, long metricTabId)
    {
      var attached = AttachCaller(session, caller);
      return new MetricTabPermissions(session, attached, metricTabId);
    }

    public MetricTabPermissions CanCreateMetricTab(long recurrenceId)
    {
      var recurrence = session.QueryOver<L10Recurrence>()
        .Where(rec => rec.Id == recurrenceId)
        .Select(rec => rec.Id)
        .SingleOrDefault<long?>();

      if (!recurrence.HasValue)
        throw new PermissionsException("Meeting does not exist.");

      if (IsAdminL10Recurrence(recurrenceId)) return this;
      if (CanEditL10Recurrence(recurrenceId)) return this;

      throw new PermissionsException();
    }

    public MetricTabPermissions CanViewMetricTab()
    {
      if (IsRadialAdmin(caller)) return this;
      if (caller.Id == ResourceModel.UserId) return this;
      if (ResourceModel.ShareToMeeting && ResourceModel.MeetingId != null)
      {
        try
        {
          ViewL10Recurrence((long) ResourceModel.MeetingId);
          return this;
        }
        catch (Exception)
        {
          throw new PermissionsException();
        }
      }

      throw new PermissionsException();
    }

    public MetricTabPermissions CanEditMetricTab()
    {
      long recurrenceId = (long) ResourceModel.MeetingId;
      bool isOwner = ResourceModel.UserId == caller.Id;
      
      if (ResourceModel.ShareToMeeting)
      {
        if (IsAdminL10Recurrence(recurrenceId)) return this;
        if (CanEditL10Recurrence(recurrenceId) && isOwner) return this;
      }
      else
      {
        // personal tab, only the owner can edit
        if (IsAdminL10Recurrence(recurrenceId) && isOwner) return this;
        if (CanEditL10Recurrence(recurrenceId) && isOwner) return this;
      }

      throw new PermissionsException();
    }

    public MetricTabPermissions CanEditCreator()
    {
      if (IsRadialAdmin(caller)) return this;
      if (ResourceModel.UserId == caller.Id) return this;
      throw new PermissionsException();
    }

    public MetricTabPermissions CanPinMetricTab()
    {
      long recurrenceId = (long) ResourceModel.MeetingId;
      if (IsAdminL10Recurrence(recurrenceId)) return this;
      if (CanEditL10Recurrence(recurrenceId)) return this;
      throw new PermissionsException();
    }
  }
}

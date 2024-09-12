using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Permissions.Models
{
  public class TrackedMetricPermissions : BloomPermissionsUtility<TrackedMetricModel>
  {
    public TrackedMetricPermissions(ISession session, UserOrganizationModel caller, object resourceId) : base(session, caller, resourceId)
    {
    }

    public static TrackedMetricPermissions Create(ISession session, UserOrganizationModel caller, long trackedMetricId)
    {
      var attached = AttachCaller(session, caller);
      return new TrackedMetricPermissions(session, attached, trackedMetricId);
    }

    public TrackedMetricPermissions CanRemoveMetricFromTab()
    {
      var metricTabPerms = MetricTabPermissions.Create(session, caller, ResourceModel.MetricTabId);
      metricTabPerms.CanEditMetricTab();
      return this;
    }
  }
}

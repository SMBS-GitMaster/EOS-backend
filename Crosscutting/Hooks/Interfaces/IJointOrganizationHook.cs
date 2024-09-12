using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces {
  public class IJointOrganizationHookRemoveUpdates {
    public long ChildOrganizationId { get; set; }
    public long OldParentOrganizationId { get; set; }

  }
  public class IJointOrganizationHookAddUpdates {
    public long ChildOrganizationId { get; set; }
    public long NewParentOrganizationId { get; set; }

  }

  public enum UpdatePayingEventType {
    Invalid,
    OnLinkRemoved,
    OnCreate,
    OnPayingUpdated,
  }

  public class IJointOrganizationHookUpdatePayingUpdates {
    public long ChildOrganizationId { get; set; }
    public long ParentOrganizationId { get; set; }
    public bool? OldParentPayingStatus { get; set; }
    public bool NewParentPayingStatus { get; set; }
    public UpdatePayingEventType EventType { get; set; }

  }

  public interface IJointOrganizationHook : IHook {
    Task AddParent(ISession s, JointOrganizationModel model, IJointOrganizationHookAddUpdates update);
    Task RemoveParent(ISession s, JointOrganizationModel model, IJointOrganizationHookRemoveUpdates update);
    Task UpdatePaying(ISession s, JointOrganizationModel model, IJointOrganizationHookUpdatePayingUpdates update);
  }
}

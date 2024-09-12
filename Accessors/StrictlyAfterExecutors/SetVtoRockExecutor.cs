using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RadialReview.Core.Accessors.StrictlyAfterExecutors {
  internal class SetVtoRockExecutor : IStrictlyAfter {
    public StrictlyAfterBehavior Behavior => new StrictlyAfterBehavior(true);

    public long recurRockId { get; }
    public bool vtoRock { get; }

    public SetVtoRockExecutor(long recurRockId, bool vtoRock) {
      this.recurRockId=recurRockId;
      this.vtoRock=vtoRock;
    }

    public async Task EnsurePermitted(ISession s, PermissionsUtility perms) {
      var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurRockId);
      perms.EditRock(recurRock.ForRock.Id, false);
      //perm.EditL10Recurrence(recurRock.L10Recurrence.Id);
    }

    public async Task AtomicUpdate(IOrderedSession s) {
      var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurRockId);
      recurRock.VtoRock = vtoRock;
      s.Update(recurRock);

    }

    public async Task AfterAtomicUpdate(ISession s, PermissionsUtility perms) {
      var recurRock = s.Get<L10Recurrence.L10Recurrence_Rocks>(recurRockId);
      var meetingId = recurRock.L10Recurrence.MeetingInProgress;
      if (meetingId != null) {
        var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.DeleteTime == null && x.ForRock.Id == recurRock.ForRock.Id && x.L10Meeting.Id == meetingId.Value).List().ToList();
        foreach (var m in meetingRocks) {
          m.VtoRock = vtoRock;
          s.Update(m);
        }
      }
      await HooksRegistry.Each<IMeetingRockHook>((ss, x) => x.UpdateVtoRock(ss, recurRock));

    }

  }
}

using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
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
  internal class UpdateRockExecutor : IStrictlyAfter {
    public StrictlyAfterBehavior Behavior => new StrictlyAfterBehavior(true);

    public long rockId { get; private set;  }
    public string message { get; }
    public long? ownerId { get; }
    public RockState? completion { get; }
    public DateTime? dueDate { get; }
    public DateTime? now { get; }
    private IRockHookUpdates updates { get; }
    private bool shouldExecute { get; set; }
    private bool anyNonStausUpdates { get; set; }
    private bool permsExecuted = false;
    private string noteId { get; set; }

    public UpdateRockExecutor(long rockId, string message = null, long? ownerId = null, RockState? completion = null, DateTime? dueDate = null, DateTime? now = null, string noteId = null) {
      now = now ?? DateTime.UtcNow;
      message = message?.Replace("&amp;", "&");
      this.updates = new IRockHookUpdates();
      this.rockId=rockId;
      this.message=message;
      this.ownerId=ownerId;
      this.completion=completion;
      this.dueDate=dueDate;
      this.now=now;
      this.noteId = noteId;
    }

    public async Task EnsurePermitted(ISession s, PermissionsUtility perms) {
      var rock = s.Get<RockModel>(rockId);
      if (message != null && rock.Name != message) {
        shouldExecute = true;
        anyNonStausUpdates = true;
        perms.EditRock(rockId, false);
      }

      if (ownerId != null && rock.ForUserId != ownerId) {
        perms.EditRock(rockId, false);  /*Must be done here. Permissions need to be checked everywhere*/
        perms.CreateRocksForUser(ownerId.Value);
        shouldExecute = true;
        anyNonStausUpdates = true;
      }

      if (dueDate != null && rock.DueDate != dueDate) {
        perms.EditRock(rockId, false);
        shouldExecute = true;
        anyNonStausUpdates = true;
      }

      if (completion != null) {
        if (completion != RockState.Indeterminate && rock.Completion != completion) {
          perms.EditRock(rockId, true); /*Must be done here. Permissions need to be checked everywhere*/
          shouldExecute = true;
        } else if ((completion == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
          perms.EditRock(rockId, true); /*Must be done here. Permissions need to be checked everywhere*/
          shouldExecute = true;
        }
      }
      if (shouldExecute) {
        perms.EditRock(rockId, !anyNonStausUpdates); /*Permissions are also with the Update method. This call cannot be relied on however.*/
      }
      permsExecuted=true;
    }

    public async Task AtomicUpdate(IOrderedSession s) {
      var rock = s.Get<RockModel>(rockId);
      if (message != null && rock.Name != message) {
        //perms.EditRock(rockId, false);
        //shouldExecute = true;
        rock.Name = message;
        updates.MessageChanged = true;
        //anyNonStausUpdates = true;
      }

      if(noteId != null && rock.PadId != noteId)
      {
        rock.PadId = noteId;
      }

      updates.OriginalAccountableUserId = rock.ForUserId;
      if (ownerId != null && rock.ForUserId != ownerId) {
        //perms.EditRock(rockId, false);  /*Must be done here. Permissions need to be checked everywhere*/
        //perms.CreateRocksForUser(ownerId.Value);
        //shouldExecute = true;
        rock.AccountableUser = s.Load<UserOrganizationModel>(ownerId.Value);
        rock.ForUserId = ownerId.Value;
        updates.AccountableUserChanged = true;
        //anyNonStausUpdates = true;
      }

      if (dueDate != null && rock.DueDate != dueDate) {
        //perms.EditRock(rockId, false);  /*Must be done here. Permissions need to be checked everywhere*/
        //shouldExecute = true;
        rock.DueDate = dueDate;
        updates.DueDateChanged = true;
        //anyNonStausUpdates = true;
      }

      if (completion != null) {
        if (completion != RockState.Indeterminate && rock.Completion != completion) {
          if (completion == RockState.Complete) {
            rock.CompleteTime = now;
          }
          //perms.EditRock(rockId, true); /*Must be done here. Permissions need to be checked everywhere*/
          //shouldExecute = true;
          rock.Completion = completion.Value;
          updates.StatusChanged = true;
        } else if ((completion == RockState.Indeterminate) && rock.Completion != RockState.Indeterminate) {
          //perms.EditRock(rockId, true); /*Must be done here. Permissions need to be checked everywhere*/
          //shouldExecute = true;
          rock.Completion = RockState.Indeterminate;
          rock.CompleteTime = null;
          updates.StatusChanged = true;
        }
      }
      if (shouldExecute) {
        //perms.EditRock(rockId, !anyNonStausUpdates); /*Permissions are also with the Update method. This call cannot be relied on however.*/
        rock.DateLastModified = Math.Floor(DateTime.UtcNow.ToUnixTimeStamp());
        s.Update(rock);
      }

    }

    public async Task AfterAtomicUpdate(ISession s, PermissionsUtility perms) {
      var rock = s.Get<RockModel>(rockId);
      var cc = perms.GetCaller();
      await HooksRegistry.Each<IRockHook>((ss, x) => x.UpdateRock(ss, cc, rock, updates));
    }


    public void SetRockId(long rockId) {
      if (permsExecuted)
        throw new PermissionsException("cannot change rock id at this point");
      this.rockId = rockId;
    }

  }
}

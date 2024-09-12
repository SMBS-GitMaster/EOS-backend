using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using RadialReview.Middleware.Services.BlobStorageProvider;
using ISession = NHibernate.ISession;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Accessors {
  public partial class UserAccessor : BaseAccessor {

    public enum TempUserStatus {
      DoesNotExists,
      Unregistered, //RegistrationEmailSent_Unregistered
      Unsent, //RegistrationEmail_Unsent,
      BadFormat
    }

    public class EditUserResult {
      public bool? OverrideEvalOnly { get; set; }
      public bool? OverrideManageringOrganization { get; set; }
      public bool? OverrideIsManager { get; set; }
      public List<string> Errors { get; set; }
      public EditUserResult() {
        Errors = new List<string>();
      }
    }
    public static CreateUserOrganizationViewModel BuildCreateUserVM(UserOrganizationModel caller, TermsCollection terms, long? recurrenceId, long? managerId = null, string name = null, bool isClient = false, long? managerNodeId = null, bool forceManager = false, bool hideIsManager = false, bool hidePosition = false, long? nodeId = null, bool hideEvalOnly = false, bool forceNoSend = false, bool forceInitialNoSend = false, bool lockSeat = false) {
      PermissionsAccessor.EnsurePermitted(caller, x => x.EditHierarchy(caller.Organization.AccountabilityChartId));
      var output = new CreateUserOrganizationViewModel();
      output.Settings.LockManager = forceManager;
      output.Settings.HideIsManager = hideIsManager;
      output.Settings.HideEvalOnly = hideEvalOnly;
      output.Settings.HideSend = forceNoSend;
      output.Settings.HideOnLeadershipTeam = !caller.ManagingOrganization;
      output.Settings.LockSeat = lockSeat;
      output.Settings.HideSetOrgAdmin = !caller.ManagingOrganization;
      output.Settings.StrictlyHierarchical = caller.Organization.StrictHierarchy;
      output.Settings.DisabledBecauseUnverified = (caller.User == null || caller.User.EmailNotVerified);
      if (output.Settings.DisabledBecauseUnverified && PermissionsAccessor.IsPermitted(caller, x => x.RadialAdmin(false))) {
        //allow super admins.
        output.Settings.DisabledBecauseUnverified = false;
      }

      var canUpgradeUsers = PermissionsAccessor.IsPermitted(caller, x => x.CanEdit(PermItem.ResourceType.UpgradeUsersForOrganization, caller.Organization.Id));
      if (!canUpgradeUsers) {
        output.PlaceholderOnly = true;
        output.Settings.LockPlaceholder = true;
        output.Settings.HideSetOrgAdmin = true;
        output.Settings.HideEvalOnly = true;
      }

      output.Settings.PotentialParents = SelectListAccessor.GetNodesWeCanAssignUsersTo(caller, terms, x => x.Id == managerId, true, true, true);
      output.Settings.PossibleRecurrences = SelectListAccessor.GetL10RecurrenceAdminable(caller, caller.Id, x => x.Id == recurrenceId);
      output.SendEmail = forceNoSend ? false : caller.Organization.SendEmailImmediately;
      if (forceInitialNoSend) {
        output.SendEmail = false;
      }

      string fname = null;
      string lname = null;
      string email = null;
      if (name != null) {
        try {
          var names = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
          var nameCount = Math.Max(2, names.Length);
          fname = string.Join(" ", names.Where((x, i) => i < nameCount - 1));
          if (nameCount > 1) {
            lname = names[nameCount - 1];
            email = EmailUtil.GuessEmail(caller, caller.Organization.Id, fname, lname);
          }
        } catch (Exception) {
        }
      }

      output.OrgId = caller.Organization.Id;
      output.ManagerNodeId = managerNodeId;
      output.IsClient = isClient;
      output.FirstName = fname;
      output.LastName = lname;
      output.Email = email;
      output.NodeId = nodeId;
      return output;
    }

   /* public static void CreateUser(UserModel userModel) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          s.Save(userModel);
          tx.Commit();
          s.Flush();
        }
      }
   }*/

    public static async Task<UserJoinResult> CreateUser(UserOrganizationModel caller, CreateUserOrganizationViewModel model) {
      return await JoinOrganizationAccessor.JoinOrganizationUnderManager(caller, model);
    }



    public static async Task<IdentityResult> CreateUser(UserManager<UserModel> UserManager, UserModel user, string password) {
      user.UserName = user.UserName.NotNull(x => x.ToLower());
      var resultx = await UserManager.CreateAsync(user, password);
      if (resultx.Succeeded) {
        AddSettings(resultx, user);
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.OnUserRegister(ses, user, new OnUserRegisterData() { DuringSelfOnboarding = false }));
            tx.Commit();
            s.Flush();
          }
        }
      }
      return resultx;
    }

    public static int CreateDeepSubordinateTree(UserOrganizationModel caller, long organizationId, DateTime now) {
      var count = 0;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).RadialAdmin();
          var existing = s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List();
          foreach (var e in existing) {
            e.DeleteTime = now;
            s.Update(e);
          }

          var allManagers = s.QueryOver<AccountabilityNode>().Where(x => x.OrganizationId == organizationId).List().ToListAlive();
          var allIds = allManagers.Select(x => x.Id).ToList();
          foreach (var id in allIds) {
            var found = s.QueryOver<DeepAccountability>().Where(x => x.ChildId == id && x.ParentId == id).List().ToListAlive();
            if (!found.Any()) {
              count++;
              s.Save(new DeepAccountability() { Links = 1, CreateTime = now, ParentId = id, ChildId = id, OrganizationId = organizationId });
            }
          }

          foreach (var manager in allManagers.Distinct(x => x.Id)) {
            var managerSubordinates = s.QueryOver<DeepAccountability>().Where(x => x.DeleteTime == null && x.ParentId == manager.Id).List().ToList();
            var allSubordinates = DeepAccessor.Dive.GetSubordinates(s, manager);
            foreach (var sub in allSubordinates) {
              var found = managerSubordinates.FirstOrDefault(x => x.ChildId == sub.Id);
              if (found == null) {
                found = new DeepAccountability() { CreateTime = now, ParentId = manager.Id, ChildId = sub.Id, Links = 0, OrganizationId = organizationId, };
              }

              found.Links += 1;
              count++;
              s.SaveOrUpdate(found);
            }
          }

          tx.Commit();
          s.Flush();
        }
      }

      return count;
    }

    /// <summary>
    /// -3 for managerId sets the user as an organization manager
    /// </summary>
    public static async Task<EditUserResult> EditUser(UserOrganizationModel caller, long userOrganizationId, bool? isManager = null, bool? manageringOrganization = null, bool? evalOnly = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perm = PermissionsUtility.Create(s, caller);
          var output = await EditUserPermissionLevel(s, perm, userOrganizationId, isManager, manageringOrganization, evalOnly);
          tx.Commit();
          s.Flush();
          return output;
        }
      }
    }

    public static async Task EditUser(UserOrganizationModel caller, IBlobStorageProvider bsp, long userId, string firstName = null, string lastName = null, IFormFile image = null) {
      string imageGuid = null;
      if (image != null) {
        imageGuid = (await ImageAccessor.UploadProfileImageForUser(caller, bsp, userId, image.FileName, image.OpenReadStream())).Id.ToString();
      }

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditUserModel(userId);
          var user = s.Get<UserOrganizationModel>(userId);
          if (user.TempUser != null) {
            user.TempUser.FirstName = firstName ?? user.TempUser.FirstName;
            user.TempUser.LastName = lastName ?? user.TempUser.LastName;
            user.TempUser.ImageGuid = imageGuid ?? user.TempUser.ImageGuid;
            s.Update(user);
            s.Update(user.TempUser);
            user.UpdateCache(s);
          } else if (user.User != null) {
            user.User.FirstName = firstName ?? user.User.FirstName;
            user.User.LastName = lastName ?? user.User.LastName;
            user.User.ImageGuid = imageGuid ?? user.User.ImageGuid;
            s.Update(user);
            s.Update(user.User);
            user.UpdateCache(s);
            var uu = user.User;
            await HooksRegistry.Each<IUpdateUserModelHook>((ses, x) => x.UpdateUserModel(ses, uu));
          } else {
            throw new PermissionsException("Cannot edit user");
          }

          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task EditUserModel(UserModel caller, string userId, string firstName, string lastName, string imageGuid, bool? sendTodoEmails, int? sendTodoTime, bool? showScorecardColors, bool? reverseScorecard, bool? disableTips, ColorMode? colorMode, bool? darkMode) {
      UserModel user;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          if (caller.Id != userId) {
            throw new PermissionsException();
          }

          user = s.Get<UserModel>(userId);
          if (firstName != null) {
            user.FirstName = firstName;
          }

          if (lastName != null) {
            user.LastName = lastName;
          }

          if (imageGuid != null) {
            user.ImageGuid = imageGuid;
          }

          if (sendTodoEmails != null) {
            user.SendTodoTime = sendTodoEmails.Value ? sendTodoTime : null;
          }

          if (showScorecardColors != null) {
            var us = s.Get<UserStyleSettings>(userId);
            us.ShowScorecardColors = showScorecardColors.Value;
            s.Update(us);
          }

          if (reverseScorecard != null) {
            user.ReverseScorecard = reverseScorecard.Value;
          }

          if (disableTips != null) {
            user.DisableTips = disableTips.Value;
          }

          if (colorMode != null) {
            user.ColorMode = colorMode.Value;
          }

          if (darkMode != null) {
            user.DarkMode = darkMode;
          }

          if (user.UserOrganization != null) {
            foreach (var u in user.UserOrganization) {
              if (u != null) {
                u.UpdateCache(s);
              }
            }
          }

          tx.Commit();
          s.Flush();
        }
      }

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await HooksRegistry.Each<IUpdateUserModelHook>((ses, x) => x.UpdateUserModel(ses, user));
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task<EditUserResult> EditUserPermissionLevel(ISession s, PermissionsUtility perm, long userOrganizationId, bool? isManager = null, bool? manageringOrganization = null, bool? evalOnly = null) {
      var o = new EditUserResult();
      await using (var rt = RealTimeUtility.Create()) {
        var found = s.Get<UserOrganizationModel>(userOrganizationId);
        var acId = found.Organization.AccountabilityChartId;
        perm.CanEdit(PermItem.ResourceType.AccountabilityHierarchy, acId, exceptionMessage: "You're not permitted to edit the accountability hierarchy.<br/> Contact your admin.");
        var deleteTime = DateTime.UtcNow;
        if (manageringOrganization != null && manageringOrganization.Value != found.ManagingOrganization) {
          if (found.Id == perm.GetCaller().Id) {
            o.OverrideManageringOrganization = found.ManagingOrganization;
            o.Errors.Add("You cannot unmanage this organization yourself.");
          } else {
            perm.ManagingOrganization(found.Organization.Id); // ! Changed the organization from callers, to found
            if (found.ManagingOrganization && !manageringOrganization.Value) {
              //maybe set manager to false
              if (!DeepAccessor.Users.HasChildren(s, perm, userOrganizationId)) {
                isManager = false;
                o.OverrideIsManager = false;
              }
            } else {
              //maybe set manager to true
              if (DeepAccessor.Users.HasChildren(s, perm, userOrganizationId)) {
                isManager = true;
                o.OverrideIsManager = true;
              }
            }

            found.ManagingOrganization = manageringOrganization.Value;
          }
        }

        if (isManager != null && (isManager.Value != found.ManagerAtOrganization)) {
          found.ManagerAtOrganization = isManager.Value;
          if (isManager == false) {
            var subordinatesTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Type == TeamType.Subordinates && x.ManagedBy == userOrganizationId && x.DeleteTime == null).List();
            foreach (var subordinatesTeam in subordinatesTeams) {
              subordinatesTeam.DeleteTime = DateTime.UtcNow;
              s.Update(subordinatesTeam);
            }
          } else {
            var anyTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Type == TeamType.Subordinates && x.ManagedBy == userOrganizationId && x.DeleteTime == null).RowCount();
            if (anyTeams == 0) {
              s.Save(OrganizationTeamModel.SubordinateTeam(perm.GetCaller(), found));
              s.Flush();
            }
          }
        }

        if (evalOnly != null) {
          perm.CanUpgradeUser(found.Id);
          var anyMeetings = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == found.Id).RowCount();
          if (anyMeetings == 0 || evalOnly.Value == false) {
            found.EvalOnly = evalOnly.Value;
          } else {
            o.OverrideEvalOnly = found.EvalOnly;
            o.Errors.Add("Could not convert to " + Config.ReviewName() + " only. Remove user from weekly meetings first.");
          }
        }

        s.Update(found);
        found.UpdateCache(s);
      }

      return o;
    }

    public static bool UpdateTempUser(UserOrganizationModel caller, long userOrgId, String firstName, String lastName, String email, DateTime? lastSent = null) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var found = s.Get<UserOrganizationModel>(userOrgId);
          if (found == null || found.DeleteTime != null) {
            throw new PermissionsException("User does not exist.");
          }

          var tempUser = found.TempUser;
          if (tempUser == null) {
            throw new PermissionsException("User has already joined.");
          }

          bool changed = false;
          if (tempUser.FirstName != firstName) {
            tempUser.FirstName = firstName;
            changed = true;
          }

          if (tempUser.LastName != lastName) {
            tempUser.LastName = lastName;
            changed = true;
          }

          if (tempUser.Email != email) {
            tempUser.Email = email;
            found.EmailAtOrganization = email;
            s.Update(found);
            changed = true;
          }

          if (lastSent != null) {
            tempUser.LastSent = lastSent.Value;
            changed = true;
          }

          if (changed) {
            PermissionsUtility.Create(s, caller).ManagesUserOrganization(userOrgId, false);
            var guid = s.Get<NexusModel>(tempUser.Guid);
            if (guid != null) {
              var existingArg = guid.GetArgs();
              existingArg[1] = tempUser.Email;
              existingArg[3] = tempUser.FirstName;
              existingArg[4] = tempUser.LastName;
              guid.SetArgs(existingArg);
              s.Update(guid);
            }

            s.Update(tempUser);
            if (found != null) {
              found.UpdateCache(s);
            }

            tx.Commit();
            s.Flush();
          }

          return changed;
        }
      }
    }

    public static async Task<ResultObject> UndeleteUser(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).RemoveUser(userId);
          var user = s.Get<UserOrganizationModel>(userId);
          if (user.DeleteTime == null) {
            throw new PermissionsException("Could not undelete");
          }

          var deleteTime = user.DeleteTime.Value;
          user.DeleteTime = null;
          user.DetachTime = null;
          if (user.User != null) {
            var newArray = user.User.UserOrganizationIds.ToList();
            newArray.Add(userId);
            user.User.UserOrganizationIds = newArray.ToArray();
          }

          var tempUser = user.TempUser;
          if (tempUser != null) {
          }

          var warnings = new List<String>();
          //new management structure
          DeepAccessor.Users.UndeleteAll_Unsafe(s, user, deleteTime, ref warnings);
          //old management structure
          var asSubordinate = s.QueryOver<ManagerDuration>().Where(x => x.SubordinateId == userId && x.DeleteTime == deleteTime).List().ToList();
          foreach (var sub in asSubordinate) {
            sub.DeletedBy = caller.Id;
            sub.DeleteTime = null;
            s.Update(sub);
            if (sub.Manager != null) {
              sub.Manager.UpdateCache(s);
            }
          }

          var subordinates = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == userId && x.DeleteTime == deleteTime).List().ToList();
          foreach (var subordinate in subordinates) {
            subordinate.DeletedBy = caller.Id;
            subordinate.DeleteTime = null;
            s.Update(subordinate);
            if (subordinate.Subordinate != null) {
              subordinate.Subordinate.UpdateCache(s);
            }
          }

          //teams
          var teams = s.QueryOver<TeamDurationModel>().Where(x => x.UserId == userId && x.DeleteTime == deleteTime).List().ToList();
          foreach (var t in teams) {
            t.DeletedBy = caller.Id;
            t.DeleteTime = null;
            s.Update(t);
          }

          //managed teams
          var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId && x.DeleteTime == deleteTime).List().ToList();
          foreach (var m in managedTeams) {
            var subordinateTeam = s.QueryOver<TeamDurationModel>().Where(x => x.TeamId == m.Id && x.DeleteTime == deleteTime).List().ToList();
            foreach (var t in subordinateTeam) {
              t.DeletedBy = caller.Id;
              t.DeleteTime = null;
              s.Update(t);
            }

            m.DeleteTime = null;
            s.Update(m);
          }

          var attendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.User.Id == userId && x.DeleteTime == deleteTime).List().ToList();
          foreach (var f in attendees) {
            f.DeleteTime = null;
            s.Update(f);
          }

          var meetingAttendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>().Where(x => x.User.Id == userId && x.DeleteTime == deleteTime).List().ToList();
          foreach (var f in meetingAttendees) {
            f.DeleteTime = null;
            s.Update(f);
          }

          s.Update(user);
          user.UpdateCache(s);
          await HooksRegistry.Each<IDeleteUserOrganizationHook>((ses, x) => x.UndeleteUser(ses, user, deleteTime));
          tx.Commit();
          s.Flush();
          if (warnings.Count() == 0) {
            return ResultObject.CreateMessage(StatusType.Success, "Successfully re-added " + user.GetFirstName() + ".");
          } else {
            return ResultObject.CreateMessage(StatusType.Warning, "Successfully re-added " + user.GetFirstName() + ".<br/><b>Warning:</b><br/>" + string.Join("<br/>", warnings));
          }
        }
      }
    }

    public static async Task<ResultObject> RemoveUser(UserOrganizationModel caller, long userId, DateTime now) {
      UserOrganizationModel user;
      var warnings = new List<String>();
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).RemoveUser(userId);
          user = s.Get<UserOrganizationModel>(userId);

          if (user.IsManagingOrganization(false) && user.User != null) {
            //admin checks
            UserModel userAlias = null;
            var anyOtherAdmins = s.QueryOver<UserOrganizationModel>()
              .JoinAlias(x => x.User, () => userAlias)
              .Where(x => x.Organization.Id == user.Organization.Id  //Same org
                    && x.DeleteTime == null  //Not deleted
                    && x.ManagingOrganization //OrgAdmin
                    && x.Id != userId // not this user
                    && !x.IsRadialAdmin // not radial admin
                    && !userAlias.IsRadialAdmin //not radialadmin
              ).Select(x => x.Id)
              .Take(1).List<long>().Any();
            if (!anyOtherAdmins) {
              throw new PermissionsException("Accounts must have at least one registered admin. Please make another user an admin before deleting this user.");
            }
          }

          user.DetachTime = now;
          user.DeleteTime = now;

          if (user.User != null) {
            var newArray = user.User.UserOrganizationIds.ToList();
            if (!newArray.Remove(userId)) {
              throw new PermissionsException("User does not exist.");
            }

            user.User.UserOrganizationIds = newArray.ToArray();
          }
          var tempUser = user.TempUser;
          if (tempUser != null) {
          }

          //new management structure
          DeepAccessor.Users.DeleteAll_Unsafe(s, user, now);

          //old management structure
          var asSubordinate = s.QueryOver<ManagerDuration>().Where(x => x.SubordinateId == userId && x.DeleteTime == null).List().ToList();
          foreach (var sub in asSubordinate) {
            sub.DeletedBy = caller.Id;
            sub.DeleteTime = now;
            s.Update(sub);
            if (sub.Manager != null) {
              sub.Manager.UpdateCache(s);
            }
          }
          var subordinates = s.QueryOver<ManagerDuration>().Where(x => x.ManagerId == userId && x.DeleteTime == null).List().ToList();
          foreach (var subordinate in subordinates) {
            subordinate.DeletedBy = caller.Id;
            subordinate.DeleteTime = now;
            s.Update(subordinate);
            if (subordinate.Subordinate != null) {
              subordinate.Subordinate.UpdateCache(s);
            }

            warnings.Add(user.GetFirstName() + " no longer manages " + subordinate.Subordinate.GetNameAndTitle() + ".");
          }
          //teams
          var teams = s.QueryOver<TeamDurationModel>().Where(x => x.UserId == userId && x.DeleteTime == null).List().ToList();
          foreach (var t in teams) {
            t.DeletedBy = caller.Id;
            t.DeleteTime = now;
            s.Update(t);
          }
          //managed teams
          var managedTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.ManagedBy == userId && x.DeleteTime == null).List().ToList();
          foreach (var m in managedTeams) {
            if (m.Type != TeamType.Subordinates) {
              m.ManagedBy = caller.Id;
              s.Update(m);
              warnings.Add("You now manage the team: " + m.GetName() + ".");
            } else {
              //teams
              var subordinateTeam = s.QueryOver<TeamDurationModel>().Where(x => x.TeamId == m.Id && x.DeleteTime == null).List().ToList();
              foreach (var t in subordinateTeam) {
                t.DeletedBy = caller.Id;
                t.DeleteTime = now;
                s.Update(t);
              }

              m.DeleteTime = now;
              s.Update(m);
            }
          }


          var l10Attendee = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.User.Id == userId && x.DeleteTime == null).List().ToList();
          foreach (var m in l10Attendee) {
            m.DeleteTime = now;
            s.Update(m);
          }

          var l10MeetingAttendee = s.QueryOver<L10Meeting.L10Meeting_Attendee>()
            .Where(x => x.User.Id == userId && x.DeleteTime == null)
            .List().ToList();
          foreach (var m in l10MeetingAttendee.OrderByDescending(x => x.Id).GroupBy(x => x.Id).Select(x => x.First())) {
            m.DeleteTime = now;
            s.Update(m);
          }
          s.Update(user);
          user.UpdateCache(s);

          tx.Commit();
          s.Flush();
        }
      }
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await HooksRegistry.Each<IDeleteUserOrganizationHook>((ses, x) => x.DeleteUser(ses, user, now));
          tx.Commit();
          s.Flush();
        }
      }
      if (warnings.Count() == 0) {
        return ResultObject.CreateMessage(StatusType.Success, "Successfully removed " + user.GetFirstName() + ".");
      } else {
        return ResultObject.CreateMessage(StatusType.Warning, "Successfully removed " + user.GetFirstName() + ".<br/><b>Warning:</b><br/>" + string.Join("<br/>", warnings));
      }
    }



    public static void EditJobDescription(UserOrganizationModel caller, long userId, string jobDescription) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).EditQuestionForUser(userId);
          var user = s.Get<UserOrganizationModel>(userId);
          if (user.JobDescription != jobDescription) {
            user.JobDescription = jobDescription;
            user.JobDescriptionFromTemplateId = null;
            s.Update(user);
            user.UpdateCache(s);
          }
          tx.Commit();
          s.Flush();
        }
      }
    }
  }
}

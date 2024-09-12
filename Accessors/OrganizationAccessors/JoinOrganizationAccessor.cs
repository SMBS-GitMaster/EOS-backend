using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.Query;
using RadialReview.Models.Application;
using System.Threading.Tasks;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Accountability;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.ViewModels;
using NHibernate;
using RadialReview.Variables;
using RadialReview.Models.Notifications;
using RadialReview.Utilities.Encrypt;
using RadialReview.Accessors.OrganizationAccessors;
using static RadialReview.Accessors.AccountabilityAccessor;
using EmailStrings = RadialReview.Core.Properties.EmailStrings;

namespace RadialReview.Accessors {
  public class AddedUser {
    public TempUserModel TempUser { get; set; }
    public UserOrganizationModel User { get; set; }
  }
  public class UserJoinResult {
    public string NexusId { get; set; }
    public UserOrganizationModel CreatedUser { get; set; }
  }

  public partial class JoinOrganizationAccessor : BaseAccessor {

		public static async Task<UserOrganizationModel> AttachUserModelToOrganization_Unsafe(ISession s, long orgId, string userModelId, bool isOrgAdmin, bool evalOnly, bool isManager, bool isFreeUser, params UserRoleType[] roles) {
			var user = s.Get<UserModel>(userModelId);
			var org = s.Get<OrganizationModel>(orgId);

			if (user == null)
				throw new PermissionsException("User does not exists");

			if (org == null)
				throw new PermissionsException("Organization does not exists");

			var newUser = new UserOrganizationModel() {
				EmailAtOrganization = user.UserName,
				EvalOnly = false,
				ClientOrganizationName = null,
				IsClient = false,
				IsPlaceholder = false,
				ManagerAtOrganization = isManager,
				ManagingOrganization = isOrgAdmin,
				Organization = org,
				IsFreeUser = isFreeUser,
				User = user,
				AttachTime = DateTime.UtcNow,
			};


			s.Save(newUser);

			user.UserOrganization.Add(newUser);
			user.UserOrganizationCount += 1;
			var newArray = user.UserOrganizationIds.NotNull(x => x.ToList()) ?? new List<long>();
			newArray.Add(newUser.Id);
			user.UserOrganizationIds = newArray.ToArray();
			s.Update(user);

			newUser.UpdateCache(s);

			await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.CreateUserOrganization(ses, newUser, new CreateUserOrganizationData() { DuringSelfOnboarding = false }));
			await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.OnUserOrganizationAttach(ses, newUser, new OnUserOrganizationAttachData() { DuringSelfOnboarding = false }));

			foreach (var role in roles) {
				await UserAccessor.AddRole(s, PermissionsUtility.CreateAdmin(s), newUser.Id, role);
			}

			return newUser;

		}

		public static async Task<AddedUser> AddUser(UserOrganizationModel caller, CreateUserOrganizationViewModel settings) {
			return await CreateUserUnderManager(caller, settings);
		}

		public static async Task<AddedUser> CreateUserUnderManager(UserOrganizationModel caller, CreateUserOrganizationViewModel settings) {
      
      return await CreateUserUnderManager(caller,settings.OrgId, settings.ManagerNodeId, settings.IsManager, settings.PositionName, settings.GetEmail(), settings.FirstName, settings.LastName, settings.IsClient, settings.ClientOrganizationName, settings.EvalOnly, settings.OnLeadershipTeam, settings.PlaceholderOnly, settings.NodeId, settings.SetOrgAdmin);
		}

		[Untested("is _AddUserToTemplateUnsafe wired up correctly?"/*,"hooks"*/)]
    private static async Task<AddedUser> CreateUserUnderManager(UserOrganizationModel caller,long orgId, long? managerNodeId, Boolean isManager, string positionName,
			String email, String firstName, String lastName, bool isClient, string organizationName, bool evalOnly, bool leadershipTeam, bool placeholder, long? seatNodeId, bool orgAdmin) {

      if (caller.Organization.Id != orgId && !caller.TestIsRadialAdmin())
        throw new PermissionsException();


			if (managerNodeId == AccountabilityAccessor.MANAGERNODE_NO_MANAGER || managerNodeId == -2/*Old magic number*/)
				managerNodeId = null;

			var nexusId = Guid.NewGuid();
			String id = null;

			var output = new AddedUser();

			var newUser = new UserOrganizationModel();
			var now = DateTime.UtcNow;
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var db = HibernateSession.GetCurrentSession()) {
					long newUserId = 0;

					var validator = new CreateUserUnderManagerValidator(firstName: firstName, lastName: lastName, email: email, isPlaceholder: placeholder, session: db);
					validator.Execute();

					using (var tx = db.BeginTransaction()) {

            var perms = PermissionsUtility.Create(db, caller).CanAddUserToOrganization(orgId);
						AccountabilityNode managerNode = null;

						if (managerNodeId != null) {
							managerNode = db.Get<AccountabilityNode>(managerNodeId.Value);
							if (managerNode == null)
								throw new PermissionsException("Parent does not exist.");
						}

						output.User = newUser;

						if (orgAdmin == true) {
              perms.ManagingOrganization(orgId);
							newUser.ManagingOrganization = orgAdmin;
						}

						newUser.EvalOnly = evalOnly;
						newUser.ClientOrganizationName = organizationName;
						newUser.IsClient = isClient;
						newUser.IsPlaceholder = placeholder;
						newUser.ManagerAtOrganization = isManager;
            newUser.Organization = db.Load<OrganizationModel>(orgId);
						newUser.EmailAtOrganization = email;
						output.TempUser = new TempUserModel() {
							FirstName = firstName,
							LastName = lastName,
							Email = email,
							Guid = nexusId.ToString(),
							LastSent = null,
              OrganizationId = orgId,
							LastSentByUserId = caller.Id,
							EmailStatus = null,
						};
						newUser.TempUser = output.TempUser;


						db.Save(newUser);
						newUser.TempUser.UserOrganizationId = newUser.Id;

						if (!string.IsNullOrWhiteSpace(positionName)) {
							var positionDuration = new PositionDurationModel(positionName, newUser.Organization.Id, caller.Id, newUser.Id) {
								CreateTime = now,
							};
							db.Save(positionDuration);
						}

						db.Update(newUser);

						if (isManager) {
							var subordinateTeam = OrganizationTeamModel.SubordinateTeam(caller, newUser);
							db.Save(subordinateTeam);
						}

						newUserId = newUser.Id;
						var validExistingSeatId = seatNodeId != null && seatNodeId != AccountabilityAccessor.SEATNODE_CREATE_SEAT;
						await using (var rt = RealTimeUtility.Create()) {
							if (managerNodeId == null && seatNodeId == AccountabilityAccessor.SEATNODE_CREATE_SEAT) {
								//Create For Root
								var rootId = db.Get<AccountabilityChart>(caller.Organization.AccountabilityChartId).RootId;
								var node = await AccountabilityAccessor.AppendNode(db, perms, rt, rootId, usersToUpdate, userIds: new List<long> { newUser.Id }, positionName: positionName);
							} else if (managerNodeId == null && seatNodeId == AccountabilityAccessor.SEATNODE_OUTSIDE_ACCOUNTABILITY_CHART) {
								//Just Create User.
								//noop
							} else if (managerNode != null && !validExistingSeatId) {
								//Create a new seat
								var node = await AccountabilityAccessor.AppendNode(db, perms, rt, managerNode.Id, usersToUpdate, userIds: new List<long> { newUser.Id }, positionName: positionName);
							} else if (validExistingSeatId) {
								await AccountabilityAccessor.AddUserToNode(db, perms, rt, seatNodeId.Value, newUser.Id, usersToUpdate);
							}
							//Just refresh the user.
							newUser.UpdateCache(db);
						}
						tx.Commit();
					}

					using (var tx = db.BeginTransaction()) {
						//Attach
						if (caller.Id != UserOrganizationModel.ADMIN_ID) {
							caller = db.Get<UserOrganizationModel>(caller.Id);
						}

						var nexus = new NexusModel(nexusId) {
							ActionCode = NexusActions.JoinOrganizationUnderManager,
							ByUserId = caller.Id,
							ForUserId = newUserId,
						};

						nexus.SetArgs(new string[] {
              "" + orgId,
							email,
							"" + newUserId,
							firstName,
							lastName,
							"" + isClient
						});
						id = nexus.Id;
						db.SaveOrUpdate(nexus);

						if (caller.Id != UserOrganizationModel.ADMIN_ID) {
							db.SaveOrUpdate(caller);
						}

						tx.Commit();
						db.Flush();
					}
					using (var tx = db.BeginTransaction()) {
						var perms = PermissionsUtility.Create(db, caller);
						if (leadershipTeam) {
							await UserAccessor.AddRole(db, perms, newUser.Id, UserRoleType.LeadershipTeamMember);
						}
						if (placeholder) {
							await UserAccessor.AddRole(db, perms, newUser.Id, UserRoleType.PlaceholderOnly);
						}
						tx.Commit();
						db.Flush();
					}

					using (var tx = db.BeginTransaction()) {
						await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.CreateUserOrganization(ses, newUser, new CreateUserOrganizationData() { DuringSelfOnboarding = false }));
						tx.Commit();
						db.Flush();
					}
				}
			}
			return output;
		}

		public static async Task<UserJoinResult> JoinOrganizationUnderManager(UserOrganizationModel caller, CreateUserOrganizationViewModel settings) {
			var addedUser = await CreateUserUnderManager(caller, settings);
			if (settings.SendEmail) {
				try {
					var mail = CreateJoinEmailToGuid(caller, addedUser.TempUser, true);
					await Emailer.SendEmail(mail);
				} catch (Exception e) {
					log.Error("invite email error", e);
					await NotificationAccessor.FireNotification_Unsafe(NotificationGroupKey.FailedInvite(addedUser.TempUser.UserOrganizationId), caller.Id, NotificationDevices.Computer, "Email invite failed to send to " + addedUser.TempUser.Email, "<a href='#' onclick='showModal(\"Resend Email\", \"/Organization/ResendJoin/" + addedUser.TempUser.UserOrganizationId + "\",\"/Organization/ResendJoin/" + addedUser.TempUser.UserOrganizationId + "\")'>Resend?</a>");
				}
			}
			return new UserJoinResult() {
				CreatedUser = addedUser.User,
				NexusId = addedUser.TempUser.Guid,
			};
		}

		public static async Task<EmailResult> ResendAllEmails(UserOrganizationModel caller, long organizationId) {
			var unsentEmails = new List<Mail>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).Or(x => x.ManagerAtOrganization(caller.Id, organizationId), x => x.RadialAdmin());

					var toSend = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId && x.TempUser != null && x.DeleteTime == null && x.User == null && x.IsPlaceholder == false).Fetch(x => x.TempUser).Eager.List().ToList();
					foreach (var user in toSend) {
						unsentEmails.Add(CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, user.TempUser, s, true));
						user.UpdateCache(s);
					}
					tx.Commit();
					s.Flush();

				}
			}
			return await Emailer.SendEmails(unsentEmails);
		}

		public static async Task<EmailResult> SendAllJoinEmails(UserOrganizationModel caller, long organizationId) {
			var unsent = new List<Mail>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagerAtOrganization(caller.Id, organizationId);

					var toSend = s.QueryOver<TempUserModel>().Where(x => x.OrganizationId == organizationId && x.LastSent == null).List().ToList();

					var toUpdate = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(toSend.Select(x => x.UserOrganizationId).ToArray()).List().ToList();
					foreach (var user in toUpdate) {
						if (user.DeleteTime != null || user.IsPlaceholder)
							toSend.RemoveAll(x => x.UserOrganizationId == user.Id);

					}

					foreach (var tempUser in toSend) {
						var found = toUpdate.FirstOrDefault(x => x.Id == tempUser.UserOrganizationId);
						if (found == null || found.DeleteTime != null)
							continue;
						unsent.Add(CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, tempUser, s, true));
					}

					foreach (var user in toUpdate) {
						user.UpdateCache(s);
					}
					tx.Commit();
					s.Flush();
				}
			}
			var output = ((await Emailer.SendEmails(unsent)));
			return output;
		}

		public static Mail CreateJoinEmailToGuid(UserOrganizationModel caller, TempUserModel tempUser, bool includeVerificationToken) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var result = CreateJoinEmailToGuid(s.ToDataInteraction(false), caller, tempUser, s, includeVerificationToken);

					var user = s.Get<UserOrganizationModel>(tempUser.UserOrganizationId);
					if (user != null)
						user.UpdateCache(s);

					tx.Commit();
					s.Flush();
					return result;
				}
			}
		}

		[Obsolete("Update userOrganization cache", false)]
		public static Mail CreateJoinEmailToGuid(DataInteraction s, UserOrganizationModel caller, TempUserModel tempUser, ISession session, bool includeVerificationToken) {
			var emailAddress = tempUser.Email;
			var firstName = tempUser.FirstName;
			var lastName = tempUser.LastName;
			var id = tempUser.Guid;

			tempUser = s.Get<TempUserModel>(tempUser.Id);
			tempUser.LastSent = DateTime.UtcNow;
			s.Merge(tempUser);

			//Send Email
			var verificationToken = "";
			if (includeVerificationToken) {
				verificationToken = "&token=" + Crypto.UniqueHash(emailAddress);
			}

			var url = "Account/Register?returnUrl=%2FOrganization%2FJoin%2F" + id + verificationToken + "&utm_source=tt_welcome_email&utm_medium=1st_link";
			url = Config.BaseUrl(caller.Organization) + url;

      var managerName = caller.GetName();
			var productName = Config.ProductName(caller.Organization);
      var orgName = caller.Organization.Name;

      var subject = string.Format(EmailStrings.JoinOrganizationUnderManager_Subject, firstName, orgName, productName, managerName);
      if (caller.IsClientSuccess()) {
        subject = string.Format("You've been invited to join {1} on {2}", firstName, orgName, productName, managerName);
      }

			return Mail.To(EmailTypes.JoinOrganization, emailAddress)
        .Subject(subject)
        .Body(session.GetSettingOrDefault(Variable.Names.JOIN_ORGANIZATION_UNDER_MANAGER_BODY, EmailStrings.JoinOrganizationUnderManager_Body), firstName, orgName, url, "Join "+orgName, Config.ProductName());

		}
	}
}

using NHibernate;
using RadialReview.Crosscutting.Flags;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.CompanyValue;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Angular.VTO;
using RadialReview.Models.Askables;
using RadialReview.Models.Components;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.Query;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Variables;
using static RadialReview.Models.Askables.Deprecated;
using RadialReview.Models.Variables;
using TimeZoneConverter;
using static RadialReview.Accessors.AccountabilityAccessor;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Accessors;
using RadialReview.Models.Application;

namespace RadialReview.Accessors {

	public class OrganizationAccessor : BaseAccessor {
		public class CreateOrganizationOutput {
			public OrganizationModel organization { get; set; }
			public UserOrganizationModel NewUser { get; set; }
			public AccountabilityNode NewUserNode { get; set; }
		}

		public static ShareVtoPages GetVtoSharedPages(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					return s.Get<OrganizationModel>(orgId).Settings.ShareVtoPages;
				}
			}
		}

		public static long GetAllMembersTeamId(ISession s, PermissionsUtility perms, long orgId) {
			perms.TryWithAlternateUsers(x => x.ViewOrganization(orgId));
			var found = s.QueryOver<OrganizationTeamModel>().Where(x => x.DeleteTime == null && x.Type == TeamType.AllMembers && x.Organization.Id == orgId).Take(1).List().ToList();
			return found.FirstOrDefault().NotNull(x => x.Id);
		}

		public static async Task<CreateOrganizationOutput> CreateOrganization(UserModel user, PaymentPlanType planType, DateTime now, OrgCreationData data, bool selfOnboard) {
			using (var usersToUpdate = new UserCacheUpdater()) {
				using (var s = HibernateSession.GetCurrentSession()) {
					var result = await CreateOrganization(s, user, planType, now, data, selfOnboard, usersToUpdate);
					s.Flush();
					return result;
				}
			}
		}

		public static async Task<CreateOrganizationOutput> CreateOrganization(ISession s, UserModel user, PaymentPlanType planType, DateTime now, OrgCreationData data, bool selfOnboard, UserCacheUpdater usersToUpdate) {
			UserOrganizationModel userOrgModel;
			OrganizationTeamModel allMemberTeam;
			PermissionsUtility perms;
			AccountabilityChart acChart;
			UserOrganizationModel primaryContact = null;
			UserOrganizationModel supportUser;

			var output = new CreateOrganizationOutput() { };

			using (var tx = s.BeginTransaction()) {

				output.organization = new OrganizationModel() {
					CreationTime = now,
					Name = data.Name,
					ManagersCanEdit = false,
					AccountType = data.AccountType
				};

				#region Set Settings
				if (data.StartDeactivated) {
					output.organization.DeleteTime = new DateTime(1, 1, 1);
				}

				output.organization.SendEmailImmediately = s.GetSettingOrDefault(Variable.Names.SEND_EMAIL_IMMEDIATELY_DEFAULT, false); //should test an update here.
				output.organization.Settings.EnableL10 = data.EnableL10;
				output.organization.Settings.EnableReview = data.EnableReview;
				output.organization.Settings.DisableAC = !data.EnableAC;
				output.organization.Settings.EnablePeople = data.EnablePeople;
				output.organization.Settings.EnableCoreProcess = data.EnableProcess;
				output.organization.Settings.EnableZapier = data.EnableZapier;
				output.organization.Settings.EnableDocs = data.EnableDocs;
				output.organization.Settings.EnableWhale = data.EnableWhale;
        output.organization.Settings.EnableBetaButton = true;
				s.Save(output.organization);
				#endregion

				#region PaymentPlan
				var paymentPlan = PaymentAccessor.GeneratePlan(planType, now, data.TrialEnd);
				PaymentAccessor.AttachPlan(s, output.organization, paymentPlan);
				output.organization.PaymentPlan = paymentPlan;
				output.organization.Organization = output.organization;
				s.Update(output.organization);
				#endregion

				#region AddUser to Organization
				user = s.Get<UserModel>(user.Id);

				userOrgModel = new UserOrganizationModel() {
					Organization = output.organization,
					User = user,
					ManagerAtOrganization = true,
					ManagingOrganization = true,
					EmailAtOrganization = user.Email,
					AttachTime = now,
					CreateTime = now,
				};

				s.Save(user);
				s.SaveOrUpdate(userOrgModel);

				#endregion

				#region Set Role
				user.UserOrganization.Add(userOrgModel);
				user.UserOrganizationCount += 1;

				var newArray = new List<long>();
				if (user.UserOrganizationIds != null) {
					newArray = user.UserOrganizationIds.ToList();
				}

				newArray.Add(userOrgModel.Id);
				user.UserOrganizationIds = newArray.ToArray();
				user.CurrentRole = userOrgModel.Id;

				output.organization.Members.Add(userOrgModel);
				s.Update(user);
				s.Save(output.organization);
        #endregion

        #region Referral Plugin
        if (data.ReferralSource != null && data.ReferralData != null)
          TermsAccessor.TryApplyTermsPluginByCode(userOrgModel, output.organization.Id, data.ReferralSource, data.ReferralData);
        #endregion

        #region Update OrganizationLookup
        s.Save(new OrganizationLookup() {
					OrgId = output.organization.Id,
					LastUserLogin = userOrgModel.Id,
					LastUserLoginTime = DateTime.UtcNow,
				});
				#endregion

				#region Create/Populate Organizational Chart
				perms = PermissionsUtility.Create(s, userOrgModel);
				acChart = AccountabilityAccessor.CreateChart(s, perms, output.organization.Id, false);
				output.organization.AccountabilityChartId = acChart.Id;

				#region set User Roles
				await UserAccessor.AddRole(s, perms, userOrgModel.Id, UserRoleType.LeadershipTeamMember);
				await UserAccessor.AddRole(s, perms, userOrgModel.Id, UserRoleType.AccountContact);
				#endregion

				supportUser = userOrgModel;

				if (selfOnboard) {
					//Need to grab account creation admin.
					var supportUserId = s.GetSettingOrDefault<long>(Variable.Names.ACCOUNT_CREATION_USER_ID, () => 457170);
					supportUser = s.Get<UserOrganizationModel>(supportUserId);
				}

				#endregion

				#region Create Teams
				//Add team for every member
				allMemberTeam = new OrganizationTeamModel() {
					CreatedBy = supportUser.Id,
					Name = output.organization.Name,
					OnlyManagersEdit = true,
					Organization = output.organization,
					InterReview = false,
					Type = TeamType.AllMembers
				};
				s.Save(allMemberTeam);
				//Add team for every manager
				var managerTeam = new OrganizationTeamModel() {
					CreatedBy = supportUser.Id,
					Name = Config.ManagerName() + "s at " + output.organization.Name,
					OnlyManagersEdit = true,
					Organization = output.organization,
					InterReview = false,
					Type = TeamType.Managers
				};
				s.Save(managerTeam);
				#endregion

				#region Update UserLookup
				try {
					if (supportUser != null) {
						supportUser.UpdateCache(s);
					}
				} catch (Exception) {

				}
				#endregion

				#region Add Default Permissions
				PermissionsAccessor.InitializePermItems_Unsafe(s, perms.GetCaller(), PermItem.ResourceType.UpgradeUsersForOrganization, output.organization.Id,
					PermTiny.Admins(),
					PermTiny.RGM(allMemberTeam.Id, admin: false)
				);
				PermissionsAccessor.InitializePermItems_Unsafe(s, perms.GetCaller(), PermItem.ResourceType.UpdatePaymentForOrganization, output.organization.Id,
					PermTiny.Admins()
				);
				PermissionsAccessor.InitializePermItems_Unsafe(s, perms.GetCaller(), PermItem.ResourceType.EditDeleteUserDataForOrganization, output.organization.Id,
					PermTiny.Admins()
				);

				#endregion


				if (selfOnboard) {
					await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.CreateUserOrganization(ses, userOrgModel, new CreateUserOrganizationData() { DuringSelfOnboarding = selfOnboard }));
					await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.OnUserOrganizationAttach(ses, userOrgModel, new OnUserOrganizationAttachData() { DuringSelfOnboarding = selfOnboard }));
				}

				tx.Commit();
			}


			using (var tx = s.BeginTransaction()) {

				var year = DateTime.UtcNow.Year;
				foreach (var q in Enumerable.Range(1, 4)) {
					s.Save(new PeriodModel() {
						Name = year + " Q" + q,
						StartTime = new DateTime(year, 1, 1).AddDays((q - 1) * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
						EndTime = new DateTime(year, 1, 1).AddDays(q * 13 * 7).StartOfWeek(DayOfWeek.Sunday),
						OrganizationId = output.organization.Id,
					});
				}

				foreach (var defaultQ in new[]{
						"What is their greatest contribution to the team?",
						"What should they start or stop doing?"
					}) {
					var r = new ResponsibilityModel() {
						Category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.FEEDBACK),
						ForOrganizationId = output.organization.Id,
						ForResponsibilityGroup = allMemberTeam.Id,
						CreateTime = now,
						Weight = WeightType.Normal,
						Required = true,
						Responsibility = defaultQ
					};
					r.SetQuestionType(QuestionType.Feedback);
					s.Save(r);

					allMemberTeam.Responsibilities.Add(r);
				}
				s.Update(allMemberTeam);

				output.NewUser = userOrgModel;
				s.Flush();
				try {
					userOrgModel.UpdateCache(s);
				} catch (Exception e) {
					log.Error(e);
				}

				tx.Commit();
			}


			using (var tx = s.BeginTransaction()) {
				data.OrgId = output.organization.Id;
				s.Save(data);
				tx.Commit();
			}



			s.Flush();

			if (selfOnboard) {
				//only add to AC if we're self onboarding
				using (var tx = s.BeginTransaction()) {
					s.Clear();
					var permsAdmin = PermissionsUtility.Create(s, UserOrganizationModel.ADMIN);
					await using (var rt = RealTimeUtility.Create(false)) {

						var messages = s.GetSettingOrDefault(Variable.Names.GET_STARTED_MESSAGES, () => new GetStartedMessages());

						AccountabilityNode visionaryNode = await _CreateNamedNode(s, acChart.RootId, data, userOrgModel, acChart, permsAdmin, rt, EosUserType.Visionary, "Visionary", messages.VisionaryRoles, usersToUpdate);
						AccountabilityNode integratorNode = await _CreateNamedNode(s, visionaryNode.Id, data, userOrgModel, acChart, permsAdmin, rt, EosUserType.Integrator, "Second-in-Command", messages.IntegratorRoles, usersToUpdate);

						AccountabilityNode salesNode = await _CreateNamedNode(s, integratorNode.Id, data, userOrgModel, acChart, permsAdmin, rt, EosUserType.SalesOrMarketing, "Sales/Marketing", messages.SalesMarketingRoles, usersToUpdate);
						AccountabilityNode operationsNode = await _CreateNamedNode(s, integratorNode.Id, data, userOrgModel, acChart, permsAdmin, rt, EosUserType.Ops, "Operations", messages.OperationsRoles, usersToUpdate);
						AccountabilityNode financeNode = await _CreateNamedNode(s, integratorNode.Id, data, userOrgModel, acChart, permsAdmin, rt, EosUserType.Finance, "Finance", messages.FinanceRoles, usersToUpdate);


						switch (data.ContactEosUserType) {
							case EosUserType.Visionary:
								output.NewUserNode = visionaryNode;
								break;
							case EosUserType.Integrator:
								output.NewUserNode = integratorNode;
								break;
							case EosUserType.Ops:
								output.NewUserNode = operationsNode;
								break;
							case EosUserType.SalesOrMarketing:
								output.NewUserNode = salesNode;
								break;
							case EosUserType.Finance:
								output.NewUserNode = financeNode;
								break;
							default:
								var parentId = integratorNode.Id;
								if (data.ContactEosUserType == EosUserType.Implementor)
									parentId = acChart.RootId;

								output.NewUserNode = await AccountabilityAccessor.AppendNode(s, permsAdmin, rt, integratorNode.Id, usersToUpdate, userIds: new List<long> { userOrgModel.Id });
								//Originally we were adding the support user here.
								if (!string.IsNullOrWhiteSpace(data.ContactPosition)) {

									var userIds = output.NewUserNode.GetUsers(s).SelectId().ToList();
									await AccountabilityAccessor.UpdateAccountabilityNode(s, rt, permsAdmin, output.NewUserNode.Id, data.ContactPosition, userIds, usersToUpdate);
								}
								break;
						}

					}
					tx.Commit();
					s.Flush();
				}
			}


			if (data != null && (data.ContactEmail != null || data.ContactFN != null || data.ContactLN != null)) {
				//Add Primary contact
				var primContact = new CreateUserOrganizationViewModel() {
					Email = data.ContactEmail,
					FirstName = data.ContactFN,
					LastName = data.ContactLN,
					SendEmail = false,
					OrgId = output.organization.Id,
					IsManager = true,
					ManagerNodeId = acChart.RootId,
					PositionName = data.ContactPosition,
					NodeId = 0
				};

				if (selfOnboard) {
					primaryContact = userOrgModel;
				} else {
					//Created by our support team.
					var result = await UserAccessor.CreateUser(userOrgModel, primContact);
					primaryContact = result.CreatedUser;
				}

				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(output.organization.Id);
					org.PrimaryContactUserId = primaryContact.Id;
					s.Update(org);
					await EventUtil.Trigger(x => x.Create(s, EventType.CreatePrimaryContact, primaryContact, primaryContact, message: primaryContact.GetName()));
					tx.Commit();
					s.Flush();
				}
			}


			using (var tx = s.BeginTransaction()) {
				//Generate Account Age Events 
				EventUtil.GenerateAccountAgeEvents(s, output.organization.Id, now);
				await EventUtil.Trigger(x => x.Create(s, EventType.CreateOrganization, userOrgModel, output.organization, message: output.organization.GetName()));
				if (data.EnableL10) {
					await EventUtil.Trigger(x => x.Create(s, EventType.EnableL10, userOrgModel, output.organization));
				}

				if (data.EnableReview) {
					await EventUtil.Trigger(x => x.Create(s, EventType.EnableReview, userOrgModel, output.organization));
				}

				tx.Commit();
			}

			using (var tx = s.BeginTransaction()) {
				var org = s.Get<OrganizationModel>(output.organization.Id);
				var nu = output.NewUser;
				await HooksRegistry.Each<IOrganizationHook>((ses, x) => x.CreateOrganization(ses, nu, org, data, new IOrganizationHookCreate() { SelfOnboard = selfOnboard }));
				tx.Commit();
			}

			if (selfOnboard)
			{
				//Create Default Leadership meeting
				using (var tx = s.BeginTransaction())
				{
					var name = output.organization.GetName() + " Leadership Team";
					var recur = await L10Accessor.CreateBlankRecurrence(s, perms, output.organization.Id, true, Models.L10.MeetingType.L10, name);
					recur.IsDefaultMeeting = true;
					s.Update(recur);
					tx.Commit();
				}
			}

			s.Flush();
			return output;
		}

		private static async Task<AccountabilityNode> _CreateNamedNode(ISession s, long parentNodeId, OrgCreationData data, UserOrganizationModel userOrgModel, AccountabilityChart acChart, PermissionsUtility permsAdmin, RealTimeUtility rt, EosUserType myType, string defaultPositionTitle, string[] roles, UserCacheUpdater usersToUpdate) {
			var isMe = (data.ContactEosUserType == myType);

			var l1 = new List<long>();
			if (isMe) {
				l1.Add(userOrgModel.Id);
			}

			var node = await AccountabilityAccessor.AppendNode(s, permsAdmin, rt, parentNodeId, usersToUpdate, userIds: l1, rolesToInclude: (roles ?? new string[0]).ToList());
			await AccountabilityAccessor.UpdateAccountabilityNode(s, rt, permsAdmin, node.Id, (isMe ? data.ContactPosition : defaultPositionTitle), node.GetUsers(s).SelectId().ToList(), usersToUpdate);

			return node;
		}

		public static IEnumerable<AngularUser> GetAngularUsers(UserOrganizationModel caller, long id) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					return GetAllUserOrganizations(db, PermissionsUtility.Create(db, caller), id).Select(x => AngularUser.CreateUser(x));
				}
			}
		}

		public static async Task<UserOrganizationModel> JoinOrganization(UserModel user, long managerId, long userOrgPlaceholder) {
			UserOrganizationModel userOrg = null;
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					var manager = db.Get<UserOrganizationModel>(managerId);
					var orgId = manager.Organization.Id;
					var organization = db.Get<OrganizationModel>(orgId);
					user = db.Get<UserModel>(user.Id);
					userOrg = db.Get<UserOrganizationModel>(userOrgPlaceholder);

					userOrg.AttachTime = DateTime.UtcNow;
					userOrg.User = user;
					userOrg.Organization = organization;
					user.CurrentRole = userOrgPlaceholder;

					user.UserOrganization.Add(userOrg);
					user.UserOrganizationCount += 1;

					var newArray = user.UserOrganizationIds.NotNull(x => x.ToList()) ?? new List<long>();
					newArray.Add(userOrg.Id);
					user.UserOrganizationIds = newArray.ToArray();

					if (user.ImageGuid == null && userOrg.TempUser.ImageGuid != null) {
						user.ImageGuid = userOrg.TempUser.ImageGuid;
					}

					db.Delete(userOrg.TempUser);

					if (user.SendTodoTime == -1) {
						user.SendTodoTime = organization.Settings.DefaultSendTodoTime;
					}

					userOrg.TempUser = null;

					db.SaveOrUpdate(user);
					userOrg.UpdateCache(db);

					tx.Commit();
					db.Flush();
				}
			}
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					await HooksRegistry.Each<ICreateUserOrganizationHook>((ses, x) => x.OnUserOrganizationAttach(ses, userOrg, new OnUserOrganizationAttachData() { DuringSelfOnboarding = false }));
					tx.Commit();
					db.Flush();
				}
			}

			return userOrg;
		}

		public static List<UserOrganizationModel> GetOrganizationMembers(UserOrganizationModel caller, long organizationId, bool teams, bool managers) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var users = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();

					if (managers) {
						var allManagers = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
						foreach (var user in users) {
							user.PopulateManagers(allManagers);
						}
					}

					if (teams) {
						var allTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == organizationId && x.DeleteTime == null).List().ToList();
						var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == organizationId).List().ToList();
						foreach (var user in users) {
							user.PopulateTeams(allTeams, allTeamDurations);
						}
					}
					return users;
				}
			}
		}

		public static OrganizationTeamModel AddOrganizationTeam(UserOrganizationModel caller, long organizationId, string teamName, bool onlyManagersEdit, bool secret) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).EditTeam(0).ViewOrganization(organizationId);


					var org = s.Get<OrganizationModel>(organizationId);

					var orgTeam = new OrganizationTeamModel() {
						Organization = org,
						CreatedBy = caller.Id,
						Name = teamName,
						OnlyManagersEdit = onlyManagersEdit,
						Secret = secret,
					};

					s.Save(orgTeam);
					tx.Commit();
					s.Flush();

					return orgTeam;
				}
			}
		}

		public static async Task Edit(UserOrganizationModel caller, long organizationId, string organizationName = null,
				bool? managersHaveAdmin = null,
				bool? strictHierarchy = null,
				bool? managersCanEditPositions = null,
				bool? sendEmailImmediately = null,
				bool? managersCanEditSelf = null,
				bool? employeesCanEditSelf = null,
				bool? managersCanCreateSurvey = null,
				bool? employeesCanCreateSurvey = null,
				string rockName = null,
				bool? onlySeeRockAndScorecardBelowYou = null,
				string timeZoneId = null,
				DayOfWeek? weekStart = null,
				ScorecardPeriod? scorecardPeriod = null,
				Month? startOfYearMonth = null,
				DateOffset? startOfYearOffset = null,
				string dateFormat = null,
				NumberFormat? numberFormat = null,
				bool? limitFiveState = null,
				int? defaultTodoSendTime = null,
				bool? allowAddClient = null,
				string primaryColorHex = null,
				long? shareVTOFromRecurrenceId = null,
				ShareVtoPages? shareVtoPages = null,
				bool? enableCoreProcess = null,
				bool? enableZapier = null,
				bool? usersCanMoveIssuesToAnyMeeting = null,
				bool? usersCanSharePHToAnyMeeting = null
			) {
			var updates = new IOrganizationHookUpdates();

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).EditOrganization(organizationId).ManagingOrganization(caller.Organization.Id);
					var org = s.Get<OrganizationModel>(organizationId);
					if (managersHaveAdmin != null && managersHaveAdmin.Value != org.ManagersCanEdit) {
						if (caller.ManagingOrganization) {
							org.ManagersCanEdit = managersHaveAdmin.Value;
						} else {
							throw new PermissionsException("You cannot change whether managers are admins at the organization.");
						}
					}
					if (!String.IsNullOrWhiteSpace(organizationName) && org.Name != organizationName) {
						updates.UpdateName = true;
						org.Name = (organizationName);
						var managers = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.Managers && x.DeleteTime == null).List().FirstOrDefault();
						if (managers != null) {
							managers.Name = Config.ManagerName() + "s at " + organizationName;
							s.Update(managers);
						}
						var allTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.AllMembers && x.DeleteTime == null).List().FirstOrDefault();
						if (allTeam != null) {
							allTeam.Name = organizationName;
							s.Update(allTeam);
						}

						var chart = s.Get<AccountabilityChart>(org.AccountabilityChartId);
						if (chart != null) {
							chart.Name = organizationName;
							s.Update(chart);
						}

					}
					if (strictHierarchy != null) {
						updates.Settings.StrictHierarchy = true;
						org.StrictHierarchy = strictHierarchy.Value;
					}

					if (managersCanEditPositions != null) {
						updates.Settings.ManagersCanEditPositions = true;
						org.ManagersCanEditPositions = managersCanEditPositions.Value;
					}

					if (sendEmailImmediately != null) {
						updates.Settings.SendEmailImmediately = true;
						org.SendEmailImmediately = sendEmailImmediately.Value;
					}



					if (managersCanEditSelf != null) {
						updates.Settings.ManagersCanEditSelf = true;
						org.Settings.ManagersCanEditSelf = managersCanEditSelf.Value;
					}

					if (limitFiveState != null) {
						updates.Settings.LimitFiveState = true;
						org.Settings.LimitFiveState = limitFiveState.Value;
					}

					if (employeesCanEditSelf != null) {
						updates.Settings.EmployeesCanEditSelf = true;
						org.Settings.EmployeesCanEditSelf = employeesCanEditSelf.Value;
					}

					if (employeesCanCreateSurvey != null) {
						updates.Settings.EmployeesCanCreateSurvey = true;
						org.Settings.EmployeesCanCreateSurvey = employeesCanCreateSurvey.Value;
					}

					if (usersCanMoveIssuesToAnyMeeting != null) {
						updates.Settings.UsersCanMoveIssuesToAnyMeeting = true;
						org.Settings.UsersCanMoveIssuesToAnyMeeting = usersCanMoveIssuesToAnyMeeting.Value;
					}

					if (usersCanSharePHToAnyMeeting != null) {
						updates.Settings.UsersCanSharePHToAnyMeeting = true;
						org.Settings.UsersCanSharePHToAnyMeeting = usersCanSharePHToAnyMeeting.Value;
					}

					if (onlySeeRockAndScorecardBelowYou != null) {
						updates.Settings.OnlySeeRocksAndScorecardBelowYou = true;
						org.Settings.OnlySeeRocksAndScorecardBelowYou = onlySeeRockAndScorecardBelowYou.Value;
					}

					if (scorecardPeriod != null) {
						updates.Settings.ScorecardPeriod = true;
						org.Settings.ScorecardPeriod = scorecardPeriod.Value;
					}

					if (dateFormat != null) {
						updates.Settings.DateFormat = true;
						org.Settings.DateFormat = dateFormat;
					}

					if (managersCanCreateSurvey != null) {
						updates.Settings.ManagersCanCreateSurvey = true;
						org.Settings.ManagersCanCreateSurvey = managersCanCreateSurvey.Value;
					}

					if (!String.IsNullOrWhiteSpace(rockName)) {
						updates.Settings.RockName = true;
						org.Settings.RockName = rockName;
					}
					TimeZoneInfo tz;
					if (!String.IsNullOrWhiteSpace(timeZoneId) && TZConvert.TryGetTimeZoneInfo(timeZoneId, out tz)) {
						updates.Settings.TimeZoneId = true;
						org.Settings.TimeZoneId = tz.Id;
					}

					if (weekStart != null) {
						updates.Settings.WeekStart = true;
						org.Settings.WeekStart = weekStart.Value;
					}

					if (startOfYearMonth != null) {
						updates.Settings.StartOfYearMonth = true;
						org.Settings.StartOfYearMonth = startOfYearMonth.Value;
					}

					if (startOfYearOffset != null) {
						updates.Settings.StartOfYearOffset = true;
						org.Settings.StartOfYearOffset = startOfYearOffset.Value;
					}

					if (numberFormat != null) {
						updates.Settings.NumberFormat = true;
						org.Settings.NumberFormat = numberFormat.Value;
					}

					if (defaultTodoSendTime != null) {
						updates.Settings.DefaultSendTodoTime = true;
						org.Settings.DefaultSendTodoTime = defaultTodoSendTime.Value;
					}

					if (allowAddClient != null) {
						updates.Settings.AllowAddClient = true;
						org.Settings.AllowAddClient = allowAddClient.Value;
					}

					if (primaryColorHex != null) {
						updates.Settings.PrimaryColor = true;
						org.Settings.PrimaryColor = ColorComponent.FromHex(primaryColorHex);
					}

					if (shareVTOFromRecurrenceId != null) {
						updates.Settings.ShareVto = true;
						await L10Accessor.SetSharedVTOVision(s, perms, org.Id, shareVTOFromRecurrenceId.Value);
					}

					if (shareVtoPages != null) {
						updates.Settings.ShareVtoPages = true;
						org.Settings.ShareVtoPages = shareVtoPages.Value;
					}

					if (enableCoreProcess != null) {
						updates.Settings.EnableCoreProcess = true;
						org.Settings.EnableCoreProcess = enableCoreProcess.Value;
					}
					if (enableZapier != null) {
						updates.Settings.EnableZapier = true;
						org.Settings.EnableZapier = enableZapier.Value;
					}


					s.Update(org);

					var all = GetAllUserOrganizations(s, perms, organizationId);

					await HooksRegistry.Each<IOrganizationHook>((ses, x) => x.UpdateOrganization(ses, org.Id, updates, caller));


					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<UserOrganizationModel> GetOrganizationManagers(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var managers = s.QueryOver<UserOrganizationModel>()
											.Where(x =>
												x.Organization.Id == organizationId &&
												(x.ManagerAtOrganization || x.ManagingOrganization) &&
												x.DeleteTime == null
											).List()
											.OrderBy(x => x.GetName())
											.ToList();
					return managers;
				}
			}
		}

		[Obsolete("broken", true)]
		public static Tree GetOrganizationTree(ISession s, PermissionsUtility perms, long orgId, long? parentId = null, bool includeTeams = false, bool includeRoles = false) {
			perms.ViewOrganization(orgId);

			var org = s.Get<OrganizationModel>(orgId);

			List<UserOrganizationModel> managers;

			if (parentId == null) {
				managers = s.QueryOver<UserOrganizationModel>()
							.Where(x => x.Organization.Id == orgId && x.ManagingOrganization)
							.List()
							.ToListAlive();
			} else {
				var parent = s.Get<UserOrganizationModel>(parentId);

				if (orgId != parent.Organization.Id) {
					throw new PermissionsException("Organizations do not match");
				}

				perms.ViewOrganization(parent.Organization.Id);
				managers = parent.AsList();
			}

			var managerIds = managers.Select(x => x.Id).ToList();

			if (includeTeams) {
				var managerTeams = s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.UserId).IsIn(managerIds).List().ToList();

				foreach (var t in managerTeams) {
					managers.First(x => x.Id == t.UserId).Teams.Add(t);
				}
			}

			var caller = perms.GetCaller();

			var deep = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);


			var managingOrg = caller.ManagingOrganization && orgId == caller.Organization.Id;

			var tree = new Tree() {
				name = org.Name,
				@class = "organizations",
				id = -1 * orgId,
				children = managers.Select(x => x.GetTree(s, perms, deep, caller.Id, force: managingOrg, includeRoles: includeRoles)).ToList()
			};

			return tree;
		}

		[Obsolete("broken", true)]
		public Tree GetOrganizationTree(UserOrganizationModel caller, long orgId, bool includeRoles = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationTree(s, perms, orgId, null, true, includeRoles);
				}
			}
		}

		public static List<QuestionCategoryModel> GetOrganizationCategories(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var orgCategories = s.QueryOver<QuestionCategoryModel>()
									.Where(x => (x.OriginId == organizationId && x.OriginType == OriginType.Organization))
									.List()
									.ToList();

					var appCategories = ApplicationAccessor.GetApplicationCategories(s);

					return orgCategories.Union(appCategories).ToList();
				}
			}
		}


		public static OrganizationModel GetOrganization(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.Get<OrganizationModel>(organizationId);
				}
			}
		}

		public static IEnumerable<CompanyValueModel> GetCompanyValues_Unsafe(ISession s, long organizationId) {
			return s.QueryOver<CompanyValueModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).Future();
		}

		public static List<CompanyValueModel> GetCompanyValues(AbstractQuery query, PermissionsUtility perms, long organizationId, DateRange range) {
			perms.ViewOrganization(organizationId);
			return GetCompanyValues_Unsafe(query, organizationId, range);
		}

		public static List<CompanyValueModel> GetCompanyValues_Unsafe(AbstractQuery query, long organizationId, DateRange range) {
			return query.Where<CompanyValueModel>(x => x.OrganizationId == organizationId).FilterRange(range).ToList();
		}

		public static List<CompanyValueModel> GetCompanyValues(UserOrganizationModel caller, long organizationId, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompanyValues(s.ToQueryProvider(true), perms, organizationId, range);
				}
			}
		}

		public static async Task EditCompanyValues(ISession s, PermissionsUtility perms, long organizationId, List<CompanyValueModel> companyValues) {
			perms.EditCompanyValues(organizationId);
			var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);

			foreach (var r in companyValues) {
				if (r.OrganizationId != organizationId) {
					throw new PermissionsException("You do not have access to this value.");
				}
				r.Category = category;
				s.SaveOrUpdate(r);
			}
			await using (var rt = RealTimeUtility.Create()) {
				var vtoIds = s.QueryOver<VtoModel>().Where(x => x.Organization.Id == organizationId).Select(x => x.Id).List<long>();
				foreach (var vtoId in vtoIds) {
					rt.UpdateVtos(vtoId).Update(new AngularVTO(vtoId) {
						Values = AngularList.Create(AngularListType.ReplaceAll, AngularCompanyValue.Create(companyValues))
					});
				}
			}


		}
		public static async Task EditCompanyValues(UserOrganizationModel caller, long organizationId, List<CompanyValueModel> companyValues) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await EditCompanyValues(s, perms, organizationId, companyValues);
					tx.Commit();
					s.Flush();
				}
			}
		}



    public static async Task SetV3BusinessPlanId(ISession s, UserOrganizationModel caller, long? v3BusinessPlanId)
    {
        var perms = PermissionsUtility.Create(s, caller);
        perms.EditOrganization(caller.Organization.Id).ManagingOrganization(caller.Organization.Id);
        OrganizationModel org = s.Get<OrganizationModel>(caller.Organization.Id);
        org.Settings.V3BusinessPlanId = v3BusinessPlanId;
        var updates = new IOrganizationHookUpdates();
        updates.Settings.V3BusinessPlanId = true;
        s.Update(org);
        await HooksRegistry.Each<IOrganizationHook>((ses, x) => x.UpdateOrganization(ses, org.Id, updates, caller));      
    }

    [Obsolete("remove", true)]
		public static List<RockModel> GetCompanyRocks(ISession s, PermissionsUtility perms, long organizationId) {
			throw new PermissionsException("cannot view");
			perms.ViewOrganization(organizationId);
			return s.QueryOver<RockModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId && x.CompanyRock).List().ToList();
		}

		[Obsolete("remove", true)]
		public static List<RockModel> GetCompanyRocks(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCompanyRocks(s, perms, organizationId);
				}
			}
		}
		public static IEnumerable<long> GetAllManagerIds(ISession s, PermissionsUtility perm, long organizationId, bool excludeClients = false) {
			perm.ViewOrganization(organizationId);

			var q = s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId && (x.ManagerAtOrganization || x.ManagingOrganization));
			if (excludeClients) {
				q = q.Where(x => !x.IsClient);
			}

			return q.Select(x => x.Id).Future<long>();
		}

		public static IEnumerable<long> GetAllUserOrganizationIds(ISession s, PermissionsUtility perm, long organizationId, bool excludeClients = false) {
			perm.ViewOrganization(organizationId);

			var q = s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId);

			if (excludeClients) {
				q = q.Where(x => !x.IsClient);
			}

			return q.Select(x => x.Id).Future<long>();
		}
		[Obsolete("Dangerous")]
		public static List<UserOrganizationModel> GetAllUserOrganizations(ISession s, PermissionsUtility perm, long organizationId) {
			perm.ViewOrganization(organizationId);
			return s.QueryOver<UserOrganizationModel>()
				.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
				.List().ToList();
		}

		public static async Task UpdateProducts(UserOrganizationModel caller, bool enableReview, bool enableL10, bool enableSurvey, bool enablePeople, bool enableCP, bool enableZapier, bool enableDocs, bool enableWhale, bool enableBetaButton, BrandingType branding) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ManagingOrganization(caller.Organization.Id);

					var org = s.Get<OrganizationModel>(caller.Organization.Id);

					if (org.Settings.EnableL10 != enableL10) {
						await EventUtil.Trigger(x => x.Create(s, enableL10 ? EventType.EnableL10 : EventType.DisableL10, caller, org));
					}

					if (org.Settings.EnableReview != enableReview) {
						await EventUtil.Trigger(x => x.Create(s, enableReview ? EventType.EnableReview : EventType.DisableReview, caller, org));
					}

					if (org.Settings.EnablePeople != enablePeople) {
						await EventUtil.Trigger(x => x.Create(s, enablePeople ? EventType.EnablePeople : EventType.DisablePeople, caller, org));
					}

					if (org.Settings.EnableCoreProcess != enableCP) {
						await EventUtil.Trigger(x => x.Create(s, enableCP ? EventType.EnableCoreProcess : EventType.DisableCoreProcess, caller, org));
					}

					if (org.Settings.EnableZapier != enableZapier) {
						await EventUtil.Trigger(x => x.Create(s, enableZapier ? EventType.EnableZapier : EventType.DisableZapier, caller, org));
					}

					if (org.Settings.EnableWhale != enableWhale) {
						await EventUtil.Trigger(x => x.Create(s, enableWhale ? EventType.EnableWhale : EventType.DisableWhale, caller, org));
					}

					if (org.Settings.EnableDocs != enableDocs) {
						await EventUtil.Trigger(x => x.Create(s, enableDocs ? EventType.EnableDocs : EventType.DisableDocs, caller, org));
					}

          if (org.Settings.EnableBetaButton != enableBetaButton)
          {
            await EventUtil.Trigger(x => x.Create(s, enableBetaButton ? EventType.EnableBetaButton : EventType.DisableBetaButton, caller, org));
          }

          org.Settings.EnableL10 = enableL10;
					org.Settings.EnableReview = enableReview;
					org.Settings.EnablePeople = enablePeople;
					org.Settings.Branding = branding;
					org.Settings.EnableSurvey = enableSurvey;
					org.Settings.EnableCoreProcess = enableCP;
          org.Settings.EnableBetaButton = enableBetaButton;
					org.Settings.EnableZapier = enableZapier;
					org.Settings.EnableWhale = enableWhale;
					org.Settings.EnableDocs = enableDocs;

					s.Update(org);

					tx.Commit();
					s.Flush();

				}
			}
		}

		public static IEnumerable<Askable> AskablesAboutOrganization(AbstractQuery query, PermissionsUtility perms, long orgId, DateRange range) {
			perms.ViewOrganization(orgId);
			return query.Where<AboutCompanyAskable>(x => x.DeleteTime == null && x.OrganizationId == orgId)
				.FilterRange(range)
				.ToList();
		}

		public static List<AboutCompanyAskable> GetQuestionsAboutCompany(UserOrganizationModel caller, long orgId, DateRange range) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					var q = s.ToQueryProvider(false);
					return AskablesAboutOrganization(q, perm, orgId, range).Cast<AboutCompanyAskable>().ToList();
				}
			}
		}

		public static void EditQuestionsAboutCompany(UserOrganizationModel caller, List<AboutCompanyAskable> questions) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller);
					questions.Select(x => x.OrganizationId)
						.Distinct()
						.ToList()
						.ForEach(x =>
							perm.EditOrganizationQuestions(x)
						);

					var cat = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.COMPANY_QUESTION);
					foreach (var q in questions) {
						q.Organization = s.Load<OrganizationModel>(q.OrganizationId);
						q.Category = cat;
						s.SaveOrUpdate(q);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static List<UserLookup> GetOrganizationMembersLookup(ISession s, PermissionsUtility perms, long organizationId, bool populatePersonallyManaging) {
			var caller = perms.GetCaller();
			perms.ViewOrganization(organizationId);
			var users = s.QueryOver<UserLookup>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();
			var isRadialAdmin = perms.HasRadialAdminFlags();
			if (!isRadialAdmin) {
				users = users.Where(x =>
        !x.Email.NotNull( y => y.ToLower().EndsWith("@mytractiontools.com")) &&
        !x.Email.NotNull(y => y.ToLower().EndsWith("@bloomgrowth.com")) &&
        !x.Email.NotNull(y => y.ToLower().EndsWith("@winterinternational.io"))
        ).ToList();
			}
			if (populatePersonallyManaging) {
				var subs = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);

				var orgManager = PermissionsAccessor.AnyTrue(s, caller, x => x.ManagingOrganization);


				users.ForEach(u =>
					u._PersonallyManaging = (isRadialAdmin || (orgManager && u.OrganizationId == organizationId) || subs.Contains(u.UserId)));
			}

			return users;
		}

		public static List<UserLookup> GetOrganizationMembersLookup(UserOrganizationModel caller, long organizationId, bool populatePersonallyManaging) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetOrganizationMembersLookup(s, perms, organizationId, populatePersonallyManaging);
				}
			}
		}

		public static List<ResponsibilityGroupModel> GetOrganizationResponsibilityGroupModels(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					return s.QueryOver<ResponsibilityGroupModel>()
								.Where(x => x.DeleteTime == null && x.Organization.Id == organizationId)
								.List().Where(x => !(x is OrganizationPositionModel)).ToList();
				}
			}
		}

		public static void EnsureAllAtOrganization(UserOrganizationModel caller, long organizationId, List<long> userIds, bool includedDeleted = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
					var q = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == organizationId);
					if (!includedDeleted) {
						q = q.Where(x => x.DeleteTime == null);
					}

					var foundIds = q.WhereRestrictionOn(x => x.Id).IsIn(userIds).Select(x => x.Id).List<long>().ToList();

					foreach (var id in userIds) {
						if (!foundIds.Any(x => x == id)) {
							throw new PermissionsException("User not part of organization");
						}
					}

				}
			}
		}

		public static async Task SetFlag(UserOrganizationModel caller, long orgId, OrganizationFlagType type, bool enabled) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					if (enabled) {
						await AddFlag(s, perms, orgId, type);
					} else {
						await RemoveFlag(s, perms, orgId, type);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task AddFlag(ISession s, PermissionsUtility perms, long orgId, OrganizationFlagType type) {
			perms.Or(x => x.ViewOrganization(orgId), x => x.RadialAdmin(true));
			var any = s.QueryOver<OrganizationFlag>().Where(x => x.OrganizationId == orgId && type == x.FlagType && x.DeleteTime == null).RowCount();
			if (any == 0) {
				s.Save(new OrganizationFlag() {
					OrganizationId = orgId,
					FlagType = type,

				});
				await HooksRegistry.Each<IOrganizationFlagHook>((ses, x) => x.AddFlag(ses, orgId, type));
			}
		}

		public static async Task RemoveFlag(ISession s, PermissionsUtility perms, long orgId, OrganizationFlagType type) {
			perms.Or(x => x.ViewOrganization(orgId), x => x.RadialAdmin(true));
			var any = s.QueryOver<OrganizationFlag>().Where(x => x.OrganizationId == orgId && type == x.FlagType && x.DeleteTime == null).List().ToList();
			if (any.Count > 0) {
				foreach (var a in any) {
					a.DeleteTime = DateTime.UtcNow;
					s.Update(a);
				}
				await HooksRegistry.Each<IOrganizationFlagHook>((ses, x) => x.RemoveFlag(ses, orgId, type));
			}
		}

		public static async Task<List<OrganizationFlagType>> GetFlags(UserOrganizationModel caller, long orgId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return await GetFlags(s, perms, orgId);
				}
			}
		}

		public static async Task<List<OrganizationFlagType>> GetFlags(ISession s, PermissionsUtility perms, long orgId) {
			perms.Or(x => x.ViewOrganization(orgId), x => x.RadialAdmin(true));
			return s.QueryOver<OrganizationFlag>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Select(x => x.FlagType).List<OrganizationFlagType>().ToList();
		}

	}
}

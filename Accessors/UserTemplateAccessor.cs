using System;
using NHibernate;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserTemplate;
using RadialReview.Utilities;
using System.Threading.Tasks;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Angular.Roles;

namespace RadialReview.Accessors {
	public class UserTemplateAccessor {
		public class UTADeprecated {
			[Obsolete("broken", true)]
			public static async Task CreateTemplate(UserOrganizationModel caller, UserTemplate_Deprecated template) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var perms = PermissionsUtility.Create(s, caller);
						await CreateTemplate(s, perms, caller.Organization, template);
						tx.Commit();
						s.Flush();
					}
				}
			}
			[Obsolete("broken", true)]
			public static async Task CreateTemplate(ISession s, PermissionsUtility perms, OrganizationModel org, UserTemplate_Deprecated template) {
				if (template.Id != 0)
					throw new PermissionsException("Id was not zero");
				if (template.AttachId == 0)
					throw new PermissionsException("AttachId was zero");
				if (template.AttachType == AttachType.Invalid)
					throw new PermissionsException("AttachType was invalid");

				var found = s.QueryOver<UserTemplate_Deprecated>().Where(x => x.DeleteTime == null && x.AttachId == template.AttachId && x.AttachType == template.AttachType).SingleOrDefault();

				if (found != null)
					throw new PermissionsException("Template already exists.");

				perms.ConfirmAndFix(template,
					x => x.OrganizationId,
					x => x.Organization,
					x => x.CreateTemplates);

				s.Save(template);
				var a = new Attach(template.AttachType, template.AttachId);

				AttachAccessor.SetTemplateUnsafe(s, a, template.Id);
				var members = AttachAccessor.GetMemberIdsUnsafe(s, a);
				foreach (var member in members) {
					await _AddUserToTemplateUnsafe(s, perms, org, template.Id, member, false);
				}
			}

			[Obsolete("broken", true)]
			public static UserTemplate_Deprecated GetUserTemplate(UserOrganizationModel caller, long utId, bool loadRocks = false, bool loadRoles = false, bool loadMeasurables = false) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var p = PermissionsUtility.Create(s, caller);
						var found = s.Get<UserTemplate_Deprecated>(utId);
						if (found == null)
							throw new PermissionsException("Template does not exist");
						p.ViewTemplate(found.Id);

						found._Attach = AttachAccessor.PopulateAttachUnsafe(s, found.AttachId, found.AttachType);

						if (loadRocks) {
							found._Rocks = s.QueryOver<UserTemplate_Deprecated.UT_Rock_Deprecated>()
								.Where(x => x.DeleteTime == null && x.TemplateId == utId)
								.Fetch(x => x.Period).Eager
								.List().ToList();
						}
						if (loadRoles) {
							throw new NotImplementedException();
						}
						if (loadMeasurables) {
							found._Measurables = s.QueryOver<UserTemplate_Deprecated.UT_Measurable_Deprecated>()
								.Where(x => x.DeleteTime == null && x.TemplateId == utId)
								.List().ToList();
						}

						return found;
					}
				}
			}

			public static UserTemplate_Deprecated _GetAttachedUserTemplateUnsafe(ISession s, long attachId, AttachType attachType) {
				var found = s.QueryOver<UserTemplate_Deprecated>()
					.Where(x => x.DeleteTime == null && x.AttachId == attachId && x.AttachType == attachType)
					.SingleOrDefault();
				return found;
			}

			public static void UpdateMeasurableTemplate(UserOrganizationModel caller, long utMeasurableId, String measurable, LessGreater goalDirection, decimal goal, DateTime? deleteTime) {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var p = PermissionsUtility.Create(s, caller);


						var utMeasurable = s.Get<UserTemplate_Deprecated.UT_Measurable_Deprecated>(utMeasurableId);
						p.EditTemplate(utMeasurable.TemplateId);


						utMeasurable.Measurable = measurable;
						utMeasurable.Goal = goal;
						utMeasurable.GoalDirection = goalDirection;
						utMeasurable.DeleteTime = deleteTime;
						s.Update(utMeasurable);

						var measurables = s.QueryOver<MeasurableModel>()
							.Where(x => x.DeleteTime == null && x.FromTemplateItemId == utMeasurable.Id)
							.List().ToList();

						foreach (var m in measurables) {
							m.Title = measurable;
							m.Goal = goal;
							m.GoalDirection = goalDirection;
							m.DeleteTime = deleteTime;
							s.Update(m);
							if (deleteTime.HasValue) {
								var u = s.Get<UserOrganizationModel>(m.AccountableUserId);

								if (u != null) {
									s.Update(u);
									s.Flush();
									u.UpdateCache(s);
								}
							}
						}
						tx.Commit();
						s.Flush();
					}
				}
			}

			[Untested("RockAccessor.CreateRock")]
			public static async Task _AddUserToTemplateUnsafe(ISession s, PermissionsUtility perms, OrganizationModel organization, long templateId, long userId, bool forceJobDescription) {

				var user = s.Get<UserOrganizationModel>(userId);
				var template = s.Get<UserTemplate_Deprecated>(templateId);



				var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
				#region Measurables
				var newMeasurables = s.QueryOver<UserTemplate_Deprecated.UT_Measurable_Deprecated>()
					.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
					.List().ToList();
				var existingMeasurables = s.QueryOver<MeasurableModel>()
					.Where(x => x.DeleteTime == null && x.AccountableUser.Id == userId)
					.List().ToList();

				var toAddMeasurables = newMeasurables.Where(x => existingMeasurables.All(y => y.FromTemplateItemId != x.Id));
				foreach (var a in toAddMeasurables) {
					s.Save(new MeasurableModel(organization) {
						AccountableUser = user,
						AccountableUserId = user.Id,
						AdminUser = user,
						AdminUserId = user.Id,
						GoalDirection = a.GoalDirection,
						Goal = a.Goal,
						Title = a.Measurable,
						FromTemplateItemId = a.Id,
					});
				}
				#endregion
				#region Rocks
				var newRocks = s.QueryOver<UserTemplate_Deprecated.UT_Rock_Deprecated>()
					.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
					.List().ToList();
				var existingRocks = s.QueryOver<RockModel>()
					.Where(x => x.DeleteTime == null && x.AccountableUser.Id == userId)
					.List().ToList();

				var toAddRocks = newRocks.Where(x => existingRocks.All(y => y.FromTemplateItemId != x.Id));
				foreach (var a in toAddRocks) {
					await RockAccessor.CreateRock(s, perms, user.Id, a.Rock, a.Id);

				}
				#endregion
				#region Job Description
				if (String.IsNullOrWhiteSpace(user.JobDescription) || forceJobDescription) {
					user.JobDescription = template.JobDescription;
					user.JobDescriptionFromTemplateId = templateId;
				}
				#endregion

				var utUser = new UserTemplate_Deprecated.UT_User_Deprecated {
					Template = template,
					TemplateId = templateId,
					User = user,

				};
				s.Save(utUser);
				s.Update(user);
				s.Flush();
				user.UpdateCache(s);
			}

			[Untested("IRoleHook correct?")]
			public static async Task AddRoleToTemplate_Deprecated(ISession s, PermissionsUtility p, long templateId, long orgId, String role) {
				var utm = new UserTemplate_Deprecated.UT_Role_Deprecated() {
					TemplateId = templateId,
				};
				p.ConfirmAndFix(utm, x => x.TemplateId, x => x.Template, x => x.EditTemplate);
				s.Save(utm);

				var category = ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
				var template = s.Get<UserTemplate_Deprecated>(templateId);

				var rm = new RoleModel_Deprecated() {
					OrganizationId = orgId,
					Role = role,
					Category = category,
				};
				s.Save(rm);


				s.Save(new RoleLink_Deprecated() {
					AttachId = template.AttachId,
					AttachType = template.AttachType,
					OrganizationId = orgId,
					RoleId = rm.Id
				});

				utm.RoleId = rm.Id;
				s.Flush();

				var users = s.QueryOver<UserTemplate_Deprecated.UT_User_Deprecated>()
						.Where(x => x.DeleteTime == null && x.TemplateId == templateId)
						.Fetch(x => x.User).Eager
						.List().ToList();
				foreach (var utu in users) {
					var user = utu.User;
					s.Update(user);
					user.UpdateCache(s);
				}
			}
		}
	}
}

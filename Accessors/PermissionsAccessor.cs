using NHibernate;
using RadialReview.Core.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Utilities;
using RadialReview.Utilities.Permissions.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Accessors {
	public class PermissionsAccessor {

		public static void EnsurePermitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted) {
			PermissionsAccessor.Permitted(caller, ensurePermitted);
		}

		public static void Permitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					ensurePermitted(PermissionsUtility.Create(s, caller));
				}
			}
		}
		public static bool IsPermitted(UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted, bool forwardExceptions = false) {
			try {
				Permitted(caller, ensurePermitted);
				return true;
			} catch (Exception) {
				if (forwardExceptions)
					throw;
				return false;
			}
		}

		public static void Permitted(ISession s, UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted) {
			ensurePermitted(PermissionsUtility.Create(s, caller));
		}

		public static bool IsPermitted(ISession s, UserOrganizationModel caller, Action<PermissionsUtility> ensurePermitted) {
			try {
				Permitted(s, caller, ensurePermitted);
				return true;
			} catch (ArgumentOutOfRangeException) {
				throw;
			} catch (Exception) {
				return false;
			}
		}
		public static bool AnyTrue(ISession s, UserOrganizationModel caller, Predicate<UserOrganizationModel> predicate) {
			if (predicate(caller))
				return true;
			return false;
		}
		
		public static bool AnyTrue(UserOrganizationModel caller, Predicate<UserOrganizationModel> predicate) {
			if (predicate(caller))
				return true;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return AnyTrue(s, caller, predicate);
				}
			}
		}

		public static IEnumerable<PermRowVM> LoadPermItem(ISession s, PermissionsUtility perms, IEnumerable<PermItem> items, PermRowSettings settings) {
			var permItems = items as IList<PermItem> ?? items.ToList();

			var groups = permItems.GroupBy(x => x.AccessorType);

			var terms = TermsAccessor.GetTermsCollection(s, perms, perms.GetCaller().Organization.Id);

			return groups.SelectMany(x => {
				var accessorPermissions = PermissionsUtility.GetAccessorPermissionsForType(x.Key);
				return accessorPermissions.ConstructRowViewModel(s, perms, terms, x.ToList(), settings);
			});



		}

		/// <summary>
		/// Only grabs perm items for explicitly specified permissions (No Creator, Admin, Members)
		/// </summary>
		/// <param name="s"></param>
		/// <param name="callerPerms"></param>
		/// <param name="forUserId"></param>
		/// <param name="resourceType"></param>
		/// <returns></returns>
		public static List<PermItem> GetExplicitPermItemsForUser(ISession s, PermissionsUtility callerPerms, long forUserId, PermItem.ResourceType resourceType) {
			var groups = ResponsibilitiesAccessor
							.GetResponsibilityGroupIdsForUser(s, callerPerms, forUserId)
							.ToList();

			List<PermItem> permList = new List<PermItem>();


			var RgmPermList = s.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResType == resourceType && x.AccessorType == PermItem.AccessType.RGM)
				.WhereRestrictionOn(x => x.AccessorId).IsIn(groups)
				.Future();

			var forUserPerms = PermissionsUtility.Create(s, s.Get<UserOrganizationModel>(forUserId));

			permList.AddRange(RgmPermList.ToList());

			var user = s.Get<UserOrganizationModel>(forUserId);
			var emailAccessorIds = new List<long>();
			if (user != null && user.User != null && !string.IsNullOrWhiteSpace(user.User.UserName)) {
				var emailPermsAccessorIds = s.QueryOver<EmailPermItem>().Where(x => x.DeleteTime == null && x.Email == user.User.UserName).Select(x => x.Id).List<long>().ToList();
				if (emailPermsAccessorIds.Any()) {
					var emailPermList = s.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResType == resourceType && x.AccessorType == PermItem.AccessType.Email)
						.WhereRestrictionOn(x => x.AccessorId).IsIn(emailPermsAccessorIds)
						.List().ToList();
					permList.AddRange(emailPermList);
				}
			}

			return permList;
		}


		public static PermissionDropdownVM GetPermItems(UserOrganizationModel caller, long resourceId, PermItem.ResourceType resourceType, string displayText = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var admin = perms.IsPermitted(x => x.CanAdmin(resourceType, resourceId));

					var resourcePerms = PermissionsUtility.GetResourcePermissionsForType(resourceType);

					var name = resourcePerms.GetName(s, resourceId);

					var rowSettings = new PermRowSettings() {
						DisableView = !admin,
						DisableEdit = !admin,
						DisableAdmin = !admin,
					};

					var itemsQ = GetPermRows(s, perms, resourceType, resourceId, rowSettings);
					//Massive evalution;
					var items = itemsQ.GetResolvedRows();
					try {
						var heading = PermissionsHeading.GetHeading(resourceType);

						AdjustRowSettings(items, heading);
					} catch (Exception) {
						int a = 0;
					}


					return new PermissionDropdownVM() {
						CanAdmin = admin,
						DisplayText = name,
						ResId = resourceId,
						ResType = resourceType,
						Items = items,
					};
				}
			}
		}

		private static void AdjustRowSettings(List<PermRowVM> items, PermissionsHeading heading) {
			foreach (var item in items) {
				item.RowSettings.ShowView = heading.ShowView;
				item.RowSettings.ShowEdit = heading.ShowEdit;
				item.RowSettings.ShowAdmin = heading.ShowAdmin;					
				AdjustRowSettings(item.ResolveChildren().ToList(), heading);
			}
		}

		public class UnresolvedRows {
			private IEnumerable<PermRowVM> Rows { get; set; }
			public UnresolvedRows(IEnumerable<PermRowVM> rows) {
				Rows = rows;
			}

			public IEnumerable<PermRowVM> GetUnresolvedRows() {
				return Rows;
			}
			public List<PermRowVM> GetResolvedRows() {
				var rows = Rows.ToList();
				foreach (var row in rows) {
					row.ResolveChildren();
				}
				return rows;
			}
		}

		public static UnresolvedRows GetPermRows(ISession s, PermissionsUtility perms, PermItem.ResourceType resourceType, long resourceId, PermRowSettings settings) {
			perms.CanViewPermissions(resourceType, resourceId);
			var items = s.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResId == resourceId && x.ResType == resourceType).List().ToList();
			return new UnresolvedRows(LoadPermItem(s, perms, items, settings));
		}





		public static PermRowVM EditPermItem(UserOrganizationModel caller, long id, bool? view, bool? edit, bool? admin) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var model = s.Get<PermItem>(id);
					if (model == null)
						throw new PermissionsException("Permission setting does not exist.");
					perms.CanAdmin(model.ResType, model.ResId);
					model.CanAdmin = admin ?? model.CanAdmin;
					model.CanEdit = edit ?? model.CanEdit;
					model.CanView = view ?? model.CanView;

					if (!model.CanView && !model.CanEdit && !model.CanAdmin) {
						if (model.AccessorType != PermItem.AccessType.Inherited) {
							model.DeleteTime = DateTime.UtcNow;
						}
					} else if (model.DeleteTime != null) {
						model.DeleteTime = null;
					}

					s.Update(model);

					perms.EnsureAdminExists(model.ResType, model.ResId);

					tx.Commit();
					s.Flush();

					return new PermRowVM(model, PermRowSettings.ALL_ALLOWED());
				}
			}
		}

    public static PermRowVM EditL10RecurrencePermItemByUserId(UserOrganizationModel caller, long recurrenceId, long userId, bool? view, bool? edit, bool? admin)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        using (var transaction = session.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(session, caller);
          var model = session.QueryOver<PermItem>()
            .Where(x => x.AccessorId == userId && x.ResId == recurrenceId && x.ResType == PermItem.ResourceType.L10Recurrence && x.DeleteTime == null)
            .OrderBy(x => x.CreateTime).Desc
            .Take(1)
            .SingleOrDefault();

          if (model == null)
            throw new PermissionsException("Permission setting does not exist.");

          perms.CanAdmin(model.ResType, model.ResId);
          model.CanAdmin = admin ?? model.CanAdmin;
          model.CanEdit = edit ?? model.CanEdit;
          model.CanView = view ?? model.CanView;

          if (!model.CanView && !model.CanEdit && !model.CanAdmin)
          {
            if (model.AccessorType != PermItem.AccessType.Inherited)
            {
              model.DeleteTime = DateTime.UtcNow;
            }
          }

          session.Update(model);

          perms.EnsureAdminExists(model.ResType, model.ResId);

          transaction.Commit();
          session.Flush();

          return new PermRowVM(model, PermRowSettings.ALL_ALLOWED());
        }
      }
    }
    public static void DeletePermItem(UserOrganizationModel caller, long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					DeletePermItem(s, perms, id);
					tx.Commit();
					s.Flush();
				}
			}
		}
		public static void DeletePermItem(ISession s, PermissionsUtility perms, long permItemId) {
			var model = s.Get<PermItem>(permItemId);
			if (model == null || model.DeleteTime != null)
				throw new PermissionsException("Permission setting does not exist.");
			perms.CanAdmin(model.ResType, model.ResId);
			model.DeleteTime = DateTime.UtcNow;
			s.Update(model);
			perms.EnsureAdminExists(model.ResType, model.ResId);
		}

		public static List<PermRowVM> AddPermItems(UserOrganizationModel caller, PermItem.ResourceType resourceType, long resourceId, params PermTiny[] items) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var res = AddPermItems(s, perms, caller, resourceType, resourceId, items);
					tx.Commit();
					s.Flush();


					return LoadPermItem(s, perms, res, PermRowSettings.ALL_ALLOWED()).ToList();
				}
			}
		}

		public static List<PermItem> AddPermItems(ISession s, PermissionsUtility perms, UserOrganizationModel creator, PermItem.ResourceType resourceType, long resourceId, params PermTiny[] items) {
			perms.CanAdmin(resourceType, resourceId);
			return InitializePermItems_Unsafe(s, creator, resourceType, resourceId, items);
		}
		public static List<PermItem> InitializePermItems_Unsafe(ISession s, UserOrganizationModel creator, PermItem.ResourceType resourceType, long resourceId, params PermTiny[] items) {
			var creatorOrgId = creator.Organization.Id;
			var creatorId = creator.Id;
			return InitializePermItems_Unsafe(s, creatorId, creatorOrgId, resourceType, resourceId, items);
		}
		public static List<PermItem> InitializePermItemsAutoGenerated_Unsafe(ISession s, long orgId, PermItem.ResourceType resourceType, long resourceId, params PermTiny[] items) {
			return InitializePermItems_Unsafe(s, -1, orgId, resourceType, resourceId, items);
		}
		private static List<PermItem> InitializePermItems_Unsafe(ISession s, long creatorId, long creatorOrgId, PermItem.ResourceType resourceType, long resourceId, params PermTiny[] items) {
			var oneAdmin = false;

			var anyAdmins = s.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.ResId == resourceId && x.ResType == resourceType && x.CanAdmin == true).RowCount();
			if (anyAdmins > 0)
				oneAdmin = true;

			var res = new List<PermItem>();
			foreach (var i in items) {
				if (i.AccessorType == PermItem.AccessType.Creator) {
					i.AccessorId = creatorId;
				}
				if (i.AccessorType == PermItem.AccessType.Email) {
					var epi = new EmailPermItem() {
						CreatorId = creatorId,
						Email = i.EmailAddress,
					};
					s.Save(epi);
					i.AccessorId = epi.Id;
				}
				if (i.AccessorType == PermItem.AccessType.UserModelAtOrganization) {
					var umpi = new UserModelAtOrganizationPermItem() {
						CreatorId = creatorId,
						UserModelId = i.UserModelId,
						OrganizationId = i.AtOrganizationId,
					};
					s.Save(umpi);
					i.AccessorId = umpi.Id;
				}

				oneAdmin = oneAdmin || i.CanAdmin;
				var pi = new PermItem() {
					CanAdmin = i.CanAdmin,
					CanEdit = i.CanEdit,
					CanView = i.CanView,
					AccessorType = i.AccessorType,
					AccessorId = i.AccessorId,
					InheritType = i.InheritType,
					ResType = resourceType,
					ResId = resourceId,
					CreatorId = creatorId,
					OrganizationId = creatorOrgId,
					IsArchtype = false,
				};
				s.Save(pi);
				i.PermItem = pi;


				res.Add(pi);
			}

			if (!oneAdmin)
				throw new PermissionsException("Requires at least one admin");
			return res;
		}

		public static bool CanView(UserOrganizationModel caller, PermItem.ResourceType resourceType, long resourceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return IsPermitted(s, caller, x => x.CanView(resourceType, resourceId));
				}
			}
		}
		public static bool CanEdit(UserOrganizationModel caller, PermItem.ResourceType resourceType, long resourceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return IsPermitted(s, caller, x => x.CanEdit(resourceType, resourceId));
				}
			}
		}
		public static bool CanAdmin(UserOrganizationModel caller, PermItem.ResourceType resourceType, long resourceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return IsPermitted(s, caller, x => x.CanAdmin(resourceType, resourceId));
				}
			}
		}

	}

	public class PermTiny {
		public PermTiny() {
			CanAdmin = true;
			CanEdit = true;
			CanView = true;
		}

		public static PermTiny Creator(bool view = true, bool edit = true, bool admin = true) {
			return new PermTiny() {
				AccessorType = PermItem.AccessType.Creator,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
			};
		}
		public static PermTiny Members(bool view = true, bool edit = true, bool admin = true) {
			return new PermTiny() {
				AccessorType = PermItem.AccessType.Members,
				AccessorId = -1,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
			};
		}
		public static PermTiny Admins(bool view = true, bool edit = true, bool admin = true) {
			return new PermTiny() {
				AccessorType = PermItem.AccessType.Admins,
				AccessorId = -1,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
			};
		}
		public static PermTiny RGM(long id, bool view = true, bool edit = true, bool admin = true) {
			return new PermTiny() {
				AccessorType = PermItem.AccessType.RGM,
				AccessorId = id,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
			};
		}
		public static PermTiny UserModelAtOrganization(string userModelId, long orgId, bool view = true, bool edit = true, bool admin = true) {
			return new PermTiny() {
				AccessorType = PermItem.AccessType.UserModelAtOrganization,
				AtOrganizationId = orgId,
				UserModelId = userModelId,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
			};
		}
		public static PermTiny Email(string email, bool view = true, bool edit = true, bool admin = false) {
			return new PermTiny() {
				AccessorType = PermItem.AccessType.Email,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
				EmailAddress = email.ToLower()
			};
		}

		#region Inherited Permissions

		public static PermTiny InheritedFrom(PermItem.ResourceType resourceType, long resourceId, bool view = true, bool edit = true, bool admin = true) {
			return _InheritedFrom(resourceType, resourceId, view, edit, admin);
		}

		public static PermTiny InheritedFromL10Recurrence(long recurrenceId, bool view = true, bool edit = true, bool admin = true) {
			return _InheritedFrom(PermItem.ResourceType.L10Recurrence, recurrenceId, view, edit, admin);
		}
		public static PermTiny InheritedFromDocumentFolder(long folderId, bool view = true, bool edit = true, bool admin = true) {
			return _InheritedFrom(PermItem.ResourceType.DocumentsFolder, folderId, view, edit, admin);
		}

		//Extracted to prevent missing arguments with helper constructors
		private static PermTiny _InheritedFrom(PermItem.ResourceType resourceType, long resourceId, bool view, bool edit, bool admin) {
			if (resourceType == PermItem.ResourceType.Invalid)
				throw new Exception("Fatal. Requested inherited permissions on Invalid");
			return new PermTiny() {
				AccessorType = PermItem.AccessType.Inherited,
				InheritType = resourceType,
				AccessorId = resourceId,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
			};
		}

		#endregion

		public static PermTiny System(bool view = true, bool edit = true, bool admin = true) {
			return new PermTiny() {
				AccessorType = PermItem.AccessType.System,
				CanView = view,
				CanEdit = edit,
				CanAdmin = admin,
			};
		}


		public PermItem.AccessType AccessorType { get; set; }
		public long AccessorId { get; set; }
		public bool CanAdmin { get; set; }
		public bool CanEdit { get; set; }
		public bool CanView { get; set; }
		public PermItem PermItem { get; set; }


		#region Special purpose variables

		/*Inherited*/
		public PermItem.ResourceType? InheritType { get; set; }

		/*Email*/
		public string EmailAddress { get; set; }

		/*UserModelAtOrganization*/
		public string UserModelId { get; set; }
		public long AtOrganizationId { get; set; }

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Utilities.Permissions.Resources;
using NHibernate;
using RadialReview.Utilities.Permissions.Accessors;

namespace RadialReview.Utilities {
	public partial class PermissionsUtility {

		private static List<IResourcePermissions> ResourcePermissions = ReflectionUtility.GetAllImplementationsOfInterface<IResourcePermissions>().ToList();
		private static List<IAccessorPermissions> AccessorPermissions = ReflectionUtility.GetAllImplementationsOfInterface<IAccessorPermissions>().ToList();

        public static IResourcePermissions GetResourcePermissionsForType(PermItem.ResourceType resourceType) {
			return ResourcePermissions.Single(x => x.ForResourceType() == resourceType);
		}
		public static IAccessorPermissions GetAccessorPermissionsForType(PermItem.AccessType accessType) {
			return AccessorPermissions.Single(x => x.ForAccessorType() == accessType);
		}

		public void UnsafeAllow(PermItem.AccessLevel level, PermItem.ResourceType resourceType, long id) {
			string key;
			switch (level) {
				case PermItem.AccessLevel.View:
					key = "CanView_" + resourceType + "~" + id;
					break;
				case PermItem.AccessLevel.Edit:
					key = "CanEdit_" + resourceType + "~" + id;
					break;
				case PermItem.AccessLevel.Admin:
					key = "CanAdmin_" + resourceType + "~" + id;
					break;
				default:
					throw new ArgumentOutOfRangeException("" + level.ToString());
			}
			new CacheChecker(key, this).Execute(() => this);
		}

        public void EnsureAdminExists(PermItem.ResourceType resourceType, long resourceId, HashSet<Tuple<PermItem.ResourceType, long>> alreadyVisited = null) {

			alreadyVisited = alreadyVisited ?? new HashSet<Tuple<PermItem.ResourceType, long>>();
			var key = Tuple.Create(resourceType, resourceId);
			alreadyVisited.Add(key);

			var items = session.QueryOver<PermItem>().Where(x => x.DeleteTime == null && x.CanAdmin && x.ResId == resourceId && x.ResType == resourceType).List();
			if (!items.Any()) {
				throw new PermissionsException("You must have an admin. Reverting setting change.") {
					NoErrorReport = true
				};
			}

			var resourcePermissions = GetResourcePermissionsForType(resourceType);
			//Cheapest first..
			foreach (var i in items.OrderBy(x => (int)x.AccessorType)) {
				switch (i.AccessorType) {
					case PermItem.AccessType.RGM:
						var users = ResponsibilitiesAccessor.GetResponsibilityGroupMembers(session, this, i.AccessorId);
						if (users.Any())
							return;
						break;
					case PermItem.AccessType.Members:
						var ids = GetMyMemeberUserIds(resourcePermissions, resourceId);
						var idsAlive = session.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(ids).Select(x => x.Id).List<long>();
						if (idsAlive.Any())
							return;
						break;
					case PermItem.AccessType.Creator:
						var creator = session.Get<UserOrganizationModel>(i.AccessorId);
						if (creator.DeleteTime == null)
							return;
						break;
					case PermItem.AccessType.Admins:
						var orgId = GetOrganizationId(resourcePermissions, resourceId);
						var org = session.Get<OrganizationModel>(orgId);
						var canEdit = org.ManagersCanEdit;
						var orgAdminsQ = session.QueryOver<UserOrganizationModel>().Where(x => x.DeleteTime == null && x.Organization.Id == orgId);
						if (canEdit) {
							orgAdminsQ = orgAdminsQ.Where(x => x.ManagingOrganization || x.ManagerAtOrganization);
						} else {
							orgAdminsQ = orgAdminsQ.Where(x => x.ManagingOrganization);
						}
						var orgAdmins = orgAdminsQ.Select(x => x.Id).List<long>();
						if (orgAdmins.Any())
							return;
						break;
					case PermItem.AccessType.Inherited:
						try {
							if (alreadyVisited.Contains(Tuple.Create(i.InheritType.Value, i.AccessorId))) {
								break;
							}

							EnsureAdminExists(i.InheritType.Value, i.AccessorId, alreadyVisited);
							return;
						} catch (PermissionsException) {
							/*eat it.*/
						}
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			throw new PermissionsException("You must have an admin. Reverting setting change.") {
				NoErrorReport = true
			};
		}

		public Dictionary<long, bool> BulkCanView(PermItem.ResourceType resourceType, long[] resourceIds, bool includeAlternateUsers) {
			PermissionsUtility _ = null;
			return CanAccessItems(includeAlternateUsers, PermItem.AccessLevel.View, resourceType, resourceIds, null, ref _);
		}
		public Dictionary<long, bool> BulkCanEdit(PermItem.ResourceType resourceType, long[] resourceIds, bool includeAlternateUsers) {
			PermissionsUtility _ = null;
			return CanAccessItems(includeAlternateUsers, PermItem.AccessLevel.Edit, resourceType, resourceIds, null, ref _);
		}
		public Dictionary<long, bool> BulkCanAdmin(PermItem.ResourceType resourceType, long[] resourceIds, bool includeAlternateUsers) {
			PermissionsUtility _ = null;
			return CanAccessItems(includeAlternateUsers, PermItem.AccessLevel.Admin, resourceType, resourceIds, null, ref _);
		}

		public PermissionsUtility CanViewPermissions(PermItem.ResourceType resourceType, long resourceId) {
			return CheckCacheFirst("CanViewPermissions_" + resourceType, resourceId).Execute(() => {
				var result = this;
				if (CanAccessItem(PermItem.AccessLevel.View, resourceType, resourceId, null, ref result))
					return result;
				throw new PermissionsException("Can not view this item.") {
					NoErrorReport = true
				};
			});
		}

		public bool CanViewPermitted(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null) {
			try {
				CanView(resourceType, resourceId, defaultAction, exceptionMessage);
				return true;
			} catch (PermissionsException) {
				return false;
			}
		}
		public bool CanEditPermitted(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null) {
			try {
				CanEdit(resourceType, resourceId, defaultAction, exceptionMessage);
				return true;
			} catch (PermissionsException) {
				return false;
			}
		}

		public bool CanAdminPermitted(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null) {
			try {
				CanAdmin(resourceType, resourceId, defaultAction, exceptionMessage);
				return true;
			} catch (PermissionsException) {
				return false;
			}
		}

		public PermissionsUtility CanView(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null, bool includeAlternateUsers = false) {
			try {
				return CheckCacheFirst("CanView_" + resourceType, resourceId, includeAlternateUsers.ToLong()).Execute(() => {
					var result = this;
					if (CanAccessItem(PermItem.AccessLevel.View, resourceType, resourceId, defaultAction, ref result, includeAlternateUsers: includeAlternateUsers))
						return result;
					throw new PermissionsException(exceptionMessage ?? "Can not view this item.") {
						NoErrorReport = true
					};
				});
			} catch (Exception e) {
				if (exceptionMessage == null) {
					throw;
				} else {
					throw new PermissionsException(exceptionMessage) {
						NoErrorReport = true
					};
				}
			}
		}

		public PermissionsUtility CanEdit(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null, bool includeAlternateUsers = false) {
			try {
				return CheckCacheFirst("CanEdit_" + resourceType, resourceId, includeAlternateUsers.ToLong()).Execute(() => {
					var result = this;
					if (CanAccessItem(PermItem.AccessLevel.Edit, resourceType, resourceId, defaultAction, ref result, includeAlternateUsers: includeAlternateUsers))
						return result;

					throw new PermissionsException(exceptionMessage ?? "Can not edit this item.") {
						NoErrorReport = true
					};
				});
			} catch (Exception e) {
				if (exceptionMessage == null) {
					throw ;
				} else {
					throw new PermissionsException(exceptionMessage) {
						NoErrorReport = true
					};
				}
			}
		}
		public PermissionsUtility CanAdmin(PermItem.ResourceType resourceType, long resourceId, Func<PermissionsUtility, PermissionsUtility> defaultAction = null, string exceptionMessage = null, bool includeAlternateUsers = false) {
			try {
				return CheckCacheFirst("CanAdmin_" + resourceType, resourceId, includeAlternateUsers.ToLong()).Execute(() => {
					var result = this;
					if (CanAccessItem(PermItem.AccessLevel.Admin, resourceType, resourceId, defaultAction, ref result, includeAlternateUsers: includeAlternateUsers))
						return result;

					throw new PermissionsException(exceptionMessage ?? "Can not administrate this item.") {
						NoErrorReport = true
					};
				});
			} catch (Exception e) {
				if (exceptionMessage == null) {
					throw ;
				} else {
					throw new PermissionsException(exceptionMessage) {
						NoErrorReport = true
					};
				}
			}
		}



		public class PermissionDataCache {
			public Dictionary<CircularReferenceInheritedPermissions, bool> ReferencesAlreadyChecked { get; set; }

			public Dictionary<long, UserOrganizationModel> UserLookup { get; set; }
			public Dictionary<Tuple<long, bool>, List<TinyRGM>> GroupLookup { get; set; }

			public PermissionDataCache() {
				ReferencesAlreadyChecked = new Dictionary<CircularReferenceInheritedPermissions, bool>();
				UserLookup = new Dictionary<long, UserOrganizationModel>();
				GroupLookup = new Dictionary<Tuple<long, bool>, List<TinyRGM>>();
			}

			public bool UserIsValid(ISession s, long id) {
				return UserIsValid(GetUser(s, id));
			}

			public bool UserIsValid(UserOrganizationModel u) {
				return u != null && u.DeleteTime == null && u.Organization.DeleteTime == null;
			}


			public UserOrganizationModel GetUser(ISession s, long id) {
				if (!UserLookup.ContainsKey(id)) {
					UserLookup[id] = s.Get<UserOrganizationModel>(id);
				}
				return UserLookup[id];
			}

			public void LoadUsers(ISession s, long[] userIds) {
				if (userIds == null) {
					return;
				}

				var fetchIds = userIds.Where(x => !UserLookup.ContainsKey(x)).ToArray();
				if (fetchIds.Any()) {
					var users = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(fetchIds).List().ToList();
					foreach (var u in users) {
						UserLookup[u.Id] = u;
					}
				}
			}

			public List<TinyRGM> GetResponsibilityGroupsForCaller(ISession session, PermissionsUtility perms, bool includeAlternateUsers) {
				var caller = perms.GetCaller();
				var key = Tuple.Create(caller.Id, includeAlternateUsers);
				if (!GroupLookup.ContainsKey(key)) {
					GroupLookup[key] = ResponsibilitiesAccessor.GetTinyResponsibilityGroupsForUser(session, perms, caller.Id, includeAlternateUsers).ToList();
					var nonAltUserKey = Tuple.Create(caller.Id, false);
					if (includeAlternateUsers && !GroupLookup.ContainsKey(nonAltUserKey)) {
						//cache the non-include alternate users as well.
						GroupLookup[nonAltUserKey] = GroupLookup[key].Where(x => x.ForUserId == caller.Id).ToList();
					}
				}
				return GroupLookup[key];
			}
		}



		public bool CanAccessItem(PermItem.AccessLevel requestedLevels, PermItem.ResourceType resourceType, long resourceId,
			Func<PermissionsUtility, PermissionsUtility> deprecatedDefaultAction, ref PermissionsUtility result, bool and = true,
			PermissionDataCache permissionDataCache = null, bool includeAlternateUsers = false) {

			return CanAccessItems(includeAlternateUsers, requestedLevels, resourceType, new long[] { resourceId }, deprecatedDefaultAction, ref result, and, permissionDataCache)[resourceId];

		}

		protected Dictionary<long, bool> CanAccessItems(bool includeAlternateUsers, PermItem.AccessLevel requestedLevels, PermItem.ResourceType resourceType, long[] resourceIds, Func<PermissionsUtility, PermissionsUtility> deprecatedDefaultAction, ref PermissionsUtility result, bool and = true, PermissionDataCache permissionDataCache = null) {
			if (resourceIds == null || !resourceIds.Any())
				throw new Exception("Expecting at least one resourceId");

			var distinctResourceIds = resourceIds.Distinct().ToArray();

			if (IsRadialAdmin(caller)) {
				return distinctResourceIds.ToDictionary(id => id, id => true);
			}

			var permItems = session.QueryOver<PermItem>()
				.Where(x => x.ResType == resourceType && x.DeleteTime == null)
				.WhereRestrictionOn(x => x.ResId).IsIn(distinctResourceIds)
				.List().ToList();

			//Only want true if we actually have permissions...
			if (!permItems.Any()) {
				//This might be redundant with the !anyFlags check.
				if (deprecatedDefaultAction != null && distinctResourceIds.Count() == 1) {
					/*For deprecated permission checks only, not bulk checks*/
					result = deprecatedDefaultAction(this);
					return distinctResourceIds.ToDictionary(id => id, id => true);
				}
				return distinctResourceIds.ToDictionary(id => id, id => false);
			}

			var initializeInProgress = false;
			if (permissionDataCache == null) {
				//called without circular reference detection.
				permissionDataCache = new PermissionDataCache();
				initializeInProgress = true;
				if (includeAlternateUsers) {
					permissionDataCache.LoadUsers(session, caller.UserIds);
				}

			}

			List<ResponsibilityGroupModel> groups = null;
			var resourcePermissions = GetResourcePermissionsForType(resourceType);

			var output = distinctResourceIds.ToDictionary(id => id, id => false);
			foreach (var resourceId in distinctResourceIds) {
				output[resourceId] = _InnerCanAccessItem(requestedLevels, resourceType, resourceId, permItems, resourcePermissions, and, initializeInProgress, includeAlternateUsers, groups, permissionDataCache);
			}
			return output;
		}

		private bool _InnerCanAccessItem(PermItem.AccessLevel requestedLevels, PermItem.ResourceType resourceType, long resourceId, List<PermItem> allPermItems, IResourcePermissions resourcePermissions, bool and, bool initializeInProgress, bool includeAlternateUsers, List<ResponsibilityGroupModel> groups, PermissionDataCache permissionDataCache) {

			var permItems = allPermItems.Where(x => x.ResId == resourceId).ToList();
			if (!permItems.Any()) {
				return false;
			}

			if (resourcePermissions.AccessDisabled(session, resourceId)) {
				return false;
			}


			//Select all testers.
			var testers = AccessorPermissions.SelectMany(acc => acc.PermissionTests().Select(pt => new {
				AccessorType = acc.ForAccessorType(),
				Method = pt,
				Order = pt.OrderOfOperation()
			})).OrderBy(x => x.Order).ToList();

			//Construct baseContext
			var baseContext = new IAccessorPermissionContextFactory(resourceId, resourcePermissions, includeAlternateUsers, permissionDataCache);

			var anyFlags = false;
			foreach (var testFlag in new[] { PermItem.AccessLevel.View, PermItem.AccessLevel.Edit, PermItem.AccessLevel.Admin }) {
				/*Ordered by cheapest first...*/
				if (requestedLevels.HasFlag(testFlag)) {
					//Prevent circular reference for inherited permissions
					if (initializeInProgress == true) {
						permissionDataCache.ReferencesAlreadyChecked[new CircularReferenceInheritedPermissions(testFlag, resourceType, resourceId, includeAlternateUsers)] = false;
					}

					//only want to return if we handled some flags.
					anyFlags = true;
					var currentTrue = false;

					foreach (var tester in testers) {
						currentTrue = currentTrue || (permItems.Any(x => x.HasFlags(testFlag) && x.AccessorType == tester.AccessorType && tester.Method.IsPermitted(session, this, baseContext.CreateContext(x, testFlag))));
						if (currentTrue) {
							break;
						}
					}







					if (!currentTrue && and)
						return false;

					if (currentTrue && !and)
						return true;
				}
			}

			if (!anyFlags)
				return false;

			//Everything passed.
			if (and)
				return true;
			else
				return false;
		}

		public bool CanUpdateQuarter() {
			return caller.IsManagingOrganization();
		}

		private Dictionary<Tuple<PermItem.ResourceType, long>, IEnumerable<PermItem>> Cache_GetAllPermItemsForUser = new Dictionary<Tuple<PermItem.ResourceType, long>, IEnumerable<PermItem>>();

		/// <summary>
		/// Grabs perm items for explicitly specified permissions(RGM, Email) and implicit (Creator, Admin, Members)
		/// </summary>
		/// <param name="forUserId"></param>
		/// <param name="resourceType"></param>
		/// <returns></returns>
		//DON'T DELETE, COULD BE USEFUL
		public IEnumerable<PermItem> GetAllPermItemsForUser(PermItem.ResourceType resourceType, long forUserId) {

			var s = session;
			var callerPerms = this;
			//return items for RGM and Email
			foreach (var e in PermissionsAccessor.GetExplicitPermItemsForUser(s, callerPerms, forUserId, resourceType))
				yield return e;

			var user = s.Get<UserOrganizationModel>(forUserId);

			var resourcePermissions = GetResourcePermissionsForType(resourceType);

			var memberOfTheseResourceIds = callerPerms.GetIdsForResourceThatUserIsMemberOf(resourcePermissions, forUserId, true);
			var userCreatedTheseResourceIds = callerPerms.GetIdsForResourcesCreatedByUser(resourcePermissions, forUserId);
			var resourceIdsAtOrganization = callerPerms.GetIdsForResourceForOrganization(resourcePermissions, user.Organization.Id);

			var allResourceIds = new List<long>();
			allResourceIds.AddRange(memberOfTheseResourceIds);
			allResourceIds.AddRange(userCreatedTheseResourceIds);
			allResourceIds.AddRange(resourceIdsAtOrganization);
			allResourceIds = allResourceIds.Distinct().ToList();


			var allPermItemsUnfiltered = s.QueryOver<PermItem>()
				.Where(x => x.ResType == resourceType && x.DeleteTime == null)
				.Where(x => x.AccessorType == PermItem.AccessType.Members || x.AccessorType == PermItem.AccessType.Admins || x.AccessorType == PermItem.AccessType.Creator)
				.WhereRestrictionOn(x => x.ResId).IsIn(allResourceIds)
				.List().ToList();

			//==Get Members==
			{
				//Things I am a member of
				//	Any member permissions?
				if (memberOfTheseResourceIds.Any()) {
					var permItems = allPermItemsUnfiltered
										.Where(x => x.ResType == resourceType && x.AccessorType == PermItem.AccessType.Members && x.DeleteTime == null)
										.Where(x => memberOfTheseResourceIds.Contains(x.ResId))
										.ToList();
					foreach (var p in permItems) {
						//Add the creator perm item(s)
						yield return p;
					}
				}
			}
			//==Get Creators==
			{
				//Things I created..
				if (userCreatedTheseResourceIds.Any()) {
					//	Any creator permissions?
					var permItems = allPermItemsUnfiltered
										.Where(x => x.ResType == resourceType && x.AccessorType == PermItem.AccessType.Creator && x.DeleteTime == null)
										.Where(x => userCreatedTheseResourceIds.Contains(x.ResId))
										.ToList();
					foreach (var p in permItems) {
						//Add the creator perm item(s)
						yield return p;
					}
				}
			}

			//==Get Admins==
			{
				if (user.IsManagingOrganization(false)) {
					if (resourceIdsAtOrganization.Any()) {
						//	Any admins permissions?
						var permItems = allPermItemsUnfiltered
							.Where(x => x.ResType == resourceType && x.AccessorType == PermItem.AccessType.Admins && x.DeleteTime == null)
							.Where(x => resourceIdsAtOrganization.Contains(x.ResId))
							.ToList();
						foreach (var p in permItems) {
							//Add the admins perm item(s)
							yield return p;
						}
					}
				}
			}
			yield break;
		}











		public class CircularReferenceInheritedPermissions {
			public CircularReferenceInheritedPermissions(PermItem.AccessLevel accessLevel, PermItem.ResourceType resourceType, long resourceId, bool includeAlternateUsers) {
				accessLevel.EnsureSingleAndValidAccessLevel();
				AccessLevel = accessLevel;
				ResourceType = resourceType;
				ResourceId = resourceId;
				IncludeAlternateUsers = includeAlternateUsers;
			}

			/// <summary>
			/// Important: Only store one flag here.
			/// </summary>
			public PermItem.AccessLevel AccessLevel { get; set; }
			public PermItem.ResourceType ResourceType { get; set; }
			public long ResourceId { get; set; }
			public bool IncludeAlternateUsers { get; set; }

			public Tuple<PermItem.AccessLevel, PermItem.ResourceType, long, bool> ToTuple() {
				return Tuple.Create(AccessLevel, ResourceType, ResourceId, IncludeAlternateUsers);
			}
			public override bool Equals(object obj) {
				if (!(obj is CircularReferenceInheritedPermissions))
					return false;
				return ToTuple().Equals(((CircularReferenceInheritedPermissions)obj).ToTuple());
			}

			public override int GetHashCode() {
				return ToTuple().GetHashCode();
			}
		}

		#region Implement for each resource type



		protected List<long> GetMyMemeberUserIds(IResourcePermissions resourcePermissions, long resourceId, bool meOnly = false) {
			return resourcePermissions.GetMembersOfResource(session, resourceId, caller.Id, meOnly).ToList();

		}

		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(IResourcePermissions resourcePermissions, long userId, bool ignoreExceptions = false) {
			try {
				return resourcePermissions.GetIdsForResourceThatUserIsMemberOf(session, userId);

			} catch {
				if (!ignoreExceptions)
					throw;
				return new long[] { };
			}
		}

		protected bool IsCreator(IResourcePermissions resourcePermissions, long resourceId, bool includeAlternateUsers, PermissionDataCache permissionDataCache) {
			var creatorId = resourcePermissions.GetCreator(session, resourceId);
			if (creatorId == null)
				return false;
			if (creatorId == caller.Id)
				return true;

			if (includeAlternateUsers) {
				if (caller.UserIds.Contains(creatorId.Value)) {
					var u = permissionDataCache.GetUser(session, creatorId.Value);
					if (u.DeleteTime == null && u.Organization.DeleteTime == null) {
						return true;
					}
				}
			}

			return false;


		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(IResourcePermissions resourcePermissions, long userId) {
			return resourcePermissions.GetIdsForResourcesCreatedByUser(session, userId);

		}

		protected long GetOrganizationId(IResourcePermissions resourcePermissions, long resourceId) {

			return resourcePermissions.GetOrganizationIdForResource(session, resourceId);

		}

		public IEnumerable<long> GetIdsForResourceForOrganization(IResourcePermissions resourcePermissions, long orgId) {
			return resourcePermissions.GetIdsForResourceForOrganization(session, orgId);
		}


		#endregion


	}
}

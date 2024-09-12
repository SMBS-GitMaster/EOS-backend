using FluentNHibernate.Mapping;
using NHibernate;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace RadialReview.Models.Accountability {

	[DebuggerDisplay("Node: {AccountabilityRolesGroup} - {GetUserNames(RadialReview.Models.Enums.GivenNameFormat.FirstAndLast,maxCount:3)}")]
	public class AccountabilityNode : ILongIdentifiable, IHistorical, IForModel {
		#region saved properties
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AccountabilityChartId { get; set; }
		public virtual long? ParentNodeId { get; set; }
		public virtual AccountabilityNode ParentNode { get; set; }
		public virtual int Ordering { get; set; }
		#endregion

		#region unsaved properties
		public virtual List<AccountabilityNode> _Children { get; set; }
		public virtual string _Name { get; set; }
		public virtual bool? _Editable { get; set; }

		public virtual long ModelId { get { return Id; } }
		public virtual string ModelType { get { return ForModel.GetModelType<AccountabilityNode>(); } }
		#endregion

		#region helper methods
		public class ImageAndName {
			public string Name { get; set; }
			public string Initials { get; set; }
			public string ImageUrl { get; set; }
			public string ToHtml() {
				return ImageUtility.GenerateImageHtml(ImageUrl, Name, Initials);
			}
		}
		public virtual List<ImageAndName> GetUserImages(int maxCount = int.MaxValue) {
			if (_Users == null)
				throw new Exception("Users were not populated");

			maxCount = Math.Max(0, maxCount);
			var output = new List<ImageAndName>();
			output.AddRange(_Users.Take(maxCount).Select(x => new ImageAndName {
				ImageUrl = x.GetImageUrl(),
				Name = x.GetName(),
				Initials = x.GetInitials()
			}));
			return output;
		}
		public virtual bool AreUsersPopulated() {
			return _Users != null;
		}
		public virtual List<UserOrganizationModel> GetUsers(ISession s, bool forceUpdate = false) {
			if (_Users == null || forceUpdate) {
				//Try not to get here.
				UserOrganizationModel userAlias = null;
				_Users = s.QueryOver<AccountabilityNodeUserMap>()
					.JoinAlias(x => x.User, () => userAlias)
					.Where(x => x.DeleteTime == null && userAlias.DeleteTime == null && x.AccountabilityNode.Id == Id)
					.Fetch(x => x.User).Eager
					.List().Select(x => x.User).ToList();
			}
			return _Users.ToList();
		}
		public virtual void SetUsers(List<AccountabilityNodeUserMap> accNodeUsers) {
			if (accNodeUsers == null) {
				_Users = null;
			} else {
				_Users = accNodeUsers.Where(x => x.AccountabilityNodeId == Id && x.DeleteTime == null && x.User.DeleteTime == null && (x.User.User == null || x.User.User.DeleteTime == null))
									.Select(x => x.User)
									.ToList();
			}
		}
		/// <summary>
		/// Set users given node-user map (unpopulated), and users.
		/// </summary>
		/// <param name="accNodeUsers"></param>
		/// <param name="users"></param>
		public virtual void SetUsers(List<AccountabilityNodeUserMap> accNodeUsers, List<UserOrganizationModel> users) {
			var userIds = accNodeUsers.Where(x => x.AccountabilityNodeId == Id && x.DeleteTime == null)
								.Select(x => x.UserId)
								.ToList();
			_Users = users.Where(x => x.DeleteTime == null && (x.User == null || x.User.DeleteTime == null))
							.Where(x => userIds.Contains(x.Id))
							.ToList();
		}
		public virtual string GetUserNames(GivenNameFormat nameFormat, string onEmpty = "nobody", int maxCount = int.MaxValue) {
			if (_Users == null)
				throw new Exception("Users were not populated");

			maxCount = Math.Max(0, maxCount);

			if (_Users.Count() == 0) {
				return onEmpty;
			} else {
				var append = "";
				var userNames = _Users.Take(maxCount).Select(x => x.GetName(nameFormat));
				if (_Users.Count() > maxCount) {
					append = " + " + (_Users.Count() - maxCount);
				}
				return string.Join(", ", userNames) + append;
			}
		}
		public virtual bool ContainsUser(long id) {
			if (_Users == null)
				throw new Exception("Users not populated.");
			return _Users.Any(x => x.Id == id);
		}

		public virtual int UserCount() {
			if (_Users == null)
				throw new Exception("Users not populated.");
			return _Users.Count();
		}

		public virtual bool ContainsOnlyUser(long id) {
			if (_Users == null)
				throw new Exception("Users not populated.");
			return _Users.Count() == 1 && ContainsUser(id);
		}

		public virtual List<SimpleRole> GetRoles() {
			return AccountabilityRolesGroup.NotNull(x => x._Roles.NotNull(y => y.SelectMany(z => z.Roles))).NotNull(x => x.ToList());
		}

		public virtual string GetPositionName() {
			return AccountabilityRolesGroup.NotNull(x => x.PositionName);
		}

		public virtual long GetAccountabilityRolesGroupId() {
			return AccountabilityRolesGroupId;
		}
		public virtual AccountabilityRolesGroup GetAccountabilityRolesGroup() {
			return AccountabilityRolesGroup;
		}

		public virtual bool Is<T>() {
			return ModelType == ForModel.GetModelType(typeof(T));
		}

		public virtual string ToPrettyString() {
			return _Name ?? string.Join(" - ", new[] {
				string.Join(",",_Users.NotNull(x => x.Select(y=>y.GetName()))),
				AccountabilityRolesGroup.NotNull(y => y.PositionName)
			}.Where(x => x != null));
		}
		#endregion

		#region deprecated and properties to avoid
		[Obsolete("Avoid, use GetUsers(s) instead")]
		private IEnumerable<UserOrganizationModel> _Users { get; set; }
		public virtual long AccountabilityRolesGroupId { get; set; }
		[Obsolete("Avoid, use GetPosition() and GetRoles() instead")]
		public virtual AccountabilityRolesGroup AccountabilityRolesGroup { get; set; }
		[Obsolete("Deprecated")]
		public virtual long? DeprecatedUserId { get; set; }
		[Obsolete("Deprecated")]
		public virtual UserOrganizationModel DeprecatedUser { get; set; }
		[Obsolete("Deprecated")]
		public virtual long? DeprecatedPositionId { get { return AccountabilityRolesGroup.DepricatedPositionId; } }




		#endregion


		public AccountabilityNode() {
			CreateTime = DateTime.UtcNow;
		}
		public class Map : ClassMap<AccountabilityNode> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Ordering);
				Map(x => x.OrganizationId);
				Map(x => x.AccountabilityChartId);

				Map(x => x.DeprecatedUserId).Column("UserId").Index("idx__AccountabilityNode_UserId").Length(20);
				References(x => x.DeprecatedUser).Column("UserId").LazyLoad().ReadOnly();

				Map(x => x.AccountabilityRolesGroupId).Column("RolesGroupId");
				References(x => x.AccountabilityRolesGroup).Column("RolesGroupId").LazyLoad().ReadOnly();

				Map(x => x.ParentNodeId).Column("ParentNodeId").Index("idx__AccountabilityNode_ParentId");
				References(x => x.ParentNode).Column("ParentNodeId").LazyLoad().ReadOnly();
			}
		}

	}

}

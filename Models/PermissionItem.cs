using System;
using System.Linq;
using FluentNHibernate.Mapping;
using RadialReview.Core.Models.Terms;
using RadialReview.Models.Interfaces;
using static RadialReview.Models.PermItem;

namespace RadialReview.Models {
	public class PermissionsHeading {
		public string ViewName { get; set; }
		public string EditName { get; set; }
		public string AdminName { get; set; }
		public string ViewHelp { get; set; }
		public string EditHelp { get; set; }
		public string AdminHelp { get; set; }


		public bool ShowView { get; set; }
		public bool ShowEdit { get; set; }
		public bool ShowAdmin { get; set; }


		public PermissionsHeading() {
			ViewName = "View";
			EditName = "Edit";
			AdminName = "Admin";
			ViewHelp = "Allowed to view this item";
			EditHelp = "Allowed to edit this item";
			AdminHelp = "Allowed to alter permissions for this item";
			ShowView = true;
			ShowEdit = true;
			ShowAdmin = true;
		}

		public static PermissionsHeading GetHeading(ResourceType type) {
			switch (type) {
				case ResourceType.UpgradeUsersForOrganization:
					return new PermissionsHeading() {
						ViewName = null,
						EditName = "Edit",
						AdminName = "Admin",
						ShowView = false,
					};
				case ResourceType.EditDeleteUserDataForOrganization:
					return new PermissionsHeading() {
						ViewName = null,
						EditName = "Edit/Delete",
						AdminName = "Admin",
						ShowView = false,
					};
				default:
					return new PermissionsHeading();
			};
		}
	}


	public static class AccessLevelExtensions {
		public static void EnsureSingleAndValidAccessLevel(this PermItem.AccessLevel accessLevel) {
			//Can't be invalid.
			if (accessLevel == PermItem.AccessLevel.Invalid) {
				throw new Exception("Fatal error. A valid flag is required.");
			}
			//We really need this to be only one flag.
			if (Enum.GetValues(typeof(PermItem.AccessLevel)).OfType<PermItem.AccessLevel>().Where(i => i != PermItem.AccessLevel.Invalid && accessLevel.HasFlag(i)).Count() != 1) {
				throw new Exception("Fatal error. Must be called with exactly one access level flag.");
			}
		}
	}

	public class PermItem : ILongIdentifiable, IHistorical {

		/// <summary>
		/// NOTICE: Don't forget to add an IAccessorPermissions for new AccessType.
		/// All IAccessorPermissions are constructed and searched at runtime
		/// </summary>
		public enum AccessType {
			Invalid = 0,
			Creator = 100,
			RGM = 200,
			Members = 300,
			Admins = 400,
			Email = 500,
			Inherited = 600,
			UserModelAtOrganization = 700,
			System = 10000
		}
		/// <summary>
		/// NOTICE: Don't forget to add an IResourcePermissions for new ResourceTypes.
		///	All IResourcePermissions are constructed and searched at runtime
		/// </summary>
		public enum ResourceType {
			Invalid = 0,
			L10Recurrence = 1,
			InvoiceForOrganization = 2,
			VTO = 3,
			AccountabilityHierarchy = 4,
			UpgradeUsersForOrganization = 5,
			[Obsolete("Old")]
			CoreProcess = 6,
			SurveyContainer = 7,
			Survey = 8,
			UpdatePaymentForOrganization = 9,
			EditDeleteUserDataForOrganization = 10,
			File = 11,

			L10Meeting = 12,
			ProcessFolder = 13,
			Process = 14,
			Whiteboard = 15,
			DocumentsFolder = 16,




		}
		[Flags]
		public enum AccessLevel {
			Invalid = 0,
			View = 1,
			Edit = 2,
			Admin = 4,
		}


		public virtual long Id { get; set; }
		public virtual bool IsArchtype { get; set; }

		public virtual long CreatorId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }


		public virtual bool CanView { get; set; }
		public virtual bool CanEdit { get; set; }
		public virtual bool CanAdmin { get; set; }



		public virtual ResourceType? InheritType { get; set; }

		public virtual bool HasFlags(AccessLevel level) {
			if (level.HasFlag(AccessLevel.View) && !CanView)
				return false;
			if (level.HasFlag(AccessLevel.Edit) && !CanEdit)
				return false;
			if (level.HasFlag(AccessLevel.Admin) && !CanAdmin)
				return false;
			return true;
		}


		public virtual AccessType AccessorType { get; set; }
		public virtual long AccessorId { get; set; }

		public virtual ResourceType ResType { get; set; }
		public virtual long ResId { get; set; }

		public PermItem() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<PermItem> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CanView);
				Map(x => x.CanEdit);
				Map(x => x.CanAdmin);
				Map(x => x.IsArchtype);
				Map(x => x.AccessorId);
				Map(x => x.AccessorType);
				Map(x => x.ResId);
				Map(x => x.ResType);

				Map(x => x.CreatorId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId);
				Map(x => x.InheritType);
			}
		}

	}

	public static class ResourceTypeExtensions {
		public static string ToFriendlyName(this PermItem.ResourceType self,TermsCollection terms) {
			switch (self) {
				case ResourceType.L10Recurrence:
					return "Meeting";
				case ResourceType.VTO:
					return terms.GetTerm(TermKey.BusinessPlan);
				case ResourceType.AccountabilityHierarchy:
					return terms.GetTerm(TermKey.OrganizationalChart);
				case ResourceType.CoreProcess:
					return "Core Process";
				case ResourceType.SurveyContainer:
					return terms.GetTerm(TermKey.Quarterly1_1);
				case ResourceType.File:
					return "File";
				case ResourceType.Process:
					return "Process";
				case ResourceType.Whiteboard:
					return "Whiteboard";
				case ResourceType.DocumentsFolder:
					return "Folder";
				default:
					return "" + self;
			}
		}
	}

}

public class UserModelAtOrganizationPermItem : IHistorical {
	public virtual long Id { get; set; }
	public virtual string UserModelId { get; set; }
	public virtual long OrganizationId { get; set; }
	public virtual long CreatorId { get; set; }
	public virtual DateTime CreateTime { get; set; }
	public virtual DateTime? DeleteTime { get; set; }

	public UserModelAtOrganizationPermItem() {
		CreateTime = DateTime.UtcNow;
	}

	public class Map : ClassMap<UserModelAtOrganizationPermItem> {
		public Map() {
			Id(x => x.Id);
			Map(x => x.UserModelId);
			Map(x => x.OrganizationId);
			Map(x => x.CreatorId);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
		}
	}
}

public class EmailPermItem : IHistorical {
	public virtual long Id { get; set; }
	public virtual string Email { get; set; }
	public virtual long CreatorId { get; set; }
	public virtual DateTime CreateTime { get; set; }
	public virtual DateTime? DeleteTime { get; set; }

	public EmailPermItem() {
		CreateTime = DateTime.UtcNow;
	}

	public class Map : ClassMap<EmailPermItem> {
		public Map() {
			Id(x => x.Id);
			Map(x => x.Email);
			Map(x => x.CreatorId);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
		}
	}


}

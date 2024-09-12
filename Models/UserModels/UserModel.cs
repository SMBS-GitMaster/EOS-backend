using System.Diagnostics;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using Microsoft.AspNetCore.Identity;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace RadialReview.Models {
	[DebuggerDisplay("{FirstName} {LastName}")]
	public class UserModel : IdentityUser, IDeletable, IStringIdentifiable {
		public virtual string FirstName { get; set; }

		public virtual string LastName { get; set; }

		public override string Email {
			get {
				return UserName;
			}
		}

		public virtual DateTime CreateTime { get; set; }

		public virtual bool DisableTips { get; set; }

		public virtual bool Hints { get; set; }

		public virtual bool ConsoleLog { get; set; }

		public virtual long CurrentRole { get; set; }

		public virtual string ImageGuid { get; set; }

		public virtual GenderType? Gender { get; set; }

		public virtual bool EmailNotVerified { get; set; }

    protected internal virtual string _UserOrganizationIds { get; set; }

		public virtual ColorMode ColorMode { get; set; }

		public virtual bool? DarkMode { get; set; }

		public virtual int? SendTodoTime { get; set; }

		public virtual int? _TimezoneOffset { get; set; }

		private string _ImageUrl { get; set; }

        public virtual bool? IsUsingV3 { get; set; }

		public virtual int NumViewedNewFeatures { get; set; }


		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual IList<UserOrganizationModel> UserOrganization { get; set; }

		public virtual int UserOrganizationCount { get; set; }

		public virtual String Name(GivenNameFormat format = GivenNameFormat.FirstAndLast) {
			return format.From(FirstName, LastName);
		}

		public virtual String GetInitials() {
			var inits = new List<string>();
			if (FirstName != null && FirstName.Length > 0)
				inits.Add(FirstName.Substring(0, 1));
			if (LastName != null && LastName.Length > 0)
				inits.Add(LastName.Substring(0, 1));
			return string.Join(" ", inits).ToUpperInvariant();
		}


		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual IList<UserRoleModel> Roles { get; set; }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public virtual IList<UserLogin> Logins { get; set; }


    public UserModel() {
			UserOrganization = new List<UserOrganizationModel>();
			Hints = true;
			SendTodoTime = -1;
			Roles = new List<UserRoleModel>();
            Logins = new List<UserLogin>();
            ConsoleLog = false;
			CreateTime = DateTime.UtcNow;
		}

		public virtual long? GetCurrentRole() {
			if (IsRadialAdmin)
				return CurrentRole;
			if (UserOrganizationIds != null && UserOrganizationIds.Any(x => x == CurrentRole))
				return CurrentRole;
			return null;
		}

		public virtual DateTime? DeleteTime { get; set; }

		public virtual bool ReverseScorecard { get; set; }

		public virtual bool IsRadialAdmin { get; set; }

		public virtual long[] UserOrganizationIds {
			get {
				return _UserOrganizationIds == null ? null : _UserOrganizationIds.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLong()).Where(x => x != 0).ToArray();
			}

			set {
				_UserOrganizationIds = String.Join("~", value.ToArray());
			}
		}

		public virtual UserStyleSettings _StylesSettings { get; set; }

		public class UserModelMap : ClassMap<UserModel> {
			public UserModelMap() {
				Id(x => x.Id).CustomType(typeof(string)).GeneratedBy.Custom(typeof(GuidStringGenerator)).Length(36);
				Map(x => x.UserName).Index("UserName_IDX").Length(400);
				Map(x => x.FirstName).Not.LazyLoad();
				Map(x => x.LastName).Not.LazyLoad();
				Map(x => x.SendTodoTime);
				Map(x => x.PasswordHash);
				Map(x => x.Hints);
				Map(x => x.CurrentRole);
				Map(x => x._UserOrganizationIds).Length(10000);
				Map(x => x.SecurityStamp);
				Map(x => x.IsRadialAdmin);
				Map(x => x.DeleteTime);
				Map(x => x.ImageGuid);
				Map(x => x.Gender);
				Map(x => x.CreateTime);
				Map(x => x.UserOrganizationCount);
				Map(x => x.ReverseScorecard);
				Map(x => x.DisableTips);
				Map(x => x.ConsoleLog);
				Map(x => x.ColorMode);
				Map(x => x.EmailNotVerified);
				Map(x => x.DarkMode);
                Map(x => x.IsUsingV3);
				Map(x => x.NumViewedNewFeatures);
				HasMany(x => x.UserOrganization).LazyLoad().Cascade.SaveUpdate();
				HasMany(x => x.Roles).Cascade.SaveUpdate();
                HasMany(x => x.Logins).KeyColumn("UserId").LazyLoad().Cascade.SaveUpdate();
      }
		}


	}
}

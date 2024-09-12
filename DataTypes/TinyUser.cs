using Newtonsoft.Json;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Models;
using RadialReview.Models.Interfaces;
using RadialReview.Models.UserModels;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RadialReview.Utilities.DataTypes {
	[DataContract]
	public class TinyUser : IForModel {
		[DataMember]
		[JsonProperty("Id")]
		public long UserOrgId { get; set; }
		[DataMember]
		public string FirstName { get; set; }
		[DataMember]
		public string LastName { get; set; }
		[DataMember]
		public string Email { get; set; }
		public string ImageGuid { get; set; }
		[DataMember]
		public string ImageUrl { get { return GetImageUrl(); } }
		[DataMember]
		public string Name { get { return GetName(); } }
		[DataMember]
		public string Initials { get { return GetInitials(); } }

		public long ModelId { get { return UserOrgId; } }
		public string ModelType { get { return ForModel.GetModelType<UserOrganizationModel>(); } }
    public gqlUserAvatarColor? UserAvatarColor { get; set; }

    public string Description {
			get {
				return null;
			}
		}

		public string ItemValue { get { return "" + UserOrgId; } }

		public Tuple<string, string, string, long> Tuplize() {
			return Tuple.Create(FirstName, LastName, Email, UserOrgId);
		}

		public override bool Equals(object obj) {
			if (obj is TinyUser) {
				return this.Tuplize().Equals(((TinyUser)obj).Tuplize());
			}
			return false;
		}

		public override int GetHashCode() {
			return this.Tuplize().GetHashCode();
		}

		public int GetUserHashCode() {
			var hash = 0;
			var str = Name;
			if (str != null && str.Length != 0) {
				foreach (var chr in str) {
					hash = ((hash << 5) - hash) + chr;
					hash |= 0; // Convert to 32bit integer
				}
			}
			hash = Math.Abs(hash) % 360;
			return hash;
		}


		public string GetImageUrl(ImageSize size = ImageSize._64) {

			return UserLookup.TransformImageSuffix(ImageGuid.NotNull(x => "/" + x + ".png"), size);
		}

		public string GetName() {
			return ((FirstName ?? "").Trim() + " " + (LastName ?? "").Trim()).Trim();
		}

		public string GetInitials() {
			var inits = new List<string>();
			if (FirstName != null && FirstName.Length > 0)
				inits.Add(FirstName.Substring(0, 1));
			if (LastName != null && LastName.Length > 0)
				inits.Add(LastName.Substring(0, 1));
			return string.Join(" ", inits).ToUpperInvariant();
		}

		public TinyUser Standardize() {
			var x = this;
			return new TinyUser() {
				Email = x.Email.NotNull(y => y.ToLower()),
				FirstName = x.FirstName.NotNull(y => y.ToLower()),
				LastName = x.LastName.NotNull(y => y.ToLower()),
				UserOrgId = x.UserOrgId
			};
		}

		public static TinyUser FromUserOrganization(UserOrganizationModel x) {
      if (x == null)
				return null;

      var userHashCode = UserOrganizationExtensions.GeUserHashCode(x);
      gqlUserAvatarColor userAvatarColor = UserOrganizationExtensions.MapToUserAvatarColor(userHashCode);

      return new TinyUser() {
				Email = x.GetEmail().NotNull(y => y.ToLower()),
				FirstName = x.GetFirstName(),
				LastName = x.GetLastName(),
				UserOrgId = x.Id,
				ImageGuid = x.User.NotNull(y => y.ImageGuid),
        UserAvatarColor = userAvatarColor
      };
		}

		public bool Is<T>() {
			return ForModel.GetModelType<UserOrganizationModel>() == ForModel.GetModelType(typeof(T));
		}

		public string ToPrettyString() {
			return GetName();
		}




	}

}

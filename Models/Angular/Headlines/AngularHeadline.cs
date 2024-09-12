using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.L10;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System;
using System.Runtime.Serialization;
using RadialReview.Crosscutting.AttachedPermission;

namespace RadialReview.Models.Angular.Headlines {
	public class AngularHeadline : BaseAngular, IAttachedPermission {

		public AngularHeadline(long id) : base(id) {
		}

		public AngularHeadline(PeopleHeadline headline) : base(headline.Id) {
			Name = headline.Message;
			DetailsUrl = Config.BaseUrl(null, "/headlines/pad/" + headline.Id);

			Owner = AngularUser.CreateUser(headline.Owner);
			if (headline.About != null)
				About = new AngularPicture(headline.About);
			else
				About = new AngularPicture(-headline.Id) {
					ImageUrl = ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder,
					Name = headline.AboutName
				};
			CloseTime = headline.CloseTime;
			CreateTime = headline.CreateTime;
			Archived = headline.CloseTime != null;
			Link = "/L10/Timeline/" + headline.RecurrenceId + "#transcript-" + headline.Id;
			if (headline.RecurrenceId != 0)
				OriginId = headline.RecurrenceId;
			HasAudio = headline.HasAudio;
		}

		public AngularHeadline() {

		}

		public string Name { get; set; }
		public string DetailsUrl { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? CloseTime { get; set; }
		public DateTime? CreateTime { get; set; }
		public AngularPicture About { get; set; }
		public bool? Archived { get; set; }

		[IgnoreDataMember]
		public string Link { get; set; }
		public PermissionDto Permission { get; set; }

		public string Origin { get; set; }
		public long OriginId { get; set; }
		public bool HasAudio { get; set; }
	}
}

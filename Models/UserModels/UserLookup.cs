using System;
using System.Linq;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Core.Properties;
using Mandrill.Models;
using System.Diagnostics;

namespace RadialReview.Models.UserModels {

	[DebuggerDisplay("UserLookup")]
	public class UserLookup : ILongIdentifiable, IHistorical {

		[Obsolete("Use UserId instead")]
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual DateTime AttachTime { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual bool IsRadialAdmin { get; set; }
		public virtual string Name { get; set; }
		public virtual int NumRocks { get; set; }
		public virtual int NumMeasurables { get; set; }
		public virtual int NumRoles { get; set; }
		public virtual string Email { get; set; }
		public virtual string Positions { get; set; }
		public virtual string Teams { get; set; }
		public virtual string Managers { get; set; }
		public virtual bool IsManager { get; set; }
		public virtual bool IsAdmin { get; set; }
		public virtual bool EnableWhale { get; set; }
		public virtual bool HasJoined { get; set; }
		public virtual bool HasSentInvite { get; set; }
		public virtual bool IsImplementer { get; set; }
		public virtual long OrganizationId { get; set; }

		public virtual V2StatusBar V2StatusBar { get; set; }

		public virtual DateTime? LastLogin { get; set; }

		public virtual bool _PersonallyManaging { get; set; }
		public virtual string _ImageUrlSuffix { get; set; }

		public virtual OrganizationModel _Organization { get; set; }

		public virtual bool IsClient { get; set; }

		public virtual WebHookEventType? EmailStatus { get; set; }
		public virtual bool EvalOnly { get; set; }
		public virtual DateTime? LastSupportCodeReset { get; set; }

		public virtual string ImageUrl(ImageSize size = ImageSize._32) {
			return TransformImageSuffix(_ImageUrlSuffix, size);
		}

		public virtual string[] GetPositions(bool removeEmpty) {
			if (string.IsNullOrWhiteSpace(Positions))
				return new string[0];

			var options = StringSplitOptions.TrimEntries;
			if (removeEmpty)
				options = options | StringSplitOptions.RemoveEmptyEntries;

			return Positions.Split(",", options).OrderBy(x => string.IsNullOrWhiteSpace(x) ? 1 : 0).ToArray();
		}

		public static string CreatePositionsString(bool removeEmpty, string[] positions) {
			var pos = positions.OrderBy(x => string.IsNullOrWhiteSpace(x) ? 1 : 0)
				.Where(x => removeEmpty ? !string.IsNullOrWhiteSpace(x) : true)
				.ToArray();
			return string.Join(",", pos);
		}


		public static string TransformImageSuffix(string imageSuffix, ImageSize size = ImageSize._32) {

			var s = size.ToString().Substring(1);
			if (imageSuffix != null && !imageSuffix.EndsWith("/i/userplaceholder"))
				return ConstantStrings.AmazonS3Location + s + imageSuffix;
			return "/i/userplaceholder";
		}

		public virtual string GetInitials() {
			var inits = (Name ?? "").Split(' ').Select(x => x.Trim()).Where(x => !String.IsNullOrEmpty(x)).Select(x => x.Substring(0, 1).ToUpperInvariant()).ToList();
			while (inits.Count > 2)
				inits.RemoveAt(1);

			return string.Join(" ", inits).ToUpperInvariant();
		}

		public class UserLookupMap : ClassMap<UserLookup> {
			public UserLookupMap() {
				Id(x => x.Id);
				Map(x => x.UserId).Index("UserLookup_UserId_IDX").Length(20);
				References(x => x._Organization).Column("OrganizationId").ForeignKey("none").Nullable().LazyLoad().ReadOnly();
				Map(x => x.AttachTime);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Name);
				Map(x => x.NumRocks);
				Map(x => x.IsClient);
				Map(x => x.IsImplementer);
				Map(x => x.IsRadialAdmin);
				Map(x => x.NumMeasurables);
				Map(x => x.NumRoles);
				Map(x => x.Email);
				Map(x => x.Positions);
				Map(x => x.Teams);
				Map(x => x.Managers);
				Map(x => x.IsManager);
				Map(x => x.IsAdmin);
				Map(x => x.EnableWhale);
				Map(x => x.HasJoined);
				Map(x => x.LastLogin);
				Map(x => x.HasSentInvite);
				Map(x => x.EmailStatus).Nullable().CustomType<WebHookEventType>();
				Map(x => x.OrganizationId).Index("UserLookup_OrganizationId_IDX");
				Map(x => x._ImageUrlSuffix);
				Map(x => x.EvalOnly);
				Map(x => x.V2StatusBar);
				Map(x => x.LastSupportCodeReset);
			}
		}
	}

	public enum V2StatusBar {
		Unset = 0,
		DoNotShow = 1,
		ShowAllowSignup = 2,
		ShowDoNotAllowSignup = 3
	}
}

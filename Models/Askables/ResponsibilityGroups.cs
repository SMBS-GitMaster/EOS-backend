using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using NHibernate.Envers.Configuration.Attributes;
using Newtonsoft.Json;
using System.Diagnostics;

namespace RadialReview.Models.Askables {
	[DataContract]
	public abstract class ResponsibilityGroupModel : ILongIdentifiable, IDeletable {
		[DataMember]
		public virtual long Id { get; set; }
		public abstract String GetName(GivenNameFormat nameFormat = GivenNameFormat.FirstAndLast);

		public virtual String GetNameExtended() {
			return GetName();
		}
		public virtual string GetNameShort() {
			return GetName();
		}
		public static string DEFAULT_IMAGE = ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder;

		public virtual string GetImageUrl() {
			return DEFAULT_IMAGE;
		}

		public abstract OriginType GetOrigin();

		public abstract String GetGroupType();

		[JsonIgnore]
		[IgnoreDataMember]
		public virtual OrganizationModel Organization { get; set; }
		[JsonIgnore]
		[IgnoreDataMember]

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual IList<ResponsibilityModel> Responsibilities { get; set; }
		protected virtual Boolean? _Editable { get; set; }

		public virtual Boolean GetEditable() {
			return _Editable.Value;
		}
		public virtual void SetEditable(bool editable) {
			_Editable = editable;
		}

		public ResponsibilityGroupModel() {
			Responsibilities = new List<ResponsibilityModel>();
		}

		#region IDeletable Members

		public virtual DateTime? DeleteTime { get; set; }

		#endregion

		public class ResponsibilityGroupModelMap : ClassMap<ResponsibilityGroupModel> {
			public ResponsibilityGroupModelMap() {
				Id(x => x.Id);
				Map(x => x.DeleteTime);
				References(x => x.Organization)
					.Not.LazyLoad()
					.Column("Organization_id");
				HasMany(x => x.Responsibilities)
					.Cascade.SaveUpdate()
					.LazyLoad();

			}
		}
	}

	public class Deprecated {
		[Audited(TargetAuditMode = RelationTargetAuditMode.NotAudited)]
		public class OrganizationPositionModel : ResponsibilityGroupModel, IHistorical {

			public virtual String CustomName { get; set; }
			public virtual long CreatedBy { get; set; }
			public virtual long? TemplateId { get; set; }
			public virtual DateTime CreateTime { get; set; }

			public override string GetName(GivenNameFormat nameFormat = GivenNameFormat.FirstAndLast) {
				return CustomName;
			}

			public override string GetNameExtended() {
				return base.GetNameExtended() + " (Position)";
			}

			public override string GetGroupType() {
				return DisplayNameStrings.position;
			}

			public override string GetImageUrl() {
				return ConstantStrings.AmazonS3Location + ConstantStrings.ImagePositionPlaceholder;
			}

			public override OriginType GetOrigin() {
				return OriginType.Position;
			}

			public OrganizationPositionModel() {

			}
		}
	}

	public class OrganizationTeamModel : ResponsibilityGroupModel, IHistorical {
		public virtual TeamType Type { get; set; }
		public virtual String Name { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual long ManagedBy { get; set; }
		public virtual Boolean OnlyManagersEdit { get; set; }
		public virtual Boolean InterReview { get; set; }
		public virtual Boolean Secret { get; set; }
		public virtual long? TemplateId { get; set; }
		public virtual DateTime CreateTime { get; set; }

		public OrganizationTeamModel() : base() {
			OnlyManagersEdit = true;
			InterReview = true;
			CreateTime = DateTime.UtcNow;

		}
		public override string GetName(GivenNameFormat nameFormat = GivenNameFormat.FirstAndLast) {
			return Name;
		}

		public override string GetNameExtended() {
			return base.GetNameExtended() + " (Team)";
		}

		public override string GetGroupType() {
			return DisplayNameStrings.team;
		}

		public override string GetImageUrl() {
			return ConstantStrings.AmazonS3Location + ConstantStrings.ImageGroupPlaceholder;
		}

		public static OrganizationTeamModel SubordinateTeam(UserOrganizationModel creator, UserOrganizationModel manager) {
			return new OrganizationTeamModel() {
				CreatedBy = creator.Id,
				InterReview = false,
				ManagedBy = manager.Id,
				Name = manager.GetNameAndTitle() + " " + Config.DirectReportName() + "s",
				OnlyManagersEdit = true,
				Organization = manager.Organization,
				Responsibilities = new List<ResponsibilityModel>(),
				Secret = false,
				Type = TeamType.Subordinates,
				_Editable = false,
			};
		}

		public override OriginType GetOrigin() {
			return OriginType.Team;
		}
	}

	public class TeamModelMap : SubclassMap<OrganizationTeamModel> {
		public TeamModelMap() {
			Map(x => x.Name);
			Map(x => x.CreatedBy);
			Map(x => x.Secret);
			Map(x => x.Type);
			Map(x => x.ManagedBy);
			Map(x => x.InterReview);
			Map(x => x.OnlyManagersEdit);
			Map(x => x.TemplateId);
			Map(x => x.CreateTime);
		}
	}

	public class OrganizationPositionModelMap : SubclassMap<Deprecated.OrganizationPositionModel> {
		public OrganizationPositionModelMap() {
			Table("OrganizationPositionModel");
			Map(x => x.CustomName);
			Map(x => x.CreateTime);
			Map(x => x.CreatedBy);
			Map(x => x.TemplateId);
		}
	}


}

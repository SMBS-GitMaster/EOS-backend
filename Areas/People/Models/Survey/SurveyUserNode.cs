using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities.DataTypes;
using System;
using System.Diagnostics;
using System.Linq;

namespace RadialReview.Areas.People.Models.Survey {
	[DebuggerDisplay("SUN: u:{UsersName} - p: {PositionName} - r:{GetRelationshipString()}")]
	public class SurveyUserNode : ILongIdentifiable, IHistorical, IForModel {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual long AccountabilityNodeId { get; set; }
		public virtual long UserOrganizationId { get; set; }
		public virtual string UsersName { get; set; }
		public virtual string PositionName { get; set; }

		public virtual AccountabilityNode AccountabilityNode { get; set; }
		public virtual UserOrganizationModel User { get; set; }
		public virtual DefaultDictionary<string, AboutType> _Relationship { get; set; }
		//public virtual bool InUse { get; set; }

		public virtual long ModelId { get { return Id; } }
		public virtual string ModelType { get { return ForModel.GetModelType<SurveyUserNode>(); } }
		[JsonProperty(PropertyName = "Hidden")]
		public virtual bool _Hidden { get; set; }

		public virtual bool Is<T>() {
#pragma warning disable CS0618 // Type or member is obsolete
			return ForModel.GetModelType(typeof(T)) == ModelType;
#pragma warning restore CS0618 // Type or member is obsolete
		}

		public static SurveyUserNode FromViewModelKey(string key) {
			var split = key.Split('_');
			if (split.Length != 2)
				throw new Exception("invalid key length");

			var byNodeId = split[0].ToLong();
			var byUserId = split[1].ToLong();

			return new SurveyUserNode() { AccountabilityNodeId = byNodeId, UserOrganizationId = byUserId };
		}

		public virtual string ToViewModelKey() {
			return AccountabilityNodeId + "_" + UserOrganizationId;
		}

		public override string ToString() {
			return ToPrettyString();
		}

		public virtual string GetRelationshipString() {
			if (_Relationship == null)
				return "?";
			var rs = _Relationship.Select(x => x.Value).Distinct();
			if (rs.Count() > 1) {
				rs = rs.Where(x => x != AboutType.NoRelationship);
			}
			return string.Join("|", rs);
		}

		public virtual string ToPrettyString() {
			var p = PositionName;
			if (!String.IsNullOrWhiteSpace(p))
				p = "(" + p + ")";

			return (UsersName + " " + p).Trim();
		}

		//[Todo]
		//public static SurveyUserNode Create(AccountabilityNode node) {
		//	throw new TodoException();/*
		//	return new SurveyUserNode {
		//		AccountabilityNode = node,
		//		AccountabilityNodeId = node.Id,
		//		User = node.User,
		//		UserOrganizationId = node.UserId.Value,
		//		UsersName = node.User.GetName(),
		//		PositionName = node.AccountabilityRolesGroup.NotNull(x => x.Position.GetName())
		//	};*/
		//}

		public SurveyUserNode() {
			CreateTime = DateTime.UtcNow;
			_Relationship = new DefaultDictionary<string, AboutType>(x => AboutType.NoRelationship);
		}

		public class Map : ClassMap<SurveyUserNode> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				//Map(x => x.InUse);
				Map(x => x.UsersName);
				//Map(x => x.Relationship);
				Map(x => x.PositionName);

				Map(x => x.AccountabilityNodeId).Column("AccountabilityNodeId").Index("idx__SurveyUserNode_AccountabilityNodeId");
				References(x => x.AccountabilityNode).Column("AccountabilityNodeId").LazyLoad().ReadOnly();

				Map(x => x.UserOrganizationId).Column("UserOrganizationId").Index("idx__SurveyUserNode_UserId");
				References(x => x.User).Column("UserOrganizationId").LazyLoad().ReadOnly();
			}
		}

		[Todo]
		public virtual void AddRelationship(SurveyUserNode sun, AboutType value) {
			string acKey = null;
			if (sun.AccountabilityNode != null) {
				acKey = sun.AccountabilityNode.ToKey();
			} else if (sun.AccountabilityNodeId != 0) {
				acKey = ForModel.Create<AccountabilityNode>(sun.AccountabilityNodeId).ToKey();
			}
			string userKey = null;
			if (sun.AccountabilityNode != null) {
				userKey = sun.User.ToKey();
			} else if (sun.UserOrganizationId != 0) {
				userKey = ForModel.Create<UserOrganizationModel>(sun.UserOrganizationId).ToKey();
			}

			_Relationship[sun.ToKey()] = _Relationship[sun.ToKey()] | value;
			if (acKey != null)
				_Relationship[acKey] = _Relationship[acKey] | value;
			if (userKey != null)
				_Relationship[userKey] = _Relationship[userKey] | value;

		}

		public virtual SurveyUserNodeTiny GetTiny() {
			return new SurveyUserNodeTiny() {
				Key = ToViewModelKey()
			};
		}

		//public static SurveyUserNode Clone(SurveyUserNode about, bool createNewEntry) {
		//	return new SurveyUserNode() {
		//		AccountabilityNode = about.AccountabilityNode,
		//		AccountabilityNodeId = about.AccountabilityNodeId,
		//		CreateTime = createNewEntry ? DateTime.UtcNow : about.CreateTime,
		//		DeleteTime = about.DeleteTime,
		//		Id = createNewEntry ? 0 : about.Id,
		//		PositionName = about.PositionName,
		//		Relationship = about.Relationship,
		//		User = about.User,
		//		UserOrganizationId = about.UserOrganizationId,
		//		UsersName = about.UsersName,
		//	};

		//}
	}


	public class SurveyUserNodeTiny {
		public string Key { get; set; }
	}

	public class ByAboutSurveyUserNodeTiny {
		public string ByKey { get; set; }
		public string AboutKey { get; set; }
		public AboutType? AboutIsThe { get; set; }
	}



	[DebuggerDisplay("BASUN: by:[ {By} ] about:[ {About} ]")]
	public class ByAboutSurveyUserNode : IByAbout {
		public bool _Hidden { get; set; }

		[Obsolete("Avoid using")]
		public ByAboutSurveyUserNode() {
		}

		public ByAboutSurveyUserNode(SurveyUserNode by, SurveyUserNode about, AboutType? aboutIsThe) {
			About = about;
			By = by;
			if (aboutIsThe != null) {
				AboutIsThe = aboutIsThe;
				about.AddRelationship(by, aboutIsThe.Value);
				by.AddRelationship(about, aboutIsThe.Value.Invert());
			}
		}

		public AboutType? AboutIsThe { get; set; }

		[JsonIgnore()]
		public SurveyUserNode About { get; set; }
		[JsonIgnore]
		public SurveyUserNode By { get; set; }

		public string Key { get { return GetViewModelKey(); } }

		public string GetViewModelKey() {
			return "sun_" + By.ToViewModelKey() + "_" + About.ToViewModelKey();
		}

		public IForModel GetAbout() {
			return About;
		}

		public IForModel GetBy() {
			return By;
		}

		public virtual ByAboutSurveyUserNodeTiny GetTiny() {
			return new ByAboutSurveyUserNodeTiny() {
				AboutIsThe = AboutIsThe,
				AboutKey = About.ToViewModelKey(),
				ByKey = By.ToViewModelKey()
			};
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.DataTypes;

namespace RadialReview.Accessors {
	public class AttachAccessor {
		[Obsolete("broken", true)]
		public static Attach PopulateAttachUnsafe(ISession s, long attachId, AttachType type) {
			var a = new Attach() {
				Id = attachId,
				Type = type,
			};

			switch (type) {

				case AttachType.Invalid:
					return a;
				case AttachType.Position:
					a.Name = s.Get<Deprecated.OrganizationPositionModel>(attachId).NotNull(x => x.CustomName);
					return a;
				case AttachType.Team:
					a.Name = s.Get<OrganizationTeamModel>(attachId).NotNull(x => x.Name);
					return a;
				case AttachType.User:
					a.Name = s.Get<UserOrganizationModel>(attachId).NotNull(x => x.GetName());
					return a;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		[Obsolete("Deprecated")]
		public static void SetTemplateUnsafe(ISession s, Attach attach, long? templateId) {
			var attachId = attach.Id;
			var type = attach.Type;
			switch (type) {
				case AttachType.Invalid:
					return;
				case AttachType.Position: {
						var p = s.Get<Deprecated.OrganizationPositionModel>(attachId);
						p.TemplateId = templateId;
						s.Update(p);
						return;
					}
				case AttachType.Team: {
						var p = s.Get<OrganizationTeamModel>(attachId);
						p.TemplateId = templateId;
						s.Update(p);
						return;
					}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		[Obsolete("broken", true)]
		public static List<long> GetMemberIdsUnsafe(ISession s, Attach attach) {
			var attachId = attach.Id;
			var type = attach.Type;
			switch (type) {
				case AttachType.Invalid:
					return new List<long>();
				case AttachType.Position: {
						return s.QueryOver<PositionDurationModel>().Where(x => x.DeleteTime == null && x.DepricatedPosition.Id == attachId)
							.Select(x => x.UserId)
							.List<long>().ToList();
					}
				case AttachType.Team: {
						return s.QueryOver<TeamDurationModel>().Where(x => x.DeleteTime == null && x.TeamId == attachId)
							.Select(x => x.UserId)
							.List<long>().ToList();
					}
				case AttachType.User: {
						return attachId.AsList();
					}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		[Obsolete("Deprecated")]
		public static long GetOrganizationId_Deprecated(ISession s, Attach attach) {
			var attachId = attach.Id;
			var type = attach.Type;

			switch (type) {
				case AttachType.Invalid:
					throw new ArgumentOutOfRangeException("type");
				case AttachType.Position: {
						var p = s.Get<Deprecated.OrganizationPositionModel>(attachId);
						return p.Organization.Id;
				}
				case AttachType.Team: {
						var p = s.Get<OrganizationTeamModel>(attachId);
						return p.Organization.Id;
				}
				case AttachType.User: {
						var p = s.Get<UserOrganizationModel>(attachId);
						return p.Organization.Id;
				}
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

	}
}
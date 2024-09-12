using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.Permissions.Resources {
	public class L10RecurrenceResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.L10Recurrence;
		}

		public long? GetCreator(ISession session, long resourceId) {
			return session.Get<L10Recurrence>(resourceId).CreatedById;
		}

		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			return session.QueryOver<L10Recurrence>()
									.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
									.Select(x => x.Id)
									.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return session.QueryOver<L10Recurrence>()
						.Where(x => x.CreatedById == userId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			return session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
										.Where(x => x.User.Id == userId && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id)
										.Future<long>().Distinct();
		}

		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			var isMember_idsQ = session.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
						.Where(x => x.L10Recurrence.Id == resourceId && x.DeleteTime == null);
			if (meOnly)
				isMember_idsQ = isMember_idsQ.Where(x => x.User.Id == callerId);
			var isMember_ids = isMember_idsQ.Select(x => x.User.Id).List<long>().ToList();
			return isMember_ids;
		}

		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			var recur = s.Get<L10Recurrence>(resourceId);
			return new ResourceMetaData() {
				Name = recur.Name,
				Picture = PictureViewModel.CreateFromInitials("Weekly Meeting", recur.Name),
			};
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return session.Get<L10Recurrence>(resourceId).OrganizationId;
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<L10Recurrence>(resourceId).DeleteTime != null;
		}
	}
}
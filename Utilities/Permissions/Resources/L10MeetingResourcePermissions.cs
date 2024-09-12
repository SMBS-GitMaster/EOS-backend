using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.Permissions.Resources {
	public class L10MeetingResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.L10Meeting;
		}

		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			var isMember_idsQ = session.QueryOver<L10Meeting.L10Meeting_Attendee>()
						.Where(x => x.L10Meeting.Id == resourceId && x.DeleteTime == null);
			if (meOnly)
				isMember_idsQ = isMember_idsQ.Where(x => x.User.Id == callerId);
			var isMember_ids = isMember_idsQ.Select(x => x.User.Id).List<long>().ToList();
			return isMember_ids;
		}


		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			var meeting = s.Get<L10Meeting>(resourceId);
			return new ResourceMetaData() {
				Name = meeting.L10Recurrence.Name,
				Picture = PictureViewModel.CreateFromInitials("Weekly Meeting", meeting.L10Recurrence.Name),
			};
		}

		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<L10Meeting>(resourceId).DeleteTime != null;
		}

		#region unimplemented
		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public long? GetCreator(ISession session, long resourceId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		#endregion
	}
}
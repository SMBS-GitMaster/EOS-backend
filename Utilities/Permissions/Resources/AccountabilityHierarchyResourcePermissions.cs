using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.Permissions.Resources {
	public class AccountabilityHierarchyResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.AccountabilityHierarchy;
		}
		public long? GetCreator(ISession session, long resourceId) {
			return null;
		}
		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			return new long[] {
				session.Get<OrganizationModel>(orgId).AccountabilityChartId
			};
		}
		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return new long[] { };
		}
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			return new long[] { session.Get<UserOrganizationModel>(userId).Organization.AccountabilityChartId };
		}

		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			var ac = session.Get<AccountabilityChart>(resourceId);
			var isMember_idsQ = session.QueryOver<UserOrganizationModel>()
				.Where(x => x.Organization.Id == ac.OrganizationId && x.DeleteTime == null);
			if (meOnly)
				isMember_idsQ = isMember_idsQ.Where(x => x.Id == callerId);
			var isMember_ids = isMember_idsQ.Select(x => x.Id).List<long>().ToList();
			return isMember_ids;
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return session.Get<AccountabilityChart>(resourceId).OrganizationId;
		}

		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			return new ResourceMetaData() {
				Name = "Organizational Chart",
				Picture = PictureViewModel.CreateFromInitials("OC", "Organizational Chart"),
			};
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<AccountabilityChart>(resourceId).DeleteTime != null;
		}

		#region unimplemented
		#endregion
	}
}
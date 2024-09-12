using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Process;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace RadialReview.Utilities.Permissions.Resources {
	public class ProcessFolderResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.ProcessFolder;
		}

		public long? GetCreator(ISession session, long resourceId) {
			return session.Get<ProcessFolder>(resourceId).CreatorId;
		}

		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			return session.QueryOver<ProcessFolder>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return session.QueryOver<ProcessFolder>()
						.Where(x => x.CreatorId == userId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return session.Get<ProcessFolder>(resourceId).OrgId;
		}

		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			var folder = s.Get<ProcessFolder>(resourceId);
			return new ResourceMetaData() {
				Name = folder.Name,
				Picture = PictureViewModel.CreateFromInitials("F", folder.Name),
			};
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<ProcessFolder>(resourceId).DeleteTime != null;
		}

		#region unimplemented
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		#endregion
	}
}
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace RadialReview.Utilities.Permissions.Resources {
	public class WhiteboardResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.Whiteboard;
		}

		public long? GetCreator(ISession session, long resourceId) {
			return session.Get<WhiteboardModel>(resourceId).CreatedBy;
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return session.QueryOver<WhiteboardModel>()
						.Where(x => x.CreatedBy == userId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return session.Get<WhiteboardModel>(resourceId).OrgId;
		}

		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			var wb = s.Get<WhiteboardModel>(resourceId);

			return new ResourceMetaData() {
				Name = wb.Name ?? "Whiteboard",
				Picture = PictureViewModel.CreateFromInitials("WB", wb.Name ?? "Whiteboard"),
			};
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<WhiteboardModel>(resourceId).DeleteTime != null;
		}


		#region unimplemented
		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		#endregion
	}
}
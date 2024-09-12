using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Documents;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace RadialReview.Utilities.Permissions.Resources {
	public class DocumentsFolderResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.DocumentsFolder;
		}

		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			var folder = s.Get<DocumentsFolder>(resourceId);
			return new ResourceMetaData() {
				Name = folder.Name,
				Picture = new PictureViewModel() {
					Url = "/content/documents/icons/folder.svg",
					Title = folder.Name,
				}
			};
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<DocumentsFolder>(resourceId).DeleteTime != null;
		}

		#region unimplemented
		public long? GetCreator(ISession session, long resourceId) {
			return session.Get<DocumentsFolder>(resourceId).CreatorId;
		}

		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return session.QueryOver<DocumentsFolder>()
					.Where(x => x.DeleteTime == null && x.CreatorId == userId)
					.Select(x => x.Id)
					.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}

		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			throw new ArgumentOutOfRangeException("resourceType");
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return session.Get<DocumentsFolder>(resourceId).OrgId;
		}
		#endregion
	}
}
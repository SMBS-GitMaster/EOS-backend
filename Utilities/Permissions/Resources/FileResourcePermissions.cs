using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Downloads;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace RadialReview.Utilities.Permissions.Resources {
	public class FileResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.File;
		}

		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<EncryptedFileModel>(resourceId).DeleteTime != null && session.Get<EncryptedFileModel>(resourceId).DeleteTime < DateTime.UtcNow;
		}

		public long? GetCreator(ISession session, long resourceId) {
			return session.Get<EncryptedFileModel>(resourceId).CreatorId;
		}

		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			return session.QueryOver<EncryptedFileModel>()
				.Where(x => x.OrgId == orgId && x.DeleteTime == null)
				.Select(x => x.Id)
				.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return session.QueryOver<EncryptedFileModel>()
				.Where(x => x.CreatorId == userId && x.DeleteTime == null)
				.Select(x => x.Id)
				.Future<long>();
		}


		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			var file = session.Get<EncryptedFileModel>(resourceId);
			var forModel = file.ParentModel;
			if (forModel == null) {
				return new List<long>();
			}
			var parentResource = forModel.GetResourceType();
			var resourcePermissions = PermissionsUtility.GetResourcePermissionsForType(parentResource);
			return resourcePermissions.GetMembersOfResource(session, forModel.ModelId, callerId, meOnly);
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}


		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			var file = s.Get<EncryptedFileModel>(resourceId);
			return new ResourceMetaData() {
				Name = file.FileName,
				Picture = PictureViewModel.CreateFromInitials("F", file.FileName),
			};
		}

		#region unimplemented
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		#endregion
	}
}

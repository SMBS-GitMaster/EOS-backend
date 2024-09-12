using NHibernate;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;


namespace RadialReview.Utilities.Permissions.Resources {
	public class EditDeleteUserDataForOrganizationResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.EditDeleteUserDataForOrganization;
		}

		public long? GetCreator(ISession session, long resourceId) {
			return null;
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return new long[] { };
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return resourceId;
		}


		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			return new ResourceMetaData() {
				Name = "",
				Picture = PictureViewModel.CreateFromInitials("", "EditDeleteUser"),
			};
		}

		public bool AccessDisabled(ISession session, long resourceId) {
			return false;
		}

		#region unimplemented
		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		#endregion
	}
}
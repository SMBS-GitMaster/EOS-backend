using NHibernate;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;

namespace RadialReview.Utilities.Permissions.Resources {
	public class UpdatePaymentForOrganizationResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.UpdatePaymentForOrganization;
		}
		public long? GetCreator(ISession session, long resourceId) {
			return null;
		}
		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			return new[] { session.Get<UserOrganizationModel>(userId).Organization.Id };
		}
		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return resourceId;
		}

		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			return new ResourceMetaData() {
				Name = "",
				Picture = PictureViewModel.CreateFromInitials("", "UpdatePayment"),
			};
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return false;
		}

		#region unimplemented
		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			throw new ArgumentOutOfRangeException("resourceType");
		}
		#endregion
	}
}
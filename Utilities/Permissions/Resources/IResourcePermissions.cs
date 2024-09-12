using NHibernate;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities.Permissions.Resources;
using System.Collections.Generic;

namespace RadialReview.Utilities.Permissions.Resources
{

    public class ResourceMetaData {
		public string Name { get; set; }
		public PictureViewModel Picture { get; set; }
	}

	public interface IResourcePermissions {

		bool AccessDisabled(ISession session, long resourceId);

		PermItem.ResourceType ForResourceType();

		long GetOrganizationIdForResource(ISession session,long resourceId);
		IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId);
		long? GetCreator(ISession session, long resourceId);
		IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId);
		IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId);
		IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly);

		ResourceMetaData GetMetaData(ISession s, long resourceId);

	}


}

namespace RadialReview
{
    public static class IResourcePermissionsExtensions {
		public static string GetName(this IResourcePermissions self, ISession s, long resourceId) {
			return self.GetMetaData(s, resourceId).NotNull(x => x.Name);
		}
	}
}
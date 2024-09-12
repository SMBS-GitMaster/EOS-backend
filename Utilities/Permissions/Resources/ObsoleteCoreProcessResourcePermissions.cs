using NHibernate;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using RadialReview.Areas.CoreProcess.Models.MapModel;

namespace RadialReview.Utilities.Permissions.Resources {
	public class CoreProcessResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.CoreProcess;
		}

		public long? GetCreator(ISession session, long resourceId) {
			var r = session.Get<ProcessDef_Camunda>(resourceId).Creator;
			if (r.ModelType == ForModel.GetModelType<UserOrganizationModel>())
				return r.ModelId;
			return null;
		}

		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			return session.QueryOver<ProcessDef_Camunda>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return session.QueryOver<ProcessDef_Camunda>()
						.Where(x => x.Creator.ModelType == ForModel.GetModelType<UserOrganizationModel>() && x.Creator.ModelId == userId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}


		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			return new List<long>();
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return session.Get<ProcessDef_Camunda>(resourceId).OrgId;
		}


		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			return new ResourceMetaData() {
				Name = "Core Process",
				Picture = PictureViewModel.CreateFromInitials("P", "Core Process"),
			};
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<ProcessDef_Camunda>(resourceId).DeleteTime != null;
		}

		#region unimplemented
		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			throw new NotImplementedException();
		}

		#endregion
	}
}
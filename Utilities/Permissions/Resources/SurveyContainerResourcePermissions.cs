using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.Permissions.Resources {
	public class SurveyContainerResourcePermissions : IResourcePermissions {

		public PermItem.ResourceType ForResourceType() {
			return PermItem.ResourceType.SurveyContainer;
		}

		public long? GetCreator(ISession session, long resourceId) {
			var creator = session.Get<SurveyContainer>(resourceId).CreatedBy;
			if (creator.Is<UserOrganizationModel>())
				return creator.ModelId;
			return null;
		}

		public IEnumerable<long> GetIdsForResourceForOrganization(ISession session, long orgId) {
			return session.QueryOver<SurveyContainer>()
						.Where(x => x.OrgId == orgId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourcesCreatedByUser(ISession session, long userId) {
			return session.QueryOver<SurveyContainer>()
						.Where(x => x.CreatedBy.ModelType == ForModel.GetModelType<UserOrganizationModel>() && x.CreatedBy.ModelId == userId && x.DeleteTime == null)
						.Select(x => x.Id)
						.Future<long>();
		}

		public IEnumerable<long> GetIdsForResourceThatUserIsMemberOf(ISession session, long userId) {
			return session.QueryOver<Survey>()
							.Where(x => x.By.ModelType == ForModel.GetModelType<UserOrganizationModel>() && x.By.ModelId == userId && x.DeleteTime == null)
							.Select(x => x.SurveyContainerId)
							.Future<long>().Distinct();
		}

		public IEnumerable<long> GetMembersOfResource(ISession session, long resourceId, long callerId, bool meOnly) {
			var isMember_idsQ = session.QueryOver<Survey>()
				.Where(x => x.SurveyContainerId == resourceId && x.DeleteTime == null)
				.Where(x => x.By.ModelType == ForModel.GetModelType<UserOrganizationModel>());
			if (meOnly)
				isMember_idsQ = isMember_idsQ.Where(x => x.By.ModelId == callerId);
			var isMember_ids = isMember_idsQ.Select(x => x.By.ModelId).List<long>().ToList();
			return isMember_ids;
		}

		public long GetOrganizationIdForResource(ISession session, long resourceId) {
			return session.Get<SurveyContainer>(resourceId).OrgId;
		}

		public ResourceMetaData GetMetaData(ISession s, long resourceId) {
			var survey = s.Get<SurveyContainer>(resourceId);
			return new ResourceMetaData() {
				Name = survey.Name,
				Picture = PictureViewModel.CreateFromInitials("QC", survey.Name),
			};
		}
		public bool AccessDisabled(ISession session, long resourceId) {
			return session.Get<SurveyContainer>(resourceId).DeleteTime != null;
		}

		#region unimplemented
		#endregion
	}
}
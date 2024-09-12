using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Utilities.Query;
using System;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor
    {
        [Obsolete("broken", true)]
        private static IEnumerableQuery GetReviewQueryProvider_Deprecated(ISession s, long orgId, long? reviewContainerId = null)
        {
            var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
            var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
            var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
            var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
            var allPositions = s.QueryOver<PositionDurationModel>().Where(x => x.OrganizationId == orgId).List();
            var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
            var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();
			var allRoleLinks = s.QueryOver<RoleLink_Deprecated>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).List();

			var queryProvider = new IEnumerableQuery(true);
            queryProvider.AddData(allOrgTeams);
            queryProvider.AddData(allTeamDurations);
            queryProvider.AddData(allMembers);
            queryProvider.AddData(allManagerSubordinates);
            queryProvider.AddData(allPositions);
			queryProvider.AddData(allRoleLinks);
			queryProvider.AddData(applicationQuestions);
			queryProvider.AddData(application);
            if (reviewContainerId != null)
            {
                var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId.Value).List();
                queryProvider.AddData(reviews);
            }

            return queryProvider;
        }

    }
}

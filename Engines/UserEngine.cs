using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System.Linq;
using RadialReview.Utilities;

namespace RadialReview.Engines {
	public class UserEngine {

		public static UserOrganizationDetails GetUserDetails(UserOrganizationModel caller, long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var foundUser = UserAccessor.GetUserOrganization(s, perms, id, false, false);

					foundUser.SetPersonallyManaging(DeepAccessor.Users.ManagesUser(s, perms, caller.Id, id, true));

					//var responsibilities = new List<String>();
					//var raaa = _ResponsibilitiesAccessor.GetResponsibilityGroup(s, perms, id);
					//var teams = TeamAccessor.GetUsersTeams(s.ToQueryProvider(true), perms, id);
					//var userResponsibility = ((UserOrganizationModel)r).Hydrate(s).Position().SetTeams(teams).Execute();

					/*responsibilities.AddRange(userResponsibility.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
					foreach (var rgId in userResponsibility.Positions.ToListAlive().Select(x => x.DepricatedPosition.Id)) {
						try {
							var positionResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(s, perms, rgId);
							responsibilities.AddRange(positionResp.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
						} catch (PermissionsException) {
							//hmm
						}
					}
					foreach (var teamId in userResponsibility.Teams.ToListAlive().Select(x => x.Team.Id)) {
						try {
							var teamResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(s, perms, teamId);
							responsibilities.AddRange(teamResp.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
						} catch (PermissionsException) {
							//hmm
						}
					}*/



					var seats = AccountabilityAccessor.GetSeatsForUser(s, perms, id).ToList();
					var model = new UserOrganizationDetails() {
						SelfId = caller.Id,
						User = foundUser,
						Seats = seats.ToList(),
						ManagingOrganization = caller.ManagingOrganization,
					};

					if (perms.IsPermitted(x => x.CanViewUserRocks(id))) {
						model.Rocks = RockAccessor.GetAllRocks_AvoidUsing(s, perms, id).Select(x => new EditRockViewModel() {
							AccountableUser = x.AccountableUser.Id,
							Id = x.Id,
							Title = x.Name,
							RecurrenceIds = RockAccessor.GetRecurrencesContainingRock(s, perms, x.Id).Select(y => y.RecurrenceId).ToArray()

						}).ToList();
						model.CanViewRocks = true;
					}

					if (perms.IsPermitted(x => x.CanViewUserMeasurables(id))) {
						var measurables = ScorecardAccessor.GetUserMeasurables(s, perms, id, true, false, includeAdmin: true);
						model.Measurables = measurables.Where(x => x.AccountableUserId == id).ToList();
						model.AdminMeasurables = measurables.Where(x => x.AdminUserId == id && x.AccountableUserId != id).ToList();
						model.CanViewMeasurables = true;
					}

					if (perms.IsPermitted(x => x.EditUserDetails(id))) {
						model.CanEditUserDetails = true;
					}
					//foundUser.PopulatePersonallyManaging(caller, caller.AllSubordinates);



					return model;
				}
			}

		}

	}
}
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Models.Accountability;
using RadialReview.Models.Angular.Accountability;
using EmailStrings = RadialReview.Core.Properties.EmailStrings;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {

		#region Create


		[Obsolete("Fix for AC")]
		public static List<Mail> CreateReviewFromPrereview(
			ISession s, DataInteraction dataInteraction,
			PermissionsUtility perms, UserOrganizationModel caller,
			ReviewsModel reviewContainer, List<WhoReviewsWho> whoReviewsWho,
			String userId = null,
			int total = 0) {
			int count = 0;

			if (reviewContainer.Executed != null)
				throw new PermissionsException("Eval already executed");

			var unsentEmails = new List<Mail>();
			var nw = DateTime.UtcNow;
			var range = new DateRange(nw, nw);
			var reviewersWRW = whoReviewsWho.GroupBy(x => x.Reviewer).ToList();
			var reviewers = whoReviewsWho.Select(x => x.Reviewer).Distinct().ToList();

			foreach (var reviewerWRW in reviewersWRW) {
				//Create review for user
				var reviewer = reviewerWRW.Key;
				var reviewees = reviewerWRW.Select(x => x.Reviewee).ToList();
				var reviewerUser = dataInteraction.Get<UserOrganizationModel>(reviewer.RGMId);
				if (reviewerUser == null || reviewerUser.DeleteTime != null)
					continue;

				var askables = GetAskables(s, dataInteraction, perms, reviewer, reviewees, range);
				if (askables.Any()) {
					QuestionAccessor.GenerateReviewForUser(dataInteraction, perms, reviewerUser, reviewContainer, askables);

					//Emails
					var guid = Guid.NewGuid();
					var nexus = new NexusModel(guid) { ForUserId = reviewerUser.Id, ActionCode = NexusActions.TakeReview };
					nexus.SetArgs("" + reviewContainer.Id);
					NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);
					var org = reviewContainer.Organization;
					var email = ConstructNewReviewEmail(reviewContainer, reviewerUser, guid, org);
					unsentEmails.Add(email);

					log.Info("CreateReview user=" + reviewer.RGMId + " for review=" + reviewContainer.Id);
				} else {
					log.Info("NO ASKABLES, Skipping CreateReview user=" + reviewer.RGMId + " review=" + reviewContainer.Id);
				}
			}

			reviewContainer.Executed = DateTime.UtcNow;
			dataInteraction.Merge(reviewContainer);
			var haventGeneratedAReview = new Func<long, bool>(revieweeId => !reviewers.Any(reviewer => reviewer.RGMId == revieweeId));
			foreach (var revieweeId in whoReviewsWho.Select(x => x.Reviewee.RGMId).Distinct().Where(haventGeneratedAReview)) {
				try {
					var user = dataInteraction.Get<UserOrganizationModel>(revieweeId);
					if (user != null) {
						QuestionAccessor.GenerateReviewForUser(dataInteraction, perms, user, reviewContainer, new AskableCollection());
					}
				} catch (Exception e) {
					log.Error("Error in creating review from prereview", e);
				}
			}

			return unsentEmails;
		}

		private static Mail ConstructNewReviewEmail(ReviewsModel reviewContainer, UserOrganizationModel reviewerUser, Guid nexusGuid, OrganizationModel org) {
			var format = org.NotNull(y => y.Settings.NotNull(z => z.GetDateFormat())) ?? "MM-dd-yyyy";
			var dueDate = (reviewContainer.DueDate.AddDays(-1)).ToString(format);
			var url = Config.BaseUrl(org) + "n/" + nexusGuid;
			var productName = Config.ProductName(org);
			var orgName = org.GetName();
			var reviewName = reviewContainer.ReviewName;
			var usersName = reviewerUser.GetName();
			return Mail.To(EmailTypes.NewReviewIssued, reviewerUser.GetEmail())
					   .Subject(EmailStrings.NewReview_Subject, orgName)
					   .Body(EmailStrings.NewReview_Body, usersName, orgName, dueDate, url, url, productName, reviewName);
		}

		[Obsolete("broken", true)]
		public static async Task<ResultObject> CreateReviewFromCustom_Deprecated(
			UserOrganizationModel caller,
			long forTeamId, DateTime dueDate, String reviewName, bool emails, bool anonFeedback, List<WhoReviewsWho> whoReviewsWho) {
			var unsentEmails = new List<Mail>();
			ReviewsModel reviewContainer;
			using (var s = HibernateSession.GetCurrentSession(singleSession: false)) {
				var userId = caller.User.UserName;

				using (var tx = s.BeginTransaction()) {

					var perms = PermissionsUtility.Create(s, caller);

					bool reviewManagers = true,
						 reviewPeers = true,
						 reviewSelf = true,
						 reviewSubordinates = true,
						 reviewTeammates = true;

					reviewContainer = new ReviewsModel() {
						AnonymousByDefault = anonFeedback,
						DateCreated = DateTime.UtcNow,
						DueDate = dueDate,
						ReviewName = reviewName,
						CreatedById = caller.Id,
						HasPrereview = false,

						ReviewManagers = reviewManagers,
						ReviewPeers = reviewPeers,
						ReviewSelf = reviewSelf,
						ReviewSubordinates = reviewSubordinates,
						ReviewTeammates = reviewTeammates,


						ForTeamId = forTeamId
					};
					ReviewAccessor.CreateReviewContainer(s, perms, caller, reviewContainer);
				}
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					perms.IssueForTeam(forTeamId);

					var organization = caller.Organization;
					OrganizationTeamModel team;

					var orgId = caller.Organization.Id;



					var dataInteraction = GetReviewDataInteraction_Deprecated(s, orgId);

					team = dataInteraction.GetQueryProvider().All<OrganizationTeamModel>().First(x => x.Id == forTeamId);
					var usersToReview = TeamAccessor.GetTeamMembers(dataInteraction.GetQueryProvider(), perms, forTeamId, false).ToListAlive();

					List<Exception> exceptions = new List<Exception>();
					var toReview = usersToReview.Select(x => x.User).ToList();

					////////////////////////////////////////////
					//HEAVY LIFTING HERE:
					var clientReviews = CreateReviewFromPrereview(s, dataInteraction, perms, caller, reviewContainer, whoReviewsWho, userId, usersToReview.Count());
					unsentEmails.AddRange(clientReviews);


					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await EventUtil.Trigger(x => x.Create(s, EventType.IssueReview, caller, reviewContainer, message: reviewContainer.ReviewName));
					tx.Commit();
					s.Flush();
				}
			}

			var emailResult = new EmailResult();
			if (emails) {
				emailResult = await Emailer.SendEmails(unsentEmails);
			}
			if (emailResult.Errors.Count() > 0) {
				var message = String.Join("\n", emailResult.Errors.Select(x => x.Message));
				return new ResultObject(new RedirectException(emailResult.Errors.Count() + " errors:\n" + message));
			}
			return ResultObject.Create(new { due = dueDate, sent = emailResult.Sent, errors = emailResult.Errors.Count() });
		}

		[Obsolete("broken", true)]
		public static DataInteraction GetReviewDataInteraction_Deprecated(ISession s, long orgId) {
			var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).Future();
			var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).Future();
			var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).Future();
			var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).Future();
			var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.DepricatedPosition).Where(x => x.Organization.Id == orgId).Future();
			var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).Future();
			var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).Future();

			var allRoles = s.QueryOver<RoleModel_Deprecated>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
			var allValues = s.QueryOver<CompanyValueModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
			var allRocks = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
			var allRGM = s.QueryOver<ResponsibilityGroupModel>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).Future();
			var allAboutCompany = s.QueryOver<AboutCompanyAskable>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).Future();

			var allRoleLinks = s.QueryOver<RoleLink_Deprecated>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
			var accountablityNodes = s.QueryOver<AccountabilityNode>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();

			var queryProvider = new IEnumerableQuery();
			queryProvider.AddData(allOrgTeams.ToList());
			queryProvider.AddData(allTeamDurations);
			queryProvider.AddData(allMembers);
			queryProvider.AddData(allManagerSubordinates);
			queryProvider.AddData(allPositions);
			queryProvider.AddData(allRoles);
			queryProvider.AddData(allValues);
			queryProvider.AddData(allRocks);
			queryProvider.AddData(allAboutCompany);
			queryProvider.AddData(allRGM);
			queryProvider.AddData(allRoleLinks);
			queryProvider.AddData(applicationQuestions);
			queryProvider.AddData(accountablityNodes);
			queryProvider.AddData(application);

			var updateProvider = new SessionUpdate(s);
			var dataInteraction = new DataInteraction(queryProvider, updateProvider);
			return dataInteraction;
		}

		[Obsolete("Fix for AC")]
		public static void CreateReviewContainer(ISession s, PermissionsUtility perms, UserOrganizationModel caller, ReviewsModel reviewContainer) {
			using (var tx = s.BeginTransaction()) {
				perms.ManagerAtOrganization(caller.Id, caller.Organization.Id);

				reviewContainer.CreatedById = caller.Id;
				reviewContainer.OrganizationId = caller.Organization.Id;
				reviewContainer.Organization = caller.Organization;

				s.SaveOrUpdate(reviewContainer);
				tx.Commit();
				s.Flush(); //ADDED
			}
		}


		[Obsolete("Fix for AC")]
		private static List<Mail> AddUserToReview(
			UserOrganizationModel caller,
			ISession s, bool updateOthers,
			DateTime dueDate, ReviewParameters parameters,
			DataInteraction dataInteraction, ReviewsModel reviewContainer, PermissionsUtility perms, OrganizationModel organization,
			OrganizationTeamModel team, ref List<Exception> exceptions, Reviewee user,
			AngularAccountabilityChart tree, List<Reviewee> accessibleUsers,
			DateRange range) {
			var unsentEmails = new List<Mail>();
			var format = caller.NotNull(x => x.Organization.NotNull(y => y.Settings.NotNull(z => z.GetDateFormat()))) ?? "MM-dd-yyyy";
			try {
				AskableCollection addToOthers = null;
				var askables = GetAskablesBidirectional(s, dataInteraction, perms, tree, user, team, parameters, accessibleUsers, range, updateOthers, ref addToOthers);
				var revieweeUser = dataInteraction.Get<UserOrganizationModel>(user.ConvertToReviewer().RGMId);
				//Create the Review
				if (askables.Askables.Any()) {
					var review = QuestionAccessor.GenerateReviewForUser(dataInteraction, perms, revieweeUser, reviewContainer, askables);
					//Generate Review Nexus
					var guid = Guid.NewGuid();
					var nexus = new NexusModel(guid) {
						ForUserId = user.ConvertToReviewer().RGMId,
						ActionCode = NexusActions.TakeReview
					};
					NexusAccessor.Put(dataInteraction.GetUpdateProvider(), nexus);
					var url = Config.BaseUrl(organization) + "n/" + guid;
					unsentEmails.Add(Mail
						.To(EmailTypes.NewReviewIssued, revieweeUser.GetEmail())
						.Subject(EmailStrings.NewReview_Subject, organization.Name)
						.Body(EmailStrings.NewReview_Body, revieweeUser.GetName(), caller.GetName(), (dueDate.AddDays(-1)).ToString(format), url, url, ProductStrings.ProductName, reviewContainer.ReviewName)
					);
				}

				//Update everyone else's review.
				if (updateOthers) {
					var reviewsLookup = dataInteraction.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainer.Id && x.DeleteTime == null);
					var newReviewers = addToOthers.Reviewers;

					foreach (var reviewer in newReviewers) {
						try {
							var r = reviewsLookup.Where(x => x.ReviewerUserId == reviewer.RGMId).SingleOrDefault();
							if (r != null) {
								var revieweeIsThe = addToOthers.RevieweeIsThe[reviewer];
								AddToReview(s, dataInteraction, perms, caller, reviewer, r.ForReviewContainerId, user, revieweeIsThe);
							}
							var revieweeReview = reviewsLookup.Where(x => x.ReviewerUserId == user.RGMId).SingleOrDefault();
							if (revieweeReview == null) {
								var u = dataInteraction.Get<UserOrganizationModel>(user.RGMId);
								QuestionAccessor.GenerateReviewForUser(dataInteraction, perms, u, reviewContainer, new AskableCollection());
							}
						} catch (Exception e) {
							log.Error(e.Message, e);
							exceptions.Add(e);
						}
					}
				}

			} catch (Exception e) {
				log.Error(e.Message, e);
				exceptions.Add(e);
			}
			return unsentEmails;
		}

		#endregion
	}
}

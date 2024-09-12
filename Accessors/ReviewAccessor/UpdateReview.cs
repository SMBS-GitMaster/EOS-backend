using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Prereview;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Models.Accountability;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		#region Edit Review
		public static void AddAskablesToReview(DataInteraction s, PermissionsUtility perms, Reviewer reviewer, ReviewModel reviewModel, bool anonymous, AskableCollection askables) {
			foreach (var q in askables) {
				switch (q.Askable.GetQuestionType()) {
					case QuestionType.Slider:
						GenerateSliderAnswers(s, reviewer, q, reviewModel, anonymous);
						break;
					case QuestionType.Thumbs:
						GenerateThumbsAnswers(s, reviewer, q, reviewModel, anonymous);
						break;
					case QuestionType.Feedback:
						GenerateFeedbackAnswers(s, reviewer, q, reviewModel, anonymous);
						break;
					case QuestionType.GWC:
						GenerateGWCAnswers(s, reviewer, q, reviewModel, anonymous);
						break;
					case QuestionType.Rock:
						GenerateRockAnswers(s, reviewer, q, reviewModel, anonymous);
						break;
					case QuestionType.CompanyValue:
						GenerateCompanyValuesAnswer(s, reviewer, q, reviewModel, anonymous);
						break;
					case QuestionType.Radio:
						GenerateRadioAnswer(s, reviewer, q, reviewModel, anonymous);
						break;
					default:
						throw new ArgumentException("Unrecognized questionType(" + q.Askable.GetQuestionType() + ")");
				}
			}

			if (reviewModel.QuestionCompletion == null) {
				reviewModel.QuestionCompletion = new Models.Components.Completion();
			}

			reviewModel.QuestionCompletion.NumRequired += askables.Count(x => x.Askable.Required);
			reviewModel.QuestionCompletion.NumOptional += askables.Count(x => !x.Askable.Required);
			s.Merge(reviewModel);
		}

		private static void DeleteAnswers_Unsafe(ISession s, IEnumerable<AnswerModel> answers, DateTime now) {
			var toDelete = answers.Where(x => x.DeleteTime == null).ToList();
			foreach (var answer in toDelete) {
				answer.DeleteTime = now;
				s.Update(answer);
			}

			var reviewIds = toDelete.Select(x => x.ForReviewId).Distinct().ToList();
			var reviews = s.QueryOver<ReviewModel>().WhereRestrictionOn(x => x.Id).IsIn(reviewIds).List().ToList();
			foreach (var r in reviews) {
				r.QuestionCompletion.NumRequired -= toDelete.Count(x => x.Required && x.ForReviewId == r.Id);
				r.QuestionCompletion.NumOptional -= toDelete.Count(x => !x.Required && x.ForReviewId == r.Id);
				r.QuestionCompletion.NumRequiredComplete -= toDelete.Count(x => x.Required && x.Complete && x.ForReviewId == r.Id);
				r.QuestionCompletion.NumOptionalComplete -= toDelete.Count(x => !x.Required && x.Complete && x.ForReviewId == r.Id);
				s.Update(r);
			}
		}

		[Obsolete("Fix for AC")]
		public static void AddToReview(UserOrganizationModel caller, Reviewer reviewer, long reviewContainerId, Reviewee reviewee) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddToReview(s, s.ToDataInteraction(true), perms, caller, reviewer, reviewContainerId, reviewee, AboutType.NoRelationship);
					tx.Commit();
					s.Flush();
				}
			}
		}

		[Obsolete("Fix for AC")]
		private static void AddToReview(ISession session, DataInteraction s, PermissionsUtility perms, UserOrganizationModel caller, Reviewer reviewer, long reviewContainerId, Reviewee reviewee, AboutType aboutType) {
			//TODO Fix permissions. Should make sure we can edit the review
			perms.ViewUserOrganization(reviewer.RGMId, false).ViewReviews(reviewContainerId, false);
			var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
			var reviews = s.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainerId && x.ReviewerUserId == reviewer.RGMId).ToList();
			if (!reviews.Any())
				throw new InvalidOperationException("Review does not exist.");
			foreach (var review in reviews) {
				perms.ViewUserOrganization(review.ReviewerUserId, false).ManageUserReview(review.Id, true);
				var range = new DateRange(reviewContainer.DateCreated, DateTime.UtcNow);
				var askables = ReviewAccessor.GetAskables(session, s, perms, reviewer, new[] { reviewee }, range);
				var forUser = s.Get<UserOrganizationModel>(review.ReviewerUserId);
				AddAskablesToReview(s, perms, new Reviewer(review.ReviewerUserId), review, reviewContainer.AnonymousByDefault, askables);
			}

			var revieweeReview = reviews.Where(x => x.ReviewerUserId == reviewee.RGMId).SingleOrDefault();
			if (revieweeReview == null) {
				var u = s.Get<UserOrganizationModel>(reviewee.RGMId);
				if (u != null) {
					QuestionAccessor.GenerateReviewForUser(s, perms, u, reviewContainer, new AskableCollection());
				}
			}
		}

		public static void RemoveFromReview(UserOrganizationModel caller, Reviewer reviewer, long reviewContainerId, Reviewee reviewee) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					RemoveFromReview(s, perms, caller, reviewer, reviewContainerId, reviewee);
					tx.Commit();
					s.Flush();
				}
			}
		}

		private static void RemoveFromReview(ISession s, PermissionsUtility perms, UserOrganizationModel caller, Reviewer reviewer, long reviewContainerId, Reviewee reviewee) {
			//TODO Fix permissions. Should make sure we can edit the review
			perms.ViewUserOrganization(reviewer.RGMId, false).ViewReviews(reviewContainerId, false);
			var revieww = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.ReviewerUserId == reviewer.RGMId).List().ToList();
			foreach (var review in revieww) {
				perms.ViewUserOrganization(review.ReviewerUserId, false);
				perms.ManageUserReview(review.Id, false);
				var q = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.ReviewerUserId == reviewer.RGMId && x.RevieweeUserId == reviewee.RGMId);
				if (reviewee.ACNodeId != null) {
					q = q.Where(x => x.RevieweeUser_AcNodeId == null || x.RevieweeUser_AcNodeId == reviewee.ACNodeId.Value);
				}

				var ans = q.List();
				var now = DateTime.UtcNow;
				DeleteAnswers_Unsafe(s, ans, now);
			}
		}

		public static void RemoveQuestionFromReviewForUser(UserOrganizationModel caller, long reviewContainerId, long userId, long askableId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var answers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.Askable.Id == askableId && x.RevieweeUserId == userId && x.DeleteTime == null).List();
					var deleteTime = DateTime.UtcNow;
					DeleteAnswers_Unsafe(s, answers, deleteTime);
					tx.Commit();
					s.Flush();
				}
			}
		}

		[Obsolete("broken", true)]
		public static void AddResponsibilityAboutUsersToReview_Deprecated(ISession s, PermissionsUtility perms, long reviewContainerId, IEnumerable<Reviewee> reviewees, long askableId) {
			DateRange range = null; //Does this need to be set?
			if (!reviewees.Any())
				return; //Nothing to do.
			var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
			var orgId = reviewContainer.Organization.Id;
			perms.ViewOrganization(orgId).Or(x => x.AdminReviewContainer(reviewContainerId), x => {
				foreach (var reviewee in reviewees) {
					var userId = reviewee.RGMId;
					x.EditQuestionForUser(userId);
				}

				return x;
			});
			var queryProvider = GetReviewQueryProvider_Deprecated(s, orgId, reviewContainerId);
			queryProvider.AddData(reviewContainer.AsList());
			var allRGMs = s.QueryOver<ResponsibilityGroupModel>().WhereRestrictionOn(x => x.Id).IsIn(reviewees.Select(x => x.RGMId).ToArray()).List();
			queryProvider.AddData(allRGMs);
			var norel = ((long)AboutType.NoRelationship);
			var inval = ((long)AboutType.Invalid);
			var allNoRelationshipsLookup = s.QueryOver<AnswerModel>().Where(x => x.DeleteTime == null && (x.AboutTypeNum == norel || x.AboutTypeNum == inval) && x.ForReviewContainerId == reviewContainerId).WhereRestrictionOn(x => x.RevieweeUserId).IsIn(reviewees.Select(x => x.RGMId).ToArray()).Select(x => x.ReviewerUserId, x => x.RevieweeUserId).List<object[]>().Select(x => new {
				Reviewer = (long)x[0],
				Reviewee = (long)x[1],
			}).GroupBy(x => x.Reviewee).ToDictionary(x => x.Key, x => x.Select(y => y.Reviewer).ToList());
			var dataInteration = new DataInteraction(queryProvider, s.ToUpdateProvider());
			//I think we want ToList, not ToListAlive
			var existingReviewUsers = dataInteration.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainerId).Select(x => new Reviewee(x.ReviewerUser.Id, null)).ToList();
			var team = dataInteration.Get<OrganizationTeamModel>(reviewContainer.ForTeamId);
			var tree = AccountabilityAccessor.GetTree(s, perms, team.Organization.AccountabilityChartId, range: range);
			var askable = s.Get<Askable>(askableId);
			foreach (var reviewee in reviewees) {
				try {
					var userId = reviewee.RGMId;
					var user = dataInteration.Get<UserOrganizationModel>(userId);
					var relationships = Relationships.GetRelationships_Filtered(dataInteration, perms, tree, reviewee, team, range, existingReviewUsers);
					//also want to get the NoRelationship people
					var usersNoRelationships = new List<long>();
					if (allNoRelationshipsLookup.ContainsKey(userId))
						usersNoRelationships = allNoRelationshipsLookup[userId];
					foreach (var nr in usersNoRelationships) {
						var u = dataInteration.Where<UserOrganizationModel>(x => x.Id == nr).Single();
						relationships.AddRelationship(new Reviewer(nr), reviewee, AboutType.NoRelationship);
					}

					foreach (var r in relationships) {
						var existingReviews = dataInteration.Where<ReviewModel>(x => x.ReviewerUserId == r.Reviewer.RGMId).ToList();
						foreach (var existingReview in existingReviews) {
							var askables = new AskableCollection();
							//Is it correct that this be inverted?
							askables.AddUnique(askable, r.RevieweeIsThe, r.Reviewee);
							AddAskablesToReview(dataInteration, perms, r.Reviewer, existingReview, reviewContainer.AnonymousByDefault, askables);
						}
					}
				} catch (Exception) {
					//fall through
				}
			}
		}

		[Obsolete("broken", true)]
		public static void AddResponsibilityAboutUserToReview_Deprecated(ISession s, PermissionsUtility perms, long reviewContainerId, Reviewee reviewee, long askableId) {
			AddResponsibilityAboutUsersToReview_Deprecated(s, perms, reviewContainerId, reviewee.AsList(), askableId);
		}

		[Obsolete("broken", true)]
		public static void AddResponsibilityAboutUserToReview(UserOrganizationModel caller, long reviewContainerId, Reviewee reviewee, long askableId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					AddResponsibilityAboutUserToReview_Deprecated(s, perms, reviewContainerId, reviewee, askableId);
					tx.Commit();
					s.Flush();
				}
			}
		}

		#endregion
		public static ResultObject RemoveUserFromReview(UserOrganizationModel caller, long reviewContainerId, long userOrganizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ManagesUserOrganization(userOrganizationId, false).AdminReviewContainer(reviewContainerId);
					var deleteTime = DateTime.UtcNow;
					var user = s.Get<UserOrganizationModel>(userOrganizationId);
					var revieww = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId && x.ReviewerUserId == userOrganizationId && x.DeleteTime == null).List().ToList();
					foreach (var review in revieww) {
						review.DeleteTime = deleteTime;
						s.Update(review);
						var answers = s.QueryOver<AnswerModel>().Where(x => (x.RevieweeUserId == userOrganizationId || x.ReviewerUserId == userOrganizationId) && x.ForReviewContainerId == reviewContainerId && x.DeleteTime == null).List();
						DeleteAnswers_Unsafe(s, answers, deleteTime);
					}

					tx.Commit();
					s.Flush();
					return ResultObject.Success("Removed " + user.GetNameAndTitle() + " from the review.");
				}
			}
		}

		#region Update
		[Obsolete("broken", true)]
		public static async Task<ResultObject> AddUserToReviewContainer_Deprecated(UserOrganizationModel caller, long reviewContainerId, Reviewee reviewee, bool sendEmails) {
			var unsent = new List<Mail>();
			string userBeingReviewed = null;
			var now = DateTime.UtcNow;
			var range = new DateRange(now, now);
			try {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var perms = PermissionsUtility.Create(s, caller).ManagesUserOrganization(reviewee.RGMId, false).ViewReviews(reviewContainerId, false);
						var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
						var dueDate = reviewContainer.DueDate;

						var reviewSelf = reviewContainer.ReviewSelf;
						var reviewManagers = reviewContainer.ReviewManagers;
						var reviewSubordinates = reviewContainer.ReviewSubordinates;
						var reviewTeammates = reviewContainer.ReviewTeammates;
						var reviewPeers = reviewContainer.ReviewPeers;
						var organization = reviewContainer.Organization;
						var team = TeamAccessor.GetTeam(s, perms, caller, reviewContainer.ForTeamId);
						var exceptions = new List<Exception>();

						var beingReviewedUser = s.Get<UserOrganizationModel>(reviewee.RGMId);
						var orgId = organization.Id;
						var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).Future();
						var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).Future();
						var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).Future();
						var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).Future();
						var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.DepricatedPosition).Where(x => x.Organization.Id == orgId).Future();
						var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).Future();
						var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).Future();
						var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId).Future();
						var allRoles = s.QueryOver<RoleModel_Deprecated>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
						var allRocks = s.QueryOver<RockModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
						var allValues = s.QueryOver<CompanyValueModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
						var allReviewContainers = s.QueryOver<ReviewsModel>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
						var allRGM = s.QueryOver<ResponsibilityGroupModel>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).Future();
						var allAboutCompany = s.QueryOver<AboutCompanyAskable>().Where(x => x.Organization.Id == orgId && x.DeleteTime == null).Future();
						var allAccNodes = s.QueryOver<AccountabilityNode>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
						var allRoleLinks = s.QueryOver<RoleLink_Deprecated>().Where(x => x.OrganizationId == orgId && x.DeleteTime == null).Future();
						var tree = AccountabilityAccessor.GetTree(s, perms, organization.AccountabilityChartId, range: range);
						var queryProvider = new IEnumerableQuery();
						queryProvider.AddData(allRGM);
						queryProvider.AddData(allOrgTeams);
						queryProvider.AddData(allTeamDurations);
						queryProvider.AddData(allMembers);
						queryProvider.AddData(allManagerSubordinates);
						queryProvider.AddData(allPositions);
						queryProvider.AddData(allRoles);
						queryProvider.AddData(allRocks);
						queryProvider.AddData(allValues);
						queryProvider.AddData(allAboutCompany);
						queryProvider.AddData(applicationQuestions);
						queryProvider.AddData(application);
						queryProvider.AddData(reviews);
						queryProvider.AddData(allReviewContainers);
						queryProvider.AddData(allRoleLinks);
						queryProvider.AddData(allAccNodes);

						var di = new DataInteraction(queryProvider, s.ToUpdateProvider());
						var availableUsers = Relationships.GetAvailableUsers(di, perms, tree, team, range, reviewContainerId);
						availableUsers.Add(new Reviewee(beingReviewedUser));
						//TODO Populate a query provider structure here..
						var toEmail = AddUserToReview(caller, s, true, dueDate, reviewContainer.GetParameters(), di, reviewContainer, perms, organization, team, ref exceptions, reviewee, tree, availableUsers, range);
						unsent.AddRange(toEmail);
						userBeingReviewed = beingReviewedUser.GetName();
						tx.Commit();
						s.Flush();
					}
				}
			} catch (Exception e) {
				return new ResultObject(e);
			}

			var result = new EmailResult();
			if (sendEmails) {
				result = await Emailer.SendEmails(unsent);
			}

			return result.ToResults("Successfully added " + userBeingReviewed + " to the review.");
		}

		public static ReviewContainerStats UpdateAllCompleted(ISession s, PermissionsUtility perms, long organizationId, FastReviewQueries.ReviewIncomplete reviewId_Incomplete, bool started, decimal durationMinutes, int additionalAnswered, int optionalAnswered) {
			var reviewId = reviewId_Incomplete.reviewId;
			var anyIncomplete = reviewId_Incomplete.numberIncomplete > 0;
			perms.EditReview(reviewId);
			var review = s.Get<ReviewModel>(reviewId);
			var output = new ReviewContainerStats(review.ForReviewContainerId);
			var updated = false;
			if (durationMinutes != 0) {
				if (review.DurationMinutes == null)
					review.DurationMinutes = 0;
				review.DurationMinutes += (decimal)Math.Min(durationMinutes, (decimal)TimingUtility.ExcludeLongerThan.TotalMinutes);
				updated = true;
			}

			if (!anyIncomplete && !review.Complete) {
				review.Complete = true;
				updated = true;
				output.Stats.ReviewsCompleted += 1;
				output.Completion.Started -= 1;
				output.Completion.Finished += 1;
			}

			if (started && !review.Started) {
				review.Started = true;
				updated = true;
				output.Completion.Started += 1;
				output.Completion.Unstarted -= 1;
			}

			if (anyIncomplete && review.Complete) {
				review.Complete = false;
				review.DurationMinutes = null;
				updated = true;
				output.Stats.ReviewsCompleted -= 1;
				output.Completion.Finished -= 1;
				output.Completion.Started += 1;
			}

			output.Stats.QuestionsAnswered += additionalAnswered;
			output.Stats.OptionalsAnswered += optionalAnswered;
			review.QuestionCompletion.NumRequiredComplete += additionalAnswered;

			s.Update(review);
			return output;
		}

		public static ReviewContainerStats UpdateAllCompleted(UserOrganizationModel caller, long reviewId, bool started, decimal durationMinutes, int additionalAnswered, int optionalAnswered) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var reviewIncomplete = FastReviewQueries.AnyUnansweredReviewQuestions(s, reviewId.AsList()).First();
					var output = UpdateAllCompleted(s, perms, caller.Organization.Id, reviewIncomplete, started, durationMinutes, additionalAnswered, optionalAnswered);
					tx.Commit();
					s.Flush();
					return output;
				}
			}
		}

		private static void UpdateCompletion(AnswerModel answer, DateTime now, ref int questionsAnsweredDelta, ref int optionalAnsweredDelta) {
			if (answer.Complete) {
				if (answer.CompleteTime == null) {
					if (!answer.Required)
						optionalAnsweredDelta += 1;
					questionsAnsweredDelta += 1;
				}

				answer.CompleteTime = now;
			} else {
				if (answer.CompleteTime != null) {
					if (!answer.Required)
						optionalAnsweredDelta -= 1;
					questionsAnsweredDelta -= 1;
				}

				answer.CompleteTime = null;
			}
		}

		public static void AddAnswerToReview(UserOrganizationModel caller, long reviewId, long answerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);
					var feedback = s.Get<AnswerModel>(answerId);
					var review = s.Get<ReviewModel>(reviewId);
					if (review.ReviewerUserId != feedback.RevieweeUserId)
						throw new PermissionsException("Answer and Review do not match.");
					review.ClientReview.FeedbackIds.Add(new LongModel() { Value = answerId });
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void RemoveAnswerFromReview(UserOrganizationModel caller, long reviewId, long answerId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManageUserReview(reviewId, false);
					var answer = s.Get<AnswerModel>(answerId);
					var review = s.Get<ReviewModel>(reviewId);
					if (review.ReviewerUserId != answer.RevieweeUserId)
						throw new PermissionsException("Answer and Review do not match.");
					foreach (var id in review.ClientReview.FeedbackIds) {
						if (id.Value == answerId)
							id.DeleteTime = DateTime.UtcNow;
					}

					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void UpdateDueDates(UserOrganizationModel caller, long reviewContainerId, DateTime? prereviewDueDate, DateTime reviewDueDate, DateTime? reportDueDate) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perm = PermissionsUtility.Create(s, caller).AdminReviewContainer(reviewContainerId);
					var reviewContainer = s.Get<ReviewsModel>(reviewContainerId);
					var update = false;
					if (prereviewDueDate != null) {
						if (reviewContainer.PrereviewDueDate != prereviewDueDate.Value) {
							reviewContainer.PrereviewDueDate = prereviewDueDate.Value;
							update = true;
							var prereviews = s.QueryOver<PrereviewModel>().Where(x => x.ReviewContainerId == reviewContainerId).List().ToList();
							foreach (var p in prereviews) {
								p.PrereviewDue = prereviewDueDate.Value;
								s.Update(p);
							}
						}
					}

					if (reviewContainer.DueDate != reviewDueDate) {
						update = true;
						reviewContainer.DueDate = reviewDueDate;
						var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List().ToList();
						foreach (var r in reviews) {
							r.DueDate = reviewDueDate;
							s.Update(r);
						}
					}

					if (reportDueDate != null && reviewContainer.ReportsDueDate != reportDueDate) {
						update = true;
						reviewContainer.ReportsDueDate = reportDueDate;
					}

					if (update) {
						s.Update(reviewContainer);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		public static void UpdateDueDate(UserOrganizationModel caller, long reviewId, DateTime dueDate) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var review = s.Get<ReviewModel>(reviewId);
					if (review == null)
						throw new PermissionsException("Review does not exist. (" + reviewId + ")");
					PermissionsUtility.Create(s, caller).AdminReviewContainer(review.ForReviewContainerId);
					review.DueDate = dueDate;
					s.Update(review);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static void EditReviewName(UserOrganizationModel caller, long reviewContainerId, String reviewName) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).AdminReviewContainer(reviewContainerId);
					var review = s.Get<ReviewsModel>(reviewContainerId);
					if (review == null)
						throw new PermissionsException("Review does not exist. (" + reviewContainerId + ")");
					review.ReviewName = reviewName;
					s.Update(review);
					var reviews = s.QueryOver<ReviewModel>().Where(x => x.ForReviewContainerId == reviewContainerId).List().ToList();
					foreach (var r in reviews) {
						r.Name = reviewName;
						s.Update(r);
					}

					tx.Commit();
					s.Flush();
				}
			}
		}
		#endregion
	}
}

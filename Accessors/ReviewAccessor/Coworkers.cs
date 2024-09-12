using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using RadialReview.Models.Angular.Accountability;
using NHibernate;

namespace RadialReview.Accessors {
	public partial class ReviewAccessor : BaseAccessor {
		public class Relationships {
			public class FromAccountabilityChart {
				public static List<AngularAccountabilityNode> GetRevieweeNode(AngularAccountabilityChart tree, Reviewee reviewee) {
					if (reviewee.ACNodeId == null)
						return AngularTreeUtil.FindUsersNodes(tree.Root, reviewee.RGMId);
					else
						return (AngularTreeUtil.FindNode(tree.Root, reviewee.ACNodeId.Value) ?? new AngularAccountabilityNode()).AsList();
				}
				[Todo]
				[Obsolete("fix me", true)]
				public static RelationshipCollection GetRelationshipsForNode(AngularAccountabilityChart tree, Reviewee reviewee) {
					var revieweesNodes = GetRevieweeNode(tree, reviewee);

					var relationships = new List<AccRelationship>();
					if (reviewee.Type == OriginType.Organization) {
						var userNodes = AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.HasUsers()).ToList();
						return new RelationshipCollection(userNodes.SelectMany(node => node.Users.Select(x => new AccRelationship() {
							Reviewer = new Reviewer(node, x.Id),
							ReviewerIsThe = AboutType.Subordinate,
							Reviewee = reviewee
						})).ToList());
					} else {
						//Not an organization
						if (!revieweesNodes.Any()) {
							//Fallback for no acc chart node.
							return new RelationshipCollection(new[] { new AccRelationship() {
									Reviewer = reviewee.ConvertToReviewer(),
									ReviewerIsThe = AboutType.Self,
									Reviewee = reviewee
							} });
						} else {
							foreach (var revieweeNode in revieweesNodes) {
								var directReports = revieweeNode.GetDirectChildren()
									.Where(x => x.HasUsers())
									.SelectMany(node => node.Users.Select(x => new AccRelationship() {
										Reviewer = new Reviewer(node, x.Id),
										ReviewerIsThe = AboutType.Subordinate,
										Reviewee = new Reviewee(revieweeNode)
									}));
								var managerNode = AngularTreeUtil.GetDirectParent(tree.Root, revieweeNode.Id);
								var managers = new List<AccRelationship>();
								if (managerNode != null && managerNode.HasUsers()) {
									managers = managerNode.Users.Select(u => new AccRelationship() {
										Reviewer = new Reviewer(managerNode, u.Id),
										ReviewerIsThe = AboutType.Manager,
										Reviewee = new Reviewee(revieweeNode)
									}).ToList();
								}
								var peers = AngularTreeUtil.GetDirectPeers(tree.Root, revieweeNode.Id)
									.Where(x => x.HasUsers())
									.SelectMany(node => node.Users.Select(x => new AccRelationship() {
										Reviewer = new Reviewer(node, x.Id),
										ReviewerIsThe = AboutType.Peer,
										Reviewee = new Reviewee(revieweeNode)
									}));
								var self = new AccRelationship() {
									Reviewer = new Reviewer(revieweeNode, reviewee.RGMId),
									ReviewerIsThe = AboutType.Self,
									Reviewee = new Reviewee(revieweeNode)
								}.AsList();
								relationships.AddRange(directReports);
								relationships.AddRange(managers);
								relationships.AddRange(peers);
								relationships.AddRange(self);
							}
							return new RelationshipCollection(relationships);
						}
					}
				}


				[Obsolete("fix me", true)]
				public static List<Reviewee> FilterTreeByTeam(AngularAccountabilityChart tree, OrganizationTeamModel team, List<UserOrganizationModel> members) {

					if (team.Type == TeamType.AllMembers)
						return members.Select(x => new Reviewee(x)).ToList();
					if (team.Type == TeamType.Managers)
						return AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.HasUsers() && x.GetDirectChildren().Any()).Select(x => new Reviewee(x)).ToList();
					if (team.Type == TeamType.Standard)
						return AngularTreeUtil.GetAllNodes(tree.Root).Where(x => x.HasUsers() && members.Any(y => x.Users.Any(u => u.Id == y.Id))).Select(x => new Reviewee(x)).ToList();
					if (team.Type == TeamType.Subordinates)
						return AngularTreeUtil.FindUsersNodes(tree.Root, team.ManagedBy).SelectMany(x => x.GetDirectChildren().Union(x.AsList())).Where(x => x.HasUsers()).Select(x => new Reviewee(x)).ToList();
					throw new ArgumentOutOfRangeException("Unrecognized team " + team.Type);

				}
			}

			public class Filters {
				protected static IEnumerable<AccRelationship> Step1_RemoveNonUsers(IEnumerable<AccRelationship> existing) {
					return existing.Where(x => x.Reviewee != null && x.Reviewer != null);
				}
				protected static IEnumerable<AccRelationship> Step2_RemoveInaccessableUsers(IEnumerable<AccRelationship> existing, List<Reviewee> accessibleUsers) {
					if (accessibleUsers == null)
						return existing;

					var anyAcNode = accessibleUsers.Where(y => y.ACNodeId == null).Select(y => y.RGMId).ToList();
					return existing.Where(x =>
						(anyAcNode.Contains(x.Reviewee.RGMId) || accessibleUsers.Any(y => y == x.Reviewee)) &&
						(anyAcNode.Contains(x.Reviewer.RGMId) || accessibleUsers.Any(y => y.RGMId == x.Reviewer.RGMId))
					);
				}
				protected static IEnumerable<AccRelationship> Step3_FilterAgainstParameters(IEnumerable<AccRelationship> existing, ReviewParameters parameters) {
					if (parameters == null)
						return existing;

					var output = new List<AccRelationship>();

					foreach (var e in existing) {
						if (parameters.ReviewSelf == false && e.RevieweeIsThe == AboutType.Self)
							continue;
						if (parameters.ReviewManagers == false && e.RevieweeIsThe == AboutType.Manager)
							continue;
						if (parameters.ReviewSubordinates == false && e.RevieweeIsThe == AboutType.Subordinate)
							continue;
						if (parameters.ReviewPeers == false && e.RevieweeIsThe == AboutType.Peer)
							continue;
						if (parameters.ReviewTeammates == false && e.RevieweeIsThe == AboutType.Teammate)
							continue;

						output.Add(e);
					}
					return output;

				}

				public static RelationshipCollection Apply(RelationshipCollection existing1, List<Reviewee> accessibleUsers, ReviewParameters parameters) {
					var existing = existing1.ToList();
					existing = Step1_RemoveNonUsers(existing).ToList();
					existing = Step2_RemoveInaccessableUsers(existing, accessibleUsers).ToList();
					existing = Step3_FilterAgainstParameters(existing, parameters).ToList();

					return new RelationshipCollection(existing);
				}
			}

			[Obsolete("fix me", true)]
			public static List<Reviewee> GetAvailableUsers(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, OrganizationTeamModel forTeam, DateRange range, long? reviewContainerId = null) {
				var allMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, forTeam.Id, true).FilterRange(range).Select(x => x.User).ToList();
				var availableUsers = FromAccountabilityChart.FilterTreeByTeam(tree, forTeam, allMembers).ToList();

				if (reviewContainerId != null) {
					//Add people not part of team but are part of review.
					var allExtraUsers = s.Where<ReviewModel>(x => x.ForReviewContainerId == reviewContainerId).Select(x => x.ReviewerUser).ToList();
					allMembers.AddRange(allExtraUsers);
					availableUsers.AddRange(allExtraUsers.Select(x => new Reviewee(x)));
				}

				availableUsers.Add(new Reviewee(forTeam.Organization.Id, null) { Type = OriginType.Organization });
				return availableUsers;
			}

			[Obsolete("fix me", true)]
			protected static RelationshipCollection GetAdditionalRelationshipsForTeam(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewee reviewee, OrganizationTeamModel forTeam, DateRange range, bool includeSelf) {
				var output = new RelationshipCollection();
				var revieweeNode = FromAccountabilityChart.GetRevieweeNode(tree, reviewee).FirstOrDefault();
				if (revieweeNode == null)
					return output;  //Not on the tree
				var teamMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, forTeam.Id, true).FilterRange(range);
				if (!teamMembers.Any(x => x.UserId == reviewee.RGMId))
					return output;  // Not on the team
				foreach (var member in teamMembers) {
					if (!includeSelf && member.UserId == reviewee.RGMId)
						continue; ;//Skip if self
					var type = AboutType.Teammate;
					if (includeSelf && member.UserId == reviewee.RGMId) {
						type = AboutType.Self;
					}
					output.AddRelationship(new Reviewer(member.UserId), new Reviewee(revieweeNode), type);
				}
				return output;
			}

			[Obsolete("fix me", true)]
			public static RelationshipCollection GetRelationships_Unfiltered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewee reviewee, OrganizationTeamModel forTeam, DateRange range, ReviewParameters parameters) {
				parameters = parameters ?? ReviewParameters.AllTrue();
				//HEY DON'T DO ANY FILTERING IN THIS METHOD. WE WANT INVERSES TO WORK (see GetAllRelationships_Filtered)
				var output = new RelationshipCollection();

				if (forTeam.Type == TeamType.Standard) {
					//We're just reviewing this team
					var newParams = ReviewParameters.AllFalse();
					newParams.ReviewTeammates = parameters.ReviewTeammates;
					newParams.ReviewSelf = parameters.ReviewSelf;
					parameters = newParams;
					output = GetAdditionalRelationshipsForTeam(s, perms, tree, reviewee, forTeam, range, true);
				} else {
					//Add Interreviewing teams
					if (parameters.ReviewTeammates) {
						var interReviewingTeams = ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(s.GetQueryProvider(), perms, reviewee.RGMId)
							.Where(x => x is OrganizationTeamModel).Cast<OrganizationTeamModel>()
							.Where(x => x.InterReview).ToList();
						foreach (var interReviewingTeam in interReviewingTeams) {
							output.AddRange(GetAdditionalRelationshipsForTeam(s, perms, tree, reviewee, interReviewingTeam, range, false));
						}
					}
					output.AddRange(FromAccountabilityChart.GetRelationshipsForNode(tree, reviewee));
				}


				return output;
			}

			[Obsolete("fix me", true)]
			public static RelationshipCollection GetRelationships_Filtered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewer reviewer, OrganizationTeamModel forTeam, DateRange range, List<Reviewee> accessibleUsers = null, ReviewParameters parameters = null) {
				var pretendReviewee = reviewer.ConvertToReviewee();
				var output = GetRelationships_Unfiltered(s, perms, tree, pretendReviewee, forTeam, range, parameters);
				foreach (var o in output)
					o.Invert();

				var reviewerNode = output.FirstOrDefault(x => x.ReviewerIsThe == AboutType.Self);
				if (reviewerNode != null) {
					output.AddRelationship(reviewerNode.Reviewer, new Reviewee(forTeam.Organization.Id, null), AboutType.Organization);
				}

				return Filters.Apply(output, accessibleUsers, parameters);
			}

			[Obsolete("fix me", true)]
			public static RelationshipCollection GetRelationships_Filtered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, Reviewee reviewee, OrganizationTeamModel forTeam, DateRange range, List<Reviewee> accessibleUsers = null, ReviewParameters parameters = null) {
				var output = GetRelationships_Unfiltered(s, perms, tree, reviewee, forTeam, range, parameters);
				return Filters.Apply(output, accessibleUsers, parameters);
			}

			[Obsolete("fix me", true)]
			public static RelationshipCollection GetAllRelationships_Filtered(DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree, OrganizationTeamModel forTeam, DateRange range, long? reviewContainerId = null, ReviewParameters parameters = null) {
				var allMembers = TeamAccessor.GetTeamMembers(s.GetQueryProvider(), perms, forTeam.Id, true).FilterRange(range).Select(x => x.User).ToList();





				allMembers = allMembers.Distinct(x => x.Id).ToList();

				var collection = new RelationshipCollection();
				foreach (var member in allMembers) {
					var memberId = member.Id;
					var allNodesForUser = AngularTreeUtil.FindUsersNodes(tree.Root, memberId);
					if (allNodesForUser.Any()) {
						foreach (var userNode in allNodesForUser) {
							var relationships = GetRelationships_Unfiltered(s, perms, tree, new Reviewee(userNode), forTeam, range, parameters);
							collection.AddRange(relationships);
						}
					} else {
						var relationships = GetRelationships_Unfiltered(s, perms, tree, new Reviewee(member), forTeam, range, parameters);
						collection.AddRange(relationships);
					}

					collection.AddRelationship(new Reviewer(memberId), new Reviewee(forTeam.Organization.Id, null) { Type = OriginType.Organization }, AboutType.Organization);
				}

				var availableUsers = GetAvailableUsers(s, perms, tree, forTeam, range, reviewContainerId);

				return Filters.Apply(collection, availableUsers, parameters);
			}
		}

		public static bool ShouldAddToReview(Askable askable, AboutType relationshipToReviewee) {
			var a = (long)askable.OnlyAsk == long.MaxValue;
			var b = (relationshipToReviewee.Invert() & askable.OnlyAsk) != AboutType.NoRelationship;
			return a || b;
		}

		[Obsolete("Fix me", true)]
		private static AskableCollection GetAskables(ISession s, DataInteraction dataInteraction, PermissionsUtility perms, Reviewer reviewer, IEnumerable<Reviewee> specifiedReviewees, DateRange range) {

			var allAskables = new AskableCollection();
			var q = dataInteraction.GetQueryProvider();

			foreach (var reviewee in specifiedReviewees) {
				var found = dataInteraction.Get<ResponsibilityGroupModel>(reviewee.RGMId);
				if (found == null || !found.AsList().FilterRangeRestricted(range).Any())
					continue;

				var revieweeAskables = AskableAccessor.GetAskablesForUser(s, q, perms, reviewee, range);
				var reviewerIsThe = RelationshipAccessor.GetRelationshipsMerged(q, perms, reviewer, reviewee, range);
				allAskables.AddAll(revieweeAskables, reviewerIsThe, reviewee);
			}
			return allAskables;
		}

		[Obsolete("Fix me", true)]
		private static AskableCollection GetAskablesBidirectional(ISession session, DataInteraction s, PermissionsUtility perms, AngularAccountabilityChart tree,
			Reviewee self, OrganizationTeamModel forTeam, ReviewParameters parameters, List<Reviewee> accessibleUsers, DateRange range, bool addMeToOthersReviews, ref AskableCollection addToOtherReviews) {


			var askableUtil = new AskableCollection();
			var reviewer = self.ConvertToReviewer();
			var whoDoIReview = Relationships.GetRelationships_Filtered(s, perms, tree, reviewer, forTeam, range, accessibleUsers, parameters);

			foreach (var imReviewing in whoDoIReview) {
				var theirQuestions = AskableAccessor.GetAskablesForUser(session, s.GetQueryProvider(), perms, imReviewing.Reviewee, range);
				askableUtil.AddAll(theirQuestions, imReviewing);
			}

			if (addMeToOthersReviews) {
				addToOtherReviews = addToOtherReviews ?? new AskableCollection();
				var me = self;
				var questionsAboutMe = AskableAccessor.GetAskablesForUser(session, s.GetQueryProvider(), perms, me, range);
				var whoReviewsMe = Relationships.GetRelationships_Filtered(s, perms, tree, me, forTeam, range, accessibleUsers, parameters);
				foreach (var myReviewer in whoReviewsMe) {
					askableUtil.AddAll(questionsAboutMe, myReviewer);
					addToOtherReviews.AddAll(questionsAboutMe, myReviewer);
				}
			}

			return askableUtil;
		}

		[Obsolete("fix me", true)]
		public static RelationshipCollection GetAllRelationships(UserOrganizationModel caller, long forTeam, ReviewParameters parameters, DateRange range = null, long? reviewContainerId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewTeam(forTeam);
					var team = s.Get<OrganizationTeamModel>(forTeam);

					var org = team.Organization;
					var orgId = org.Id;

					//Order is important
					var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).Where(range.Filter<OrganizationTeamModel>()).Future();
					var allTeamDurations = s.QueryOver<TeamDurationModel>().Where(range.Filter<TeamDurationModel>()).JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).Future();
					var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId && !x.IsClient).Where(range.Filter<UserOrganizationModel>()).Future();
					var tree = AccountabilityAccessor.GetTree(s, perms, org.AccountabilityChartId, range: range);
					var queryProvider = new IEnumerableQuery(true);

					queryProvider.AddData(allOrgTeams);
					queryProvider.AddData(allTeamDurations);
					queryProvider.AddData(allMembers);
					var d = new DataInteraction(queryProvider, s.ToUpdateProvider());

					return Relationships.GetAllRelationships_Filtered(d, perms, tree, team, range, reviewContainerId: reviewContainerId, parameters: parameters);
				}
			}
		}



	}

}



using FluentNHibernate.Mapping;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;

using RadialReview.Variables;
using static RadialReview.Models.L10.L10Recurrence;
using RadialReview.Models.Enums;
using RadialReview.Models.Rocks;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Accessors.Suggestions {

	public enum SuggestionType {
		Invalid = 0,
		Issue = 1,
		Todo = 2,
		TodoAll = 3,
		Headline = 4,
	}

	public class SuggestionModel {
		public virtual long Id { get; set; }
		public virtual long OrgId { get; set; }
		public virtual long RecurrenceId { get; set; }
		public virtual long MeetingId { get; set; }
		public virtual L10PageType PageType { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual ForModel ForModel { get; set; }
		public virtual string About { get; set; }
		public virtual string Title { get; set; }
		public virtual string Details { get; set; }
		public virtual string ModalTitle { get; set; }
		public virtual string ModalDetails { get; set; }
		public virtual int Weight { get; set; }
		public virtual int Ordering { get; set; }
		public virtual string Page { get; set; }
		public virtual SuggestionType SuggestionType { get; set; }
		public SuggestionModel() {
			CreateTime = DateTime.UtcNow;

		}
		public SuggestionModel(int weight, ForModel forModel, L10MeetingVM meeting, L10PageType pageType, SuggestionType suggestionType, string about, string title, string details, string modalTitle, string modalDetails) : this() {
			OrgId = meeting.ForOrganizationId;
			RecurrenceId = meeting.Recurrence.Id;
			MeetingId = meeting.Meeting.Id;
			Page = meeting.SelectedPageId;
			ForModel = forModel;
			PageType = pageType;
			About = about;
			Title = title;
			Details = details;
			ModalTitle = modalTitle;
			ModalDetails = modalDetails;
			SuggestionType = suggestionType;
			Weight = weight;
			Ordering = SuggestionsAccessor.RAND.Next(weight);
		}

		public class Map : ClassMap<SuggestionModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.OrgId);
				Map(x => x.RecurrenceId);
				Map(x => x.MeetingId);
				Map(x => x.PageType);
				Map(x => x.SuggestionType);
				Map(x => x.About);
				Map(x => x.Title);
				Map(x => x.Details);
				Map(x => x.ModalTitle);
				Map(x => x.ModalDetails);
				Map(x => x.Weight);
				Map(x => x.Page);
				Component(x => x.ForModel).ColumnPrefix("ForModel_");
			}
		}
	}


	public class SuggestionsAccessor : BaseAccessor {

		public static Random RAND = new Random();

		public static IEnumerable<SuggestionModel> GetOrCreateHeadlineSuggestions(UserOrganizationModel caller, L10MeetingVM meeting) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var settings = GetIssueSuggestionsSettings(s);
					if (!IsSuggestionsEnabled(caller,meeting,settings)) {
						return new List<SuggestionModel>();
					}
					if (meeting.HeadlineType == PeopleHeadlineType.HeadlinesBox) {
						return new List<SuggestionModel>();
					}

					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meeting.Meeting.Id);
					perms.ViewL10Page(meeting.SelectedPageId);

					try {
						var existing = s.QueryOver<SuggestionModel>().Where(x => x.MeetingId == meeting.Meeting.Id).List().ToList();
						if (existing.Any(x => x.PageType == L10PageType.Headlines)) {
							return existing;
						}

						var messages = new List<SuggestionModel>();
						var headlineCount = meeting.Headlines.Count();
						if (headlineCount < settings.HeadlineFew.Objective) {
							messages.AddRange(settings.HeadlineFew.BuildSuggestionModel(100, ForModel.Create(meeting.Meeting), meeting));
						}

						messages = messages.OrderByDescending(x => x.Ordering).Take(settings.MaxPerPage).ToList();
						if (messages.Any()) {
							foreach (var m in messages) {
								s.Save(m);
							}
							tx.Commit();
							s.Flush();
						}
						existing.AddRange(messages);
						return existing;

					} catch (Exception e) {
						int a = 0;
					}
					return new List<SuggestionModel>();
				}
			}
		}
		public static IEnumerable<SuggestionModel> GetOrCreateIDSSuggestions(UserOrganizationModel caller, L10MeetingVM meeting) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var settings = GetIssueSuggestionsSettings(s);
					if (!IsSuggestionsEnabled(caller,meeting,settings))
						return new List<SuggestionModel>();

					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meeting.Meeting.Id);
					perms.ViewL10Page(meeting.SelectedPageId);

					try {
						var existing = s.QueryOver<SuggestionModel>().Where(x => x.MeetingId == meeting.Meeting.Id).List().ToList();
						if (existing.Any(x => x.PageType == L10PageType.IDS)) {
							return existing;
						}


						var messages = new List<SuggestionModel>();
						var issueCount = meeting.Issues.Count();
						if (issueCount > 5) {
							var durs = meeting.Issues.OrderBy(x => DateTime.UtcNow - x.CreateTime).TakeWhile((x, i) => i <= issueCount * .2);
							var totalDur = DateTime.UtcNow - durs.Last().CreateTime;
							if (totalDur > TimeSpan.FromDays(settings.IdsOldIssue.Objective)) {
								messages.AddRange(settings.IdsOldIssue.BuildSuggestionModel(100, ForModel.Create(meeting.Meeting), meeting, durs.Count()));
							}
						}

						if (issueCount > settings.IdsLongIssue.Objective) {
							messages.AddRange(settings.IdsLongIssue.BuildSuggestionModel(100, ForModel.Create(meeting.Meeting), meeting, issueCount));
						}

						try {
							var issueOptions = settings.IDS;
							var msg = issueOptions[RAND.Next(issueOptions.Length)];

							messages.Add(new SuggestionModel(500, ForModel.Create(meeting.Meeting), meeting, L10PageType.IDS, SuggestionType.Issue, "Issues", "Issue-of-the-Week™", msg, msg, null));
						} catch (Exception e) {

						}

						messages = messages.OrderByDescending(x => x.Ordering).Take(settings.MaxPerPage).ToList();
						if (messages.Any()) {
							foreach (var m in messages) {
								s.Save(m);
							}
							tx.Commit();
							s.Flush();
						}
						existing.AddRange(messages);
						return existing;

					} catch (Exception e) {
						int a = 0;
					}

					return new List<SuggestionModel>();
				}
			}
		}

		public static bool IsSuggestionsEnabledForOrganization(UserOrganizationModel caller, long orgId) {
			IssueSuggstionsData settings;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewOrganization(orgId);
					settings = GetIssueSuggestionsSettings(s);
					tx.Commit();
					s.Flush();
				}
			}
			if (orgId.IsEOSW())
				return false;
			if (!settings.Enabled)
				return false;
			return true;
		}

		private static bool IsSuggestionsEnabled(UserOrganizationModel caller, L10MeetingVM meeting, IssueSuggstionsData settings) {
			if (caller.Organization.Id.IsEOSW())
				return false;
			if (!settings.Enabled)
				return false;
			if (meeting.Recurrence.EnableSuggestions)
				return true;   
			return false;
		}

		public static IEnumerable<SuggestionModel> GetOrCreateScorecardSuggestions(UserOrganizationModel caller, L10MeetingVM meeting) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var settings = GetIssueSuggestionsSettings(s);
					if (!IsSuggestionsEnabled(caller, meeting, settings))
						return new List<SuggestionModel>();
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meeting.Meeting.Id);
					perms.ViewL10Page(meeting.SelectedPageId);
					try {
						var existing = s.QueryOver<SuggestionModel>().Where(x => x.MeetingId == meeting.Meeting.Id).List().ToList();
						if (existing.Any(x => x.PageType == L10PageType.Scorecard)) {
							return existing;
						}

						var messages = new List<SuggestionModel>();
						var measurables = meeting.Meeting._MeetingMeasurables.ToDefaultDictionary(x => x.Measurable.NotNull(y => y.Id), x => x.Measurable.NotNull(y => y.Title));
						var measurableOwner = meeting.Meeting._MeetingMeasurables.ToDefaultDictionary(x => x.Measurable.NotNull(y => y.Id), x => x.Measurable.NotNull(y => y.AccountableUser.GetFirstName()));

						foreach (var g in meeting.Scores.GroupBy(x => x.MeasurableId)) {
							var ordered = g.OrderByDescending(x => x.ForWeek).Where(x => x.ForWeek <= DateTime.UtcNow.AddDaysSafe(7).StartOfWeek(DayOfWeek.Sunday));

							var met = ordered.TakeWhile(x => x.Measured == null || x.MeetGoal()).Count(x => x.MeetGoal());
							var failed = ordered.TakeWhile(x => x.Measured == null || !x.MeetGoal()).Count(x => !x.MeetGoal());
							var empty = ordered.TakeWhile(x => x.Measured == null).Count();

							var forModel = ForModel.Create(g.First());

							if (met > settings.ScorecardGoalMet.Objective) {
								messages.AddRange(settings.ScorecardGoalMet.BuildSuggestionModel(100 * met, forModel, meeting, measurables[g.Key], met, measurableOwner[g.Key]));

							}

							if (failed > settings.ScorecardGoalMissed.Objective) {
								messages.AddRange(settings.ScorecardGoalMissed.BuildSuggestionModel(100 * failed, forModel, meeting, measurables[g.Key], failed, measurableOwner[g.Key]));

							}

							if (empty > settings.ScorecardGoalEmpty.Objective) {
								messages.AddRange(settings.ScorecardGoalEmpty.BuildSuggestionModel(300 * empty, forModel, meeting, measurables[g.Key], empty, measurableOwner[g.Key]));
							}

						}

						messages = messages.OrderByDescending(x => x.Ordering).Take(settings.MaxPerPage).ToList();
						if (messages.Any()) {
							foreach (var m in messages) {
								s.Save(m);
							}
							tx.Commit();
							s.Flush();
						}
						existing.AddRange(messages);
						return existing;
					} catch (Exception e) {
						int a = 0;
					}
					return new List<SuggestionModel>();
				}
			}
		}
		public static IEnumerable<SuggestionModel> GetOrCreateRockSuggestions(UserOrganizationModel caller, L10MeetingVM meeting) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var settings = GetIssueSuggestionsSettings(s);
					if (!IsSuggestionsEnabled(caller, meeting, settings))
						return new List<SuggestionModel>();

					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meeting.Meeting.Id);
					perms.ViewL10Page(meeting.SelectedPageId);

					try {
						var existing = s.QueryOver<SuggestionModel>().Where(x => x.MeetingId == meeting.Meeting.Id).List().ToList();
						if (existing.Any(x => x.PageType == L10PageType.Rocks)) {
							return existing;
						}

						var rockCount = meeting.Rocks.Count();
						DefaultDictionary<long, DateTime> lastStatusChange = null;

						if (settings.UseAudit && rockCount > 0) {
							try {
								var rockIds = meeting.Rocks.Select(x => x.ForRock.Id).ToList();
								var data = s.CreateSQLQuery("select r.Askable_id,r.REV,r.Completion, i.REVTSTMP from RockModel_AUD r join REVINFO i on i.REV = r.REV where Askable_id in (:rockIds) order by REV desc limit 150;")
									.SetParameterList("rockIds", rockIds)
									.List<object[]>()
									.Select(x => new {
										RockId = (long)x[0],
										RevId = Convert.ToInt64(x[1]),
										Completion = (string)x[2],
										RevisionDate = (DateTime)x[3]
									}).ToList();

								lastStatusChange = new DefaultDictionary<long, DateTime>(x => meeting.Rocks.FirstOrDefault(y => y.Id == x).NotNull(y => y.CreateTime));
								var groups = data.GroupBy(x => x.RockId);
								foreach (var r in groups) {
									var ri = r.First();
									var initialCompletion = ri.Completion;
									var best = ri;
									for (var i = 1; i < r.Count(); i++) {
										if (r.ElementAt(i).Completion != initialCompletion) {
											best = r.ElementAt(i - 1);
											break;
										}
									}
									lastStatusChange[ri.RockId] = (best ?? ri).RevisionDate;
								}
							} catch (Exception e) {
								log.Error(e);
							}
						}


						var messages = new List<SuggestionModel>();


						var mrocks = meeting.Rocks;

						var offTrackDeadline = new List<SuggestionModel>();
						var onTrackDeadline = new List<SuggestionModel>();
						var offTrackForNWeek = new List<SuggestionModel>();
						var newDone = new List<SuggestionModel>();
						var milestonesOffTrack = new List<SuggestionModel>();

						foreach (var mrock in mrocks) {
							var rock = mrock.ForRock;
							var dueInWeeks = 0.0;
							var lastStatusChangeWeeks = 0.0;
							var shouldUseDueDate = false;
							var friendlyDueMessage = "";

							var milestones = meeting.Milestones.NotNull(x => x.Where(y => y.RockId == rock.Id).ToList()) ?? new List<Milestone>();

							if (rock.DueDate.HasValue) {
								dueInWeeks = (rock.DueDate.Value-DateTime.UtcNow).TotalDays / 7.0;
								shouldUseDueDate = true;
								//Off Track and Due Soon
								friendlyDueMessage = "soon";
								switch ((int)Math.Floor(dueInWeeks)) {
									case 0:
										friendlyDueMessage = "in less than a week";
										break;
									case 1:
										friendlyDueMessage = "in about a week";
										break;
									case 2:
										friendlyDueMessage = "in a couple of weeks";
										break;
									default:
										break;
								}
							}
							if (lastStatusChange != null) {
								lastStatusChangeWeeks = (DateTime.UtcNow - lastStatusChange[rock.Id]).TotalDays / 7.0;
							}

							if (shouldUseDueDate && rock.Completion == RockState.AtRisk && dueInWeeks <= settings.RocksOffTrackDueSoon.Objective) {
								//At risk, almost due
								offTrackDeadline.AddRange(settings.RocksOffTrackDueSoon.BuildSuggestionModel(600, ForModel.Create(rock), meeting,	rock.Name, dueInWeeks, friendlyDueMessage, rock.AccountableUser.GetFirstName()));
							} else if (shouldUseDueDate && rock.Completion == RockState.OnTrack && dueInWeeks <= settings.RocksOnTrackDueSoon.Objective) {
								//On track almost due
								onTrackDeadline.AddRange(settings.RocksOnTrackDueSoon.BuildSuggestionModel(400, ForModel.Create(rock), meeting, rock.Name, dueInWeeks, friendlyDueMessage, rock.AccountableUser.GetFirstName()));
							} else if (rock.Completion == RockState.AtRisk && lastStatusChange != null && lastStatusChangeWeeks >= settings.RocksOffTrackNWeeks.Objective) {
								//At risk for N weeks
								var weight = (int)(200* Math.Max(1,lastStatusChangeWeeks / settings.RocksOffTrackNWeeks.Objective));
								offTrackForNWeek.AddRange(settings.RocksOffTrackNWeeks.BuildSuggestionModel(weight, ForModel.Create(rock), meeting, rock.Name, lastStatusChangeWeeks, rock.AccountableUser.GetFirstName()));
							} else if (rock.Completion == RockState.Complete && lastStatusChangeWeeks <= settings.RocksDoneWeeks.Objective) {
								//Completed
								newDone.AddRange(settings.RocksDoneWeeks.BuildSuggestionModel(100, ForModel.Create(rock), meeting,		rock.Name, lastStatusChangeWeeks, rock.AccountableUser.GetFirstName()));
							}

							if (rock.Completion != RockState.Complete) {
								var count = milestones.Count(x => x.DueDate < DateTime.UtcNow && x.CompleteTime == null);
								if (count > 0) {
									var plural = "milestone";
									var isAre = "Milestone is";
									var friendlyCount = "is a milestone";
									var milestoneListStr = milestones.First().Name;
									if (count > 1) {
										isAre = "Milestones are";
										friendlyCount = "are " + count + " milestones";
										plural = "milestones";
										milestoneListStr = string.Join("\n", milestones.Select((x, i) => (i + 1) + "." + x.Name));
									}
									milestonesOffTrack.AddRange(settings.RockMilestonesOffTrack.BuildSuggestionModel(50, ForModel.Create(rock), meeting,	 rock.Name, rock.AccountableUser.GetFirstName(), count, friendlyCount, plural, isAre, milestoneListStr));
								}
							}
						}

						messages.AddRange(offTrackDeadline);
						messages.AddRange(onTrackDeadline);
						messages.AddRange(offTrackForNWeek);
						messages.AddRange(newDone);
						messages.AddRange(milestonesOffTrack);



						//Any rocks and all done
						var rocks = mrocks.Select(x => x.ForRock);
						if (rockCount > 0 && rocks.All(x => x.Completion == RockState.Complete) && rocks.Any(x => x.CompleteTime != null && (DateTime.UtcNow - x.CompleteTime.Value).TotalDays / 7.0 <= settings.RocksAllDone.Objective)) {
							messages.AddRange(settings.RocksAllDone.BuildSuggestionModel(600, ForModel.Create(meeting.Meeting), meeting));
						}

						messages = messages.OrderByDescending(x => x.Ordering).Take(settings.MaxPerPage).ToList();
						if (messages.Any()) {
							foreach (var m in messages) {
								s.Save(m);
							}
							tx.Commit();
							s.Flush();
						}
						existing.AddRange(messages);
						return existing;

					} catch (Exception e) {
						int a = 0;
					}

					return new List<SuggestionModel>();
				}
			}
		}
		public static IEnumerable<SuggestionModel> GetOrCreateTodoSuggestions(UserOrganizationModel caller, L10MeetingVM meeting) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var settings = GetIssueSuggestionsSettings(s);
					if (!IsSuggestionsEnabled(caller, meeting, settings))
						return new List<SuggestionModel>();

					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meeting.Meeting.Id);
					perms.ViewL10Page(meeting.SelectedPageId);

					try {
						var existing = s.QueryOver<SuggestionModel>().Where(x => x.MeetingId == meeting.Meeting.Id).List().ToList();
						if (existing.Any(x => x.PageType == L10PageType.Todo)) {
							return existing;
						}


						var messages = new List<SuggestionModel>();
						var todoCount = meeting.Todos.Count();

						var todoRatio = new Ratio();
						var todos = meeting.Todos;

						foreach (var todo in todos) {
							if (todo.CreateTime < meeting.Meeting.StartTime) {
								todoRatio.Add(todo.CompleteTime != null ? 1 : 0, 1);
							}
						}

						if (todoCount > 6 && (double)todoRatio.GetValue(0) < settings.TodoLowCompletion.Objective) {
							messages.AddRange(settings.TodoLowCompletion.BuildSuggestionModel(100, ForModel.Create(meeting.Meeting), meeting, todoRatio.GetValue(0) * 100));
						}

						if (todoCount > 6 && (double)todoRatio.GetValue(0) >= settings.TodoHighCompletion.Objective) {
							messages.AddRange(settings.TodoHighCompletion.BuildSuggestionModel(100, ForModel.Create(meeting.Meeting), meeting, todoRatio.GetValue(0) * 100));
						}

						messages = messages.OrderByDescending(x => x.Ordering).Take(settings.MaxPerPage).ToList();
						if (messages.Any()) {
							foreach (var m in messages) {
								s.Save(m);
							}
							tx.Commit();
							s.Flush();
						}
						existing.AddRange(messages);
						return existing;

					} catch (Exception e) {
						int a = 0;
					}

					return new List<SuggestionModel>();
				}
			}
		}


		#region Helpers
		public class IssueSuggstionsData {
			public bool Enabled { get; set; }
			public SuggestionConstants ScorecardGoalMet { get; set; }
			public SuggestionConstants ScorecardGoalMissed { get; set; }
			public SuggestionConstants ScorecardGoalEmpty { get; set; }

			public SuggestionConstants IdsOldIssue { get; set; }
			public SuggestionConstants IdsLongIssue { get; set; }

			public SuggestionConstants HeadlineFew { get; set; }
			public SuggestionConstants TodoLowCompletion { get; set; }
			public SuggestionConstants TodoHighCompletion { get; set; }

			public SuggestionConstants RocksOffTrackDueSoon { get; set; }
			public SuggestionConstants RocksOnTrackDueSoon { get; set; }
			public SuggestionConstants RocksOffTrackNWeeks { get; set; }
			public SuggestionConstants RocksDoneWeeks { get; set; }
			public SuggestionConstants RockMilestonesOffTrack { get; set; }
			public SuggestionConstants RocksAllDone { get; set; }

			public string[] IDS { get; set; }
			public bool UseAudit { get; set; }
			public int MaxPerPage { get; set; }
		}

		#region constants
		private static IssueSuggstionsData GetIssueSuggestionsSettings(ISession s) {
			return s.GetSettingOrDefault(Variable.Names.ISSUE_SUGGESTIONS, new IssueSuggstionsData {
				Enabled = true,
				UseAudit = true,
				MaxPerPage = 4,

				//GOALS (Used to be Rocks)
				RockMilestonesOffTrack = new SuggestionConstants(60, 1, L10PageType.Rocks, SuggestionType.Todo,
					"{0}", "{0} - {5} off track",
					"There {3} that are off track.<br/> <i>Take to-dos to complete the {4}?</i>",
					"Complete the overdue {4} for {0}",
					"{6}"
				),

				RocksAllDone = new SuggestionConstants(1, 1, L10PageType.Rocks, SuggestionType.Headline,
					"Goals", "Congratulations",
					"You're team has completed all its goals! Take a moment to celebrate.",
					"100% Goal Completion!",
					null
				),
				RocksDoneWeeks = new SuggestionConstants(1, 1, L10PageType.Rocks, SuggestionType.Headline,
					"{0}", "Congratulations",
					"{2} completed their goal, '{0}'!",
					"{0} was completed by {2}",
					null
				),
				RocksOffTrackDueSoon = new SuggestionConstants(1, 1, L10PageType.Rocks, SuggestionType.Issue,
					"{0}", "{0}",
					"Goal is off track and due {2}",
					"{0} - Off track and due {2}",
					null
				),
				RocksOnTrackDueSoon = new SuggestionConstants(1, 1, L10PageType.Rocks, SuggestionType.Issue,
					"{0}", "{0}",
					"Goal is on track and due {2}",
					"{0} - due {2}",
					null
				),
				RocksOffTrackNWeeks = new SuggestionConstants(1, 1, L10PageType.Rocks, SuggestionType.Issue,
					"{0}", "{0}",
					"Goal has been off track for {1,0:0} weeks",
					"{0} has been off track for {1,0:0} weeks",
					null
				),

				//ISSUES

				IdsOldIssue = new SuggestionConstants(60, 1, L10PageType.IDS, SuggestionType.TodoAll,
					"Issues", "Old Issues",
					"There are {0} issues that are more than 60 days old.<br/> <i>Take to-dos to clean up the issues list?</i>",
					"Clear up the {1} issues list",
					"There are {0} issues that are more than 60 days old."
				),
				IdsLongIssue = new SuggestionConstants(5, 1, L10PageType.IDS, SuggestionType.TodoAll,
					"Issues", "High Issue Count",
					"There are {0} issues.<br/> <i>Take to-dos to clean up the issues list?</i>",
					"Clear up the {1} issues list",
					"There are {0} issues."
				),

				//Metrics
				ScorecardGoalMet = new SuggestionConstants(4, 1, L10PageType.Scorecard, SuggestionType.Issue,
					"{0}", "Goal hit! {0}",
					"Congratulations! Goal hit {1} times in a row.<br/> <i> Do we need to increase the goal?</i>",
					"{0} - Increase the goal",
					"Goal hit {1} times in a row."
				),
				ScorecardGoalMissed = new SuggestionConstants(4, 1, L10PageType.Scorecard, SuggestionType.Issue,
					"{0}", "Goal missed: {0}",
					"Goal missed {1} times in a row. <br/> <i>Is this the right goal? Is {2} being held accountable?</i>",
					"{0} - Goal was missed",
					"Goal missed {1} times in a row. Is this the right goal? Is {2} being held accountable?"
				),

				ScorecardGoalEmpty = new SuggestionConstants(4, 1, L10PageType.Scorecard, SuggestionType.Issue,
					"{0}", "Score empty: {0}",
					"The measureable has been empty {1} times in a row. <br/> <i>Is this the right measureable? Is {2} being held accountable?</i>",
					"{0} is empty",
					"{0} has been empty {1} times in a row. Is this the right measureable? Is {2} being held accountable?"
				),

				//HEADLINES
				HeadlineFew = new SuggestionConstants(1, 1, L10PageType.Headlines, SuggestionType.Todo,
					"Headlines", "Recognize peers",
					"Let's create a culture of recognition. <br/> <i>Take a to-do to recognize and praise peers?</i>",
					"Recognize and praise peers",
					null
				),

				//TODOS
				TodoLowCompletion = new SuggestionConstants(.50, 1, L10PageType.Todo, SuggestionType.Issue,
					"To-dos", "To-do Completion",
					"To-do completion rates have a direct effect on the velocity of the organization.<br/> <i> Add an issue regarding to-do completion?</i>",
					"To-do completion percentage is low",
					"To-do completion rates have a direct effect on the velocity of the organization."
				),
				TodoHighCompletion = new SuggestionConstants(.9, 1, L10PageType.Todo, SuggestionType.Headline,
					"To-dos", "Congratulations!",
					"Nice work on a to-do completion percentage of {0,0:0}%<br/><i>Take a moment to celebrate.</i>",
					"{0,0:0}% To-do completion!",
					""
				),
				//ISSUE OF THE WEEK

				IDS = new[] {
					"What issue are you procrastinating?",
					"What is the elephant in the room?",
					"What is the most important issue we should be solving?",
					"What can we do to make decisions faster?",
					"How can we solve Issues better?",
					"What is not working company-wide?",
					"What is not working with our communication?",
					"What is not working with our accountabilities?",
					"What is not working with our implementation of the tools?",
					"What is the highest impact people decision we could make if we had the money?",
					"What topic are we uncomfortable with bringing up?",
					"What is not working with our Processes?",
					"What is not working with our Data?",
					"What is not working with our Issues?",
					"What is not working with our Vision?",
					"What is not working with our People?",
					"What is not working with our Execution?",
                    "What are we doing that is not in alignment with our Business Plan?",
					"What is holding us back?",
					"What would it take to transform our business?",
					"What are our blind-spots",
					"What red flags are we not addressing?",
					"Are we getting to the root of the issue?",
					"Are we solving the most important issues?",
					"Do we have a metric that enables us to be proactive?"
				}
			});

		}


		#endregion
		#endregion
	}


	public class SuggestionConstants {
		public SuggestionConstants(double objective, double weightMultiplier, L10PageType pageType, SuggestionType suggestionType,
			string aboutBuilder, string titleBuilder, string detailsBuilder, string modalTitleBuilder, string modalDetailsBuilder) {
			PageType = pageType;
			SuggestionType = suggestionType;
			AboutBuilder = aboutBuilder;
			TitleBuilder = titleBuilder;
			DetailsBuilder = detailsBuilder;
			ModalTitleBuilder = modalTitleBuilder;
			ModalDetailsBuilder = modalDetailsBuilder;
			WeightMultiplier = weightMultiplier;
			Objective = objective;

		}

		public L10PageType PageType { get; set; }
		public SuggestionType SuggestionType { get; set; }
		public string AboutBuilder { get; set; }
		public string TitleBuilder { get; set; }
		public string DetailsBuilder { get; set; }
		public double WeightMultiplier { get; set; }
		public double Objective { get; set; }
		public string ModalTitleBuilder { get; set; }
		public string ModalDetailsBuilder { get; set; }
		public bool Disable { get; set; }
		public SuggestionConstants() {
		} 

		public IEnumerable<SuggestionModel> BuildSuggestionModel(int weight, ForModel forModel, L10MeetingVM meeting, params object[] replacements) {
			try {
				if (!Disable) {
					string about = AboutBuilder.NotNull(x => string.Format(x, replacements));
					string title = TitleBuilder.NotNull(x => string.Format(x, replacements));
					string details = DetailsBuilder.NotNull(x => string.Format(x, replacements));
					string modalTitle = ModalTitleBuilder.NotNull(x => string.Format(x, replacements));
					string modalDetails = ModalDetailsBuilder.NotNull(x => string.Format(x, replacements));

					return new[] {
						new SuggestionModel(
							(int)Math.Ceiling(weight * WeightMultiplier), forModel, meeting,
							PageType, SuggestionType,
							about, title, details,
							modalTitle,modalDetails
						)
					};
				}
			} catch (Exception e) {
			}
			return new SuggestionModel[] { };
		}

	}
}

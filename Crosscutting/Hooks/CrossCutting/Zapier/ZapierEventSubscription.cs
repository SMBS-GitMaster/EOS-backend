using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using RadialReview.Accessors;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Issues;
using RadialReview.Models.Scorecard;
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Utilities;
using RadialReview.Hangfire;
using Hangfire;
using RadialReview.Crosscutting.Schedulers;
using log4net;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Models.Process.Execution;
using RadialReview.Models.Process;
using RadialReview.Crosscutting.Hangfire.Debounce;

namespace RadialReview.Crosscutting.Hooks.CrossCutting.Zapier {

	public class ZapierUpdateData {

		public const string TODO = "todo";
		public const string MEASURABLE = "measurable";
		public const string ISSUE = "issue";
		public const string ROCK = "goal";
		public const string HEADLINE = "headline";
		public const string SCORE = "score";
		public const string PROCESS = "process";
		public const string PROCESS_STEP = "process_step";

		[Obsolete("Do not use")]
		public ZapierUpdateData() {
		}

		private ZapierUpdateData(string type, string operation, List<string> fields) {
			Type = type;
			Operation = operation;
			Fields = fields;
			Timestamp = DateTime.UtcNow;
		}

		public ZapierUpdateData Add(string field, bool include) {
			if (include) {
				Fields.Add(field);
			}
			return this;
		}

		public string ToJsonString() {
			return JsonConvert.SerializeObject(this);
		}

		public static ZapierUpdateData FromJsonString(string str) {
			try {
				return JsonConvert.DeserializeObject<ZapierUpdateData>(str);
			} catch (Exception e) {
				return null;
			}
		}


		public string Type { get; set; }
		public string Operation { get; set; }
		public DateTime Timestamp { get; set; }
		public List<string> Fields { get; set; }

		public static ZapierUpdateData Create(string type) {
			return new ZapierUpdateData(type, "create", null);
		}
		public static ZapierUpdateData Update(string type) {
			return new ZapierUpdateData(type, "update", new List<string>());
		}
		public static ZapierUpdateData Delete(string type) {
			return new ZapierUpdateData(type, "delete", null);
		}
		public static ZapierUpdateData Attach(string type) {
			return new ZapierUpdateData(type, "attach", null);
		}
		public static ZapierUpdateData Started(string type) {
			return new ZapierUpdateData(type, "started", null);
		}
		public static ZapierUpdateData Completed(string type) {
			return new ZapierUpdateData(type, "completed", null);
		}
		public static ZapierUpdateData RevertCompleted(string type) {
			return new ZapierUpdateData(type, "decompleted", null);
		}
	}


	public class ZapierEventSubscription : ITodoHook, IRockHook, IIssueHook, IMeasurableHook, IHeadlineHook, IMeetingRockHook, IMeetingMeasurableHook, IScoreHook, IProcessHook, IProcessExecutionHook, IProcessExecutionStepHook {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


		#region Todo
		public async Task CreateTodo(ISession s, UserOrganizationModel caller, TodoModel todo) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, todo.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierTodo_Hangfire(todo.Id, ZapierEvents.new_todo, ZapierUpdateData.Create(ZapierUpdateData.TODO).ToJsonString()));
			}
		}
		public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, todo.OrganizationId)) {
				var update = ZapierUpdateData.Update(ZapierUpdateData.TODO)
								.Add("name", updates.MessageChanged)
								.Add("due_date", updates.DueDateChanged)
								.Add("complete", updates.CompletionChanged)
								.Add("owner_id", updates.AccountableUserChanged)
								.ToJsonString();
				Scheduler.Enqueue(() => ZapierTodo_Hangfire(todo.Id, ZapierEvents.update_todo, update));
			}
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierTodo_{0}")]
		public async static Task ZapierTodo_Hangfire(long todoId, ZapierEvents @event, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var todoModel = s.Get<TodoModel>(todoId);
					var todo = new AngularTodo(todoModel);

					var meeting_id = todoModel.ForRecurrenceId;
					string meeting_name = null;
					if (meeting_id != null) {
						meeting_name = s.Get<L10Recurrence>(meeting_id.Value).Name;
					}

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, todoModel.Organization.Id, @event)
															  .GetSubscriptionResponses(todoId, todo.Owner.Id, todo.L10RecurrenceId, x => x.ViewTodo(todoId), ctx => new {
																  id = todo.Id,
																  name = todo.Name,
																  complete = todo.Complete == true,
																  due_date = todo.DueDate,
																  create_time = todo.CreateTime,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(todo.Owner.Id, false), todo.Owner.Id, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(todo.Owner.Id, false), todo.Owner.Name, null),
																  meeting_id = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), (long?)meeting_id, null),
																  meeting_name = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), meeting_name, null),
                                                                  pad_id = todo.GetPadId(),
                                                                  updates = updates
															  });

					await PostEventToZapier(subscriptionResponses);
				}
			}
		}
		#endregion

		#region Goal (Used to be Rock)
		public async Task CreateRock(ISession s, UserOrganizationModel caller, RockModel rock) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, rock.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierRock_Hangfire(rock.Id, ZapierEvents.new_rock, ZapierUpdateData.Create(ZapierUpdateData.ROCK).ToJsonString()));
			}
		}
		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, rock.OrganizationId)) {
				var update = ZapierUpdateData.Update(ZapierUpdateData.ROCK)
								.Add("name", updates.MessageChanged)
								.Add("due_date", updates.DueDateChanged)
								.Add("status", updates.StatusChanged)
								.Add("owner_id", updates.AccountableUserChanged)
								.ToJsonString();
				Scheduler.Enqueue(() => ZapierRock_Hangfire(rock.Id, ZapierEvents.update_rock, update));
			}
		}
		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//Noop
		}


		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierRock_{0}")]
		public async static Task ZapierRock_Hangfire(long rockId, ZapierEvents @event, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var rockModel = s.Get<RockModel>(rockId);
					var rock = new AngularRock(rockModel, null);

					L10Recurrence recurAlias = null;
					var recurrences = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
										.JoinAlias(x => x.L10Recurrence, () => recurAlias)
										.Where(x => x.ForRock.Id == rock.Id && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id, x => recurAlias.Name)
										.List<object[]>()
										.Select(x => new { Name = (string)x[1], Id = (long)x[0] })
										.ToArray();

					var recurrenceIds = recurrences.Select(x => x.Id).ToArray();


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, rockModel.OrganizationId, @event)
															  .GetSubscriptionResponses(rockId, rock.Owner.Id, recurrenceIds, x => x.ViewRock(rockId), ctx => new {
																  id = rock.Id,
																  name = rock.Name,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(rock.Owner.Id, false), rock.Owner.Id, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(rock.Owner.Id, false), rock.Owner.Name, null),
																  create_time = rock.CreateTime,
																  status = rock.Completion,
																  complete = rock.Complete == true,
																  due_date = rock.DueDate,
																  updates = updates
															  });
					await PostEventToZapier(subscriptionResponses);
				}
			}
		}


		public async Task UnArchiveRock(ISession s, RockModel rock, bool v) {
			// Noop
		}
		public async Task UndeleteRock(ISession s, RockModel rock) {
			//Nothing
		}

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, rock.OrganizationId)) {
				Scheduler.Enqueue(() => AttachRockInZapier_Hangfire(rock.Id, recurRock.L10Recurrence.Id, ZapierUpdateData.Attach(ZapierUpdateData.ROCK).ToJsonString()));
			}
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierRockAttach_{0}")]
		public async static Task AttachRockInZapier_Hangfire(long rockId, long recurrenceId, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var rockModel = s.Get<RockModel>(rockId);
					var rockOwnerName = rockModel.AccountableUser.NotNull(x => x.GetName());
					var recurrenceModel = s.Get<L10Recurrence>(recurrenceId);


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, rockModel.OrganizationId, ZapierEvents.attach_rock)
														  .GetSubscriptionResponses(rockId, rockModel.AccountableUser.Id, recurrenceId, x => x.ViewRock(rockId).ViewL10Recurrence(recurrenceId), ctx => new {
															  rock_id = rockId,
															  rock_name = rockModel.Name,
															  rock_owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(rockModel.AccountableUser.Id, false), rockModel.AccountableUser.Id, null),
															  rock_owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(rockModel.AccountableUser.Id, false), rockOwnerName, null),
															  meeting_id = recurrenceId,
															  meeting_name = recurrenceModel.Name,
															  updates = updates
														  });
					await PostEventToZapier(subscriptionResponses);
				}
			}
		}

		public async Task DetachRock(ISession s, RockModel rock, long recurrenceId, IMeetingRockHookUpdates updates) {
			// Noop
		}

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			// Noop
		}

		#endregion

		#region Issue
		public async Task CreateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, issue.Issue.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierIssue_Hangfire(issue.Id, ZapierEvents.new_issue, ZapierUpdateData.Create(ZapierUpdateData.ISSUE).ToJsonString()));
			}
		}

		public async Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue, IIssueHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, issue.Issue.OrganizationId)) {
				var u = ZapierUpdateData.Update(ZapierUpdateData.ISSUE)
					.Add("name", updates.MessageChanged)
					.Add("complete", updates.CompletionChanged)
					.Add("owner_id", updates.OwnerChanged)
					.Add("meeting_id", updates.MovedToRecurrence != null)
					.ToJsonString();
				Scheduler.Enqueue(() => ZapierIssue_Hangfire(issue.Id, ZapierEvents.update_issue, u));
			}
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierIssue_{0}")]
		public async static Task ZapierIssue_Hangfire(long issueRecurrenceId, ZapierEvents @event, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);


					var meeting_id = issueRecur.Recurrence.NotNull(x => (long?)x.Id);
					string meeting_name = null;
					if (meeting_id != null) {
						meeting_name = s.Get<L10Recurrence>(meeting_id.Value).Name;
					}

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, issueRecur.Issue.Organization.Id, @event)
															  .GetSubscriptionResponses(issueRecurrenceId, issueRecur.Owner.Id, issueRecur.Recurrence.Id, x => x.ViewIssue(issueRecur.Issue.Id), ctx => new {
																  id = issueRecur.Id,
																  name = issueRecur.Issue.Message,
																  create_time = issueRecur.CreateTime,
																  complete = issueRecur.CloseTime != null,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(issueRecur.Owner.Id, false), issueRecur.Owner.Id, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(issueRecur.Owner.Id, false), issueRecur.Owner.Name, null),
																  meeting_id = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), meeting_id, null),
																  meeting_name = meeting_id == null ? null : ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id.Value), meeting_name, null),
																  updates = updates
															  });

					await PostEventToZapier(subscriptionResponses);
				}
			}
		}
		#endregion

		#region Headlines
		public async Task CreateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, headline.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierHeadline_Hangfire(headline.Id, ZapierEvents.new_headline, ZapierUpdateData.Create(ZapierUpdateData.HEADLINE).ToJsonString()));
			}
		}
		public async Task UpdateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline, IHeadlineHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, headline.OrganizationId)) {
				var angularHeadline = new AngularHeadline(headline);
				var u = ZapierUpdateData.Update(ZapierUpdateData.HEADLINE)
						.Add("name", updates.MessageChanged)
						.ToJsonString();
				Scheduler.Enqueue(() => ZapierHeadline_Hangfire(headline.Id, ZapierEvents.update_headline, u));
			}
		}
		public async Task ArchiveHeadline(ISession s, PeopleHeadline headline) {
			// Noop
		}

		public async Task UnArchiveHeadline(ISession s, PeopleHeadline headline) {
			// Noop
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierHeadline_{0}")]
		public async static Task ZapierHeadline_Hangfire(long headlineId, ZapierEvents @event, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var headline = s.Get<PeopleHeadline>(headlineId);
					var meeting_id = headline.RecurrenceId;
					string meeting_name = s.Get<L10Recurrence>(meeting_id).Name;
					string owner_name = headline.Owner.NotNull(x => x.GetName());

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, headline.OrganizationId, @event)
															  .GetSubscriptionResponses(headline.Id, headline.OwnerId, headline.RecurrenceId, x => x.ViewHeadline(headline.Id), ctx => new {
																  id = headline.Id,
																  name = headline.Message,
																  create_time = headline.CreateTime,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(headline.OwnerId, false), headline.OwnerId, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(headline.OwnerId, false), owner_name, null),
																  meeting_id = ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id), (long?)meeting_id, null),
																  meeting_name = ctx.ShowIfPermitted(x => x.ViewL10Recurrence(meeting_id), meeting_name, null),
																  updates = updates
															  });

					await PostEventToZapier(subscriptionResponses);
				}
			}
		}

		#endregion

		#region Measurable	

		public async Task CreateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> createdScores) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, measurable.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierMeasurable_Hangfire(measurable.Id, ZapierEvents.new_measurable, ZapierUpdateData.Create(ZapierUpdateData.MEASURABLE).ToJsonString()));
			}
		}
		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, measurable.OrganizationId)) {
				var u = ZapierUpdateData.Update(ZapierUpdateData.MEASURABLE)
						.Add("name", updates.MessageChanged)
						.Add("owner_id", updates.AccountableUserChanged)
						.Add("target", updates.GoalChanged)
						.Add("target_alt", updates.AlternateGoalChanged)
						.Add("target_dir", updates.GoalDirectionChanged)
						.Add("units", updates.UnitTypeChanged)
						.ToJsonString();
				Scheduler.Enqueue(() => ZapierMeasurable_Hangfire(measurable.Id, ZapierEvents.update_measurable, u));
			}
		}
		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {
			// Noop
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierMeasurable_{0}")]
		public async static Task ZapierMeasurable_Hangfire(long measurableId, ZapierEvents @event, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {


					var measurableModel = s.Get<MeasurableModel>(measurableId);
					L10Recurrence recurAlias = null;
					var recurrences = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
										.JoinAlias(x => x.L10Recurrence, () => recurAlias)
										.Where(x => x.Measurable.Id == measurableId && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id, x => recurAlias.Name)
										.List<object[]>()
										.Select(x => new { Name = (string)x[1], Id = (long)x[0] })
										.ToArray();

					var recurrenceIds = recurrences.Select(x => x.Id).ToArray();

					var ownerName = measurableModel.AccountableUser.NotNull(x => x.GetName());


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, measurableModel.OrganizationId, @event)
															  .GetSubscriptionResponses(measurableId, measurableModel.AccountableUserId, recurrenceIds, x => x.ViewMeasurable(measurableId), ctx => new {
																  id = measurableId,
																  name = measurableModel.Title,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableModel.AccountableUserId, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), ownerName, null),
																  create_time = measurableModel.CreateTime,
																  target = measurableModel.Goal,
																  target_alt = measurableModel.AlternateGoal,
																  target_dir = measurableModel.GoalDirection,
																  units = measurableModel.UnitType,
																  updates = updates
															  });
					await PostEventToZapier(subscriptionResponses);
				}
			}
		}

		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, measurable.OrganizationId)) {
				Scheduler.Enqueue(() => ZapierMeasurableAttach_Hangfire(measurable.Id, recurMeasurable.L10Recurrence.Id, ZapierUpdateData.Attach(ZapierUpdateData.MEASURABLE).ToJsonString()));
			}
		}
		public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
			// Noop
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierMeasurableAttach_{0}")]
		public async static Task ZapierMeasurableAttach_Hangfire(long measurableId, long recurrenceId, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var measurableModel = s.Get<MeasurableModel>(measurableId);
					var measurableOwnerName = measurableModel.AccountableUser.NotNull(x => x.GetName());
					var recurrenceModel = s.Get<L10Recurrence>(recurrenceId);


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, measurableModel.OrganizationId, ZapierEvents.attach_measurable)
														  .GetSubscriptionResponses(measurableId, measurableModel.AccountableUserId, recurrenceId, x => x.ViewMeasurable(measurableId).ViewL10Recurrence(recurrenceId), ctx => new {
															  measurable_id = measurableId,
															  measurable_name = measurableModel.Title,
															  measurable_owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableModel.AccountableUserId, null),
															  measurable_owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableOwnerName, null),
															  meeting_id = recurrenceId,
															  meeting_name = recurrenceModel.Name,
															  updates = updates
														  });
					await PostEventToZapier(subscriptionResponses);

				}
			}
		}
    #endregion

    #region Scores
    public async Task CreateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates)
    {
      // noop
    }

    public async Task UpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			var enabledOrgs = scoreAndUpdates.Select(x => x.score.OrganizationId)
											 .Distinct()
											 .Where(x => ZapierAccessor.IsZapierEnabled_Unsafe(s, x))
											 .ToList();

			var enabledScores = scoreAndUpdates.Where(x => enabledOrgs.Contains(x.score.OrganizationId)).ToList();

			if (enabledScores.Any()) {
				foreach (var scoreId in enabledScores.Select(x => x.score.Id).Distinct()) {
					Scheduler.Enqueue(() => ZapierScore_Hangfire(scoreId, ZapierUpdateData.Update(ZapierUpdateData.SCORE).Add("value", true).ToJsonString()));
				}
			}
		}


		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierScore_{0}")]
		public async static Task ZapierScore_Hangfire(long scoreId, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var scoreModel = s.Get<ScoreModel>(scoreId);
					var measurableModel = scoreModel.Measurable;
					var measurableOwner = measurableModel.AccountableUser.NotNull(x => x.GetName());

					L10Recurrence recurAlias = null;
					var recurrences = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
										.JoinAlias(x => x.L10Recurrence, () => recurAlias)
										.Where(x => x.Measurable.Id == measurableModel.Id && x.DeleteTime == null)
										.Select(x => x.L10Recurrence.Id, x => recurAlias.Name)
										.List<object[]>()
										.Select(x => new { Name = (string)x[1], Id = (long)x[0] })
										.ToArray();

					var recurrenceIds = recurrences.Select(x => x.Id).ToArray();


					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, measurableModel.OrganizationId, ZapierEvents.update_score)
															  .GetSubscriptionResponses(measurableModel.Id, measurableModel.AccountableUserId, recurrenceIds, x => x.ViewScore(scoreModel.Id), ctx => new {
																  id = scoreModel.Id,
																  title = measurableModel.Title,
																  owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableModel.AccountableUserId, null),
																  owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(measurableModel.AccountableUserId, false), measurableOwner, null),
																  week = scoreModel.DataContract_ForWeek,
																  measurable_id = measurableModel.Id,
																  date_entered = scoreModel.DateEntered,
																  value = scoreModel.Measured,
																  target = scoreModel.OriginalGoal,
																  target_alt = scoreModel.AlternateOriginalGoal,
																  target_dir = measurableModel.GoalDirection,
																  units = measurableModel.UnitType,
																  target_met = scoreModel.MeetGoal(),
																  updates = updates
															  });
					await PostEventToZapier(subscriptionResponses);


















				}
			}
		}

		public async Task PreSaveUpdateScores(ISession s, List<ScoreAndUpdates> scoreAndUpdates) {
			//Noop
		}

		public async Task RemoveFormula(ISession ses, long measurableId) {
			//Noop
		}

		public async Task PreSaveRemoveFormula(ISession s, long measurableId) {
			//Noop
		}
		#endregion

		#region Process
		#region Execution
		public async Task ProcessExecutionStarted(ISession s, long startedById, ProcessExecution processExecution) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, processExecution.OrgId)) {
				var u = ZapierUpdateData.Started(ZapierUpdateData.PROCESS).ToJsonString();
				Scheduler.Enqueue(() => ZapierProcessExecution_Hangfire(processExecution.Id, ZapierEvents.start_process, u));
			}
		}
		public async Task ProcessExecutionCompleted(ISession s, bool complete, long completedById, ProcessExecution processExecution) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, processExecution.OrgId)) {
				if (complete) {
					var u = ZapierUpdateData.Completed(ZapierUpdateData.PROCESS).ToJsonString();
					Scheduler.Enqueue(() => ZapierProcessExecution_Hangfire(processExecution.Id, ZapierEvents.complete_process, u));
				} else{
					var u = ZapierUpdateData.RevertCompleted(ZapierUpdateData.PROCESS).ToJsonString();
					Scheduler.Enqueue(() => ZapierProcessExecution_Hangfire(processExecution.Id, ZapierEvents.complete_process, u));
				}
			}
		}

		public async Task ProcessExecutionConclusionForced(ISession s, long forcedByUserId, ProcessExecution processExecution) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, processExecution.OrgId)) {
				var u = ZapierUpdateData.Delete(ZapierUpdateData.PROCESS).ToJsonString();
				Scheduler.Enqueue(() => ZapierProcessExecution_Hangfire(processExecution.Id, ZapierEvents.delete_process, u));
			}
		}


		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierProcessExecution_{0}")]
		public async static Task ZapierProcessExecution_Hangfire(long executionId, ZapierEvents @event, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var execution = s.Get<ProcessExecution>(executionId);
					var owner = s.Get<UserOrganizationModel>(execution.ExecutedBy);

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, execution.OrgId, @event)
														  .GetSubscriptionResponses(executionId, execution.ExecutedBy, new long[] { }, x => x.ViewProcess(execution.ProcessId), ctx => new {
															  id = executionId,
															  process_id = execution.ProcessId,
															  execution_name = execution.Name,
															  execution_description = execution.Description,
															  execution_owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(execution.ExecutedBy, false), owner.GetName(), null),
															  execution_owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(execution.ExecutedBy, false), execution.ExecutedBy, null),
															  updates = updates
														  });
					await PostEventToZapier(subscriptionResponses);

				}
			}
		}

		#endregion
		#region Process
		public async Task CreateProcess(ISession s, ProcessModel process) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, process.OrgId)) {
				var u = ZapierUpdateData.Create(ZapierUpdateData.PROCESS).ToJsonString();
				Scheduler.Enqueue(() => ZapierProcess_Hangfire(process.Id, ZapierEvents.new_process, u, null));
			}
		}

		public async Task UpdateProcess(ISession s, ProcessModel process, IProcessHookUpdates updates) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, process.OrgId)) {
				var u = ZapierUpdateData.Update(ZapierUpdateData.PROCESS)
						.Add("process_name", updates.NameChanged)
						.Add("process_description", updates.DescriptionChanged)
						.Add("process_owner_name", updates.OwnerChanged)
						.Add("process_owner_id", updates.OwnerChanged)
						.Add("step_updates", updates.StepAltered)
						.ToJsonString();

				Dictionary<string, object> step_updates = null;
				if (updates.StepAltered) {
					var step = s.Get<ProcessStep>(updates.StepUpdates.ForStepId);
					step_updates = GetUpdateStep(updates.StepUpdates, step);
				}

				Scheduler.Enqueue(() => ZapierProcess_Hangfire(process.Id, ZapierEvents.update_process, u, step_updates));
			}
		}

		private Dictionary<string, object> GetUpdateStep(IProcessHookUpdates_StepUpdate updates, ProcessStep step) {
			if (updates == null)
				return null;

			var output = new Dictionary<string, object>();
			output["step_id"] = updates.ForStepId;
			switch (updates.Kind) {
				case StepUpdateKind.AppendStep:
					output["kind"] = "append";
					output["name_update"] = step.Name;
					output["details_update"] = step.Details;
					break;
				case StepUpdateKind.EditStep:
					output["kind"] = "edit";
					if (updates.NameChanged) {
						output["name_update"] = step.Name;
					}
					if (updates.DetailsChanged) {
						output["details_update"] = step.Details;
					}
					break;
				case StepUpdateKind.RemoveStep:
					output["kind"] = "remove";
					break;
				case StepUpdateKind.ReorderStep:
					output["kind"] = "reorder";
					output["old_parent_step"] = updates.OldStepParent;
					output["new_parent_step"] = updates.NewStepParent;
					output["old_index"] = updates.OldStepIndex;
					output["new_index"] = updates.NewStepIndex;
					break;
				default:
					break;
			}
			return output;
		}


		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierProcess_{0}")]
		public async static Task ZapierProcess_Hangfire(long processId, ZapierEvents @event, string updatesStr, Dictionary<string, object> step_updates) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var process = s.Get<ProcessModel>(processId);
					var owner = s.Get<UserOrganizationModel>(process.OwnerId);

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, process.OrgId, @event)
														  .GetSubscriptionResponses(processId, process.OwnerId, new long[] { }, x => x.ViewProcess(process.Id), ctx => new {
															  id = processId,
															  process_name = process.Name,
															  process_description = process.Description,
															  process_owner_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(process.OwnerId, false), owner.GetName(), null),
															  process_owner_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(process.OwnerId, false), process.OwnerId, null),
															  step_updates = step_updates,
															  updates = updates
														  });
					await PostEventToZapier(subscriptionResponses);
				}
			}
		}
		#endregion
		#region Steps

		public async Task CompleteStep(ISession s, bool complete, long completedById, ProcessExecution processExecution, ProcessExecutionStep stepExecution) {
			if (ZapierAccessor.IsZapierEnabled_Unsafe(s, stepExecution.OrgId)) {
				var u = ZapierUpdateData.Update(ZapierUpdateData.PROCESS_STEP)
							.Add("complete", true)
							.ToJsonString();
				Scheduler.Enqueue(() => ZapierProcessExecutionStep_Hangfire(stepExecution.Id, complete, completedById, ZapierEvents.complete_process_step, u));
			}
		}

		[Queue(HangfireQueues.Immediate.ZAPIER_EVENTS)]
		[AutomaticRetry(Attempts = 0)]
		[Debounce(6, "ZapierProcessExecutionStep_{0}")]
		public async static Task ZapierProcessExecutionStep_Hangfire(long executionStepId, bool complete, long completedById, ZapierEvents @event, string updatesStr) {
			var updates = ZapierUpdateData.FromJsonString(updatesStr);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var step = s.Get<ProcessExecutionStep>(executionStepId);
					var execution = s.Get<ProcessExecution>(step.ProcessExecutionId);
					var byUser = s.Get<UserOrganizationModel>(completedById);

					var subscriptionResponses = ZapierAccessor.GetZapierSubscriptions_unsafe(s, step.OrgId, @event)
														  .GetSubscriptionResponses(step.StepId, completedById, new long[] { }, x => x.ViewProcess(step.ProcessId), ctx => new {
															  id = executionStepId,
															  step_id = step.StepId,
															  step_name = step.Name,
															  step_details = step.Details,
															  complete = complete,
															  process_id = step.ProcessId,
															  execution_id = execution.Id,
															  execution_name = execution.Name,
															  execution_description = execution.Description,
															  by_user_name = ctx.ShowIfPermitted(x => x.ViewUserOrganization(byUser.Id, false), byUser.GetName(), null),
															  by_user_id = ctx.ShowIfPermitted<long?>(x => x.ViewUserOrganization(byUser.Id, false), byUser.Id, null),
															  updates = updates
														  });
					await PostEventToZapier(subscriptionResponses);

				}
			}
		}

		#endregion
		#endregion

		private async static Task PostEventToZapier(List<ZapierSubscriptionQuery.SubscriptionResponse> subscriptionResponses) {
			if (subscriptionResponses.Any()) {
				//send to any subscriptions to this event for this user to zapier
				using (HttpClient httpClient = new HttpClient()) {
					foreach (var sr in subscriptionResponses) {
						StringContent httpContent = new StringContent(sr.Serialized, Encoding.UTF8, "application/json");
						HttpResponseMessage response = await httpClient.PostAsync(sr.Subscription.TargetUrl, httpContent);

					}
				}
			}

		}

    public async Task SendIssueTo(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence sourceIssue, IssueModel.IssueModel_Recurrence destIssue)
    {
      //noop
    }

    #region MISC
    public HookPriority GetHookPriority() {
			return HookPriority.Low;
		}
		public bool AbsorbErrors() {
			return true;
		}

		public bool CanRunRemotely() {
			return false;
		}






		#endregion

	}
}

using Newtonsoft.Json;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.Encrypt;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Models.Issues.IssueModel;
using NHibernate;

namespace RadialReview.Accessors {
	public class OrgExport : BaseAccessor {


		public static class ObjectToDictionaryHelper {

			public static IDictionary<string, object> ToDictionary(object source) {
				if (source == null) {
					ThrowExceptionWhenSourceArgumentIsNull();
				}

				var dictionary = new Dictionary<string, object>();
				foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source)) {
					AddPropertyToDictionary<object>(property, source, dictionary);
				}

				return dictionary;
			}

			private static void AddPropertyToDictionary<T>(PropertyDescriptor property, object source, Dictionary<string, T> dictionary) {
				object value = property.GetValue(source);
				if (IsOfType<T>(value)) {
					dictionary.Add(property.Name, (T)value);
				}
			}

			private static bool IsOfType<T>(object value) {
				return value is T;
			}

			private static void ThrowExceptionWhenSourceArgumentIsNull() {
				throw new ArgumentNullException("source", "Unable to convert object to a dictionary. The source object is null.");
			}
		}
		public class JsonOrg {

			public object Users { get; set; }
			public object Meetings { get; set; }
			public object Rocks { get; set; }
			public object Milestones { get; set; }
			public object Measurables { get; set; }
			public object Scores { get; set; }
			public object Todos { get; set; }
			public object Headlines { get; set; }
			public object Issues { get; set; }
			public object MeetingAttendees { get; set; }
			public object MeetingRocks { get; set; }
			public object MeetingMeasurables { get; set; }
			public object AccountabilityChartNodes { get; set; }
			public object AccountabilityChartRoleGroups { get; set; }
			public object AccountabilityChartRoles { get; set; }
			public object AccountabilityChartRolesMap { get; set; }
			public object AccountabilityChartPositions { get; set; }
			public object Vtos { get; set; }
			public object VtoStrings { get; set; }
			public object VtoKVs { get; set; }
			public object VtoMarketingStrategies;
			public object PermissionItems { get; set; }
			public object Organization { get; set; }
			public object AccountabilityChartRolesMap_Deprecated { get; set; }
			public object Roles { get; set; }
			public object Values { get; set; }


		}





		public static string GetUsers(UserOrganizationModel caller, long orgId) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.RadialAdmin(true);

					var userOrgs = s.QueryOver<UserOrganizationModel>()
						.Where(x => x.Organization.Id == orgId && x.DeleteTime == null)
						.List().ToList();




					var str = JsonConvert.SerializeObject(userOrgs.Where(x => x.User != null).Select(x => new {
						x.User.UserName,
						x.User.FirstName,
						x.User.LastName,
						x.User.Id,
						x.User.CreateTime,
						x.User.DeleteTime,
						x.User.SendTodoTime,
						Password = x.User.PasswordHash,
						ImageUrl = x.User.ImageGuid,
					}).ToList(), Formatting.Indented);

					var encryptKey = Config.GetAppSetting("V2_UserEncryptionKey", null);
					if (encryptKey == null) {
						throw new Exception("null-key");
					}

					var res = EncryptionUtility.Encrypt(str, encryptKey);
					var _test = EncryptionUtility.Decrypt(res, encryptKey);

					if (str != _test)
						throw new Exception("Decrypt Failed");

					return res;
				}
			}
		}

		#region Private Org Export Models

		private class OrgExportUser {
			public DateTime AttachTime { get; set; }
			public string ClientOrganizationName { get; set; }

			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public DateTime? DetachTime { get; set; }
			public string EmailAtOrganization_DONTUSE { get; set; }
			public bool EvalOnly { get; set; }
			public long Id { get; set; }
			 public bool IsClient { get; set; }
			public bool IsImplementer { get; set; }
			public bool IsPlaceholder { get; set; }
			public bool IsManager { get; set; } //= x.ManagerAtOrganization,
			public bool IsOrgAdmin { get; set; } // = x.ManagingOrganization,
			public string Name { get; set; }
			public long OrgId { get; set; } // = x.Organization.Id
			public string UserModelId { get; set; } //  = x.User.NotNull(y => y.Id),

        }

        #endregion
        private async Task<List<OrgExportUser>> getUserOrganizationModelDataAsync(UserOrganizationModel caller, long orgId) {

			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var users = s.QueryOver<UserOrganizationModel>()
								.Where(x => x.DeleteTime == null && x.Organization.Id == orgId)
								.List().Select(x => new OrgExportUser {
									AttachTime = x.AttachTime,
									ClientOrganizationName = x.ClientOrganizationName,
									CreateTime = x.CreateTime,
									DeleteTime = x.DeleteTime,
									DetachTime = x.DetachTime,
									EmailAtOrganization_DONTUSE = x.EmailAtOrganization,
									EvalOnly = x.EvalOnly,
									Id = x.Id,
									IsClient = x.IsClient,
									IsImplementer = x.IsImplementer,
									IsPlaceholder = x.IsPlaceholder,
									IsManager = x.ManagerAtOrganization,
									IsOrgAdmin = x.ManagingOrganization,
									Name = x.Name,
									OrgId = x.Organization.Id,
									UserModelId = x.User.NotNull(y => y.Id),
								}).ToList();
						return users;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data users from UserOrganizationModel : ", e);
				return new List<OrgExportUser>();
			}
		}

		private class OrgExportMeeting {
			public bool AttendingOffByDefault { get; set; }
			public bool CombineRocks { get; set; }
			public bool CountDown { get; set; }
			public long CreatedById { get; set; }
			public DateTime CreateTime { get; set; }
			public int CurrentWeekHighlightShift { get; set; }
			public long? DefaultIssueOwner { get; set; }
			public long DefaultTodoOwner { get; set; }
			public DateTime? DeleteTime { get; set; }
			public bool EnableTranscription { get; set; }
			public string ForumCode { get; set; }
			public ForumStep ForumStep { get; set; }
			public string HeadlinesId { get; set; }
			public Models.Enums.PeopleHeadlineType HeadlineType { get; set; }
			public long Id { get; set; }
			public bool IncludeAggregateTodoCompletion { get; set; }
			public bool IncludeAggregateTodoCompletionOnPrintout { get; set; }
			public bool IncludeIndividualTodos { get; set; }
			public long? MeetingInProgress { get; set; }
			public MeetingType MeetingType { get; set; }
			public string Name { get; set; }
			public string OrderIssueBy { get; set; }
			public long OrgId { get; set; }
			public bool PrintOutRockStatus { get; set; }
			public PrioritizationType Prioritization { get; set; }
			public bool Pristine { get; set; }
			public bool ReverseScorecard { get; set; }
			public Models.Enums.L10RockType RockType { get; set; }
			public string ZoomId { get; set; }
			public DayOfWeek? StartOfWeekOverride { get; set; }
			public L10TeamType TeamType { get; set; }
			public string VideoId { get; set; }
			public long VtoId { get; set; }

		}

		private async Task<List<OrgExportMeeting>> getL10RecurrenceDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var meetings = s.QueryOver<L10Recurrence>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new OrgExportMeeting{
							AttendingOffByDefault = x.AttendingOffByDefault,
							CombineRocks = x.CombineRocks,
							CountDown = x.CountDown,
							CreatedById = x.CreatedById,
							CreateTime = x.CreateTime,
							CurrentWeekHighlightShift = x.CurrentWeekHighlightShift,
							DefaultIssueOwner = x.DefaultIssueOwner,
							DefaultTodoOwner = x.DefaultTodoOwner,
							DeleteTime = x.DeleteTime,
							EnableTranscription = x.EnableTranscription,
							ForumCode = x.ForumCode,
							ForumStep = x.ForumStep,
							HeadlinesId = x.HeadlinesId,
							HeadlineType = x.HeadlineType,
							Id = x.Id,
							IncludeAggregateTodoCompletion = x.IncludeAggregateTodoCompletion,
							IncludeAggregateTodoCompletionOnPrintout = x.IncludeAggregateTodoCompletionOnPrintout,
							IncludeIndividualTodos = x.IncludeIndividualTodos,
							MeetingInProgress = x.MeetingInProgress,
							MeetingType = x.MeetingType,
							Name = x.Name,
							OrderIssueBy = x.OrderIssueBy,
							OrgId = x.Organization.Id,
							PrintOutRockStatus = x.PrintOutRockStatus,
							Prioritization = x.Prioritization,
							Pristine = x.Pristine,
							ReverseScorecard = x.ReverseScorecard,
							RockType = x.RockType,
							ZoomId = x.SelectedVideoProvider.NotNull(y => y.Url),
							StartOfWeekOverride = x.StartOfWeekOverride,
							TeamType = x.TeamType,
							VideoId = x.VideoId,
							VtoId = x.VtoId,
						}).ToList();
						return meetings;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data meetings from UserOrganizationModel : ", e);
				return new List<OrgExportMeeting>();
			}
		}

		private class OrgExportRock {
			public long OwnerId { get; set; }
			public bool Archived { get; set; }
			public DateTime? CompleteTime { get; set; }
			public Models.Enums.RockState Completion { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public DateTime? DueDate { get; set; }
			public long ForUserId { get; set; }
			public long Id { get; set; }
			public string Name { get; set; }
			public Models.Enums.AboutType OnlyAsk { get; set; }
			public long	OrganizationId { get; set; }
			public string PadId { get; set; }
			public string Rock { get; set; }
		}

		private async Task<List<OrgExportRock>> getRockModelDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var rocks = s.QueryOver<RockModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new OrgExportRock {
							OwnerId = x.AccountableUser.Id,
							Archived = x.Archived,
							CompleteTime = x.CompleteTime,
							Completion = x.Completion,
							CreateTime = x.CreateTime,
							DeleteTime = x.DeleteTime,
							DueDate = x.DueDate,
							ForUserId = x.ForUserId,
							Id = x.Id,
							Name = x.Name,
							OnlyAsk = x.OnlyAsk,
							OrganizationId = x.OrganizationId,
							PadId = x.PadId,
							Rock = x.Rock,
						}).ToList();
						return rocks;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data goals (rocks) from RockModel : ", e);
				return new List<OrgExportRock>();
			}
		}

		private class OrgExportMilestone {
			public DateTime? CompleteTime { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public DateTime DueDate { get; set; }
			public long Id { get; set; }
			public string Name { get; set; }
			public string PadId { get; set; }
			public bool Required { get; set; }
			public long RockId { get; set; }
			public MilestoneStatus Status { get; set; }
		}

		private async Task<List<OrgExportMilestone>> getMilestoneDataAsync(UserOrganizationModel caller, long orgId, List<OrgExportRock> rocks) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var milestones = s.QueryOver<Milestone>()
						.WhereRestrictionOn(x => x.RockId).IsIn(rocks.Select(y => y.Id).ToList())
						.Where(x => x.DeleteTime == null)
						.List().Select(x => new OrgExportMilestone {
							CompleteTime = x.CompleteTime,
							CreateTime = x.CreateTime,
							DeleteTime = x.DeleteTime,
							DueDate = x.DueDate,
							Id = x.Id,
							Name = x.Name,
							PadId = x.PadId,
							Required = x.Required,
							RockId = x.RockId,
							Status = x.Status,
						}).ToList();
						return milestones;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data milestones from Milestone : ", e);
				return new List<OrgExportMilestone>();
			}
		}

		private class OrgExportMeasurable {
			public long AccountableUserId { get; set; }
			public long AdminUserId { get; set; }
			public decimal? AlternateGoal { get; set; }
			public bool Archived { get; set; }
			public DateTime? AverageRange { get; set; }
			public long[] BackReferenceMeasurables { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? CumulativeRange { get; set; }
			public DateTime? DeleteTime { get; set; }
			public string Formula { get; set; }
			public decimal? Goal { get; set; }
			public Models.Enums.LessGreater GoalDirection { get; set; }
			public bool HasFormula { get; set; }
			public long Id { get; set; }
			public long OrganizationId { get; set; }
			public bool ShowAverage { get; set; }
			public bool ShowCumulative { get; set; }
			public string Title { get; set; }
			public Models.Enums.UnitType UnitType { get; set; }
		}

		private async Task<List<OrgExportMeasurable>> getMeasurableModelDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var measurables = s.QueryOver<MeasurableModel>()
							.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
							.List().Select(x => new OrgExportMeasurable {
								AccountableUserId = x.AccountableUserId,
								AdminUserId = x.AdminUserId,
								AlternateGoal = x.AlternateGoal,
								Archived = x.Archived,
								AverageRange = x.AverageRange,
								BackReferenceMeasurables = x.BackReferenceMeasurables,
								CreateTime = x.CreateTime,
								CumulativeRange = x.CumulativeRange,
								DeleteTime = x.DeleteTime,
								Formula = x.Formula,
								Goal = x.Goal,
								GoalDirection = x.GoalDirection,
								HasFormula = x.HasFormula,
								Id = x.Id,
								OrganizationId = x.OrganizationId,
								ShowAverage = x.ShowAverage,
								ShowCumulative = x.ShowCumulative,
								Title = x.Title,
								UnitType = x.UnitType,
							}).ToList();
						return measurables;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data measurables from Milestone : ", e);
				return new List<OrgExportMeasurable>();
			}
		}

		private class OrgExportScore {
			public long AccountableUserId { get; set; }
			public decimal? AlternateOriginalGoal { get; set; }
			public DateTime? DateEntered { get; set; }
			public DateTime? DeleteTime { get; set; }
			public DateTime ForWeek { get; set; }
			public long Id { get; set; }
			public long MeasurableId { get; set; }
			public decimal? Measured { get; set; }
			public long OrganizationId { get; set; }
			public decimal? OriginalGoal { get; set; }
			public Models.Enums.LessGreater? OriginalGoalDirection {get; set; }
		}

		private async Task<List<OrgExportScore>> getScoreModelDataAsync(UserOrganizationModel caller, long orgId, DateTime? start, DateTime? end) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var scores = s.QueryOver<ScoreModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId && x.ForWeek > start && x.ForWeek <= end)
						.List().Select(x => new OrgExportScore {
							AccountableUserId = x.AccountableUserId,
							AlternateOriginalGoal = x.AlternateOriginalGoal,
							DateEntered = x.DateEntered,
							DeleteTime = x.DeleteTime,
							ForWeek = x.ForWeek,
							Id = x.Id,
							MeasurableId = x.MeasurableId,
							Measured = x.Measured,
							OrganizationId = x.OrganizationId,
							OriginalGoal = x.OriginalGoal,
							OriginalGoalDirection = x.OriginalGoalDirection,
						}).ToList();
						return scores;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data scores from ScoreModel : ", e);
				return new List<OrgExportScore>();
			}
		}

		private class OrgExportTodo {
			public long AccountableUserId { get; set; }
			public long? ClearedInMeeting { get; set; }
			public DateTime? CloseTime { get; set; }
			public long? CompleteDuringMeetingId { get; set; }
			public DateTime? CompleteTime { get; set; }
			public long CreatedById { get; set;}
			public long? CreatedDuringMeetingId { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public DateTime DueDate { get; set; }
			public string ForModel { get; set; }
			public long ForModelId { get; set; }
			public long? ForRecurrenceId { get; set; }
			public long Id { get; set; }
			public string Message { get; set; }
			public long Ordering { get; set; }
			public long OrganizationId { get; set; }
			public string PadId { get; set; }
			public TodoType TodoType { get; set; }
		}

		private async Task<List<OrgExportTodo>> getTodoModelDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var todos = s.QueryOver<TodoModel>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new OrgExportTodo {
							AccountableUserId = x.AccountableUserId,
							ClearedInMeeting = x.ClearedInMeeting,
							CloseTime = x.CloseTime,
							CompleteDuringMeetingId = x.CompleteDuringMeetingId,
							CompleteTime = x.CompleteTime,
							CreatedById = x.CreatedById,
							CreatedDuringMeetingId = x.CreatedDuringMeetingId,
							CreateTime = x.CreateTime,
							DeleteTime = x.DeleteTime,
							DueDate = x.DueDate,
							ForModel = x.ForModel,
							ForModelId = x.ForModelId,
							ForRecurrenceId = x.ForRecurrenceId,
							Id = x.Id,
							Message = x.Message,
							Ordering = x.Ordering,
							OrganizationId = x.OrganizationId,
							PadId = x.PadId,
							TodoType = x.TodoType
						}).ToList();
						return todos;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data todos from TodoModel : ", e);
				return new List<OrgExportTodo>();
			}
		}

		private class OrgExportHeadline {
			public long? AboutId { get; set; }
			public string AboutIdText { get; set; }
			public string AboutName { get; set; }
			public long? CloseDuringMeetingId { get; set; }
			public DateTime? CloseTime { get; set; }
			public long CreatedBy { get; set; }
			public long? CreatedDuringMeetingId { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public string HeadlinePadId { get; set; }
			public long Id { get; set; }
			public string Message { get; set; }
			public long Ordering { get; set; }
			public long OrganizationId { get; set; }
			public long OwnerId { get; set; }
			public long RecurrenceId { get; set; }
		}

		private async Task<List<OrgExportHeadline>> getPeopleHeadlineDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var headlines = s.QueryOver<PeopleHeadline>()
						.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
						.List().Select(x => new OrgExportHeadline{
							AboutId = x.AboutId,
							AboutIdText = x.AboutIdText,
							AboutName = x.AboutName,
							CloseDuringMeetingId = x.CloseDuringMeetingId,
							CloseTime = x.CloseTime,
							CreatedBy = x.CreatedBy,
							CreatedDuringMeetingId = x.CreatedDuringMeetingId,
							CreateTime = x.CreateTime,
							DeleteTime = x.DeleteTime,
							HeadlinePadId = x.HeadlinePadId,
							Id = x.Id,
							Message = x.Message,
							Ordering = x.Ordering,
							OrganizationId = x.OrganizationId,
							OwnerId = x.OwnerId,
							RecurrenceId = x.RecurrenceId,
						}).ToList();
						return headlines;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data headlines from PeopleHeadline : ", e);
				return new List<OrgExportHeadline>();
			}
		}

		private class OrgExportIssue {
			public bool AwaitingSolve { get; set; }
			public DateTime? CloseTime { get; set; }
			public long CopiedFromIssueRecurrenceId { get; set; }
			public UserOrganizationModel CreatedBy { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public long Id { get; set; }
			public DateTime LastUpdate_Priority { get; set; }
			public bool MarkedForClose { get; set; }
			public long? Ordering { get; set; }
			public long OwnerId { get; set; }
			public int Priority { get; set; }
			public int Rank { get; set;  }
			public long RecurrenceId { get; set; }
			public long Issue_Id { get; set; }
			public string Issue_Message { get; set; }
			public string Issue_PadId { get; set; }
			public long Issue_CreatedById { get; set; }
			public long? Issue_CreatedDuringMeetingId { get; set; }
			public DateTime Issue_CreateTime { get; set; }
			public DateTime? Issue_DeleteTime { get; set; }
			public string Issue_ForModel { get; set; }
			public long Issue_ForModelId { get; set; }
		}

		private async Task<List<OrgExportIssue>> getIssueModel_RecurrenceDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						L10Recurrence recurAlias = null;
						var issues = s.QueryOver<IssueModel_Recurrence>()
							.JoinAlias(x => x.Recurrence, () => recurAlias)
							.Where(x => x.DeleteTime == null && x.Recurrence != null && recurAlias.OrganizationId == orgId)
							.List().Select(x => new OrgExportIssue {
								AwaitingSolve = x.AwaitingSolve,
								CloseTime = x.CloseTime,
								CopiedFromIssueRecurrenceId = x.CopiedFrom.NotNull(y => y.Id),
								CreatedBy = x.CreatedBy,
								CreateTime = x.CreateTime,
								DeleteTime =x.DeleteTime,
								Id = x.Id,
								LastUpdate_Priority = x.LastUpdate_Priority,
								MarkedForClose = x.MarkedForClose,
								Ordering = x.Ordering,
								OwnerId = x.Owner.NotNull(y => y.Id),
								Priority = x.Priority,
								Rank = x.Rank,
								RecurrenceId = x.Recurrence.NotNull(y => y.Id),

								Issue_Id = x.Issue.NotNull(y => y.Id),
								Issue_Message = x.Issue.NotNull(y => y.Message),
								Issue_PadId = x.Issue.NotNull(y => y.PadId),
								Issue_CreatedById = x.Issue.NotNull(y => y.CreatedById),
								Issue_CreatedDuringMeetingId = x.Issue.NotNull(y => y.CreatedDuringMeetingId),
								Issue_CreateTime = x.Issue.NotNull(y => y.CreateTime),
								Issue_DeleteTime = x.Issue.NotNull(y => y.DeleteTime),
								Issue_ForModel = x.Issue.NotNull(y => y.ForModel),
								Issue_ForModelId = x.Issue.NotNull(y => y.ForModelId),
							}).ToList();
						return issues;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from IssueModel_Recurrence : ", e);
				return new List<OrgExportIssue>();
			}
		}

		private class OrgExportAttendee {
			public long Id { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public long RecurrenceId { get; set; }
			public L10Recurrence.SharePeopleAnalyzer SharePeopleAnalyzer { get; set; }
			public DateTime? StarDate { get; set; }
			public long UserId { get; set; }

		}

		private async Task<List<OrgExportAttendee>> getL10Recurrence_AttendeeDataAsync( UserOrganizationModel caller, long orgId, long[] meetingIds) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var attendees = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
							.WhereRestrictionOn(x => x.L10Recurrence.Id)
							.IsIn(meetingIds)
							.Where(x => x.DeleteTime == null)
							.List().Select(x => new OrgExportAttendee {
								Id = x.Id,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								RecurrenceId = x.L10Recurrence.Id,
								SharePeopleAnalyzer = x.SharePeopleAnalyzer,
								StarDate = x.StarDate,
								UserId = x.User.Id
							}).ToList();
						return attendees;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from L10Recurrence_Attendee : ", e);
				return new List<OrgExportAttendee>();
			}
		}

		private class OrgExportAttachedRock {
			public long Id { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public long RockId { get; set; }
			public long RecurrenceId { get; set; }
			public bool OnTheVto { get; set; }

		}

		private async Task<List<OrgExportAttachedRock>> getL10Recurrence_RocksDataAsync(UserOrganizationModel caller, long orgId, long[] meetingIds) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var attachedRocks = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>()
							.WhereRestrictionOn(x => x.L10Recurrence.Id)
							.IsIn(meetingIds)
							.Where(x => x.DeleteTime == null)
							.List().Select(x => new OrgExportAttachedRock {
								Id = x.Id,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								RockId = x.ForRock.Id,
								RecurrenceId = x.L10Recurrence.Id,
								OnTheVto = x.VtoRock,

							}).ToList();
						return attachedRocks;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from L10Recurrence_Rocks : ", e);
				return new List<OrgExportAttachedRock>();
			}
		}

		private class OrgExportAttachedMeasurable {
			public long Id { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public bool IsDivider { get; set; }
			public long MeasurableId { get; set; }
			public int Ordering { get; set; }
			public long RecurrenceId { get; set; }

		}

		private async Task<List<OrgExportAttachedMeasurable>> getL10Recurrence_MeasurableDataAsync(UserOrganizationModel caller, long orgId, long[] meetingIds) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var attachedMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
							.WhereRestrictionOn(x => x.L10Recurrence.Id)
							.IsIn(meetingIds)
							.Where(x => x.DeleteTime == null && x.Measurable != null)
							.List().Select(x => new OrgExportAttachedMeasurable {
								Id = x.Id,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								IsDivider = x.IsDivider,
								MeasurableId = x.Measurable.Id,
								Ordering = x._Ordering,
								RecurrenceId = x.L10Recurrence.Id
							}).ToList();
						return attachedMeasurables;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from L10Recurrence_Measurable : ", e);
				return new List<OrgExportAttachedMeasurable>();
			}
		}

		private class OrgExportAccountabilityNode {
			public long AccountabilityChartId { get; set; }
			public long AccountabilityRoleGroupId { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public long Id { get; set; }
			public long ModelId { get; set; }
			public string ModelType { get; set; }
			public int Ordering { get; set; }
			public long? ParentNodeId { get; set; }
			public long[] UserIds { get; set; }
		}

		private async Task<List<OrgExportAccountabilityNode>> getAccountabilityNodeDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var acNodes = s.QueryOver<AccountabilityNode>()
							.Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
							.List().Select(x => new OrgExportAccountabilityNode {
								AccountabilityChartId = x.AccountabilityChartId,
								AccountabilityRoleGroupId = x.GetAccountabilityRolesGroupId(),
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								Id = x.Id,
								ModelId = x.ModelId,
								ModelType = x.ModelType,
								Ordering = x.Ordering,
								ParentNodeId = x.ParentNodeId,
								UserIds = x.GetUsers(s).SelectId().ToArray(),
							}).ToList();
						return acNodes;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from AccountabilityNode : ", e);
				return new List<OrgExportAccountabilityNode>();
			}
		}

		private class OrgExportAccountabilityRolesGroup {
			public long AccountabilityChartId { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public long Id { get; set; }
			public long OrganizationId { get; set; }
			public string PositionName { get; set; }
		}

		private async Task<List<OrgExportAccountabilityRolesGroup>> getAccountabilityRolesGroupDataAsync( UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var acRoleGroups = s.QueryOver<AccountabilityRolesGroup>()
							.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
							.List().Select(x => new OrgExportAccountabilityRolesGroup {
								AccountabilityChartId = x.AccountabilityChartId,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								Id = x.Id,
								OrganizationId = x.OrganizationId,
								PositionName = x.PositionName,
							}).ToList();
						return acRoleGroups;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from AccountabilityRolesGroup : ", e);
				return new List<OrgExportAccountabilityRolesGroup>();
			}
		}

		private class OrgExportSimpleRole {
			public long Id { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
			public string Name { get; set; }
			public long NodeId { get; set; }
			public int Ordering { get; set; }
		}

		private async Task<List<OrgExportSimpleRole>> getSimpleRoleDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var _roles = s.QueryOver<SimpleRole>()
							.Where(x => x.OrgId == orgId && x.DeleteTime == null)
							.List().Select(x => new OrgExportSimpleRole {
								Id = x.Id,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								Name = x.Name,
								NodeId = x.NodeId,
								Ordering = x.Ordering,
							}).ToList();
						return _roles;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from SimpleRole : ", e);
				return new List<OrgExportSimpleRole>();
			}
		}

		#region VTO 
		private class OrgExportVtoModel {
			public long? CopiedFrom { get; set; }
            public string CoreFocus_Title { get; set; }
            public string CoreFocus_Niche { get; set; }
            public string CoreFocus_Purpose { get; set; }
			public string CoreFocus_PurposeTitle { get; set; }
            public string CoreValueTitle { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime? DeleteTime { get; set; }
            public long Id { get; set; }
            public string IssuesListTitle { get; set; }
            public long? RecurrenceId { get; set; }
            public string Name { get; set; }
            public DateTime? OneYearPlan_FutureDate { get; set; }
            public string OneYearPlan_Title { get; set; }
            public bool OrganizationWide { get; set; }
            public DateTime? QuarterlyRocks_FutureDate { get; set; }
            public string QuarterlyRocks_Title { get; set; }
			public string TenYearTarget { get; set; }
            public string TenYearTargetTitle { get; set; }
			public DateTime? FutureDate { get; set; }
            public string ThreeYearPictureTitle { get; set; }

        }

		private async Task<List<OrgExportVtoModel>> getVtoModelDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var vtos = s.QueryOver<VtoModel>()
							.Where(x => x.Organization.Id == orgId && x.DeleteTime == null)
							.List().Select(x => new OrgExportVtoModel {
								CopiedFrom = x.CopiedFrom,
								CoreFocus_Title = x.CoreFocus.CoreFocusTitle,
								CoreFocus_Niche = x.CoreFocus.Niche,
								CoreFocus_Purpose = x.CoreFocus.Purpose,
								CoreFocus_PurposeTitle = x.CoreFocus.PurposeTitle,
								CoreValueTitle = x.CoreValueTitle,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								Id = x.Id,
								IssuesListTitle = x.IssuesListTitle,
								RecurrenceId = x.L10Recurrence,
								Name = x.Name,
								OneYearPlan_FutureDate = x.OneYearPlan.FutureDate,
								OneYearPlan_Title = x.OneYearPlan.OneYearPlanTitle,
								OrganizationWide = x.OrganizationWide,
								QuarterlyRocks_FutureDate = x.QuarterlyRocks.FutureDate,
								QuarterlyRocks_Title = x.QuarterlyRocks.RocksTitle,
								TenYearTarget = x.TenYearTarget,
								TenYearTargetTitle = x.TenYearTargetTitle,
								FutureDate = x.ThreeYearPicture.FutureDate,
								ThreeYearPictureTitle = x.ThreeYearPicture.ThreeYearPictureTitle,
							}).ToList();
						return vtos;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from VtoModel : ", e);
				return new List<OrgExportVtoModel>();
			}
		}

		private class OrgExportVtoItemString {
			public long BaseId { get; set; }
            public long? CopiedFrom { get; set; }
            public DateTime CreateTime { get; set; }
            public string Data { get; set; }
            public DateTime? DeleteTime { get; set; }
            public ForModel ForModel { get; set; }
            public long Id { get; set; }
            public long? MarketingStrategyId { get; set; }
            public int Ordering { get; set; }
            public VtoItemType Type { get; set; }
            public long VtoId { get; set; }
        }

		private async Task<List<OrgExportVtoItemString>> getVtoItem_StringDataAsync(UserOrganizationModel caller, long orgId, long[] vtoIds) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var vtoStrings = s.QueryOver<VtoItem_String>()
							.WhereRestrictionOn(x => x.Vto.Id)
							.IsIn(vtoIds)
							.Where(x => x.DeleteTime == null)
							.List().Select(x => new OrgExportVtoItemString {
								BaseId = x.BaseId,
								CopiedFrom = x.CopiedFrom,
								CreateTime = x.CreateTime,
								Data = x.Data,
								DeleteTime = x.DeleteTime,
								ForModel = x.ForModel,
								Id = x.Id,
								MarketingStrategyId = x.MarketingStrategyId,
								Ordering = x.Ordering,
								Type = x.Type,
								VtoId = x.Vto.Id
							}).ToList();
						return vtoStrings;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from VtoItem_String : ", e);
				return new List<OrgExportVtoItemString>();
			}
		}

		private class OrgExportVtoItem_KV {
			public long BaseId { get; set; }
            public long? CopiedFrom { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime? DeleteTime { get; set; }
            public string K { get; set; }
            public ForModel ForModel { get; set; }
            public long Id { get; set; }
            public int Ordering { get; set; }
            public VtoItemType Type { get; set; }
            public string V { get; set; }
            public long VtoId { get; set; }

        }

		private async Task<List<OrgExportVtoItem_KV>> getVtoItem_KVDataAsync(UserOrganizationModel caller, long orgId, long[] vtoIds) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var vtoKVs = s.QueryOver<VtoItem_KV>()
							.WhereRestrictionOn(x => x.Vto.Id)
							.IsIn(vtoIds)
							.Where(x => x.DeleteTime == null)
							.List().Select(x => new OrgExportVtoItem_KV {
								BaseId = x.BaseId,
								CopiedFrom = x.CopiedFrom,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								K = x.K,
								ForModel = x.ForModel,
								Id = x.Id,
								Ordering = x.Ordering,
								Type = x.Type,
								V = x.V,
								VtoId = x.Vto.Id
							}).ToList();
						return vtoKVs;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from VtoItem_KV : ", e);
				return new List<OrgExportVtoItem_KV>();
			}
		}

		private class OrgExportMarketingStrategyModel {
			public DateTime CreateTime { get; set; }
			public DateTime? DeleteTime { get; set; }
            public string Guarantee { get; set; }
            public long Id { get; set; }
            public string MarketingStrategyTitle { get; set; }
            public string ProvenProcess { get; set; }
            public string TargetMarket { get; set; }
            public string Title { get; set; }
            public long Vto { get; set; }

        }

		private async Task<List<OrgExportMarketingStrategyModel>> getMarketingStrategyModelDataAsync(UserOrganizationModel caller, long orgId, long[] vtoIds) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var marketingStrategies = s.QueryOver<MarketingStrategyModel>()
							.WhereRestrictionOn(x => x.Vto)
							.IsIn(vtoIds)
							.Where(x => x.DeleteTime == null)
							.List().Select(x => new OrgExportMarketingStrategyModel {
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								Guarantee = x.Guarantee,
								Id = x.Id,
								MarketingStrategyTitle = x.MarketingStrategyTitle,
								ProvenProcess = x.ProvenProcess,
								TargetMarket = x.TargetMarket,
								Title = x.Title,
								Vto = x.Vto,
							}).ToList();
						return marketingStrategies;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from MarketingStrategyModel : ", e);
				return new List<OrgExportMarketingStrategyModel>();
			}
		}

		private class OrgExportCompanyValueModel {
			public long Id { get; set; }
			public DateTime CreateTime { get; set; }
            public DateTime? DeleteTime { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
        }

		private async Task<List<OrgExportCompanyValueModel>> getCompanyValueModelDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var _values = s.QueryOver<CompanyValueModel>()
							.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
							.List().Select(x => new OrgExportCompanyValueModel {
								Id = x.Id,
								CreateTime = x.CreateTime,
								DeleteTime = x.DeleteTime,
								Title = x.CompanyValue,
								Description = x.CompanyValueDetails,
							}).ToList();
						return _values;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from CompanyValueModel : ", e);
				return new List<OrgExportCompanyValueModel>();
			}
		}
		#endregion

		#region Permissions
		private class OrgExportAccessor {
            public long Id { get; set; }
            public PermItem.AccessType Type { get; set; }
        }

		private class OrgExportResource {
            public long Id { get; set; }
            public PermItem.ResourceType Type { get; set; }
        }

		private class OrgExportPermItem {
			public long Id { get; set; }
			public OrgExportAccessor Accessor { get; set; }
            public OrgExportResource Resource { get; set; }
            public bool CanView { get; set; }
            public bool CanEdit { get; set; }
            public bool CanAdmin { get; set; }
            public DateTime CreateTime { get; set; }
            public long CreatorId { get; set; }
            public DateTime? DeleteTime { get; set; }
        }

		private async Task<List<OrgExportPermItem>> getPermItemDataAsync( UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var _permItems = s.QueryOver<PermItem>()
							.Where(x => x.OrganizationId == orgId && x.DeleteTime == null)
							.List().Select(x => new OrgExportPermItem {
								Id = x.Id,
								Accessor = new OrgExportAccessor  { Id = x.AccessorId, Type = x.AccessorType },
								Resource = new OrgExportResource { Id = x.ResId, Type = x.ResType },
								CanView = x.CanView,
								CanEdit = x.CanEdit,
								CanAdmin = x.CanAdmin,
								CreateTime = x.CreateTime,
								CreatorId = x.CreatorId,
								DeleteTime = x.DeleteTime,
							}).ToList();
						return _permItems;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from PermItem : ", e);
				return new List<OrgExportPermItem>();
			}
		}

		private class OrgExportEmailPermItem {
			public long AccessorId { get; set; }
			public string Email { get; set; }
			public DateTime CreateTime { get; set; }
			public long CreatorId { get; set; }
			public DateTime? DeleteTime { get; set; }
		}

		private Utilities.DataTypes.DefaultDictionary<long, OrgExportEmailPermItem> getEmailPermItemData(UserOrganizationModel caller, long orgId, long[] _emailPermIds) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var emailPermItems = s.QueryOver<EmailPermItem>()
							.WhereRestrictionOn(x => x.Id).IsIn(_emailPermIds)
							.Where(x => x.DeleteTime == null)
							.List().Select(x => new OrgExportEmailPermItem  {
								AccessorId = x.Id,
								Email = x.Email,
								CreateTime = x.CreateTime,
								CreatorId = x.CreatorId,
								DeleteTime = x.DeleteTime,
							}).ToDefaultDictionary(x => x.AccessorId, x => x);
						return emailPermItems;
                    }
                }
            }
			catch (Exception e) {
				log.Error("Error exporting data issues from EmailPermItem : ", e);
				return null;
			}
		}
		#endregion

		#region Org
		private class OrgExportTask {
            public long? CreatedFromTaskId { get; set; }
            public DateTime CreateTime { get; set; }
            public DateTime? DeleteTime { get; set; }
            public bool EmailOnException { get; set; }
            public int ExceptionCount { get; set; }
            public DateTime? Executed { get; set; }
            public DateTime Fire { get; set; }
            public DateTime? FirstFire { get; set; }
            public long Id { get; set; }
            public int? MaxException { get; set; }
            public TimeSpan? NextSchedule { get; set; }
            public long OriginalTaskId { get; set; }
            public DateTime? Started { get; set; }
            public string TaskName { get; set; }
            public string Url { get; set; }
        }

		private class OrgExportPayment {
            public string Description { get; set; }
            public DateTime FreeUntil { get; set; }
            public long Id { get; set; }
            public DateTime? LastExecuted { get; set; }
            public DateTime PlanCreated { get; set; }
            public Models.Enums.PaymentPlanType PlanType { get; set; }
            public decimal BaselinePrice { get; set; }
            public int FirstN_Users_Free { get; set; }
            public DateTime? L10FreeUntil { get; set; }
            public decimal L10PricePerPerson { get; set; }
            public bool NoChargeForClients { get; set; }
            public bool NoChargeForUnregisteredUsers { get; set; }
            public long OrgId { get; set; }
            public DateTime? ReviewFreeUntil { get; set; }
            public decimal ReviewPricePerPerson { get; set; }
            public SchedulePeriodType? SchedulePeriod { get; set; }
            public OrgExportTask Task { get; set; }

        }

		private class OrgExportSettings {
            public bool ManagersCanEdit { get; set; }
            public bool ManagersCanEditPositions { get; set; }
            public bool ManagersCanRemoveUsers { get; set; }
            public long? PrimaryContactUserId { get; set; }
            public bool SendEmailImmediately { get; set; }
			public bool StrictHierarchy { get; set; }
            public bool AllowAddClient { get; set; }
            public bool AutoUpgradePayment { get; set; }
            public Models.Enums.BrandingType Branding { get; set; }
            public string DateFormat { get; set; }
            public int? DefaultSendTodoTime { get; set; }
            public bool DisableAC { get; set; }
			public bool DisableUpgradeUsers { get; set; }
            public bool EmployeeCanCreateL10 { get; set; }
            public bool EmployeesCanCreateSurvey { get; set; }
            public bool EmployeesCanEditSelf { get; set; }
            public bool EmployeesCanViewScorecard { get; set; }
			public bool EnableCoreProcess { get; set; }
			public bool EnableL10 { get; set; }
			public bool EnablePeople { get; set; }
			public bool EnableReview { get; set; }
			public bool EnableSurvey { get; set; }
            public bool EnableZapier { get; set; }
            public string ImageGuid { get; set; }
            public bool LimitFiveState { get; set; }
            public bool ManagersCanCreateL10 { get; set; }
            public bool ManagersCanCreateSurvey { get; set; }
            public bool ManagersCanEditSelf { get; set; }
            public bool ManagersCanEditSubordinateL10 { get; set; }
            public bool ManagersCanViewScorecard { get; set; }
            public bool ManagersCanViewSubordinateL10 { get; set; }
            public NumberFormat NumberFormat { get; set; }
            public bool OnlySeeRocksAndScorecardBelowYou { get; set; }
            public Models.Components.ColorComponent PrimaryColor { get; set; }
			public string RockName { get; set; }
			public ScorecardPeriod ScorecardPeriod { get; set; }
            public ShareVtoPages ShareVtoPages { get; set; }
            public Models.Enums.Month StartOfYearMonth { get; set; }
            public Models.Enums.DateOffset StartOfYearOffset { get; set; }
            public Models.Components.ColorComponent TextColor { get; set; }
            public string TimeZoneId { get; set; }
            public DayOfWeek WeekStart { get; set; }
            public Models.Application.YearStart YearStart { get; set; }
        }

		private class OrgExportOrganization {
			public long AccountabilityChartId { get; set; }
            public Models.Enums.AccountType AccountType { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTime? DeleteTime { get; set; }
			public long Id { get; set; }
			public string ImageUrl { get; set; }
            public string ImplementerEmail { get; set; }
            public LockoutType LockoutStatus { get; set; }
			public OrgExportSettings Settings { get; set; }
            public string Name { get; set; }
            public OrgExportPayment Payment { get; set; }
            
        }

		private async Task<OrgExportOrganization> getOrganizationDataAsync(UserOrganizationModel caller, long orgId) {
			try {
                using (var s = HibernateSession.GetCurrentSession())
                {
                    using (var tx = s.BeginTransaction())
                    {
                        var perms = PermissionsUtility.Create(s, caller);
						perms.RadialAdmin(true);

						var _org = s.Get<OrganizationModel>(orgId);

						var paymentPlan = s.Get<PaymentPlan_Monthly>(_org.PaymentPlan.Id);

						var org = new OrgExportOrganization {
							AccountabilityChartId = _org.AccountabilityChartId,
							AccountType = _org.AccountType,
							CreationTime = _org.CreationTime,
							DeleteTime = _org.DeleteTime,
							Id = _org.Id,
							ImageUrl = _org.Image.NotNull(y => y.Url),
							ImplementerEmail = _org.ImplementerEmail,
							LockoutStatus = _org.Lockout,
							Settings = new OrgExportSettings {
								ManagersCanEdit = _org.ManagersCanEdit,
								ManagersCanEditPositions = _org.ManagersCanEditPositions,
								ManagersCanRemoveUsers = _org.ManagersCanRemoveUsers,
								PrimaryContactUserId = _org.PrimaryContactUserId,
								SendEmailImmediately = _org.SendEmailImmediately,
								StrictHierarchy = _org.StrictHierarchy,
								AllowAddClient = _org.Settings.AllowAddClient,
								AutoUpgradePayment = _org.Settings.AutoUpgradePayment,
								Branding = _org.Settings.Branding,
								DateFormat = _org.Settings.DateFormat,
								DefaultSendTodoTime = _org.Settings.DefaultSendTodoTime,
								DisableAC = _org.Settings.DisableAC,
								DisableUpgradeUsers = _org.Settings.DisableUpgradeUsers,
								EmployeeCanCreateL10 = _org.Settings.EmployeeCanCreateL10,
								EmployeesCanCreateSurvey = _org.Settings.EmployeesCanCreateSurvey,
								EmployeesCanEditSelf = _org.Settings.EmployeesCanEditSelf,
								EmployeesCanViewScorecard = _org.Settings.EmployeesCanViewScorecard,
								EnableCoreProcess = _org.Settings.EnableCoreProcess,
								EnableL10 = _org.Settings.EnableL10,
								EnablePeople = _org.Settings.EnablePeople,
								EnableReview = _org.Settings.EnableReview,
								EnableSurvey = _org.Settings.EnableSurvey,
								EnableZapier = _org.Settings.EnableZapier,
								ImageGuid = _org.Settings.ImageGuid,
								LimitFiveState = _org.Settings.LimitFiveState,
								ManagersCanCreateL10 = _org.Settings.ManagersCanCreateL10,
								ManagersCanCreateSurvey = _org.Settings.ManagersCanCreateSurvey,
								ManagersCanEditSelf = _org.Settings.ManagersCanEditSelf,
								ManagersCanEditSubordinateL10 = _org.Settings.ManagersCanEditSubordinateL10,
								ManagersCanViewScorecard = _org.Settings.ManagersCanViewScorecard,
								ManagersCanViewSubordinateL10 = _org.Settings.ManagersCanViewSubordinateL10,
								NumberFormat = _org.Settings.NumberFormat,
								OnlySeeRocksAndScorecardBelowYou = _org.Settings.OnlySeeRocksAndScorecardBelowYou,
								PrimaryColor = _org.Settings.PrimaryColor,
								RockName = _org.Settings.RockName,
								ScorecardPeriod = _org.Settings.ScorecardPeriod,
								ShareVtoPages = _org.Settings.ShareVtoPages,
								StartOfYearMonth = _org.Settings.StartOfYearMonth,
								StartOfYearOffset = _org.Settings.StartOfYearOffset,
								TextColor = _org.Settings.TextColor,
								TimeZoneId = _org.Settings.TimeZoneId,
								WeekStart = _org.Settings.WeekStart,
								YearStart = _org.Settings.YearStart,
							},
							Name = _org.Name,
							Payment = new OrgExportPayment {
								Description = paymentPlan.Description,
								FreeUntil = paymentPlan.FreeUntil,
								Id = paymentPlan.Id,
								LastExecuted = paymentPlan.LastExecuted,
								PlanCreated = paymentPlan.PlanCreated,
								PlanType = paymentPlan.PlanType,

								BaselinePrice = paymentPlan.BaselinePrice,
								FirstN_Users_Free = paymentPlan.FirstN_Users_Free,
								L10FreeUntil = paymentPlan.L10FreeUntil,
								L10PricePerPerson = paymentPlan.L10PricePerPerson,
								NoChargeForClients = paymentPlan.NoChargeForClients,
								NoChargeForUnregisteredUsers = paymentPlan.NoChargeForUnregisteredUsers,
								OrgId = paymentPlan.OrgId,
								ReviewFreeUntil = paymentPlan.ReviewFreeUntil,
								ReviewPricePerPerson = paymentPlan.ReviewPricePerPerson,
								SchedulePeriod = paymentPlan.SchedulePeriod,
								Task = new OrgExportTask {
									CreatedFromTaskId = paymentPlan.Task.CreatedFromTaskId,
									CreateTime = paymentPlan.Task.CreateTime,
									DeleteTime = paymentPlan.Task.DeleteTime,
									EmailOnException = paymentPlan.Task.EmailOnException,
									ExceptionCount = paymentPlan.Task.ExceptionCount,
									Executed = paymentPlan.Task.Executed,
									Fire = paymentPlan.Task.Fire,
									FirstFire = paymentPlan.Task.FirstFire,
									Id = paymentPlan.Task.Id,
									MaxException = paymentPlan.Task.MaxException,
									NextSchedule = paymentPlan.Task.NextSchedule,
									OriginalTaskId = paymentPlan.Task.OriginalTaskId,
									Started = paymentPlan.Task.Started,
									TaskName = paymentPlan.Task.TaskName,
									Url = paymentPlan.Task.Url,
								},
							}
						};


						return org;
					}
				}
			}
			catch (Exception e) {
				log.Error("Error exporting data issues from OrganizationModel or PaymentPlan_Monthly : ", e);
				return null;
			}
		}

		#endregion

		public async static Task<object> GetJsonSensitiveAsync(UserOrganizationModel caller, long orgId, DateTime? start = null, DateTime? end = null) {
			JsonOrg res = null;

			OrgExport export = new OrgExport();

			//using (var s = HibernateSession.GetCurrentSession())
			//{
			//	using (var tx = s.BeginTransaction())
			//	{
					var users = await export.getUserOrganizationModelDataAsync(caller, orgId);
					var meetings = await export.getL10RecurrenceDataAsync(caller, orgId);
					var rocks = await export.getRockModelDataAsync(caller, orgId);
					var milestones = await export.getMilestoneDataAsync( caller, orgId, rocks);
					var measurables = await export.getMeasurableModelDataAsync(caller, orgId);

					start = start ?? new DateTime(2010, 1, 1);
					end = end ?? DateTime.UtcNow.AddDays(14);

					var scores = await export.getScoreModelDataAsync(caller, orgId, start, end);
					var todos = await export.getTodoModelDataAsync(caller, orgId);
					var headlines = await export.getPeopleHeadlineDataAsync(caller, orgId);
					var issues = await export.getIssueModel_RecurrenceDataAsync(caller, orgId);

					// Meeting Attachments
					var meetingIds = meetings.Select(x => x.Id).ToArray();
					var attendees = await export.getL10Recurrence_AttendeeDataAsync(caller, orgId, meetingIds);
					var attachedRocks = await export.getL10Recurrence_RocksDataAsync(caller, orgId, meetingIds);
					var attachedMeasurables = await export.getL10Recurrence_MeasurableDataAsync(caller, orgId, meetingIds);

					// AC
					var acNodes = await export.getAccountabilityNodeDataAsync(caller, orgId);
					var acRoleGroups = await export.getAccountabilityRolesGroupDataAsync(caller, orgId);
					var _roles = await export.getSimpleRoleDataAsync(caller, orgId);

					// VTO
					var vtos = await export.getVtoModelDataAsync(caller, orgId);

					var vtoIds = vtos.Select(x => x.Id).ToArray();

					var vtoStrings = await export.getVtoItem_StringDataAsync(caller, orgId, vtoIds);
					var vtoKVs = await export.getVtoItem_KVDataAsync(caller, orgId, vtoIds);

					// Vto Market Strategy
					var marketingStrategies = await export.getMarketingStrategyModelDataAsync(caller, orgId, vtoIds);
					var _values = await export.getCompanyValueModelDataAsync(caller, orgId);

					//Special meeting permissions
					var _permItems = await export.getPermItemDataAsync(caller, orgId);
					var _emailPermIds = _permItems.Where(x => x.Accessor.Type == PermItem.AccessType.Email)
											.Select(x => x.Accessor.Id)
											.ToArray();
					var emailPermItems = export.getEmailPermItemData(caller, orgId, _emailPermIds);
					var permissions = _permItems.Select(x => new
					{
						x.Id,
						Accessor = new { x.Accessor.Id, x.Accessor.Type, Email = emailPermItems[x.Accessor.Id] },
						Resource = new { x.Resource.Id, x.Resource.Type },
						x.CanView,
						x.CanEdit,
						x.CanAdmin,
						x.CreateTime,
						x.CreatorId,
						x.DeleteTime,
					});

					// Organization
					var org = await export.getOrganizationDataAsync(caller, orgId);

					res = new JsonOrg()
					{
						Users = users,
						Meetings = meetings,
						Rocks = rocks,
						Milestones = milestones,
						Measurables = measurables,
						Scores = scores,
						Todos = todos,
						Headlines = headlines,
						Issues = issues,
						MeetingAttendees = attendees,
						MeetingRocks = attachedRocks,
						MeetingMeasurables = attachedMeasurables,
						AccountabilityChartNodes = acNodes,
						AccountabilityChartRoleGroups = acRoleGroups,
						Roles = _roles,
						Vtos = vtos,
						VtoStrings = vtoStrings,
						VtoKVs = vtoKVs,
						Values = _values,
						VtoMarketingStrategies = marketingStrategies,
						PermissionItems = permissions,
						Organization = org,

					};

					return res;

			


		}
	}
}

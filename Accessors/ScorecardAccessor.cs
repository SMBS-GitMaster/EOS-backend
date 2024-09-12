using Hangfire;
using Humanizer;
using Microsoft.AspNetCore.Mvc.Rendering;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Core.Accessors.StrictlyAfterExecutors;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Core.Models.Terms;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Models;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Application;
using RadialReview.Models.Downloads;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static RadialReview.Accessors.L10Accessor;
using static RadialReview.Utilities.FormulaUtility;
using static RadialReview.Utilities.GraphUtility;
using static RadialReview.DateTimeExtensions;
using Twilio.Rest.Trunking.V1;
using MetricCustomGoalModel = RadialReview.GraphQL.Models.MetricCustomGoalQueryModel;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Reflection;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;
using RestSharp.Extensions;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.Utilities.Types;
using RadialReview.Core.GraphQL.Common;
using RadialReview.Models.L10.VM;
using static RadialReview.Models.L10.VM.L10MeetingVM;
using NHibernate.Util;
using RadialReview.Variables;

namespace RadialReview.Accessors {
  public class MeasurableBuilder {
    private long AccountableUserId { get; set; }
    private long? AdminUserId { get; set; }
    private string Message { get; set; }
    private decimal? Goal { get; set; }
    private UnitType UnitType { get; set; }
    private LessGreater GoalDirection { get; set; }
    private long? TemplateItemId { get; set; }
    private decimal? AlternateGoal { get; set; }
    private bool ShowCumulative { get; set; }
    private DateTime? CumulativeRange { get; set; }
    private bool ShowAverage { get; set; }
    private DateTime? AverageRange { get; set; }
    private DateTime Now { get; set; }
    private bool _ensured { get; set; }
    public DateTime? ProgressiveDate { get; set; }
    public Frequency Frequency { get; set; }
    public string NotesId { get; set; }
    public IList<MetricCustomGoal> CustomGoals { get; set; }
    public bool HasV3Config { get; set; }
    private MeasurableBuilder(string message, decimal? goal, UnitType unitType, LessGreater goalDirection, long accountableUserId, long? adminUserId, decimal? alternateGoal, long? templateItemId, bool showCumulative, DateTime? cumulativeRange, bool showAverage, DateTime? averageRange, DateTime? now = null, DateTime? progressiveData = null, Frequency? frequency = null, IList<MetricCustomGoal> customGoal = null, string notesId = null, bool hasV3Config = false) {
      AccountableUserId = accountableUserId;
      AdminUserId = adminUserId;
      Message = message;
      Goal = goal;
      UnitType = unitType;
      TemplateItemId = templateItemId;
      AlternateGoal = alternateGoal;
      ShowCumulative = showCumulative;
      CumulativeRange = cumulativeRange;
      ShowAverage = showAverage;
      AverageRange = averageRange;
      Now = now ?? DateTime.UtcNow;
      GoalDirection = goalDirection;
      ProgressiveDate = progressiveData;
      Frequency = frequency ?? default;
      CustomGoals = customGoal;
      NotesId = notesId;
      HasV3Config = hasV3Config;
    }

    public static MeasurableBuilder Build(string message, long accountableUserId, long? adminUserId = null, UnitType type = UnitType.None, decimal? goal = null, LessGreater goalDirection = LessGreater.GreaterThan, decimal? alternateGoal = null, bool showCumulative = false, DateTime? cumulativeRange = null, bool showAverage = false, DateTime? averageRange = null, DateTime? now = null, DateTime? progressiveData = null, Frequency? frequency = null, IList<MetricCustomGoal> customGoals = null, string notesId = null, bool hasV3Config = false) {
      return new MeasurableBuilder(message, goal, type, goalDirection, accountableUserId, adminUserId, alternateGoal, null, showCumulative, cumulativeRange, showAverage, averageRange, now, progressiveData, frequency, customGoals, notesId, hasV3Config);
    }

    public static MeasurableBuilder CreateMeasurableFromTemplate() {
      throw new NotImplementedException();
    }

    private void EnsurePermitted(PermissionsUtility perms, long orgId) {
      _ensured = true;
      perms.ViewOrganization(orgId);
      perms.ViewUserOrganization(AccountableUserId, false);
      perms.CreateMeasurableForUser(AccountableUserId);
      if (AdminUserId.HasValue) {
        perms.ViewUserOrganization(AdminUserId.Value, false);
        perms.CreateMeasurableForUser(AdminUserId.Value);
      }
    }

    public MeasurableModel Generate(ISession s, PermissionsUtility perms) {
      var creator = perms.GetCaller();
      var orgId = creator.Organization.Id;
      EnsurePermitted(perms, orgId);
      var adminId = AdminUserId ?? AccountableUserId;
      return new MeasurableModel() {
        AccountableUserId = AccountableUserId,
        AccountableUser = s.Load<UserOrganizationModel>(AccountableUserId),
        AdminUserId = adminId,
        AdminUser = s.Load<UserOrganizationModel>(adminId),
        AlternateGoal = AlternateGoal,
        AverageRange = AverageRange,
        CumulativeRange = CumulativeRange,
        CreateTime = Now, //DueTime -- depricated.
                          //DueDate
        FromTemplateItemId = TemplateItemId,
        Goal = Goal,
        GoalDirection = GoalDirection, //NextGeneration
        Organization = creator.Organization,
        OrganizationId = orgId,
        ShowCumulative = ShowCumulative,
        Title = Message,
        UnitType = UnitType,
        ShowAverage = ShowAverage,
        ProgressiveDate = ProgressiveDate,
        Frequency = Frequency,
        CustomGoals = CustomGoals,
        NotesId = NotesId,
        HasV3Config = HasV3Config,
      };
    }
  }

  public class ScorecardAccessor {
    public static DateTime MINIMUM_SCORE_WEEK = new DateTime(1990, 1, 1);
    #region Create
    public static async Task<MeasurableModel> CreateMeasurable(UserOrganizationModel caller, MeasurableBuilder measurableBuilder) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var m = await CreateMeasurable(s, perms, measurableBuilder);
          tx.Commit();
          s.Flush();
          return m;
        }
      }
    }

    public static async Task<MeasurableModel> CreateMeasurable(ISession s, PermissionsUtility perms, MeasurableBuilder measurableBuilder) {
      var m = measurableBuilder.Generate(s, perms);
      s.Save(m);
      List<ScoreModel> scores = await _GenerateScoreModels_AddMissingScores_Unsafe(s, new DateRange(DateTime.UtcNow.AddDays(-120), DateTime.UtcNow.AddDays(7)), new List<long> { m.Id }, new List<ScoreModel>(), frequency: measurableBuilder.Frequency);
      var cc = perms.GetCaller();
      await HooksRegistry.Each<IMeasurableHook>((ses, x) => x.CreateMeasurable(ses, cc, m, scores));
      return m;
    }

    public static async Task CreateMeasurableHelper(RedLockNet.IDistributedLockFactory redLockFactory, UserOrganizationModel caller, AngularMeasurable model, decimal? lower, decimal? upper, bool? enableCumulative, DateTime? cumulativeStart, bool? enableAverage, DateTime? averageStart, string formula, long[] recurrenceIds) {
      var builder = MeasurableBuilder.Build(model.Name, model.Owner.Id, model.Admin.NotNull(x => (long?)x.Id), model.Modifiers ?? UnitType.None, lower ?? model.Target ?? 0, model.Direction ?? LessGreater.GreaterThan, upper ?? model.AltTarget, enableCumulative ?? false, cumulativeStart, enableAverage ?? false, averageStart, DateTime.UtcNow, null, (Frequency)(caller.Organization.Settings.ScorecardPeriod));
      var created = await ScorecardAccessor.CreateMeasurable(caller, builder);
      if (!string.IsNullOrWhiteSpace(formula)) {
        await ScorecardAccessor.SetFormula(caller, created.Id, formula);
      }

      foreach (var i in recurrenceIds ?? new long[0]) {
        await L10Accessor.AttachMeasurable(redLockFactory, caller, i, created.Id);
      }
    }

    [Obsolete("Commit afterwards")]
    public static async Task<List<ScoreModel>> _GenerateScoreModels_AddMissingScores_Unsafe(ISession s, DateRange range, List<long> measurableIds, List<ScoreModel> existing, Frequency? frequency = Frequency.WEEKLY) {
      //var weeks = new List<DateTime>();
      //var i = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
      //var end = range.EndTime.AddDays(6.999).StartOfWeek(DayOfWeek.Sunday);
      //while (i<=end) {
      //	weeks.Add(i);
      //	i = i.AddDays(7);
      //}
      IEnumerable<DateTime> dates = null;

      //Cap range to be after 2010 and no more than 4 years of data
      if (range != null)
      {
        range.StartTime = Math2.Max(range.StartTime, new DateTime(2010, 1, 1));
        range.EndTime = Math2.Min(range.EndTime, range.StartTime.AddYears(4));
      }

      switch (frequency)
      {
        case Frequency.WEEKLY:
          dates = TimingUtility.GetWeeksBetween(range);
          break;
        case Frequency.MONTHLY:
          dates = TimingUtility.GetMonthsBetween(range.StartTime, range.EndTime);
          break;
        case Frequency.QUARTERLY:
          dates = TimingUtility.GetQuarterlyBetween(range.StartTime, range.EndTime);
          break;
        case Frequency.DAILY:
          dates = TimingUtility.GetDaysBetween(range.StartTime, range.EndTime);
          break;
        default:
          dates = TimingUtility.GetWeeksBetween(range);
          break;
      }

      return await _GenerateScoreModels_AddMissingScores_Unsafe(s, dates, measurableIds, existing, frequency);
    }

    [Obsolete("Commit afterwards")]
    public static async Task<List<ScoreModel>> _GenerateScoreModels_AddMissingScores_Unsafe(ISession s, IEnumerable<DateTime> weeks, List<long> measurableIds, List<ScoreModel> existing, Frequency? frequency = Frequency.WEEKLY) {
      //var measurableLU = measurables.ToDefaultDictionary(x => x.Id, x => x, x => null);
      //var measurableIds = measurables.Select(x => x.Id).ToList();
      var weekMeasurables = new List<Tuple<DateTime, long>>();
      foreach (var week in weeks) {
        foreach (var mid in measurableIds) {
          weekMeasurables.Add(Tuple.Create(week, mid));
        }
      }

      List<ScoreModel> added = await _GenerateScoreModels_AddMissingScores_Unsafe(s, weekMeasurables, existing, frequency);
      return added;
    }

    private static DateTime getStartDateByFrequency(DateTime date, Frequency? frequency)
    {
      switch (frequency)
      {
        case Frequency.WEEKLY:
          return date.StartOfWeek(DayOfWeek.Sunday);
        case Frequency.MONTHLY:
          return date.StartOfMonth(DayOfWeek.Sunday);
        case Frequency.QUARTERLY:
          // v1 saves the quarterly metrics in the first week of the first month of the quarter, however, when it is not the current year, it saves it in the second week.
          if (frequency == Frequency.QUARTERLY && TimingUtility.IsPreviousYear(date))
            return date.FirstWeekOfQuarter().AddDays(7);

          return date.FirstWeekOfQuarter();
        case Frequency.DAILY:
          return date.Date.AddSeconds(1);
        default:
          return date.StartOfWeek(DayOfWeek.Sunday);
      }
    }

    private static async Task<List<ScoreModel>> _GenerateScoreModels_AddMissingScores_Unsafe(ISession s, IEnumerable<Tuple<DateTime, long>> weekMeasurables, List<ScoreModel> existing, Frequency? frequency = Frequency.WEEKLY) {
      var measurableToGet = new List<long>();
      var toAdd_WeekMeasurable = new List<Tuple<DateTime, long>>();
       foreach (var wm in weekMeasurables.Where(x => x.Item1 >= MINIMUM_SCORE_WEEK)) {
        var week = getStartDateByFrequency(wm.Item1, frequency);
        var mid = wm.Item2;
        if (!existing.Any(x => x.ForWeek == week && x.MeasurableId == mid) && !toAdd_WeekMeasurable.Any(x => x.Item1 == week && x.Item2 == mid)) {
          measurableToGet.Add(mid);
          toAdd_WeekMeasurable.Add(Tuple.Create(week, mid));
        }
      }

      var added = new List<ScoreModel>();
      if (measurableToGet.Any()) {
        var calc = new List<ScoreModel>();
        var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Id).IsIn(measurableToGet.Distinct().ToList()).List().ToDefaultDictionary(x => x.Id, x => x, x => null);
        foreach (var d in toAdd_WeekMeasurable) {
          var m = measurables[d.Item2];
          var week = d.Item1;
          if (m != null) {
            var w = frequency == Frequency.DAILY ? week :  week.StartOfWeek(DayOfWeek.Sunday);
            var curr = new ScoreModel() {
              AccountableUserId = m.AccountableUserId,
              DateDue = DateTime.MaxValue, //Not Used
              ForWeek = w, //	ForWeekNumber = TimingUtility.GetWeekSinceEpoch(w),
              MeasurableId = m.Id,
              Measurable = m,
              OrganizationId = m.OrganizationId,
              OriginalGoal = m.Goal,
              OriginalGoalDirection = m.GoalDirection,
              AlternateOriginalGoal = m.AlternateGoal,
              AccountableUser = s.Load<UserOrganizationModel>(m.AccountableUserId)
            };

            if (m.HasFormula) {
              calc.Add(curr);
            }

            s.Save(curr);
            added.Add(curr);
          }
        }

        await UpdateTheseCalculatedScores_Unsafe(s, calc);
      }

      return added;
    }

    [Obsolete("Commit afterwards")]
    public static async Task<bool> _GenerateScoreModels_Unsafe(ISession s, IEnumerable<DateTime> weeks, IEnumerable<long> measurableIds) {
      var any = false;
        weeks = weeks.Select(x => x.StartOfWeek(DayOfWeek.Sunday)).Distinct();
        if (weeks.Any()) {
          var min = weeks.Min();
          var max = weeks.Max();
          var existing = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.ForWeek >= min && x.ForWeek <= max).WhereRestrictionOn(x => x.MeasurableId).IsIn(measurableIds.ToArray()).List().ToList();
          await _GenerateScoreModels_AddMissingScores_Unsafe(s, weeks, measurableIds.ToList(), existing);
        }

      return any;
    }

    public static async Task<ScoreModel> _GenerateScoreModel_Unsafe(ISession session, MeasurableModel measurable, DateTime periodDate, decimal? value = null)
    {
      var existingScore = session.Query<ScoreModel>()
        .FirstOrDefault(s => s.MeasurableId == measurable.Id && s.ForWeek.Date == periodDate.Date && s.DeleteTime == null);

      if (existingScore is not null)
      {
        throw new InvalidOperationException("A score for this measurable and date already exists.");
      }

      var scoreModel = new ScoreModel()
      {
        AccountableUserId = measurable.AccountableUserId,
        ForWeek = periodDate.Date,
        DateEntered = periodDate,
        MeasurableId = measurable.Id,
        Measurable = measurable,
        Measured = value,
        OrganizationId = measurable.OrganizationId,
        OriginalGoal = measurable.Goal,
        OriginalGoalDirection = measurable.GoalDirection,
        AlternateOriginalGoal = measurable.AlternateGoal,
        AccountableUser = session.Load<UserOrganizationModel>(measurable.AccountableUserId)
      };

      session.Save(scoreModel);

      var createdScoreList = new List<ScoreAndUpdates>() { new ScoreAndUpdates { score = scoreModel } };
      await HooksRegistry.Each<IScoreHook>((ses, x) => x.CreateScores(ses, createdScoreList));

      return scoreModel;
    }

    #endregion
    #region Getters
    public static async Task<AngularScorecard> GetAngularScorecardForUser(UserOrganizationModel caller, long userId, int periods) {
      var scorecardStart = TimingUtility.PeriodsAgo(DateTime.UtcNow, periods, caller.Organization.Settings.ScorecardPeriod);
      var scorecardEnd = DateTime.UtcNow.AddDays(14);
      return await ScorecardAccessor.GetAngularScorecardForUser(caller, userId, new DateRange(scorecardStart, scorecardEnd), true, now: DateTime.UtcNow);
    }

    public static async Task<AngularScorecard> GetAngularScorecardForUser(UserOrganizationModel caller, long userId, DateRange range, bool includeAdmin = true, bool includeNextWeek = true, DateTime? now = null, bool includeScores = true, bool generateMissingData = true) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var scorecard = await GetAngularScorecardForUser(s, perms, userId, range, includeAdmin, includeNextWeek, now, includeScores, generateMissingData);
          //Commit is required!
          tx.Commit();
          s.Flush();
          return scorecard;
        }
      }
    }

    [Obsolete("Commit after calling this")]
    private static async Task<AngularScorecard> GetAngularScorecardForUser(ISession s, PermissionsUtility perms, long userId, DateRange range, bool includeAdmin = true, bool includeNextWeek = true, DateTime? now = null, bool includeScores = true, bool generateMissingData = true) {
      var measurables = GetUserMeasurables(s, perms, userId, true, true, true);
      List<ScoreModel> scores = null;
      if (includeScores) {
        var scorecardStart = range.StartTime.StartOfWeek(DayOfWeek.Sunday);
        var scorecardEnd = range.EndTime.AddDaysSafe(13).StartOfWeek(DayOfWeek.Sunday);
        scores = (await GetUserScoresAndFillIn(s, perms, userId, scorecardStart, scorecardEnd, includeAdmin, generateMissingData)).ToList();
      }

      return new AngularScorecard(-1, perms.GetCaller(), measurables.Select(x => new AngularMeasurable(x)), scores, now, range, includeNextWeek, now);
    }

    public static List<MeasurableModel> GetVisibleMeasurables(ISession s, PermissionsUtility perms, long organizationId, bool loadUsers, long? currentRecurenceId = null) {
      var caller = perms.GetCaller();
      var managing = caller.Organization.Id == organizationId && caller.ManagingOrganization;
      IQueryOver<MeasurableModel, MeasurableModel> q;
      List<long> userIds = null;
      List<NameId> visibleMeetings = null;
      List<long> currentMeetingMetricIds = null;
      if(currentRecurenceId != null)
      {
        List<MeasurableModel> currentMeetingMetrics = GetMeasurablesByRecurrenceIdUnsafe(s, (long)currentRecurenceId);
        currentMeetingMetricIds = currentMeetingMetrics.Select(x => x.Id).ToList();
      }
      var getUserIds = new Func<List<long>>(() => {
        userIds = userIds ?? DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id);
        return userIds;
      });
      var getVisibleMeetings = new Func<List<NameId>>(() => {
        if (visibleMeetings == null) {
          visibleMeetings = getUserIds().SelectMany(x => L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, x, true)).Distinct(x => x.Id).ToList();
        }

        return visibleMeetings;
      });
      if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou && !managing) {
        //var userIds = DeepSubordianteAccessor.GetSubordinatesAndSelf(s, caller, caller.Id);
        q = s.QueryOver<MeasurableModel>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).WhereRestrictionOn(x => x.AccountableUserId).IsIn(getUserIds());
        if (loadUsers) {
          q = q.Fetch(x => x.AccountableUser).Eager;
        }
        if (currentMeetingMetricIds != null)
        {
          q.WhereRestrictionOn(x => x.Id).Not.IsIn(currentMeetingMetricIds);
        }
        var results = q.List().ToList();
        var visibleMeetingIds = getVisibleMeetings().Select(x => x.Id).ToList();
        if (currentRecurenceId != null)
          visibleMeetingIds.Remove((long)currentRecurenceId);

        var additionalFromL10 = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(visibleMeetingIds).Select(x => x.Measurable).List<MeasurableModel>().ToList();
        results.AddRange(additionalFromL10);
        results = results.Where(x => x != null).Distinct(x => x.Id).ToList();
        if (loadUsers) {
          foreach (var r in results) {
            try {
              r.AccountableUser.GetName();
              r.AdminUser.GetName();
            } catch (Exception) {
            }
          }
        }

        return results.Where(x => x.DeleteTime == null).ToList();
      } else {
        //q = s.QueryOver<MeasurableModel>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null);
        if (perms.IsPermitted(x => x.ViewOrganizationScorecard(organizationId))) {
          return GetOrganizationMeasurables(s, perms, organizationId, loadUsers, currentMeetingMetricIds).Where(x => x.DeleteTime == null).ToList();
        } else {
          var results = GetUserMeasurables(s, perms, perms.GetCaller().Id, loadUsers, false, true, currentMeetingMetricIds);
          var visibleMeetingIds = getVisibleMeetings().Select(x => x.Id).ToList();
          if (currentRecurenceId != null)
            visibleMeetingIds.Remove((long)currentRecurenceId);
          var additionalFromL10 = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.L10Recurrence.Id).IsIn(visibleMeetingIds).Select(x => x.Measurable).List<MeasurableModel>().ToList();
          results.AddRange(additionalFromL10);
          results = results.Where(x => x != null).Distinct(x => x.Id).ToList();
          if (loadUsers) {
            foreach (var r in results) {
              try {
                r.AccountableUser.GetName();
                r.AdminUser.GetName();
              } catch (Exception) {
              }
            }
          }

          return results.Where(x => x.DeleteTime == null).ToList();
        }
      }
    }

    public static List<MeasurableModel> GetVisibleMeasurables(UserOrganizationModel caller, long organizationId, bool loadUsers) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetVisibleMeasurables(s, perms, organizationId, loadUsers);
        }
      }
    }

    public static List<MeasurableModel> GetOrganizationMeasurables(ISession s, PermissionsUtility perms, long organizationId, bool loadUsers, List<long> currentMeetingMetricIds = null) {
      perms.ViewOrganizationScorecard(organizationId);
      var measurables = s.QueryOver<MeasurableModel>();
      if (loadUsers) {
        measurables = measurables.Fetch(x => x.AccountableUser).Eager;
      }
      measurables.Where(x => x.OrganizationId == organizationId && x.DeleteTime == null);
      if (currentMeetingMetricIds != null)
      {
        measurables.WhereRestrictionOn(x => x.Id).Not.IsIn(currentMeetingMetricIds);
      }
      return measurables.List().ToList();
    }

    public static List<MeasurableModel> GetMeasurablesByRecurrenceIdUnsafe(ISession s, long recurrenceId) {
      return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>()
        .Where(x => x.DeleteTime == null && x.L10Recurrence.Id == recurrenceId && x.Measurable.Id != 0)
        .Select(x => x.Measurable).List<MeasurableModel>().ToList();
    }

    public static List<MeasurableModel> GetOrganizationMeasurables(UserOrganizationModel caller, long organizationId, bool loadUsers) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetOrganizationMeasurables(s, perms, organizationId, loadUsers);
        }
      }
    }

    public static List<MeasurableModel> GetPotentialMeetingMeasurables(UserOrganizationModel caller, long recurrenceId, bool loadUsers) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
          var userIds = L10Accessor.GetL10Recurrence(s, perms, recurrenceId, LoadMeeting.True())._DefaultAttendees.Select(x => x.User.Id).ToList();
          if (caller.Organization.Settings.OnlySeeRocksAndScorecardBelowYou) {
            userIds = DeepAccessor.Users.GetSubordinatesAndSelf(s, caller, caller.Id).Intersect(userIds).ToList();
          }

          var measurables = s.QueryOver<MeasurableModel>();
          if (loadUsers) {
            measurables = measurables.Fetch(x => x.AccountableUser).Eager;
          }

          return measurables.Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.AccountableUserId).IsIn(userIds).List().ToList();
        }
      }
    }

    public static List<MeasurableModel> GetUserMeasurables(ISession s, PermissionsUtility perms, long userId, bool loadUsers, bool ordered, bool includeAdmin, List<long> currentMeetingMetricIds = null) {
      perms.ViewUserOrganization(userId, false);
      var foundQuery = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null);
      if (includeAdmin) {
        foundQuery = foundQuery.Where(x => x.AdminUserId == userId || x.AccountableUserId == userId);
      } else {
        foundQuery = foundQuery.Where(x => x.AccountableUserId == userId);
      }
      if(currentMeetingMetricIds != null)
      {
        foundQuery.WhereRestrictionOn(x => x.Id).Not.IsIn(currentMeetingMetricIds);
      }
      var found = foundQuery.List().ToList();
      var userIds = found.SelectMany(x => new[] { x.AdminUserId, x.AccountableUserId }).Distinct().ToList();
      var __users = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(userIds).List().ToList();
      if (ordered) {
        var order = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null)
          .WhereRestrictionOn(x => x.Measurable.Id).IsIn(found.Select(x => x.Id).Distinct().ToArray())
          .Select(x => x.Measurable.Id, x => x.L10Recurrence.Id, x => x._Ordering).List<object[]>().Select(x => new {
          Measurable = (long)x[0],
          Meeting = (long)x[1],
          Order = (int?)x[2]
        }).ToList();
        order = order.GroupBy(x => x.Meeting).OrderByDescending(x => x.Count()).ThenBy(x => x.First().Meeting).Select(x => x.OrderBy(y => y.Order ?? int.MaxValue).ThenBy(y => y.Measurable)).SelectMany(x => x).Distinct(x => x.Measurable).ToList();
        var lookup = order.Select((x, i) => Tuple.Create(x, i)).ToDictionary(x => x.Item1.Measurable, x => x.Item2);
        foreach (var o in found) {
          if (lookup.ContainsKey(o.Id)) {
            o._Ordering = lookup[o.Id];
          }
        }

        found = found.OrderBy(x => x._Ordering).ToList();
      }

      L10Accessor._RecalculateCumulative_Unsafe(s, null, found, null, null);
      if (loadUsers) {
        foreach (var f in found) {
          var a = f.AdminUser.GetName();
          var b = f.AdminUser.GetImageUrl();
          var c = f.AccountableUser.GetName();
          var d = f.AccountableUser.GetImageUrl();
        }
      }

      return found;
    }

    public static List<MeasurableModel> GetUserMeasurables(UserOrganizationModel caller, long userId, bool ordered = false, bool includeAdmin = false) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetUserMeasurables(s, perms, userId, true, ordered, includeAdmin);
        }
      }
    }

    public static List<ScoreModel> GetMeasurableScores(UserOrganizationModel caller, long measurableId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var measureable = s.Get<MeasurableModel>(measurableId);
          PermissionsUtility.Create(s, caller).Or(x => x.ViewMeasurable(measurableId), x => x.ViewOrganizationScorecard(measureable.OrganizationId));
          return s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == measurableId && x.DeleteTime == null).List().ToList();
        }
      }
    }

    public static List<ScoreModel> GetMeasurablesScores(UserOrganizationModel caller, List<long> measurableIds)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var measureables = s.QueryOver<MeasurableModel>()
          .Where(x => x.DeleteTime == null)
          .WhereRestrictionOn(x => x.Id).IsIn(measurableIds)
          .Select(x => x.Id, x => x.OrganizationId)
          .Future<object[]>().Select(x => new MeasurableModel
          {
            Id = (long)x[0],
            OrganizationId = (long)x[1],
          }).ToList();
          foreach(var m in measureables)
          {
            PermissionsUtility.Create(s, caller).Or(x => x.ViewMeasurable(m.Id), x => x.ViewOrganizationScorecard(m.OrganizationId));
          }
          return s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null)
            .WhereRestrictionOn(x => x.MeasurableId).IsIn(measurableIds)
            .Fetch(SelectMode.Fetch , x => x.Measurable) // We need to load the "Measurable" relationship because we need to use the Frequency property.
            .List().ToList();
        }
      }
    }

    public static MeasurableModel GetMeasurable(UserOrganizationModel caller, long id) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetMeasurable(s, perms, id);
        }
      }
    }

    public static MeasurableModel GetMeasurable(ISession s, PermissionsUtility perms, long measurableId) {
      perms.ViewMeasurable(measurableId);
      return s.Get<MeasurableModel>(measurableId);
    }

    public static List<L10Recurrence.L10Recurrence_Measurable> GetRecurrenceMeasurablesForUser(UserOrganizationModel caller, long userId, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var p = PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
          MeasurableModel measurable = null;
          var q = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().JoinAlias(x => x.Measurable, () => measurable).Where(x => x.L10Recurrence.Id == recurrenceId && measurable.AccountableUserId == userId);
          q = q.Where(x => x.DeleteTime == null && measurable.DeleteTime == null);
          var found = q.Fetch(x => x.Measurable).Eager.List().ToList();
          foreach (var f in found) {
            if (f.Measurable.AccountableUser != null) {
              var a = f.Measurable.AccountableUser.GetName();
              var b = f.Measurable.AccountableUser.ImageUrl(true, ImageSize._32);
            }
          }

          return found;
        }
      }
    }

    public static async Task<ScoreModel> GetScore(UserOrganizationModel caller, long measurableId, long weekId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var score = await GetScore(s, perms, measurableId, weekId);
          tx.Commit();
          s.Flush();
          return score;
        }
      }
    }

    [Obsolete("Call commit")]
    private static async Task<ScoreModel> GetScore_Unsafe(ISession s, long measurableId, long weekId) {
      var week = TimingUtility.GetDateSinceEpoch(weekId);
      await _GenerateScoreModels_Unsafe(s, week.AsList(), measurableId.AsList());
      var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.ForWeek == week && x.MeasurableId == measurableId).List().ToList();
      var found = scores.OrderBy(x => x.Id).FirstOrDefault();
      return found;
    }

    [Obsolete("Call commit")]
    private static async Task<ScoreModel> GetScore(ISession s, PermissionsUtility perms, long measurableId, long weekId) {
      perms.ViewMeasurable(measurableId);
      return await GetScore_Unsafe(s, measurableId, weekId);
    }

    [Obsolete("Call commit")]
    private static async Task<ScoreModel> GetScore(ISession s, PermissionsUtility perms, long measurableId, DateTime week) {
      return await GetScore(s, perms, measurableId, TimingUtility.GetWeekSinceEpoch(week));
    }

    public static ScoreModel GetScore(UserOrganizationModel caller, long id) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var found = s.Get<ScoreModel>(id);
          PermissionsUtility.Create(s, caller).ViewMeasurable(found.MeasurableId);
          return found;
        }
      }
    }

    [Obsolete("Commit after calling this")]
    private static async Task<List<ScoreModel>> GetUserScoresAndFillIn(ISession s, PermissionsUtility perms, long userId, DateTime sd, DateTime ed, bool includeAdmin = false, bool generateMissingData = true) {
      perms.ViewUserOrganization(userId, false);
      var scorecardStart = sd.StartOfWeek(DayOfWeek.Sunday);
      var scorecardEnd = ed.AddDaysSafe(6.999).StartOfWeek(DayOfWeek.Sunday);
      var weeks = TimingUtility.GetWeeksBetween(scorecardStart, scorecardEnd);
      var measurableIdQs = s.QueryOver<MeasurableModel>();
      if (includeAdmin) {
        measurableIdQs = measurableIdQs.Where(x => x.DeleteTime == null && (x.AdminUserId == userId || x.AccountableUserId == userId));
      } else {
        measurableIdQs = measurableIdQs.Where(x => x.DeleteTime == null && x.AccountableUserId == userId);
      }

      var measurableIds = measurableIdQs.Select(x => x.Id).List<long>().ToList();
      var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.ForWeek >= scorecardStart && x.ForWeek <= scorecardEnd);
      if (includeAdmin) {
        //var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && (x.AdminUserId == userId || x.AccountableUserId == userId)).Select(x => x.Id).List<long>().ToList();
        scoresQ = scoresQ.WhereRestrictionOn(x => x.MeasurableId).IsIn(measurableIds);
      } else {
        scoresQ = scoresQ.Where(x => x.AccountableUserId == userId); //already checked delete time above
      }

      var scoresWithDups = scoresQ.List().ToList();
      var scores = scoresWithDups.OrderBy(x => x.Id).Distinct(x => Tuple.Create(x.ForWeek, x.Measurable.Id)).ToList();
      //Generate blank ones
      if (generateMissingData) {
        var extra = await _GenerateScoreModels_AddMissingScores_Unsafe(s, weeks, measurableIds, scores);
        scores.AddRange(extra);
      }

      return scores;
    }

    #endregion
    #region Edit
    public static async Task UpdateMeasurable(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, UserOrganizationModel caller, long measurableId, string name = null, LessGreater? direction = null, decimal? target = null, long? accountableId = null, long? adminId = null, string connectionId = null, bool updateFutureOnly = true, decimal? altTarget = null, bool? showCumulative = null, NullableField<DateTime?> cumulativeRange = null, UnitType? unitType = null, bool? showAverage = null, NullableField<DateTime?>? averageRange = null, MetricEditModel metricEditModel = null, bool hasV3Config = false)
    {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await UpdateMeasurable(dbContext, s, perms, measurableId, name, direction, target, accountableId, adminId, connectionId, updateFutureOnly, altTarget, showCumulative, cumulativeRange, unitType, showAverage, averageRange, metricEditModel, hasV3Config);
          tx.Commit();
          s.Flush();
        }
      }
    }

    private static void EnsurePermittedEditMeasurable(UserOrganizationModel caller, ISession session, long IdMeasurable)
    {
      var perms = PermissionsUtility.Create(session, caller);
      perms.EditMeasurable(IdMeasurable);
    }

    public static async Task<MetricCustomGoal> CreateCustomGoal(UserOrganizationModel caller, CustomGoalCreateModel customGoalCreateModel)
    {
      var session = HibernateSession.GetCurrentSession();
      var measurable = session.Get<MeasurableModel>(customGoalCreateModel.MetricId);

      if (measurable == null)
        throw new PermissionsException("Measurable does not exist.");

      EnsurePermittedEditMeasurable(caller, session, measurable.Id);

      var rule = EnumHelper.ConvertToNonNullableEnum<gqlLessGreater>(customGoalCreateModel.Rule).ToLessGreater();

      var goal = new MetricCustomGoal()
      {
        StartDate = customGoalCreateModel.StartDate,
        EndDate = customGoalCreateModel.EndDate,
        MaxGoalValue = customGoalCreateModel.MaxGoalValue,
        MinGoalValue = customGoalCreateModel.MinGoalValue,
        Rule = rule,
        SingleGoalValue = customGoalCreateModel.SingleGoalValue,

        DateLastModified = DateTime.UtcNow.ToUnixTimeStamp(),
        LastUpdatedBy = measurable.AccountableUser.User.Id
      };

      measurable.CustomGoals.Add(goal);
      using var transaction = session.BeginTransaction();

      await session.UpdateAsync(measurable);
      await transaction.CommitAsync();

      await HooksRegistry.Each<IMetricHook>((sess, x) => x.CreateMetricCustomGoal(sess, caller, goal));

      return goal;
    }

    public static async Task<MetricCustomGoal> EditCustomGoal(UserOrganizationModel caller, CustomGoalEditModel customGoalEditModel)
    {
      var session = HibernateSession.GetCurrentSession();
      var goal = session.Get<MetricCustomGoal>(customGoalEditModel.Id);

      if (goal == null)
        throw new PermissionsException("CustomGoal does not exist.");

      EnsurePermittedEditMeasurable(caller, session, goal.Measurable.Id);

      LessGreater? rule = null;

      if (!string.IsNullOrEmpty(customGoalEditModel.Rule))
        rule = EnumHelper.ConvertToNonNullableEnum<gqlLessGreater>(customGoalEditModel.Rule).ToLessGreater();

      //Rule
      if (rule != null && goal.Rule != rule)
        goal.Rule = (LessGreater) rule;

      //StartDate
      if (customGoalEditModel.StartDate.HasValue && goal.StartDate != customGoalEditModel.StartDate)
        goal.StartDate = customGoalEditModel.StartDate;

      //EndDate
      if (customGoalEditModel.EndDate.HasValue && goal.EndDate != customGoalEditModel.EndDate)
        goal.EndDate = customGoalEditModel.EndDate;

      //MaxGoalValue
      if (!String.IsNullOrEmpty(customGoalEditModel.MaxGoalValue) && goal.MaxGoalValue != customGoalEditModel.MaxGoalValue)
        goal.MaxGoalValue = customGoalEditModel.MaxGoalValue;

      //MinGoalValue
      if (!String.IsNullOrEmpty(customGoalEditModel.MinGoalValue) && goal.MinGoalValue != customGoalEditModel.MinGoalValue)
        goal.MinGoalValue = customGoalEditModel.MinGoalValue;

      //SingleGoalValue
      if (!String.IsNullOrEmpty(customGoalEditModel.SingleGoalValue) && goal.SingleGoalValue != customGoalEditModel.SingleGoalValue)
        goal.SingleGoalValue = customGoalEditModel.SingleGoalValue;

      goal.DateLastModified = DateTime.UtcNow.ToUnixTimeStamp();

      using var transaction = session.BeginTransaction();

      await session.UpdateAsync(goal);

      await HooksRegistry.Each<IMetricHook>((sess, x) => x.UpdateMetricCustomGoal(sess, caller, goal, new IMetricCustomGoalHookUpdates() ));

      transaction.Commit();

      return goal;
    }
    public static async Task<MetricCustomGoal> DeleteCustomGoal(UserOrganizationModel caller, CustomGoalDeleteModel customGoalDeleteModel)
    {
      var session = HibernateSession.GetCurrentSession();
      var customGoal = session.Get<MetricCustomGoal>(customGoalDeleteModel.Id);

      if (customGoal == null)
        throw new PermissionsException("CustomGoal does not exist.");

      EnsurePermittedEditMeasurable(caller, session, customGoal.Measurable.Id);

      customGoal.DeleteTime = DateTime.UtcNow;
      var updates = new IMetricCustomGoalHookUpdates {
        Deleted = true
      };

      using var transaction = session.BeginTransaction();

      session.Update(customGoal);

      await HooksRegistry.Each<IMetricHook>((sess, x) => x.UpdateMetricCustomGoal(sess, caller, customGoal, updates));

      transaction.Commit();

      return customGoal;
    }

    private static void AddCustomGoal(CustomGoalEdit customGoal, MeasurableModel measurable)
    {
      var goal = new MetricCustomGoal()
      {
        StartDate = customGoal.StartDate,
        EndDate = customGoal.EndDate,
        MaxGoalValue = customGoal.MaxGoalValue,
        MinGoalValue = customGoal.MinGoalValue,
        Rule = customGoal.Rule?.DehumanizeTo<LessGreater>() ?? default,
        SingleGoalValue = customGoal.SingleGoalValue,

        DateLastModified = DateTime.UtcNow.ToUnixTimeStamp(),
        LastUpdatedBy = measurable.AccountableUser.User.Id
      };

      measurable.CustomGoals.Add(goal);
    }

    private static void UpdateCustomGoal(CustomGoalEdit customGoal, MeasurableModel measurable)
    {
      var goal = measurable.CustomGoals.FirstOrDefault(x => x.Id == customGoal.Id && x.DeleteTime == null);
      if (goal == null)
        return;

      var rule = (LessGreater) EnumHelper.ConvertToNonNullableEnum<gqlLessGreater>(customGoal?.Rule);

      //Rule
      if (!String.IsNullOrEmpty(customGoal.Rule) && goal.Rule != rule)
        goal.Rule = rule;

      //StartDate
      if (customGoal.StartDate.HasValue && goal.StartDate != customGoal.StartDate)
        goal.StartDate = customGoal.StartDate;

      //EndDate
      if (customGoal.EndDate.HasValue && goal.EndDate != customGoal.EndDate)
        goal.EndDate = customGoal.EndDate;

      //MaxGoalValue
      if (!String.IsNullOrEmpty(customGoal.MaxGoalValue) && goal.MaxGoalValue != customGoal.MaxGoalValue)
        goal.MaxGoalValue = customGoal.MaxGoalValue;

      //MinGoalValue
      if (!String.IsNullOrEmpty(customGoal.MinGoalValue) && goal.MinGoalValue != customGoal.MinGoalValue)
        goal.MinGoalValue = customGoal.MinGoalValue;

      //SingleGoalValue
      if (!String.IsNullOrEmpty(customGoal.SingleGoalValue) && goal.SingleGoalValue != customGoal.SingleGoalValue)
        goal.SingleGoalValue = customGoal.SingleGoalValue;


      goal.DateLastModified = DateTime.UtcNow.ToUnixTimeStamp();
      goal.LastUpdatedBy = measurable.AccountableUser.User.Id;
    }

    public static async Task UpdateMeasurable(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, ISession s, PermissionsUtility perms, long measurableId, string name = null, LessGreater? direction = null, decimal? target = null, long? accountableId = null, long? adminId = null, string connectionId = null, bool updateFutureOnly = true, decimal? altTarget = null, bool? showCumulative = null, NullableField<DateTime?> cumulativeRangeField = null, UnitType? unitType = null, bool? showAverage = null, NullableField<DateTime?> averageRangeField = null, MetricEditModel metricEditModel = null, bool hasV3Config = false)
    {
      var measurable = s.Get<MeasurableModel>(measurableId);
      if (measurable == null) {
        throw new PermissionsException("Measurable does not exist.");
      }

      perms.EditMeasurable(measurableId);
      var updates = new IMeasurableHookUpdates();
      var scoresToUpdate = new List<ScoreModel>();
      var meetingMeasurableIds = s.QueryOver<L10Meeting.L10Meeting_Measurable>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurable.Id).Select(x => x.Id).List<long>().ToList();
      //Message
      if (name != null && measurable.Title != name) {
        measurable.Title = name;
        updates.MessageChanged = true;
      }

      if (metricEditModel != null)
      {
        await UpdateMetric(dbContext, s,metricEditModel, measurable, perms);
      }

      //Show Cumulative
      if (showCumulative != null && measurable.ShowCumulative != showCumulative) {
        measurable.ShowCumulative = showCumulative.Value;
        updates.ShowCumulativeChanged = true;
      }

      //Cumulative Range
      if (cumulativeRangeField != null && measurable.CumulativeRange != cumulativeRangeField.Value) {
        measurable.CumulativeRange = cumulativeRangeField.Value;
        updates.CumulativeRangeChanged = true;
      }

      //Show Average
      if (showAverage != null && measurable.ShowAverage != showAverage) {
        measurable.ShowAverage = showAverage.Value;
        updates.ShowAverageChanged = true;
      }

      //Average Range
      if (averageRangeField != null && measurable.AverageRange != averageRangeField.Value) {
        measurable.AverageRange = averageRangeField.Value;
        updates.AverageRangeChanged = true;
      }

      //Direction
      if ((direction != null && measurable.GoalDirection != direction.Value) || !updateFutureOnly) {
        measurable.GoalDirection = direction.Value;
        var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
        updates.UpdateAboveWeek = DateTime.MinValue;
        if (updateFutureOnly) {
          var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
          scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
          updates.UpdateAboveWeek = nowSunday;
        }

        var scores = scoresQ.List().ToList();
        foreach (var score in scores) {
          score.OriginalGoalDirection = direction.Value;
          s.Update(score);
        }

        scoresToUpdate = scores;
        updates.GoalDirectionChanged = true;
      }

      //Target
      if ((target != null && measurable.Goal != target.Value) || !updateFutureOnly) {
        if (target != null) {
          measurable.Goal = target.Value;
          updates.GoalChanged = true;
        }

        var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
        updates.UpdateAboveWeek = DateTime.MinValue;
        if (updateFutureOnly) {
          var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
          scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
          updates.UpdateAboveWeek = nowSunday;
        }

        var scores = scoresQ.List().ToList();
        foreach (var score in scores) {
          score.OriginalGoal = measurable.Goal;
          s.Update(score);
        }

        scoresToUpdate = scores;
        //updates.GoalChanged=true is above.
      }

      //Alt Target
      if ((altTarget != null && measurable.AlternateGoal != altTarget.Value) || !updateFutureOnly) {
        if (altTarget != null) {
          measurable.AlternateGoal = altTarget.Value;
          updates.AlternateGoalChanged = true;
        }

        var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
        updates.UpdateAboveWeek = DateTime.MinValue;
        if (updateFutureOnly) {
          var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
          scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
          updates.UpdateAboveWeek = nowSunday;
        }

        var scores = scoresQ.List().ToList();
        foreach (var score in scores) {
          score.AlternateOriginalGoal = measurable.AlternateGoal;
          s.Update(score);
        }

        scoresToUpdate = scores;
        //updates.AlternateGoalChanged=true is above.
      }

      //Accountable User
      updates.OriginalAccountableUserId = measurable.AccountableUserId;
      if (accountableId != null && measurable.AccountableUserId != accountableId.Value) {
        perms.ViewUserOrganization(accountableId.Value, false);
        var user = s.Get<UserOrganizationModel>(accountableId.Value);
        measurable.AccountableUserId = accountableId.Value;
        measurable.AccountableUser = user;
        updates.AccountableUserChanged = true;
      }

      //Admin User
      updates.OriginalAdminUserId = measurable.AdminUserId;
      if (adminId != null) {
        perms.ViewUserOrganization(adminId.Value, false);
        var user = s.Get<UserOrganizationModel>(adminId.Value);
        measurable.AdminUserId = adminId.Value;
        measurable.AdminUser = user;
        updates.AdminUserChanged = true;
      }

      //hasV3Config
      bool showV3Features = VariableAccessor.Get(Variable.Names.V3_SHOW_FEATURES, () => false);

      if (showV3Features && hasV3Config && hasV3Config != measurable.HasV3Config)
      {
        measurable.HasV3Config = hasV3Config;
        updates.HasV3Config = hasV3Config;
      }

      //User type
      if (unitType != null && measurable.UnitType != unitType.Value) {
        measurable.UnitType = unitType.Value;
        s.Update(measurable);
        var scoresQ = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.MeasurableId == measurable.Id);
        var nowSunday = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
        scoresQ = scoresQ.Where(x => x.ForWeek > nowSunday);
        var scores = scoresQ.List().ToList();
        scoresToUpdate = scores;
        updates.UnitTypeChanged = true;
      }
      var cc = perms.GetCaller();
      await HooksRegistry.Each<IMeasurableHook>((ses, x) => x.UpdateMeasurable(ses, cc, measurable, scoresToUpdate, updates));
    }

    private static async Task UpdateMetric(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, ISession session , MetricEditModel metricEditModel, MeasurableModel measurable, PermissionsUtility perms)
    {
      var frequency = (Frequency?)(EnumHelper.ConvertToNullableEnum<gqlMetricFrequency>(metricEditModel.Frequency.Value)) ?? default;
      var progressiveDate = metricEditModel.ProgressiveData.NotNull(x => x == null ? default(DateTime?) : x.Value.TargetDate.FromUnixTimeStamp());
      var recurrence = ScorecardAccessor.GetMeasurablesRecurrenceIds(perms.GetCaller(), measurable.Id).FirstOrDefault();

      //Archived
      if (metricEditModel.Archived != null && metricEditModel.Archived.Value != measurable.Archived)
      {
        var deleteTime = DateTime.UtcNow;
        measurable.Archived = metricEditModel.Archived.Value;
        measurable.DeleteTime = deleteTime;
        // if metric is not related to any recurrence, the result of recurrence Id will be 0 so won't be neccesary detach any measurable. 
        if(recurrence != 0)
        {
          await L10Accessor.DetachMeasurable(dbContext, perms.GetCaller(), recurrence, measurable.Id, deleteTime, archiveIfNoLongerInMeetings: false);
        }
      }

      //Frequency
      if (measurable.Frequency != frequency && !String.IsNullOrEmpty(metricEditModel.Frequency.Value))
      {
        measurable.Frequency = frequency;
      }

      //Formula
      if (metricEditModel.Formula.HasValue && metricEditModel.Formula != measurable.Formula)
        measurable.Formula = metricEditModel.Formula;

      //ProgressiveData
      if (metricEditModel.ProgressiveData != null && measurable.ProgressiveDate != progressiveDate)
      {
        measurable.ProgressiveDate = progressiveDate;
      }

      //notesId
      if (!String.IsNullOrEmpty(metricEditModel.NotesId) && metricEditModel.NotesId != measurable.NotesId)
      {
        measurable.NotesId = metricEditModel.NotesId;
      }

      //CustomGoals
      if(metricEditModel.CustomGoals != null)
      {
        foreach (var custom in metricEditModel.CustomGoals ?? null)
        {
          //update database records
          if (custom.Id.HasValue)
          {
            UpdateCustomGoal(custom, measurable);
            continue;
          }

          //Add database record
          AddCustomGoal(custom, measurable);
        }
      }
      session.Update(measurable);
    }

    public static async Task<ScoreModel> UpdateScore(UserOrganizationModel caller, long scoreId, TOptional<decimal?> value, TOptional<string> notesText = default) {
      return await UpdateScore(caller, scoreId, 0, DateTime.MinValue, value, notesText);
    }

    public static async Task<ScoreModel> UpdateScore(UserOrganizationModel caller, long measurableId, DateTime week, decimal? value) {
      return await UpdateScore(caller, 0, measurableId, week, value);
    }

    public static async Task<ScoreModel> UpdateScore(UserOrganizationModel caller, long scoreId, long measurableId, DateTime week, TOptional<decimal?> value, TOptional<string> notesText = default) {
      ScoreModel score = null;
      var createdScoreList = new List<ScoreAndUpdates>();

      // this flag is to know when is a created score since update and created uses the same method
      var createdFlag = scoreId <= 0 ? true : false;
      if (scoreId <= 0) {
        //Create score only once...
          using (var s = HibernateSession.GetCurrentSession())
          {
            using (var tx = s.BeginTransaction())
            {
              var perms = PermissionsUtility.Create(s, caller);
              var frequency = L10Accessor.GetMeasurablesByIdsUnsafe(measurableId.AsList(), s).FirstOrDefault().Frequency;
              var startOfWeek = getStartDateByFrequency(week, frequency);

              score = await GetScore(s, perms, measurableId, startOfWeek);
              // this line change the value of scoreId so the validation below will fall if we use scoreId to know when is created/updated reason of creation of "createdFlag"
              scoreId = (score).Id;
              createdScoreList.Add(new ScoreAndUpdates { score = score });
              tx.Commit();
              s.Flush();
            }
          }
      }

      //var scoreExecutor = new UpdateScoreExecutor(scoreId, value, notesText);
      //await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateScore(scoreId), scoreExecutor);
      ScoreModel result;
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          result = await Restricted.UpdateScore_Unsafe(s, scoreId, value, notesText: notesText);
          tx.Commit();
          s.Flush();
        }
      }

      if (createdFlag)
      {
        await HooksRegistry.Each<IScoreHook>((ses, x) => x.CreateScores(ses, createdScoreList));
      }

      return result;
    }

    #region Unordered
    [Obsolete("Only use when you know you won't be overwritten. Does not ensure 'StrictlyAfter'.")]
    public static async Task<ScoreModel> UpdateScore_Unordered(ISession s, PermissionsUtility perms, long measurableId, DateTime week, decimal? value) {
      return await UpdateScore_Unordered(s, perms, 0, measurableId, week, value);
    }

    [Obsolete("Only use when you know you won't be overwritten. Does not ensure 'StrictlyAfter'.")]
    public static async Task<ScoreModel> UpdateScore_Unordered(ISession s, PermissionsUtility perms, long scoreId, long measurableId, DateTime week, decimal? value) {
      if (scoreId <= 0) {
        scoreId = (await GetScore(s, perms, measurableId, week)).Id;
      }

      var scoreExecutor = new UpdateScoreExecutor(scoreId, value);
      await SyncUtil.ExecuteNonAtomically(s, perms, scoreExecutor);
      return scoreExecutor.GetResult();
      //return await UpdateScore(OrderedSession.Indifferent(s), perms, scoreId, value);
    }

    #endregion
    /// <summary>
    /// SyncAction.UpdateScore(scoreId)
    /// </summary>
    /// <param name = "s"></param>
    /// <param name = "perms"></param>
    /// <param name = "scoreId"></param>
    /// <param name = "value"></param>
    /// <returns></returns>
    //[Untested("StrictlyAfter")]
    //[Obsolete("use strictly after method",true)]
    //public static async Task<ScoreModel> UpdateScore(IOrderedSession s, PermissionsUtility perms, long scoreId, decimal? value) {
    //  //if (scoreId <= 0) {
    //  //  throw new PermissionsException("ScoreId was negative");
    //  //}

    // //perms.EditScore(scoreId);
    //  //SyncUtil.EnsureStrictlyAfter(perms.GetCaller(), s, SyncAction.UpdateScore(scoreId));
    //  //return await UpdateScore_Unsafe(s, scoreId, value);
    //}

    protected class ScoreUpdates {
      public ScoreUpdates(ScoreModel score, TOptional<decimal?> value = default, TOptional<string> notesText = default) {
        Score = score;
        Value = value;
        NotesText = notesText;
      }

      public ScoreModel Score { get; set; }

      public TOptional<decimal?> Value { get; set; }

      public bool Calculated { get; set; }
      public TOptional<string> NotesText { get; set; }
    }

    public class Restricted {
      public static async Task<ScoreModel> UpdateScore_Unsafe(ISession s, long scoreId, TOptional<decimal?> value, DateTime? absoluteUpdateTime = null, TOptional<string> notesText = default) {
        UserOrganizationModel userAlias = null;
        ScoreModel score = null;

        var scoreWithUser = s.QueryOver(() => score)
            .JoinAlias(() => score.AccountableUser, () => userAlias)
            .Where(() => score.Id == scoreId)
            .SingleOrDefault();

        var updates = new List<ScoreUpdates> { new ScoreUpdates(scoreWithUser, value, notesText) };
        return (await ScorecardAccessor.UpdateScore_Unsafe(s, updates, absoluteUpdateTime)).First();
      }
    }


    protected static async Task<List<ScoreModel>> UpdateScore_Unsafe(ISession s, List<ScoreUpdates> scoreUpdates, DateTime? absoluteUpdateTime = null) {
      var scoresToSave = await UpdateScoreButDoNotSave_Unsafe(s, scoreUpdates, absoluteUpdateTime);
      var savedScores = new List<ScoreModel>();
      foreach (var unsavedScore in scoresToSave) {
        var score = unsavedScore.Score;
        s.Update(score);
        savedScores.Add(score);
      }
      return savedScores;
    }

    protected class ScoreToSave {
      public ScoreToSave(ScoreModel score) {
        Score = score;
      }

      public ScoreModel Score { get; set; }
    }


    [Obsolete("probably want to use UpdateScore_Unsafe instead")]
    protected static async Task<List<ScoreToSave>> UpdateScoreButDoNotSave_Unsafe(ISession s, List<ScoreUpdates> scoreUpdates, DateTime? absoluteUpdateTime = null) {
      var o = new List<ScoreToSave>();
      var updateLater = new List<ScoreAndUpdates>();

      absoluteUpdateTime = absoluteUpdateTime ?? HibernateSession.GetDbTime(s);
      foreach (var scoreUpdate in scoreUpdates) {
        var score = scoreUpdate.Score;
        var value = scoreUpdate.Value;
        var notesText = scoreUpdate.NotesText;
        var updates = new IScoreHookUpdates();
        //var score = s.Get<ScoreModel>(scoreId);
        if (value.HasValue && score.Measured != value) {
          if (value == null) {
              score.DateEntered = null;
          } else {
              score.DateEntered = DateTime.UtcNow;
            }

            score.Measured = value;
            updates.ValueChanged = true;
            s.Evict(score);
          }

        if (notesText.HasValue && score.NoteText != notesText)
          score.NoteText = notesText;

        updates.AbsoluteUpdateTime = absoluteUpdateTime.Value;
        updates.Calculated = scoreUpdate.Calculated;
        updateLater.Add(new ScoreAndUpdates { score = score, updates = updates });
        o.Add(new ScoreToSave(score));
      }
      if (updateLater.Any()) {
        await HooksRegistry.Each<IScoreHook>((ses, x) => x.UpdateScores(ses, updateLater));
        await HooksRegistry.Now<IScoreHook>((x) => x.PreSaveUpdateScores(s, updateLater));
      }

      return o;
    }

      #endregion
      #region Delete Measurable
      public static async Task DeleteMeasurable(UserOrganizationModel caller, long measurableId, DateTime deleteTime) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          await DeleteMeasurable(s, perms, measurableId, deleteTime);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public static async Task DeleteMeasurable(ISession s, PermissionsUtility perms, long measurableId, DateTime deleteTime) {
      perms.EditMeasurable(measurableId);
      var m = s.Get<MeasurableModel>(measurableId);
      m.DeleteTime = deleteTime; //Edited. Was 'null'
      m.Archived = true;
      s.Update(m);
      await HooksRegistry.Each<IMeasurableHook>((ses, x) => x.DeleteMeasurable(ses, m));
    }

    public static void UndeleteMeasurable(UserOrganizationModel caller, long measurableId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).EditMeasurable(measurableId);
          var m = s.Get<MeasurableModel>(measurableId);
          var deleteTime = m.DeleteTime;
          m.DeleteTime = null;
          m.Archived = false;
          s.Update(m);
          //add back to recurrence
          var rrs = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.Measurable.Id == measurableId && x.DeleteTime > deleteTime.Value.AddMinutes(-3) && x.DeleteTime < deleteTime.Value.AddMinutes(3) && x.DeleteTime != null).List().ToList();
          foreach (var rr in rrs) {
            rr.DeleteTime = null;
            s.Update(rr);
          }

          //add back to meetings
          var mrs = s.QueryOver<L10Meeting.L10Meeting_Measurable>().Where(x => x.Measurable.Id == measurableId && x.DeleteTime > deleteTime.Value.AddMinutes(-3) && x.DeleteTime < deleteTime.Value.AddMinutes(3) && x.DeleteTime != null).List().ToList();
          foreach (var mr in mrs) {
            mr.DeleteTime = null;
            s.Update(mr);
          }

          tx.Commit();
          s.Flush();
        }
      }
    }

    public static void RemoveAdmin(UserOrganizationModel caller, long measurableId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).EditMeasurable(measurableId);
          var m = s.Get<MeasurableModel>(measurableId);
          m.AdminUserId = m.AccountableUserId;
          s.Update(m);
          tx.Commit();
          s.Flush();
        }
      }
    }

    #endregion
    public static Csv Listing(UserOrganizationModel caller, long organizationId, DateRange range) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);
          var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List().ToList();
          var data = ScorecardData.FromScores(scores);
          var csv = ExportAccessor.GenerateScorecardCsv("Measurable", data, range, caller.GetTimezoneOffset());
          return csv;
        }
      }
    }

    public static async Task<bool> UserScorecardCsv(UserOrganizationModel caller, TermsCollection terms, long userId, DateRange range, FileOrigin origin) {
      long fileId;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).Self(userId);
          fileId = FileAccessor.SaveGeneratedFilePlaceholder_Unsafe(s, caller.Id, "Personal "+terms.GetTerm(TermKey.Metrics), "csv", "generated " + DateTime.UtcNow.AddMinutes(-caller.GetTimezoneOffset()), origin, FileOutputMethod.Download, null, null, TagModel.Create("Metrics"), TagModel.Create<UserOrganizationModel>(userId, "Metrics"));
          tx.Commit();
          s.Flush();
        }
      }

      Scheduler.Enqueue(() => UserScorecardCsv_Hangfire(caller.Id, fileId, userId, range, FileNotification.NotifyCaller(caller), default(IBlobStorageProvider)));
      return true;
    }

    [AutomaticRetry(Attempts = 0)]
    [Queue(HangfireQueues.Immediate.GENERATE_USER_SCORECARD)]
    public static async Task<bool> UserScorecardCsv_Hangfire(long callerId, long fileId, long userId, DateRange range, FileNotification notify, [ActivateParameter] IBlobStorageProvider bsp) {
      UserOrganizationModel caller;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          caller = s.Get<UserOrganizationModel>(callerId);
        }
      }

      var data = await GetAngularScorecardForUser(caller, userId, range, true, true, null, true, false);
      var csv = ExportAccessor.GenerateScorecardCsv("Metrics", data, range, caller.GetTimezoneOffset());
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await FileAccessor.Save_Unsafe(s, bsp, fileId, csv.ToCsv().ToStream(), notify);
          tx.Commit();
          s.Flush();
        }
      }
      return true;
    }

    protected class FormulaVariable {
      public long MeasurableId { get; set; }
      public int Offset { get; set; }
      public string Variable { get; set; }
    }

    protected static List<FormulaVariable> GetVariables(FormulaUtility.ParsedFormula formula) {
      return formula.GetVariables().Select(x => {
        var split = x.Split('(');
        var offset = 0;
        var mid = long.Parse(split[0]);
        if (split.Count() > 1) {
          offset = int.Parse(split[1].SplitAndTrim(')', ',')[0]);
        }

        return new FormulaVariable { Variable = x, MeasurableId = mid, Offset = offset };
      }).Distinct(x => x).ToList();
    }

    public static async Task SetFormula(UserOrganizationModel caller, long measurableId, string formula) {
      var exec = new UpdateFormulaExecutable(measurableId, formula);
      await exec.Run(caller);
    }


    /// <summary>
    /// Perform one-off recalculation
    /// </summary>
    /// <param name = "parsed">the parsed formula</param>
    /// <param name = "scoreLookup">lookup containing all score needed to evaluate this cell. [measurableId][weekId] </param>
    /// <param name = "variableLookup">lookup containing parsed variable information. [variableStr] </param>
    /// <param name = "weekId">current week</param>
    /// <param name = "scoreToUpdate">the score to update with calculated value</param>
    /// <returns></returns>
    private static ScoreUpdates GenerateUpdateForCalculatedScore_Unsafe(FormulaUtility.ParsedFormula parsed, DefaultDictionary<long, DefaultDictionary<long, double?>> scoreLookup, Dictionary<string, FormulaVariable> variableLookup, long weekId, ScoreModel scoreToUpdate) {
      try {
        var value = parsed.Evaluate(variable => {
          var item = variableLookup[variable];
          return scoreLookup[item.MeasurableId][weekId + item.Offset];
        });
        if (value.HasValue && double.IsNaN(value.Value)) {
          value = null;
        }
        return new ScoreUpdates(scoreToUpdate, (decimal?)value) { Calculated = true };
      } catch (InvalidOperationException e) {
        throw new PermissionsException("Formula Error: " + e.Message, true) { NoErrorReport = true, };
      }
    }

    public static async Task UpdateTheseCalculatedScores_Unsafe(ISession s, List<ScoreModel> scores) {
      var measurableToUpdate = scores.Distinct(x => x.MeasurableId).Select(x => x.Measurable).ToList();
      var measurableLookup = measurableToUpdate.ToDictionary(x => x.Id, x => x);
      var variablesLookup = new DefaultDictionary<long, ParsedFormula>(x => FormulaUtility.Parse(measurableLookup[x].Formula));
      var dataNeeded = scores.SelectMany(c => {
        return GetVariables(variablesLookup[c.MeasurableId]).Select(x => new {
          measurableId = x.MeasurableId,
          weekId = TimingUtility.GetWeekSinceEpoch(c.ForWeek) + x.Offset
        });
      }).Distinct().ToList();
      //Get queries for data needed for calculations
      IEnumerable<ScoreModel> actualScoreDataQ;
      {
        var criteria = s.CreateCriteria<ScoreModel>();
        var ors = Restrictions.Disjunction();
        foreach (var cell in dataNeeded) {
          var ands = Restrictions.Conjunction();
          ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.ForWeek), TimingUtility.GetDateSinceEpoch(cell.weekId)));
          ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.MeasurableId), cell.measurableId));
          ors.Add(ands);
        }

        criteria.Add(ors);
        actualScoreDataQ = criteria.Future<ScoreModel>();
      }

      var actualScores = actualScoreDataQ.ToList();
      var genOnlyData = dataNeeded.Select(x => Tuple.Create(TimingUtility.GetDateSinceEpoch(x.weekId), x.measurableId)).Distinct().ToList();
      var actualData = actualScores.Select(x => new {
        week = TimingUtility.GetWeekSinceEpoch(x.ForWeek), //(DateTime)x[0]),
        measurableId = x.MeasurableId, //(long)x[1],
        measured = x.Measured, //(decimal?)x[2]
      }).ToList();
      var scoreLookup = actualData.GroupBy(x => x.measurableId).ToDefaultDictionary(x => x.Key, x => x.ToDefaultDictionary(y => y.week, y => (double?)y.measured, y => null), x => new DefaultDictionary<long, double?>(y => null));
      var dbUpdates = new List<ScoreUpdates>();
      foreach (var u in scores) {
        var parsed = variablesLookup[u.MeasurableId];
        var variables = GetVariables(parsed).Distinct(x => x.Variable).ToDictionary(x => x.Variable, x => x);
        var theScore = u; //actualScores.SingleOrDefault(x => TimingUtility.GetWeekSinceEpoch(x.ForWeek) == u.weekId && x.MeasurableId == u.measurableId);
        if (theScore != null) {
          var dbUpdate = GenerateUpdateForCalculatedScore_Unsafe(parsed, scoreLookup, variables, TimingUtility.GetWeekSinceEpoch(u.ForWeek), theScore);
          dbUpdates.Add(dbUpdate);
        }
      }

      await UpdateScore_Unsafe(s, dbUpdates);
    }

    public static MetricFormulaModel UpdateScoreEmptyValue(long recurrenceId)
    {
      var listMesurableUpdate = new List<MetricEditModel>();
      var listMesurableBasic = new List<MetricEditModel>();
      var formulaMesurable = new MetricFormulaModel();

      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var mesurables = s.Query<L10Recurrence.L10Recurrence_Measurable>()
            .Where(x => x.L10Recurrence.Id == recurrenceId && x.DeleteTime == null
              && (x.Measurable.Formula != null && x.Measurable.Formula != "")).ToList()
            .Select(x => x.Measurable).ToList();

          foreach (var m in mesurables)
          {
            var meetings = s.Query<L10Recurrence.L10Recurrence_Measurable>()
              .Where(x => x.Measurable.Id == m.Id).ToList().Select(x => x.L10Recurrence.Id).ToList().ToArray<long>();

            var basicMesurable = new MetricEditModel()
            {
              Formula = m.Formula,
              Meetings = meetings,
              MetricId = m.Id
            };

            var updateMesurable = new MetricEditModel()
            {
              Formula = $"({m.Formula})",
              Meetings = meetings,
              MetricId = m.Id
            };

            listMesurableUpdate.Add(updateMesurable);
            listMesurableBasic.Add(basicMesurable);
          }
          tx.Commit();
          s.Flush();
        }
      }

      formulaMesurable.updateFormula = listMesurableUpdate;
      formulaMesurable.basicFormula = listMesurableBasic;

      return formulaMesurable;
    }

    /// <summary>
    /// When these scores are updated, also update their dependencies
    /// </summary>
    /// <param name = "s"></param>
    /// <param name = "scores"></param>
    /// <returns></returns>
    public static async Task UpdateCalculatedScores_FromUpdatedScore_Unsafe(ISession s, List<ScoreModel> scores) {
      if (!scores.Any()) {
        return;
      }

      var measurableToUpdate = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(scores.SelectMany(x => x.Measurable.BackReferenceMeasurables).Distinct().ToList()).List().ToList();
      var isDailyMetric = false;

      if (measurableToUpdate.Any())
      {
        isDailyMetric = measurableToUpdate.Select(x => x.Frequency).FirstOrDefault() == Frequency.DAILY;
      }
      else
      {
        return;
      }

      var measurableLookup = measurableToUpdate.ToDictionary(x => x.Id, x => x);
      var variablesLookup = new DefaultDictionary<long, ParsedFormula>(x => FormulaUtility.Parse(measurableLookup[x].Formula));
      //Get all cells needing updates
      var cellsToUpdate = scores.SelectMany(score => {
        var curMeasurable = score.MeasurableId;
        var curWeek = isDailyMetric ? TimingUtility.GetDaysSinceEpoch(score.ForWeek) : TimingUtility.GetWeekSinceEpoch(score.ForWeek);
        return measurableToUpdate.Where(m => m.Formula != null).SelectMany(m => {
          var variables = GetVariables(variablesLookup[m.Id]);
          return variables.Where(x => x.MeasurableId == curMeasurable).Select(x => new {
            measurableId = m.Id,
            weekId = curWeek - x.Offset,
          });
        });
      }).Distinct().ToList();
      //Get all data needed to update cells
      var dataNeeded = cellsToUpdate.SelectMany(c => {
        return GetVariables(variablesLookup[c.measurableId]).Select(x => new {
          measurableId = x.MeasurableId,
          weekId = c.weekId + x.Offset
        });
      }).Distinct().ToList();
      //Get queries for cells to update
      IEnumerable<ScoreModel> actualScoresToUpdateQ;
      {
        var criteria = s.CreateCriteria<ScoreModel>();
        criteria.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.DeleteTime), null));
        var ors = Restrictions.Disjunction();
        foreach (var cell in cellsToUpdate) {
          var ands = Restrictions.Conjunction();
          ands.Add(
            Restrictions.Eq(
              Projections.Property<ScoreModel>(x => x.ForWeek),
              isDailyMetric ? TimingUtility.GetDateByDaySinceEpoch(cell.weekId) : TimingUtility.GetDateSinceEpoch(cell.weekId)
            )
          );
          ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.MeasurableId), cell.measurableId));
          ors.Add(ands);
        }

        criteria.Add(ors);
        actualScoresToUpdateQ = criteria.Future<ScoreModel>();
      }

      //Get queries for data needed for calculations
      IEnumerable<object[]> actualScoreDataQ;
      {
        var criteria = s.CreateCriteria<ScoreModel>();
        criteria.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.DeleteTime), null));
        var ors = Restrictions.Disjunction();
        foreach (var cell in dataNeeded) {
          var ands = Restrictions.Conjunction();
          ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.ForWeek), isDailyMetric ? TimingUtility.GetDateByDaySinceEpoch(cell.weekId) : TimingUtility.GetDateSinceEpoch(cell.weekId)));
          ands.Add(Restrictions.Eq(Projections.Property<ScoreModel>(x => x.MeasurableId), cell.measurableId));
          ors.Add(ands);
        }

        criteria.Add(ors);
        actualScoreDataQ = criteria.SetProjection(Projections.Property<ScoreModel>(x => x.ForWeek), Projections.Property<ScoreModel>(x => x.MeasurableId), Projections.Property<ScoreModel>(x => x.Measured)).Future<object[]>();
      }

      var actualScores = actualScoresToUpdateQ.ToList();
      //Generate some missing datas...
      var datesNeeded = dataNeeded.Select(x => isDailyMetric ? TimingUtility.GetDateByDaySinceEpoch(x.weekId) : TimingUtility.GetDateSinceEpoch(x.weekId)).ToList();
      datesNeeded.AddRange(cellsToUpdate.Select(x => isDailyMetric ? TimingUtility.GetDateByDaySinceEpoch(x.weekId) : TimingUtility.GetDateSinceEpoch(x.weekId)));
      var measurablesNeeded = dataNeeded.Select(x => x.measurableId).ToList();
      measurablesNeeded.AddRange(cellsToUpdate.Select(x => x.measurableId));
      var genOnlyData = cellsToUpdate.Select(x => Tuple.Create(isDailyMetric ? TimingUtility.GetDateByDaySinceEpoch(x.weekId) : TimingUtility.GetDateSinceEpoch(x.weekId), x.measurableId)).Distinct().ToList();
      var gen = await _GenerateScoreModels_AddMissingScores_Unsafe(s, genOnlyData, actualScores, frequency: isDailyMetric ? Frequency.DAILY : Frequency.WEEKLY);
      actualScores.AddRange(gen);
      var actualData = actualScoreDataQ.Select(x => new {
        week = isDailyMetric ? TimingUtility.GetDaysSinceEpoch((DateTime)x[0]) : TimingUtility.GetWeekSinceEpoch((DateTime)x[0]),
        measurableId = (long)x[1],
        measured = (decimal?)x[2]
      }).ToList();
      var scoreLookup = actualData.GroupBy(x => x.measurableId).ToDefaultDictionary(x => x.Key, x => x.ToDefaultDictionary(y => y.week, y => (double?)y.measured, y => null), x => new DefaultDictionary<long, double?>(y => null));
      var dbUpdates = new List<ScoreUpdates>();
      foreach (var u in cellsToUpdate) {
        var parsed = variablesLookup[u.measurableId];
        var variables = GetVariables(parsed).Distinct(x => x.Variable).ToDictionary(x => x.Variable, x => x);
        var theScore = actualScores.Where(x => (isDailyMetric ? TimingUtility.GetDaysSinceEpoch(x.ForWeek) : TimingUtility.GetWeekSinceEpoch(x.ForWeek)) == u.weekId && x.MeasurableId == u.measurableId).OrderBy(x => x.Measured ?? decimal.MinValue).LastOrDefault();
        if (theScore != null) {
          var dbUpdate = GenerateUpdateForCalculatedScore_Unsafe(parsed, scoreLookup, variables, u.weekId, theScore);
          dbUpdates.Add(dbUpdate);
        } else {
          var a = 0;
        }
      }

      await UpdateScore_Unsafe(s, dbUpdates);
    }


    #region Update Formula Executable
    private class UpdateFormulaExecutable {
      public long measurableId;
      public string newFormula;

      public UpdateFormulaExecutable(long measurableId, string newFormula) {
        this.measurableId = measurableId;
        this.newFormula = newFormula;
      }

      public async Task Run(UserOrganizationModel caller) {
        string oldFormula;
        List<FormulaVariable> newVariables;
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);
            newVariables = GetVariables(FormulaUtility.Parse(newFormula));
            EnsureCanEditFormula(perms, measurableId, newVariables);
            var m = s.Get<MeasurableModel>(measurableId);
            if (!HasFormulaChanged(m.Formula, newFormula))
              return;
            oldFormula = m.Formula;
            await ForceSetFormula(s, m, newFormula);
            tx.Commit();
            s.Flush();
          }
        }

        var oldVariables = GetVariables(FormulaUtility.Parse(oldFormula ?? ""));
        try {
          var unsavedScores = new List<ScoreToSave>();
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              var measurable = s.Get<MeasurableModel>(measurableId);
              var addRemove = SetUtility.AddRemove(oldVariables.Select(x => x.MeasurableId).Distinct(), newVariables.Select(x => x.MeasurableId).Distinct());
              foreach (var v in addRemove.RemovedValues) {
                var m = s.Get<MeasurableModel>(v);
                var list = m.BackReferenceMeasurables.ToList();
                list.RemoveAll(x => x == measurableId);
                m.BackReferenceMeasurables = list.Distinct().ToArray();
                s.Update(m);
              }
              foreach (var v in addRemove.AddedValues) {
                var m = s.Get<MeasurableModel>(v);
                var list = m.BackReferenceMeasurables.ToList();
                list.Add(measurableId);
                m.BackReferenceMeasurables = list.Distinct().ToArray();
                s.Update(m);
              }
              unsavedScores = await UpdateAllCalculatedScoresButDoNotSave_Unsafe(s, measurable);

              tx.Commit();
              s.Flush();
            }
          }
          if (unsavedScores.Any()) {
            using (var s = HibernateSession.GetDatabaseSessionFactory().OpenStatelessSession()) {
              using (var tx = s.BeginTransaction()) {
                foreach (var score in unsavedScores.Select(x => x.Score).ToList()) {
                  s.Update(score);
                }
                tx.Commit();
              }
            }
          }

        } catch (Exception e) {
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              var perms = PermissionsUtility.Create(s, caller);
              var measurable = s.Get<MeasurableModel>(measurableId);
              await ForceSetFormula(s, measurable, oldFormula);

              tx.Commit();
              s.Flush();
            }
          }

        }

      }

      private bool HasFormulaChanged(string oldFormula, string newFormula) {
        return (oldFormula ?? "").Trim() != (newFormula ?? "").Trim();
      }

      private void EnsureCanEditFormula(PermissionsUtility perms, long measurableId, List<FormulaVariable> variables) {
        perms.EditMeasurable(measurableId);
        foreach (var v in variables) {
          perms.ViewMeasurable(v.MeasurableId);
        }
      }

      private async Task ForceSetFormula(ISession s, MeasurableModel measurable, string formula) {
        measurable.Formula = formula;
        ConfirmNoCircularRefs(s, measurable);
        s.Update(measurable);
      }

      protected static void _ConfirmNoCircularRefs(ISession s, MeasurableModel m, List<Node> existingNodes, List<long> visitedMeasurables) {
        var backNodes = m.BackReferenceMeasurables.Select(x => new Node() { ParentId = m.Id, Id = x }).ToList();
        var forwardNodes = GetVariables(FormulaUtility.Parse(m.Formula)).Select(x => new Node() { Id = m.Id, ParentId = x.MeasurableId }).ToList();

        var potentialNewNodes = backNodes.ToList();
        potentialNewNodes.AddRange(forwardNodes);
        existingNodes.AddRange(potentialNewNodes);
        var circular = GraphUtility.HasCircularDependency(existingNodes);
        if (circular) {
          throw new PermissionsException("Formula Error: circular reference found.") { NoErrorReport = true };
        }
        visitedMeasurables.Add(m.Id);
        var allNewMeasurableIds = potentialNewNodes.SelectMany(x => new long?[] { x.Id, x.ParentId }).Where(x => x != null).Select(x => x.Value).Where(x => !visitedMeasurables.Any(y => y == x)).ToList();
        var newMeasurables = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(allNewMeasurableIds).List().ToList();
        foreach (var meas in newMeasurables) {
          _ConfirmNoCircularRefs(s, meas, existingNodes, visitedMeasurables);
        }
      }

      protected static void ConfirmNoCircularRefs(ISession s, MeasurableModel editedMeasurable) {
        _ConfirmNoCircularRefs(s, editedMeasurable, new List<Node>(), new List<long>());
      }



      [Obsolete("Expensive")]
      protected static async Task<List<ScoreToSave>> UpdateAllCalculatedScoresButDoNotSave_Unsafe(ISession s, MeasurableModel measurable) {
        var measurableId = measurable.Id;
        var isDaily = measurable.Frequency == Frequency.DAILY;
        var formula = measurable.Formula;
        if (string.IsNullOrWhiteSpace(formula)) {
          await HooksRegistry.Each<IScoreHook>((ses, x) => x.RemoveFormula(ses, measurableId));
          await HooksRegistry.Now<IScoreHook>((x) => x.PreSaveRemoveFormula(s, measurableId));
          return new List<ScoreToSave>();
        }

        var parsed = FormulaUtility.Parse(formula);
        //need to get all the relavent scores..
        var measurables = GetVariables(parsed);
        var uniqueMeasurableIds = measurables.Select(x => x.MeasurableId).Union(new[] { measurableId }).Distinct().ToList();

        var scores = s.QueryOver<ScoreModel>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.MeasurableId).IsIn(uniqueMeasurableIds).List().ToList();
        var scoreLookup = scores.GroupBy(x => x.MeasurableId)
          .ToDefaultDictionary(
            x => x.Key,
            x => x.ToDefaultDictionary(y => isDaily ? TimingUtility.GetDaysSinceEpoch(y.ForWeek) : TimingUtility.GetWeekSinceEpoch(y.ForWeek), y => (double?)y.Measured, y => null),
            x => new DefaultDictionary<long, double?>(y => null)
          );
        var measurableLookup = measurables.Distinct(x => x.Variable).ToDictionary(x => x.Variable, x => x);
        var i = DateTime.UtcNow;
        var end = DateTime.UtcNow.AddDays(8);
        if (scores.Any()) {
          i = scores.Min(x => x.ForWeek);
          end = Math2.Max(end, scores.Max(x => x.ForWeek));
        }

        if(!isDaily)
        {
          i = TimingUtility.ToScorecardDate(i);
          end = TimingUtility.ToScorecardDate(end);
        }

        var allMeasurableScores = scores.Where(x => x.MeasurableId == measurableId).ToList();
        var gen = await _GenerateScoreModels_AddMissingScores_Unsafe(s, new DateRange(i, end), measurableId.AsList(), allMeasurableScores, measurable.HasV3Config ? measurable.Frequency : Frequency.WEEKLY);
        allMeasurableScores.AddRange(gen);
        var allMeasurableScoresLookup = allMeasurableScores.GroupBy(x => x.ForWeek).ToDictionary(x => x.Key, x => x.OrderBy(y => y.Measured ?? decimal.MinValue).Last());
        var updates = new List<ScoreUpdates>();
        while (i <= end) {
          var timeId = isDaily ? TimingUtility.GetDaysSinceEpoch(i) : TimingUtility.GetWeekSinceEpoch(i);
          if(allMeasurableScoresLookup.TryGetValue(i, out var score))
          {
            var update = GenerateUpdateForCalculatedScore_Unsafe(parsed, scoreLookup, measurableLookup, timeId, score);
            updates.Add(update);
          }
          i = isDaily ? i.AddDays(1) : TimingUtility.PeriodsFromNow(i, 1, measurable.Frequency.ToScorecardPeriod());
        }
        return await UpdateScoreButDoNotSave_Unsafe(s, updates);
      }
    }
    #endregion

    #region Removed
    #endregion
    #region Deprecated
    [Obsolete("Avoid using")]
    [Untested("attach to meeting")]
    public static async Task EditMeasurables(UserOrganizationModel caller, long userId, List<MeasurableModel> measurables, bool updateAllL10s) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          await using (var rt = RealTimeUtility.Create()) {
            if (measurables.Any(x => x.AccountableUserId != userId)) {
              throw new PermissionsException("Measurable UserId does not match UserId");
            }

            var perm = PermissionsUtility.Create(s, caller).EditQuestionForUser(userId);
            var user = s.Get<UserOrganizationModel>(userId);
            var orgId = user.Organization.Id;
            var added = measurables.Where(x => x.Id == 0).ToList();
            foreach (var r in measurables) {
              r.OrganizationId = orgId;
              //var added = r.Id == 0;
              if (r.Id == 0) {
                s.Save(r);
              } else {
                s.Merge(r);
              }
            }

            var now = DateTime.UtcNow;
            var toDelete = measurables.Where(x => x.DeleteTime != null).Select(x => x.Id).ToList();
            if (toDelete.Any()) {
              var recurMeasurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.Measurable.Id).IsIn(toDelete).List();
              foreach (var m in recurMeasurables) {
                m.DeleteTime = now;
                s.Update(m);
              }
            }

            if (updateAllL10s) {
              var allL10s = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.User.Id == userId).List().Where(x => x.L10Recurrence.DeleteTime == null).ToList();
              foreach (var r in added) {
                var r1 = r;
                foreach (var o in allL10s.Select(x => x.L10Recurrence)) {
                  if (o.OrganizationId != caller.Organization.Id) {
                    throw new PermissionsException("Cannot access the Weekly Meeting");
                  }

                  perm.UnsafeAllow(PermItem.AccessLevel.View, PermItem.ResourceType.L10Recurrence, o.Id);
                  perm.UnsafeAllow(PermItem.AccessLevel.Edit, PermItem.ResourceType.L10Recurrence, o.Id);
                  await L10Accessor.AttachMeasurable(s, perm, o.Id, r1.Id, true, now: now);
                }
              }
            }

            s.SaveOrUpdate(user);
            user.UpdateCache(s);
            tx.Commit();
            s.Flush();
          }
        }
      }
    }

    [Obsolete("Avoid using")]
    public static async Task<AngularRecurrence> GetReview_Scorecard(UserOrganizationModel caller, long reviewId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return await GetReview_Scorecard(s, perms, reviewId);
        }
      }
    }

    [Obsolete("Avoid using")]
    public static async Task<AngularRecurrence> GetReview_Scorecard(ISession s, PermissionsUtility perms, long reviewId) {
      var review = ReviewAccessor.GetReview(s, perms, reviewId, false, false);
      var start = review.DueDate.AddDays(-7 * TimingUtility.STANDARD_SCORECARD_WEEKS);
      var end = review.DueDate.AddDays(14);
      var scorecard = await GetAngularScorecardForUser(s, perms, review.ReviewerUserId, new DateRange(start, end), includeNextWeek: true, now: review.DueDate);
      foreach (var m in scorecard.Measurables) {
        m.Disabled = true;
      }

      foreach (var ss in scorecard.Scores) {
        ss.Disabled = true;
        if (ss.Measurable != null) {
          ss.Measurable.Disabled = true;
        }
      }

      var container = new AngularRecurrence(-1) {
        Scorecard = scorecard,
        date = new AngularDateRange() { startDate = start, endDate = end }
      };
      return container;
    }

    public static async Task<List<MeasurableModel>> Search(UserOrganizationModel caller, long orgId, string search, long[] excludeLong = null, int take = int.MaxValue) {
      excludeLong = excludeLong ?? new long[] { };
      var visible = ScorecardAccessor.GetVisibleMeasurables(caller, orgId, true).Where(x => !excludeLong.Any(y => y == x.Id)).Where(x => x.Id > 0);
      var splits = search.ToLower().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
      var dist = new DiscreteDistribution<MeasurableModel>(0, 9, true);
      foreach (var u in visible) {
        var fname = false;
        var lname = false;
        var ordered = false;
        var fnameStart = false;
        var lnameStart = false;
        var wasFirst = false;
        var exactFirst = false;
        var exactLast = false;
        var containsText = false;
        var names = new List<string[]>();
        names.Add(new string[] { u.AccountableUser.GetFirstName().ToLower(), u.AccountableUser.GetLastName().ToLower(), });
        if (u.AccountableUserId != u.AdminUserId) {
          names.Add(new string[] { u.AdminUser.GetFirstName().ToLower(), u.AdminUser.GetLastName().ToLower(), });
        }

        foreach (var n in names) {
          var f = n[0];
          var l = n[1];
          foreach (var t in splits) {
            if (f.Contains(t)) {
              fname = true;
            }
            if (f == t) {
              exactFirst = true;
            }
            if (f.StartsWith(t)) {
              fnameStart = true;
            }
            if (l.Contains(t)) {
              lname = true;
            }
            if (l.StartsWith(t)) {
              lnameStart = true;
            }
            if (fname && !wasFirst && lname) {
              ordered = true;
            }
            if (l == t) {
              exactLast = true;
            }
            if (u.Title != null && u.Title.ToLower().Contains(t)) {
              containsText = true;
            }
            wasFirst = true;
          }
        }

        var score = fname.ToInt() + lname.ToInt() + ordered.ToInt() + fnameStart.ToInt() + lnameStart.ToInt() + exactFirst.ToInt() + exactLast.ToInt() + containsText.ToInt() * 2;
        if (score > 0) {
          dist.Add(u, score);
        }
      }

      return dist.GetProbabilities().OrderByDescending(x => x.Value).Select(x => x.Key).Take(take).ToList();
    }

    public static CreateMeasurableViewModel BuildCreateMeasurableVM(UserOrganizationModel caller, TermsCollection terms, dynamic ViewBag, long? selectedUserId, long? selectedRecurId, bool hideMeetings, List<SelectListItem> potentialUsers = null) {
      if (potentialUsers == null) {
        potentialUsers = SelectListAccessor.GetUsersWeCanCreateMeaurableFor(caller, terms, x => x.Id == selectedUserId);
      }

      if (!potentialUsers.Any()) {
        throw new PermissionsException("No users. Add an attendee first.");
      }

      var meetings = SelectListAccessor.GetL10RecurrenceAdminable(caller, caller.Id, x => x.CanAdmin && x.Id == selectedRecurId);

      if (selectedRecurId != null && !meetings.Any(x => x.Value == "" + selectedRecurId)) {
        //add the selected meeting in case we're not within the right org.
        var view = PermissionsAccessor.IsPermitted(caller, x => x.CanView(PermItem.ResourceType.L10Recurrence, selectedRecurId.Value, includeAlternateUsers: true));
        var admin = PermissionsAccessor.IsPermitted(caller, x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, selectedRecurId.Value, includeAlternateUsers: true));
        if (view) {
          meetings.Add(new SelectListItem() {
            Value = "" + selectedRecurId,
            Text = "(this meeting)",
            Selected = admin,
            Disabled = !admin
          });
        }
      }



      ViewBag.PossibleRecurrences = meetings;
      ViewBag.PossibleOwners = potentialUsers;
      ViewBag.IsCreate = true;
      ViewBag.HideMeetings = hideMeetings;
      ViewBag.CannotAttachToMeeting = selectedRecurId != null && !meetings.Any(x => x.Selected);
      var measurable = new AngularMeasurable();
      if (selectedUserId != null) {
        measurable.Owner = new AngularUser(selectedUserId.Value);
      }

      return new CreateMeasurableViewModel() {
        Measurable = measurable //AccountableUser = selected.Value.ToLong(),
                                //PotentialUsers = potentialUsers,
      };
    }

    public static List<long> GetMeasurablesRecurrenceIds(UserOrganizationModel caller, long measurableId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewMeasurable(measurableId);
          return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId).Select(x => x.L10Recurrence.Id).List<long>().ToList();
        }
      }
    }

    public static List<NameId> GetMeasurablesRecurrences(UserOrganizationModel caller, long measurableId) {
      L10Recurrence recurAlias = null;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewMeasurable(measurableId);
          return s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().JoinAlias(x => x.L10Recurrence, () => recurAlias).Where(x => x.DeleteTime == null && x.Measurable.Id == measurableId).Select(x => x.L10Recurrence.Id, x => recurAlias.Name).List<object[]>().Select(x => new NameId((string)x[1], (long)x[0])).ToList();
        }
      }
    }
    #endregion

  }
}
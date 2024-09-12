using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Enumerations
{

  #region Enumerations

  public enum gqlCheckInType
  {
    TRADITIONAL = 0,
    ICEBREAKER = 1
  }

  public enum gqlGroupBy
  {
    DAY = 0,
    WEEK = 1,
    MONTH = 2,
    YEAR = 3
  }

  public enum gqlFavoriteType
  {
    MEETING = 0,
    WORKSPACE = 1,
  }

  public enum gqlFeedbackStyle
  {
    INDIVIDUAL = 0,
    ALL_PARTICIPANTS = 1
  }

  public enum gqlGoalStatus
  {
    OFF_TRACK = 0,
    ON_TRACK = 1,
    COMPLETED = 2,
  }

  /// <summary>
  /// Per Frontend
  /// Recurrence.Prioritization: Rank = 'PRIORITY' in v3.
  /// Recurrence.Prioritization: Priority = 'STAR' in v3.
  /// </summary>
  public enum gqlIssueVoting
  {
    STAR = 1,
    PRIORITY = 2,
  }

  public enum gqlIssueVotingState
  {
    INACTIVE,
    ACTIVE,
    COMPLETE
  }


  public enum gqlLessGreater
  {
    LESS_THAN_OR_EQUAL = -2,
    LESS_THAN = -1,
    GREATER_THAN = 1,
    EQUAL_TO = 0,
    GREATER_THAN_NOT_EQUAL = 2,
    BETWEEN = -3
  }

  public enum gqlUnitType
  {
    NONE = 0,
    DOLLAR = 1,
    PERCENT = 2,
    POUND = 3,
    EUROS = 4,
    PESOS = 5,
    YEN = 6,
    YESNO = 7,  // NOTE: This does not exist in V1 version of this enum.
    INR = 8,
  }

  public enum gqlMetricFrequency
  {
    WEEKLY = 0,
    MONTHLY = 1,
    QUARTERLY = 2,
    DAILY = 3,
  }

  public enum gqlMilestoneStatus
  {
    INCOMPLETED,
    COMPLETED,
    OVERDUE
  }

  public enum gqlPageType
  {
    TITLE_PAGE = 0,
    CHECK_IN = 1,
    METRICS = 2,
    GOALS = 3,
    HEADLINES = 4,
    TODOS = 5,
    ISSUES = 6,
    WRAP_UP = 7,
    NOTES_BOX = 8,
    EXTERNAL_PAGE = 9,
    WHITEBOARD = 10,
    HTML = 899
  }

  public enum gqlPrioritizationType
  {
    PRIORITY = 1,
    RANK = 2
  }

  public enum gqlRatingPrivacy
  {
    Anonymous,
    Public,
  }

  public enum gqlSendEmailSummaryTo
  {
    NONE = 0,
    ALL_ATTENDEES = 1,
    ALL_ATTENDEES_RATED_MEETING = 2,
  }

  // Meeting types enum
  public enum gqlAgendaType
  {
    WEEKLY = 0, // weekly meeting
    ONE_ON_ONE = 1 // samgePage meeting
  }

  public enum gqlMeetingType
  {
    LEADERSHIP = 1, // Leadership Team
    DEPARTMENTAL = 2, // Departmental Team
    ONE_ON_ONE = 3, // 1:1 Meeting
    OTHER = 100 // other
  }

  public enum gqlUserAvatarColor
  {
    COLOR1 = 0,
    COLOR2 = 1,
    COLOR3 = 2,
    COLOR4 = 3,
    COLOR5 = 4,
  }

  public enum gqlDrawerView
  {
    EMBEDDED = 0,
    SLIDE = 1,
  }

  public enum gqlTileType {
    // Old V1 Reference are in TileModel.cs
    INVALID = 0,
    USER_PROFILE = 1, // USER_PROFILE
    PERSONAL_METRICS = 2,
    PERSONAL_TODOS = 3, // PERSONAL_TODOS
    ROLES = 4,
    PERSONAL_GOALS = 5,
    VALUES = 6,
    MANAGE = 7,
    URL = 8,
    MEETING_TODOS = 9, // MEETING_TODOS
    MEETING_METRICS = 10,
    MEETING_GOALS = 11,
    MEETING_HEADLINES = 12,
    MEETING_ISSUES = 13,
    FAQGUIDE = 14,
    NOTIFICATIONS = 15,
    MEETING_SOLVED_ISSUES = 16,
    TASKS = 17,
    PROCESSES = 18,
    MILESTONES = 19,
    MEETING_STATS = 20,
    PROCESS_EXECUTIONS = 21,
    PERSONAL_NOTES = 22,
    MEETING_NOTES = 23
  }

  public enum gqlDateRangeStats {
    WEEK = 0,
    MONTH = 1,
    QUARTER = 2,
    SIX_MONTHS = 3,
    YTD = 4,
    YEAR = 5,
    ALL = 6
  }

  public enum gqlDashboardType
  {
    STANDARD = 0,
    DIRECT_REPORT = 1,
    CLIENT = 2,
    MEETING = 3,
  }

  public enum gqlTilePlacementMode
  {
    SET_NEW_LINE_Y = 0,
    SET_FIRST_FREE_Y = 1,
    FIND_BEST_FIT = 2,
  }

  #endregion

  #region Helper Class

  public static class EnumHelper
  {

    #region Public Methods

    public static T? ConvertToNullableEnum<T>(string value)
      where T : struct, System.Enum
    {
      if (string.IsNullOrEmpty(value)) return default(T?);
      return (T)Enum.Parse(typeof(T), value);
    }

    public static T ConvertToNonNullableEnum<T>(string value)
      where T : struct, System.Enum
    {
      if (string.IsNullOrEmpty(value)) throw new ArgumentNullException("value", $"Expected enum string value, got `null`") ;
      return (T)Enum.Parse(typeof(T), value);
    }

    public static T ConvertToNonNullable<T>(object value) where T : System.Enum
    {
      return (T)value;
    }

    /// <summary>
    /// Converts the provided string value to an enum of type T. Returns the default value if the input is null. 
    /// Throws an ArgumentException if the input is non-null and does not match any enum value.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <param name="defaultValue">The default value to return if the input is null.</param>
    /// <returns>The corresponding enum value of type T or the default value.</returns>
    /// <exception cref="ArgumentException">Thrown when the input does not match any enum value.</exception>
    public static T ConvertToEnumOrDefaultOnNull<T>(string value, T defaultValue = default) where T : struct
    {
      if (string.IsNullOrEmpty(value)) return defaultValue;
      if (Enum.TryParse<T>(value, true, out T result)) return result;
      throw new ArgumentException($"Couldn't find any enum member that matches the string '{value}'");
    }


    #endregion

  }

  #endregion

}
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Scorecard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Common
{
  public static class EnumConversionExtensions
  {
    public static LessGreater ToLessGreater(this gqlLessGreater lessGreater) => lessGreater switch
    {
      gqlLessGreater.EQUAL_TO => LessGreater.EqualTo,
      gqlLessGreater.LESS_THAN => LessGreater.LessThan,
      gqlLessGreater.LESS_THAN_OR_EQUAL => LessGreater.LessThanOrEqual,
      gqlLessGreater.GREATER_THAN => LessGreater.GreaterThan,
      gqlLessGreater.GREATER_THAN_NOT_EQUAL => LessGreater.GreaterThanNotEqual,
      gqlLessGreater.BETWEEN => LessGreater.Between,
    };

    public static ConcludeSendEmail ToConcludeSendEmail(this gqlSendEmailSummaryTo sentTo) => sentTo switch
    {
      gqlSendEmailSummaryTo.NONE => ConcludeSendEmail.None,
      gqlSendEmailSummaryTo.ALL_ATTENDEES => ConcludeSendEmail.AllAttendees,
      gqlSendEmailSummaryTo.ALL_ATTENDEES_RATED_MEETING => ConcludeSendEmail.AllRaters,
    };

    public static MeetingType ToMeetingType(this gqlAgendaType agenda) => agenda switch
    {
      gqlAgendaType.WEEKLY => MeetingType.L10,
      gqlAgendaType.ONE_ON_ONE => MeetingType.SamePage
    };
    public static L10TeamType ToL10TeamType(this gqlMeetingType meetingType) => meetingType switch
    {
      gqlMeetingType.LEADERSHIP => L10TeamType.LeadershipTeam,
      gqlMeetingType.DEPARTMENTAL => L10TeamType.DepartmentalTeam,
      gqlMeetingType.ONE_ON_ONE => L10TeamType.SamePageMeeting,
      gqlMeetingType.OTHER => L10TeamType.Other,
    };

    public static ScorecardPeriod ToScorecardPeriod(this Frequency metricFrequency) => metricFrequency switch
    {
      Frequency.MONTHLY => ScorecardPeriod.Monthly,
      Frequency.QUARTERLY => ScorecardPeriod.Quarterly,
      Frequency.WEEKLY => ScorecardPeriod.Weekly,
      _ => throw new ArgumentOutOfRangeException(nameof(metricFrequency), $"Unhandled frequency: {metricFrequency}")
    };

    public static Frequency ToFrequency(this ScorecardPeriod scorecardPeriod) => scorecardPeriod switch
    {
      ScorecardPeriod.Monthly => Frequency.MONTHLY,
      ScorecardPeriod.Quarterly => Frequency.QUARTERLY,
      ScorecardPeriod.Weekly => Frequency.WEEKLY,
      _ => throw new ArgumentOutOfRangeException(nameof(scorecardPeriod), $"Unhandled scorecardPeriod: {scorecardPeriod}")
    };

    public static Frequency? ToFrequency(this gqlMetricFrequency? metricFrequency)
    {
      if (metricFrequency is null) return null;

      return metricFrequency switch
      {
        gqlMetricFrequency.DAILY => Frequency.DAILY,
        gqlMetricFrequency.WEEKLY => Frequency.WEEKLY,
        gqlMetricFrequency.MONTHLY => Frequency.MONTHLY,
        gqlMetricFrequency.QUARTERLY => Frequency.QUARTERLY,
        _ => throw new ArgumentOutOfRangeException(nameof(metricFrequency), $"Unhandled frequency: {metricFrequency}")
      };
    }

    public static gqlMetricFrequency ToGqlMetricFrequency(this Frequency frequency) => frequency switch
    {
      Frequency.DAILY => gqlMetricFrequency.DAILY,
      Frequency.WEEKLY => gqlMetricFrequency.WEEKLY,
      Frequency.MONTHLY => gqlMetricFrequency.MONTHLY,
      Frequency.QUARTERLY => gqlMetricFrequency.QUARTERLY,
      _ => throw new ArgumentOutOfRangeException(nameof(frequency), $"The provided value '{frequency}' is unrecognized and cannot be mapped. Please ensure it is within the supported range."),
    };

    /// <summary>
    /// Converts a nullable Frequency value to a nullable gqlMetricFrequency.
    /// Returns null if the input 'frequency' is null, maintaining nullability semantics.
    /// Otherwise, converts the non-null value to its gqlMetricFrequency equivalent.
    /// </summary>
    /// <param name="frequency">The nullable Frequency value to convert.</param>
    /// <returns>A nullable gqlMetricFrequency equivalent, or null if 'frequency' is null.</returns>
    public static gqlMetricFrequency? ToNullableGqlMetricFrequency(this Frequency? frequency)
    {
      if (frequency is null) return null;

      return frequency.Value.ToGqlMetricFrequency();
    }
  }
}

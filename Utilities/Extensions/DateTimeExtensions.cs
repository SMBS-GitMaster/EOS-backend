using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Utilities;
using System;

namespace RadialReview {
  public static class DateTimeExtensions {

    public static bool IsBetween(this DateTime self, DateTime start, DateTime end) {
      return self.IsAfter(start) && self.IsBefore(end);
    }

    public static bool IsAfter(this DateTime self, DateTime other) {
      return self > other;
    }
    public static bool IsBefore(this DateTime self, DateTime other) {
      return self < other;
    }

    public static DateTime AddDaysSafe(this DateTime self, double days) {
      try {
        return self.AddDays(days);
      } catch (ArgumentOutOfRangeException) {
        if (days > 0) {
          return DateTime.MaxValue;
        } else {
          return DateTime.MinValue;
        }
      }
    }

    public static DateTime StartOfPeriod(this DateTime dt, EventFrequency period) {
      switch (period) {
        case EventFrequency.Hourly:
          return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, 0, 0);
        case EventFrequency.Daily:
          return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);
        case EventFrequency.Weekly:
          return StartOfWeek(dt, DayOfWeek.Sunday);
        case EventFrequency.Biweekly:
          return TimingUtility.GetDateSinceEpoch((int)(TimingUtility.GetWeekSinceEpoch(dt) / 2) * 2);
        case EventFrequency.Monthly:
          return new DateTime(dt.Year, dt.Month, 1);
        case EventFrequency.Yearly:
          return new DateTime(dt.Year, 1, 1);
        default:
          throw new ArgumentOutOfRangeException("period", "" + period);
      }
    }

    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek) {
      var diff = dt.DayOfWeek - startOfWeek;
      if (diff < 0) {
        diff += 7;
      }
      if (dt == DateTime.MinValue) {
        return dt;
      }

      try {
        return dt.AddDaysSafe(-1 * diff).Date;
      } catch (ArgumentOutOfRangeException) {
        return dt;
      }
    }

    public static DateTime StartOfMonth(this DateTime dt, DayOfWeek startOfWeek)
    {
      if (dt == DateTime.MinValue)
      {
        return dt;
      }

      try
      {
        DateTime firstDayOfMonth = new DateTime(dt.Year, dt.Month, 1);
        int difference = ((int)startOfWeek - (int)firstDayOfMonth.DayOfWeek + 7) % 7;
        return firstDayOfMonth.AddDays(difference);
      }
      catch (ArgumentOutOfRangeException)
      {
        return dt;
      }
    }

    [Obsolete("I dont think this works...")]
    public static DateTime SafeSubtract(this DateTime dt, TimeSpan ts) {
      return Math2.Max(dt, new DateTime(ts.Ticks)).Subtract(ts);
    }

    public static string ToIso8601(this DateTime dt) {
      return dt.ToString("yyyy-MM-ddTHH\\:mm\\:ssZ");
    }

    public static double ToUnixTimeStamp(this DateTime dt) {
      return dt.ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }
    public static double? ToUnixTimeStamp(this DateTime? dt) {
      if (dt == null)
        return null;
      return ((DateTime)dt).ToUniversalTime().Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
    }

    public static DateTime FromUnixTimeStamp(this double ts) {
      return new DateTime(1970, 1, 1).AddSeconds(ts);
    }
    public static DateTime? FromUnixTimeStamp(this double? ts) {
      if (ts == null)
        return null;
      return new DateTime(1970, 1, 1).AddSeconds(ts.Value);
    }

    public static DateTime DateTimeWithoutMilliseconds(this DateTime dt)
    {
      return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
    }

    public static DateTime StartOfQuarter(this DateTime date)
    {
      int quarterNumber = (date.Month - 1) / 3 + 1;
      return new DateTime(date.Year, (quarterNumber - 1) * 3 + 1, 1);
    }

    public static DateTime FirstWeekOfQuarter(this DateTime date)
    {
      var startOfQuarter = StartOfQuarter(date);
      return startOfQuarter.AddDays((7 - (int)startOfQuarter.DayOfWeek) % 7);
    }

    public static DateTime StartOfDay(this DateTime date, int seconds = 0) => date.Date.AddSeconds(seconds);
  }
}

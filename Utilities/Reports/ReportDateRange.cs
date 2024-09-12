using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Reports {
  public class ReportDateRange {
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    private DateTimeKind Kind { get; set; }
    public TimeSpan Duration { get { return End - Start; } }

    public ReportDateRange(DateTime start, DateTime end, DateTimeKind kind) {
      Start = new DateTime(Math.Min(start.Ticks, end.Ticks), kind).ToUniversalTime();
      End = new DateTime(Math.Max(start.Ticks, end.Ticks), kind).ToUniversalTime();
      Kind = DateTimeKind.Utc;
    }

    public ReportDateRange(TimeSpan before, DateTime end, DateTimeKind kind) : this(end - before, end, kind) { }
    public ReportDateRange(double minutesBefore, DateTime end, DateTimeKind kind) : this(end - TimeSpan.FromMinutes(minutesBefore), end, kind) { }

    public ReportDateRange(DateTime start, TimeSpan after, DateTimeKind kind) : this(start, start + after, kind) { }
    public ReportDateRange(DateTime start, double minutesAfter, DateTimeKind kind) : this(start, start + TimeSpan.FromMinutes(minutesAfter), kind) { }
    //public ReportDateRange(DateTime start, ReportDuration duration, int periods, DateTimeKind kind) : this(start, start.AddDays(duration.GetTotalDays() * periods), kind) { }

    public static ReportDateRange At(DateTime time, DateTimeKind kind) {
      return Around(time, TimeSpan.FromSeconds(0), kind);
    }

    public bool Contains(DateTime date) {
      return (Start <= date && date <= End);
    }

    public DateTimeKind GetKind() {
      return Kind;
    }

    public TimeSpan GetTimezoneOffset() {
      if (Kind == DateTimeKind.Utc) {
        return TimeSpan.Zero;
      }
      //probably can't get here.
      throw new NotImplementedException();
    }


    public static ReportDateRange Around(DateTime time, TimeSpan totalSpan, DateTimeKind kind) {
      var r = new TimeSpan(totalSpan.Ticks / 2);

      return new ReportDateRange(time - r, time + r, kind);
    }
    public static ReportDateRange Around(TimeSpan totalSpan, DateTime time, DateTimeKind kind) {
      var r = new TimeSpan(totalSpan.Ticks / 2);
      return new ReportDateRange(time - r, time + r, kind);
    }

    public static ReportDateRange Around(double minutes, DateTime time, DateTimeKind kind) {
      return Around(TimeSpan.FromMinutes(minutes), time, kind);
    }
    public static ReportDateRange Before(double minutes, DateTime time, DateTimeKind kind) {
      return new ReportDateRange(time - TimeSpan.FromMinutes(minutes), time, kind);
    }

    public static ReportDateRange Before(TimeSpan duration, DateTime time, DateTimeKind kind) {
      return new ReportDateRange(time - duration, time, kind);
    }

    public override string ToString() {
      return Start.Ticks + "_" + End.Ticks + "_" + Kind;
    }
  }
}

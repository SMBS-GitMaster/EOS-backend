using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RadialReview.Core.Utilities.Reports {
  [Flags]
  public enum FlagType {
    None = 0,
    UserFlag = 1,
    UnusuallyLongRequest = 2,
    PotentialCauses = 4,
    LikelyCause = 8,
    ByGuid = 16,
    HasError = 32,
    Fixed = 64,
    Clipped = 128,
  }
  public interface ILogLine {

    string Guid { get; set; }
    DateTime EndTime { get; }
    DateTime StartTime { get; }
    FlagType Flag { get; set; }
    int GroupNumber { get; set; }
    string Title { get; }
    string Description { get; set; }


  }
  public interface ILogLineIO : ILogLine {
    string[] GetHeaders(bool additionalData);
    string[] GetLine(DateTime firstLogStartTime, bool additionalData);
    ILogLine ConstructFromLine(string line);
    IEnumerable<string> GetLines(string file);
  }


  public delegate string ILogLineField<LINE>(LINE line) where LINE : ILogLine;
  public delegate DateTime ILogLineDateField<LINE>(LINE line) where LINE : ILogLine;

  public class LogFile {
    public static IEnumerable<string> ToStringLines<LINEIO>(LogFile<LINEIO> file, string separator, bool additionalData = true) where LINEIO : ILogLineIO {
      var f = file.GetFilteredLines();
      var first = f.FirstOrDefault();
      var date = file.StartRange;
      var title = "";
      if (first != null) {
        date = first.StartTime;
        title = string.Join(separator, first.GetHeaders(additionalData));
      }
      var lines = new List<string> { title };
      lines.AddRange(f.Select(x => string.Join(separator, x.GetLine(date, additionalData))));
      return lines;
    }


    public static void Save<LINEIO>(LogFile<LINEIO> file, string path, string record_separator) where LINEIO : ILogLineIO {
      var lines = ToStringLines(file, record_separator);
      File.WriteAllLines(path, lines);
      if (!lines.Any()) {
        //Log.Warn("No lines.", true);
      }
    }
  }

  public class LogFile<LINE> where LINE : ILogLine {
    public string Path { get; set; }
    public DateTime ParseTime { get; set; }
    public DateTime StartRange { get; set; }
    public DateTime EndRange { get; set; }

    protected Func<LINE, object> Ordering { get; set; }
    protected List<LINE> Lines { get; set; }
    protected List<IFilter<LINE>> Filters { get; set; }
    protected List<IFilter<LINE>> RelativeRangeFilter { get; set; }
    protected Func<LINE, object> Grouping { get; set; }
    protected int SkipLines { get; set; }
    protected List<Action<LINE>> ForEachs { get; set; }

    protected Func<LINE, string> Description { get; set; }

    protected List<Tuple<Func<LINE, bool>, FlagType>> Flags { get; set; }
    protected bool FlagsToTop { get; private set; }
    protected List<TimeSlice> Slices { get; set; }

    protected List<LINE> FilteredLineCache = null;


    public LogFile<LINE> Clone() {
      return new LogFile<LINE>() {
        Path = Path,
        ParseTime = ParseTime,
        StartRange = StartRange,
        EndRange = EndRange,
        Ordering = Ordering,
        Lines = Lines.ToList(),
        Filters = Filters.ToList(),
        RelativeRangeFilter = RelativeRangeFilter.ToList(),
        Flags = Flags.ToList(),
        SkipLines = SkipLines,
        Grouping = Grouping,
        FlagsToTop = FlagsToTop,
        ForEachs = ForEachs.ToList(),
        Slices = Slices.ToList(),
        FilteredLineCache = FilteredLineCache.NotNull(x => x.ToList()),
        Description = Description,

      };
    }

    public LogFile() {
      Lines = new List<LINE>();
      ParseTime = DateTime.UtcNow;
      Filters = new List<IFilter<LINE>>();
      StartRange = DateTime.MaxValue;
      EndRange = DateTime.MinValue;
      RelativeRangeFilter = new List<IFilter<LINE>>();
      Flags = new List<Tuple<Func<LINE, bool>, FlagType>>();
      ForEachs = new List<Action<LINE>>();
      Slices = new List<TimeSlice>();
    }

    public LINE AddLine(LINE line) {
      ResetCache();
      StartRange = new DateTime(Math.Min(line.StartTime.Ticks, StartRange.Ticks));
      EndRange = new DateTime(Math.Max(line.EndTime.Ticks, EndRange.Ticks));
      Lines.Add(line);
      return line;
    }
    public void SetGrouping<T>(Func<LINE, T> groupBy) {
      ResetCache();
      Grouping = x => groupBy(x);
    }

    public void SetOrdering<T>(Func<LINE, T> order) {
      ResetCache();
      Ordering = x => order(x);
    }

    public void AddFilters(params IFilter<LINE>[] filters) {
      ResetCache();
      foreach (var filter in filters) {
        TestFilterForConflits(filter);
        Filters.Add(filter);
      }
    }

    private void TestFilterForConflits(IFilter<LINE> filter) {
      var anyConfilts = Filters.Where(x => x.Conflit(filter));
      if (anyConfilts.Any()) {
        throw new Exception("This filter:\n\t" + filter.ToString() + "\nconflicts with the following filters:\n" + string.Join("\n", anyConfilts.Select(x => "\t" + x.ToString())));
      }
    }

    public void Skip(int lines) {
      ResetCache();
      SkipLines = lines;
    }

    public void Flag(Func<LINE, bool> condition, FlagType flagType = FlagType.UserFlag) {
      ResetCache();
      Flags.Add(Tuple.Create(condition, flagType));
    }

    public void AddLines(LogFile<LINE> logFile) {
      foreach (var line in logFile.GetFilteredLines()) {
        AddLine(line);
      }
    }
    public void AddLines(IEnumerable<LINE> logLines) {
      foreach (var line in logLines) {
        AddLine(line);
      }
    }

    public void FilterExact(ILogLineField<LINE> field, params string[] exclude) {
      AddFilters(exclude.Select(x => new StringFilter<LINE>(x, FilterType.Exclude, field, true)).ToArray());
    }

    public LogFile<LINE> Filter(ILogLineField<LINE> field, params string[] exclude) {
      AddFilters(exclude.Select(x => new StringFilter<LINE>(x, FilterType.Exclude, field)).ToArray());
      return this;
    }

    public LogFile<LINE> Where(Func<LINE, bool> predicate) {
      AddFilters(new CustomFilter<LINE>(predicate, FilterType.Include));
      return this;
    }

    public ReportDateRange GetTimeRange() {
      var found = GetFilteredLines();
      if (!found.Any())
        return new ReportDateRange(DateTime.MinValue, DateTime.MaxValue, DateTimeKind.Utc);
      return new ReportDateRange(found.Min(x => x.StartTime).ToUniversalTime(), found.Max(x => x.EndTime).ToUniversalTime(), DateTimeKind.Utc);
    }

    public LogFile<LINE> Filter(Func<LINE, bool> predicate, FilterType type = FilterType.Exclude) {
      AddFilters(new CustomFilter<LINE>(predicate, type));
      return this;
    }
    public LogFile<LINE> FilterRange(ReportDateRange range, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
      return FilterRange(range.Start, range.End, type);
    }
    public LogFile<LINE> FilterRange(ReportDateRange range, double expandBy, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
      return FilterRange(range.Start, range.End, expandBy, type);
    }
    public LogFile<LINE> FilterRange(DateTime start, DateTime end, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
      AddFilters(new DateRangeFilter<LINE>(start, end, type, x => x.StartTime, x => x.EndTime));
      return this;
    }
    public LogFile<LINE> FilterRange(DateTime start, DateTime end, double expandByMinutes, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
      AddFilters(new DateRangeFilter<LINE>(start.AddMinutes(-expandByMinutes / 2), end.AddMinutes(expandByMinutes / 2), type, x => x.StartTime, x => x.EndTime));
      return this;
    }

    [Obsolete("Must be just before save")]
    public LogFile<LINE> FilterRelativeRange(double startMinutes, double? endMinutes = null, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
      FilterRelativeRange(TimeSpan.FromMinutes(startMinutes), TimeSpan.FromMinutes(endMinutes ?? 1000000), type);
      return this;
    }

    [Obsolete("Must be just before save")]
    public LogFile<LINE> FilterRelativeRange(TimeSpan start, TimeSpan? end = null, DateRangeFilterType type = DateRangeFilterType.PartlyInRange) {
      ResetCache();
      var first = GetFilteredLines().First();
      var s = new DateTime(Math.Min(first.StartTime.Ticks, first.EndTime.Ticks));
      var d = first.EndTime - first.StartTime;
      var filter = new DateRangeFilter<LINE>(s + start, s + d + (end ?? TimeSpan.FromMinutes(1000000)), type, x => x.StartTime, x => x.EndTime);
      TestFilterForConflits(filter);
      RelativeRangeFilter.Add(filter);
      return this;
    }


    public int Count() {
      return GetFilteredLines().Count();
    }


    public IEnumerable<LINE> GetFilteredLines() {

      if (FilteredLineCache != null) {
        return FilteredLineCache;
      }


      //Apply filters
      var f = Lines.Where(line => Filters.All(filter => filter.Include(line)));
      //apply orderings
      if (Ordering != null) {
        f = f.OrderBy(Ordering);
      }
      //Apply relative filter
      f = f.Where(line => RelativeRangeFilter.All(filter => filter.Include(line)));


      //Handle Grouping
      if (Grouping != null) {
        var i = 0;
        var groups = f.GroupBy(Grouping);
        f = groups.SelectMany(x => {
          x.ToList().ForEach(y => { y.GroupNumber = i; });
          i += 1;
          return x.ToList();
        });
        var groupingsLookup = groups.ToDictionary(x => x.Key, x => x.ToList());
        if (Ordering != null) {
          f = f.OrderByDescending(x => groupingsLookup[Grouping(x)].Count()).ThenBy(x => groupingsLookup[Grouping(x)].First().StartTime).ThenBy(Grouping).ThenBy(Ordering);
        } else {
          f = f.OrderByDescending(x => groupingsLookup[Grouping(x)].Count()).ThenBy(x => groupingsLookup[Grouping(x)].First().StartTime).ThenBy(Grouping);
        }
        f = f.ToList();
        var j = -1;
        var prevGroup = -1;
        foreach (var item in f) {
          var ign = item.GroupNumber;
          if (ign != prevGroup) {
            j += 1;
            prevGroup = item.GroupNumber;
          }
          item.GroupNumber = j;
        }

      }

      //Apply flags
      foreach (var h in Flags) {
        foreach (var line in f.ToList()) {
          var flag = h.Item1(line);
          if (flag) {
            line.Flag |= h.Item2;
          }
        }
      }

      //Skip Lines
      f = f.Skip(SkipLines);

      //Add flags to top
      f = f.ToList();
      if (FlagsToTop) {
        var orderedFlags = f.Where(x => x.Flag != FlagType.None).OrderByDescending(x => x.Flag).ToList();
        orderedFlags.AddRange(f.ToList());
        f = orderedFlags;
      }

      foreach (var line in f) {
        foreach (var each in ForEachs) {
          each(line);
        }
        line.Description = Description != null ? Description(line) : null;
      }
      FilteredLineCache = f.ToList();
      return f;
    }

    public List<TimeSlice> GetSlices() {
      return Slices.ToList();
    }

    public void ForEach(Action<LINE> action) {
      ResetCache();
      ForEachs.Add(action);
    }

    public List<T> InTheseButNotThose<T, GROUP>(Func<LINE, GROUP> groupOn, Func<LINE, T> field, params GROUP[] inThese) {
      var lineGroups = GetFilteredLines().GroupBy(groupOn).ToList();

      var inGroups = lineGroups.Where(x => inThese.Contains(x.Key));
      var outItems = lineGroups.Where(x => !inThese.Contains(x.Key)).SelectMany(x => x.Select(y => field(y))).Distinct().ToList();

      //Intersect all inGroups
      var hasAll = inGroups.FirstOrDefault().NotNull(x => x.Select(y => field(y)).Distinct());
      if (hasAll == null)
        return new List<T>();
      foreach (var inG in inGroups.Skip(1)) {
        hasAll = hasAll.Intersect(inG.Select(x => field(x)).Distinct());
      }

      //Remove things from outGroup
      hasAll = hasAll.Where(x => !outItems.Contains(x));

      return hasAll.ToList();

    }



    //public SetUtility.AddedRemoved<T, T> Similarity<T>(Func<LINE, bool> interesting, Func<LINE, T> differencesOn) {
    //  var lineGroup = GetFilteredLines().ToList();

    //  var left = new Dictionary<T, bool>();
    //  var right = new Dictionary<T, bool>();

    //  foreach (var g in lineGroup) {
    //    if (interesting(g)) {
    //      left[differencesOn(g)] = true;
    //    } else {
    //      right[differencesOn(g)] = true;
    //    }
    //  }

    //  return SetUtility.AddRemove(right.Select(x => x.Key), left.Select(x => x.Key));
    //}

    public void SetDescription(Func<LINE, string> p) {
      Description = p;
    }

    //public PivotTable<LINE> ToPivotTable(ILogLineField<LINE> x, ILogLineField<LINE> y, Func<List<LINE>, string> cell) {
    //  return new PivotTable<LINE>(this, x, y, cell);
    //}

    //public PreMatrix<LINE, XTYPE, YTYPE> ToMatrixBuilder<XTYPE, YTYPE>(Func<LINE, XTYPE> xs, Func<LINE, YTYPE> ys) {
    //  return Matrix.Create(this, xs, ys);
    //}

    public void FlagsAtTop() {
      FlagsToTop = true;
    }

    public void AddSlice(ReportDateRange range, string name, string color = null) {
      Slices.Add(new TimeSlice(range, name, null, color));
    }

    public void AddSlice(DateTime time, DateTimeKind kind, string name, string color = null) {
      Slices.Add(new TimeSlice(time, kind, name, null, color));
    }

    public void AddSlice(DateTime start, DateTime endTime, string name = "", string color = null) {
      Slices.Add(new TimeSlice(new ReportDateRange(start, endTime, start.Kind), name, null, color));
    }

    public void AddSlice(TimeSlice slice) {
      Slices.Add(slice);
    }


    protected void ResetCache() {
      FilteredLineCache = null;
    }
  }
}
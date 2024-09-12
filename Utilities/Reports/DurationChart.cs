using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Diagnostics;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities;

namespace RadialReview.Core.Utilities.Reports {

  [Obsolete("Do not use. For Reports only")]
  public class ReportHelpers {
    [DebuggerDisplay("({X}, {Y})")]
    [Serializable]
    public class Point {
      /// <summary>
      /// Use utc time
      /// </summary>
      public DateTime X { get; set; }
      public decimal? Y { get; set; }

      public Point() {
      }

      public Point(DateTime x, decimal? y) {
        X = x;
        Y = y;
      }
      public Point(long utc, decimal? y) : this(utc.ToDateTime(), y) {
      }

    }

    public class DataChartModel {
      public string Color { get; set; }
      public string Name { get; set; }
      public List<Point> Datapoints { get; set; }
      //public Stat Statistic { get; set; }
    }

    public interface IEvent {
      string Name { get; }
      DateTime Start { get; }
    }
    public interface IEvents {
      List<IEvent> Events { get; set; }
    }
    public interface IGroupedEvent : IEvent {
      string GroupId { get; }
    }

    public interface IEventRange : IEvent {
      DateTime End { get; }
    }


    public interface IColoredEvent : IEvent {
      string Color { get; }
    }


    public static IEnumerable<object> GetIndividualFlags(Enum value) {
      return GetFlags(value, GetFlagValues(value.GetType()).ToArray());
    }

    private static IEnumerable<Enum> GetFlags(Enum value, Enum[] values) {
      ulong bits = Convert.ToUInt64(value);
      List<Enum> results = new List<Enum>();
      for (int i = values.Length - 1; i >= 0; i--) {
        ulong mask = Convert.ToUInt64(values[i]);
        if (i == 0 && mask == 0L)
          break;
        if ((bits & mask) == mask) {
          results.Add(values[i]);
          bits -= mask;
        }
      }
      if (bits != 0L)
        return Enumerable.Empty<Enum>();
      if (Convert.ToUInt64(value) != 0L)
        return results.Reverse<Enum>();
      if (bits == Convert.ToUInt64(value) && values.Length > 0 && Convert.ToUInt64(values[0]) == 0L)
        return values.Take(1);
      return Enumerable.Empty<Enum>();
    }
    private static IEnumerable<Enum> GetFlagValues(Type enumType) {
      ulong flag = 0x1;
      foreach (var value in Enum.GetValues(enumType).Cast<Enum>()) {
        ulong bits = Convert.ToUInt64(value);
        if (bits == 0L)
          //yield return value;
          continue; // skip the zero value
        while (flag < bits)
          flag <<= 1;
        if (flag == bits)
          yield return value;
      }
    }


    public static string GetColor(EventType type) {
      switch (type) {
        case EventType.CreateOrganization:
          return "darkgreen";
        case EventType.EnableL10:
          return "#ff5e00";
        case EventType.DisableL10:
          return "#521e00";
        case EventType.EnableReview:
          return "#00c3ff";
        case EventType.DisableReview:
          return "#006240";
        case EventType.SignupStep:
          return "#ba34eb";
        case EventType.CreateLeadershipMeeting:
          return "#eb8634";
        case EventType.CreateDepartmentMeeting:
          return "#ad751f";
        case EventType.ConcludeMeeting:
          return "#20c9b6";
        case EventType.DeleteMeeting:
          return "#994500";
        case EventType.IssueReview:
          return "#3094b3";
        case EventType.NoReview_3m:
          return "#1c8da340";
        case EventType.NoReview_4m:
          return "#176e8040";
        case EventType.NoReview_6m:
          return "#11515e40";
        case EventType.NoReview_8m:
          return "#0e424d40";
        case EventType.NoReview_12m:
          return "#0a323b40";
        case EventType.NoLeadershipMeetingCreated_1w:
          return "#8a4f2040";
        case EventType.NoLeadershipMeetingCreated_2w:
          return "#8a4f2040";
        case EventType.NoLeadershipMeetingCreated_3w:
          return "#8a4f2040";
        case EventType.NoLeadershipMeetingCreated_4w:
          return "#8a4f2040";
        case EventType.NoLeadershipMeetingCreated_6w:
          return "#8a4f2040";
        case EventType.NoLeadershipMeetingCreated_8w:
          return "#8a4f2040";
        case EventType.NoLeadershipMeetingCreated_10w:
          return "#8a4f2040";
        case EventType.NoLeadershipMeetingCreated_12w:
          return "#8a4f2040";
        case EventType.NoDepartmentMeetingCreated_2w:
          return "#855a1840";
        case EventType.NoDepartmentMeetingCreated_4w:
          return "#855a1840";
        case EventType.NoDepartmentMeetingCreated_6w:
          return "#855a1840";
        case EventType.NoDepartmentMeetingCreated_8w:
          return "#855a1840";
        case EventType.NoDepartmentMeetingCreated_10w:
          return "#855a1840";
        case EventType.NoDepartmentMeetingCreated_12w:
          return "#855a1840";
        case EventType.NoMeeting_1w:
          return "#733e1440";
        case EventType.NoMeeting_2w:
          return "#733e1440";
        case EventType.NoMeeting_3w:
          return "#733e1440";
        case EventType.NoMeeting_4w:
          return "#733e1440";
        case EventType.NoMeeting_6w:
          return "#733e1440";
        case EventType.NoMeeting_8w:
          return "#733e1440";
        case EventType.NoMeeting_10w:
          return "#733e1440";
        case EventType.NoMeeting_12w:
          return "#733e1440";
        case EventType.NoLogins_3d:
          return "#6e0a2840";
        case EventType.NoLogins_5d:
          return "#6e0a2840";
        case EventType.NoLogins_1w:
          return "#6e0a2840";
        case EventType.NoLogins_2w:
          return "#6e0a2840";
        case EventType.NoLogins_3w:
          return "#6e0a2840";
        case EventType.NoLogins_4w:
          return "#6e0a2840";
        case EventType.NoLogins_6w:
          return "#6e0a2840";
        case EventType.NoLogins_8w:
          return "#6e0a2840";
        case EventType.NoLogins_10w:
          return "#6e0a2840";
        case EventType.NoLogins_12w:
          return "#6e0a2840";
        case EventType.AccountAge_1d:
          return "#76a17440";
        case EventType.AccountAge_2d:
          return "#76a17440";
        case EventType.AccountAge_3d:
          return "#76a17440";
        case EventType.AccountAge_4d:
          return "#76a17440";
        case EventType.AccountAge_5d:
          return "#76a17440";
        case EventType.AccountAge_6d:
          return "#76a17440";
        case EventType.AccountAge_1w:
          return "#76a17440";
        case EventType.AccountAge_2w:
          return "#76a17440";
        case EventType.AccountAge_3w:
          return "#76a17440";
        case EventType.AccountAge_monthly:
          return "#76a17440";
        case EventType.PaymentFree:
          return "#d4b40085";
        case EventType.PaymentReceived:
          return "#d4b400";
        case EventType.PaymentFailed:
          return "#e4673d";
        case EventType.UndeleteMeeting:
          return "#d45f00";
        case EventType.CreatePrimaryContact:
          return "#00d696";
        case EventType.CreateMeeting:
          return "#d45f00";
        case EventType.StartLeadershipMeeting:
          return "#ffae52";
        case EventType.StartDepartmentMeeting:
          return "#ffc552";
        case EventType.EnablePeople:
          return "#e61565";
        case EventType.DisablePeople:
          return "#940039";
        case EventType.PaymentEntered:
          return "#ffbb00";
        case EventType.EnableCoreProcess:
          return "#3824ed";
        case EventType.DisableCoreProcess:
          return "#0b0073";
        case EventType.EnableBetaButton:
          return "#3824ed";
        case EventType.DisableBetaButton:
          return "#0b0073";
        case EventType.EnableZapier:
          return "#ff4a00";
        case EventType.DisableZapier:
          return "#a83100";
        case EventType.EnableDocs:
          return "#00e3eb";
        case EventType.DisableDocs:
          return "#008a8f";
        default:
          return "#ff00ff";
      }
    }
  }


  public class DurationChartOptions<LINE> where LINE : ILogLine {



    public DurationChartOptions(string outputPath, LogFile<LINE> logs) {
      if (outputPath == null)
        throw new ArgumentNullException(nameof(outputPath));
      if (logs == null)
        throw new ArgumentNullException(nameof(logs));

      OutputPath = outputPath;
      Logs = logs;
    }

    public string OutputPath { get; set; }
    public LogFile<LINE> Logs { get; set; }
    public LogFile<EventLine> Events { get; set; }
    public Coloring<LINE> LineColor { get; set; }
    public Coloring<ReportHelpers.IEvent> EventColor { get; set; }

    public IEnumerable<ReportHelpers.DataChartModel> Charts { get; set; }
  }

  public class Coloring<T> {
    public Coloring(Func<T, object> selector, IPallet pallet) {
      Selector = selector;
      Pallet = pallet;
    }

    public Func<T, object> Selector { get; set; }
    public IPallet Pallet { get; set; }
  }

  public class DurationChart {

    //public static string[] Pallet1 = new string[] { "#7C4338", "#745D7B", "#318382", "#7A9349", "#834548", "#646785", "#358975", "#959241", "#844A5A", "#51728A", "#478E66", "#B19041", "#7F526C", "#3D7B89", "#5F9257", "#CA8C4A" };
    //public static string[] Pallet2 = new string[] { "#f23d3d", "#e5b073", "#3df2ce", "#c200f2", "#e57373", "#f2b63d", "#3de6f2", "#e639c3", "#ff2200", "#d9d26c", "#0099e6", "#d9368d", "#d96236", "#cad900", "#73bfe6", "#d90057", "#ffa280", "#aaff00", "#397ee6", "#f27999", "#ff6600", "#a6d96c", "#4073ff", "#d9986c", "#50e639", "#3d00e6", "#e57a00", "#36d98d", "#b56cd9"};
    //public static string[] Scale_Pallet1 = new string[] { "#80423C", "#8A474C", "#904D5D", "#93566E", "#926080", "#8D6C91", "#8478A0", "#7785AC", "#6692B4", "#549EB8", "#43A9B7", "#39B4B2", "#3DBEAA", "#4FC79E", "#67CF91", "#83D582", "#A2DA74", "#C1DE69", "#E2E062" };

    private static string Escape(string str) {
      return str.NotNull(x => x.Replace("<", "&lt;").Replace(">", "&gt;"));
    }


    public static void Save<LINE>(DurationChartOptions<LINE> options) where LINE : ILogLine {
      if (options == null)
        throw new ArgumentNullException(nameof(options));

      string outputPath = options.OutputPath;
      LogFile<LINE> file = options.Logs;
      LogFile<EventLine> eventFile = options.Events;
      //Func<LINE, object> colorBy = options.ColorBy;
      //IPallet pallet = options.ColorPallet;
      IEnumerable<ReportHelpers.DataChartModel> charts = options.Charts;


      outputPath = outputPath.Replace("{datetime}", DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
      outputPath = outputPath.Replace("{date}", DateTime.UtcNow.ToString("yyyyMMdd"));
      outputPath = outputPath.Replace("{time}", DateTime.UtcNow.ToString("HHmmss"));


      var dir = Path.GetDirectoryName(outputPath);
      if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);

      //Clear file
      File.WriteAllText(outputPath, string.Empty);

      using (var builder = new StreamWriter(outputPath)) {
        BuildDurationChart(builder, file, eventFile, options.LineColor, charts);
      }
    }

    public static string AsString(LogFile<LogLine> file, LogFile<EventLine> eventFile, Coloring<LogLine> lineColor = null, IEnumerable<ReportHelpers.DataChartModel> charts = null) {
      using (var ms = new MemoryStream()) {
        using (var builder = new StreamWriter(ms)) {
          BuildDurationChart(builder, file, eventFile, lineColor, charts);
        }
        return Encoding.UTF8.GetString(ms.ToArray());
      }
    }
    public static object AsObject(LogFile<LogLine> file, LogFile<EventLine> eventFile, Coloring<LogLine> lineColor = null, IEnumerable<ReportHelpers.DataChartModel> charts = null) {
      using (var ms = new MemoryStream()) {
        using (var builder = new StreamWriter(ms)) {
          return BuildDurationChart(builder, file, eventFile, lineColor, charts);
        }
      }
    }






    private static object BuildDurationChart<LINE>(StreamWriter builder, LogFile<LINE> logs, LogFile<EventLine> eventFile, Coloring<LINE> lineColor = null, IEnumerable<ReportHelpers.DataChartModel> charts = null) where LINE : ILogLine {
      var colorBy = lineColor.NotNull(x => x.Selector);
      var pallet = lineColor.NotNull(x => x.Pallet);
      var lines = logs.GetFilteredLines();
      var start = DateTime.MinValue;
      var end = DateTime.MaxValue;
      if (lines.Any()) {
        start = lines.Min(x => x.StartTime);
        end = lines.Max(x => x.EndTime);
      }
      var colorLookup = GetColorLookup(colorBy, lines, pallet);
      var totalDuration = end - start;

      List<TimeSlice> allSlices = new List<TimeSlice>();
      if (eventFile != null) {
        var eventSlices = eventFile.FilterRange(start, end).GetFilteredLines()
                      .Select(x => new TimeSlice(x.StartTime, DateTimeKind.Utc, x.machine, x.message, "yellowgreen"))
                      .ToList();
        allSlices.AddRange(eventSlices);
      }

      allSlices.AddRange(logs.GetSlices());



      var secondAsPercentage = TimeSpan.FromSeconds(1).TotalSeconds / totalDuration.TotalSeconds * 100.0;
      builder.Write("<html style='overflow-x: hidden;'>");
      AppendStyles(builder, secondAsPercentage);


      ///////////////////////////////////
      // Bar Container
      ///////////////////////////////////
      builder.Write("<body>");
      builder.Write("<input type='checkbox' id='left-shift' title='Shift left' />");
      builder.Write("<div class='bar-container'>");
      var legend = new StringBuilder();//AppendCharts(builder, start, end, charts);
      foreach (var color in allSlices.GroupBy(x => x.Color ?? "blue")) {
        legend.Append("<button onclick='toggleSlices(this)' color='" + color.Key + "'>" + color.Key + " slices</button>");
      }

      legend.Append("<a href='#stats-container'>#Stats</a> <a href='#slice-data-container'>#Slices</a> ");
      legend.Append("<input type='text' onkeyup='filterSlices(this)' placeholder='Filter slices...' style='display:block;width:calc(100% - 1px);'/>");

      builder.Write("<div id='scaler'>");
      var i = 0;
      foreach (var line in lines) {
        var startOffset = (line.StartTime - start).TotalSeconds / totalDuration.TotalSeconds * 100;
        var duration = (line.EndTime - line.StartTime).TotalSeconds;
        var width = duration / totalDuration.TotalSeconds * 100;
        var barColor = "#aaa";
        if (colorBy != null) {
          barColor = colorLookup[colorBy(line)];
        }

        var flag = "";
        var flagText = "";
        if (line.Flag != FlagType.None) {
          flag += "flag ";
          foreach (var f in ReportHelpers.GetIndividualFlags(line.Flag)) {
            flag += "flag-" + f + " ";
            flagText += f + " ";
          }
        }

        var titleLoc = "right";
        if (startOffset > 50) {
          titleLoc = "left";
        }

        if (width > 50) {
          titleLoc = "center";
        }

        var stripe = (line.GroupNumber % 2 == 1 ? "stripe" : "");

        builder.Write("<div class='row " + flag + stripe + " group-" + line.GroupNumber + "' id='line_" + i + "' data-guid='" + line.Guid + "'>");
        builder.Write("<span class='click-flag-text' title='" + flagText + "'>(flag)</span>");
        builder.Write("<div class='bar ' data-start='" + line.StartTime.ToJsMs() + "' data-duration='" + (line.EndTime - line.StartTime).TotalMilliseconds + "' style='left:" + startOffset + "%;width:" + width + "%;background-color:" + barColor + ";" +/*border-left:1px solid " + barColor +";"*/ "' title='" + line.StartTime.ToString("HH:mm:ss") + " [" + ((int)(duration * 1000)) / 1000.0 + "s] "/*+ line.EndTime.ToString("HH:mm:ss")*/ + "'>");
        builder.Write("<span class='title title-" + titleLoc + "'>" + (Escape(line.Title)) + "</span>");
        builder.Write("<span class='line-description line-description-" + titleLoc + "' title='" + Escape(line.Description) + "'>" + Escape(line.Description) + "</span>");

        if (typeof(ReportHelpers.IEvents).IsAssignableFrom(typeof(LINE))) {
          var lineEvents = ((ReportHelpers.IEvents)line);
          if (lineEvents.Events != null) {
            var colorCount = (double)Pallets.Stratified.GetColorCount();
            foreach (var evt in ((ReportHelpers.IEvents)line).Events.Where(x => start <= x.Start && x.Start <= end)) {
              var evtStartOffset = Math.Round((evt.Start - line.StartTime).TotalSeconds / (line.EndTime - line.StartTime).TotalSeconds * 100, 2);
              var evtBarColor = Pallets.Stratified.GetColor((evt.Name.GetHashCode() % colorCount) / colorCount);

              if (typeof(ReportHelpers.IColoredEvent).IsAssignableFrom(evt.GetType())) {
                var coloredEvt = (ReportHelpers.IColoredEvent)evt;
                if (!string.IsNullOrWhiteSpace(coloredEvt.Color)) {
                  evtBarColor = coloredEvt.Color;
                }
              }

              if (typeof(ReportHelpers.IEventRange).IsAssignableFrom(evt.GetType())) {
                var evtWidth = Math.Round((((ReportHelpers.IEventRange)evt).End - evt.Start).TotalSeconds / (line.EndTime - line.StartTime).TotalSeconds * 100, 2);

                builder.Write($@"<div class='event ranged' style='left:{evtStartOffset}%;width:{evtWidth}%;border-color:{evtBarColor};background:{evtBarColor};'><span>{evt.Name}</span></div>");

              } else {
                builder.Write($@"<div class='event' style='left:{evtStartOffset}%;border-color:{evtBarColor};'><span>{evt.Name}</span></div>");
              }
            }
          }
        }


        builder.Write("</div>\n");

        if (line.Flag != FlagType.None) {
          builder.Write("<div class='flag-icon-container'>");
          foreach (var t in EnumExtensions.GetAllFlags<FlagType>()) {
            builder.Write("<span class='flag-icon flag-icon-" + t + "' title='" + t + "'>(flag)(" + t + ")</span>");
          }
          builder.Write("</div>");
        }

        builder.Write("</div>");
        i++;
      }
      builder.Write("</div>");


      ///////////////////////////////////
      // Data Container
      ///////////////////////////////////
      builder.Write("</div><div class='data-container'>");
      builder.Write("<div class='row-data json'>");
      i = 0;
      foreach (var line in lines) {

        builder.Write("<div class='hidden line-data fixedFont copyable1' id='line_" + i + "_data'>" + Escape(JsonConvert.SerializeObject(line, Formatting.Indented)) + "</div>");
        i++;
      }
      builder.Write("</div>");


      builder.Write(legend);


      var sliceData = GetSliceData(allSlices);

      // Info Container			
      var info = new {
        start = start,
        end = end,
        duration = totalDuration,
        //allUsersCount = lines.GroupByField(false).Count(),
        //activeUsersCount = lines.GroupByUsers(true).Count(),
        //activeUsers = lines.GroupByUsers(true).Select(x => new { name = x.Key, pages = x.Count() }).OrderByDescending(x => x.pages)
      };
      builder.Write($@"
<div class='info fixedFont json copyable1'>
	<div id='stats-container' class='stats'>{JsonConvert.SerializeObject(info, Formatting.Indented)}</div>
	<div class='slice-data-container' id='slice-data-container'>{sliceData}</div>
</div>");



      // Status Container			
      builder.Write("<div class='status fixedFont'><span class='status-text'>&nbsp;</span></div>");


      //builder.Write("</div>");

      ///////////////////////////////////
      // Screen Overlays
      ///////////////////////////////////
      builder.Write("<div class='mousebar fixedFont'><span></span></div>");
      builder.Write("<div class='dragbar hidden fixedFont'><span style='background:#f8f8ffcf;'></span></div>");
      builder.Write("<div class='dragbarv hidden fixedFont'></div>");
      //builder.Write("");


      ////////////////////////////
      //Slices
      ////////////////////////////
      AppendSlices(builder, allSlices, start, end);


      builder.WriteLine(@"<script src=""https://code.jquery.com/jquery-3.3.1.min.js"" integrity=""sha256-FgpCb/KJQlLNfOu91ta32o/NMZxltwRo8QtmkMRdAu8="" crossorigin=""anonymous""></script>");
      //var jquery = File.ReadAllText("Resources/Jquery.js");
      //builder.Write("<script>" + jquery + "</script>");
      AppendScripts(builder, start, end);
      builder.Write("</body>");
      builder.Write("</html>");
      //File.WriteAllText(output, builder.ToString());

      return new {
        slices = allSlices,
        secondAsPercentage = secondAsPercentage,
        lines = lines,
        info = info
      };

    }


    private static IEnumerable<Enum> GetFlagValues(Type enumType) {
      ulong flag = 0x1;
      foreach (var value in Enum.GetValues(enumType).Cast<Enum>()) {
        ulong bits = Convert.ToUInt64(value);
        if (bits == 0L)
          //yield return value;
          continue; // skip the zero value
        while (flag < bits)
          flag <<= 1;
        if (flag == bits)
          yield return value;
      }
    }



    private static string GetSliceData(List<TimeSlice> slices) {
      var sb = new StringBuilder();
      foreach (var a in slices.OrderBy(x => x.Range.Start)) {
        sb.Append("<div class='slice-data slice-data-" + a.Color + "' style='border-color:" + (a.Color) + "' guid='" + a.GUID + "'>");
        sb.Append("<h1 style='float:left'>" + Escape(a.Name) + "</h1>");
        sb.Append("<table style='float:right'><tr><td>" + a.Range.Start.ToLocalTime().ToShortTimeString() + "-" + a.Range.End.ToLocalTime().ToShortTimeString() + "</td></tr><tr><td>" + (Math.Round(a.Range.Duration.TotalSeconds * 1000) / 1000) + "s</td></tr></table>");
        sb.Append("<div style='clear:both'>" + Escape(a.Description) + "</div>");
        sb.Append("</div>");
      }
      return sb.ToString();
    }

    private static void AppendSlices(StreamWriter builder, List<TimeSlice> slices, DateTime start, DateTime end) {
      builder.Write("<div class='slice-container'>");
      var totalDuration = end.ToLocalTime() - start.ToLocalTime();
      foreach (var s in slices) {
        var startOffset = (s.Range.Start.ToLocalTime() - start.ToLocalTime()).TotalSeconds / totalDuration.TotalSeconds * 100;
        var width = (s.Range.End.ToLocalTime() - s.Range.Start.ToLocalTime()).TotalSeconds / totalDuration.TotalSeconds * 100;
        var nonZero = "non-zero";
        if (width == 0) {
          nonZero = "";
        }

        builder.Write("<div data-start='" + s.Range.Start.ToJsMs() + "' data-duration='" + s.Range.Duration.TotalMilliseconds + "' class='slice " + nonZero + " " + s.GUID + " slice-color-" + s.Color + "' style='left:" + startOffset + "%;width:" + width + "%;border-color:" + (s.Color ?? "blue") + ";'>");
        builder.Write("<span>" + Escape(s.Name) + "</span><div class='fill' style='background:" + s.Color + "'></div></div>");
      }
      builder.Write("</div>");
    }

    //private static StringBuilder AppendCharts(StreamWriter chartBuilder, DateTime start, DateTime end, IEnumerable<ReportHelpers.DataChartModel> charts) {
    //  var legendBuider = new StringBuilder();
    //  if (charts != null) {

    //    var pallet = Pallets.Stratified;

    //    legendBuider.Append("<form style='font-size: 1.1vh'>");
    //    var i = 0;

    //    var maxs = new DefaultDictionary<string, double?>(x => null);
    //    var mins = new DefaultDictionary<string, double?>(x => null);
    //    foreach (var c in charts) {
    //      if (c.Statistic.MaxKey != null) {
    //        if (c.Datapoints.Any()) {
    //          maxs[c.Statistic.MaxKey] = Math.Max((double)(c.Datapoints.Max(y => y.Y ?? 0)), (double)(maxs[c.Statistic.MaxKey] ?? 0));
    //        }
    //      }
    //    }
    //    foreach (var c in charts) {
    //      if (c.Statistic.MinKey != null) {
    //        if (c.Datapoints.Any()) {
    //          double deflt = (c.Statistic.Max ?? maxs[c.Statistic.MinKey]) ?? double.MaxValue;
    //          mins[c.Statistic.MinKey] = Math.Min((double)(c.Datapoints.Min(y => (double?)y.Y ?? deflt)), (double)(mins[c.Statistic.MinKey] ?? deflt));
    //        }
    //      }
    //    }
    //    foreach (var c in mins) {
    //      if (c.Value == double.MaxValue) {
    //        mins[c.Key] = 0;
    //      }
    //    }

    //    foreach (var c in charts) {
    //      var color = c.Color ?? pallet.NextColor();

    //      if (c.Datapoints.Any()) {
    //        var keys = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\" };
    //        var key = "";
    //        if (i < keys.Length) {
    //          key = keys[i];
    //        }
    //        var mc = new MetricChart(c, start, end, "50%", "10%");
    //        var builder = new StringBuilder();
    //        MetricChart.CreateChart("50%", "10%", builder, start, end, legendBuider, key, c, color, i, c.Statistic.MinKey.NotNull(y => mins[y]), c.Statistic.MaxKey.NotNull(y => maxs[y]));
    //        chartBuilder.Write(builder.ToString());
    //        i += 1;
    //      }
    //    }

    //    legendBuider.Append("</form>");
    //  }
    //  return legendBuider;
    //}



    private static DefaultDictionary<object, string> GetColorLookup<LINE>(Func<LINE, object> colorBy, IEnumerable<LINE> lines, IPallet pallet) where LINE : ILogLine {
      var colorLookup = new DefaultDictionary<object, string>(x => "red");
      if (colorBy != null) {
        pallet = pallet ?? new StratifiedPallet();
        var colorKeys = lines.GroupBy(colorBy).Select(x => x.Key).Distinct().OrderBy(x => x).ToArray();
        var i = 0.0;
        var rangeable = Rangeable.GetRangeable(colorKeys);

        foreach (var c in colorKeys) {
          if (rangeable != Rangeable.Invalid) {
            colorLookup[c] = pallet.GetColor(rangeable.GetPercentage(c));
          } else {
            colorLookup[c] = pallet.GetColor(i / (colorKeys.Length - 1));
          }
          i++;
        }
      }
      return colorLookup;
    }

    private static void AppendStyles(StreamWriter builder, double secondAsPercentage) {
      builder.Write(
      @"<style>
	body{
		margin: 0px;
	}

	svg{
		position:fixed;
		bottom:0px;
		z-index:1;
		pointer-events:none;
	}

	polyline{
		fill: none;
		stroke-width: 2;
	}

	.legend-dot{
		width: 1vh;
		height: 1vh;
		border-radius: 1vh;
		display: inline-block;
		top: -0.3vh;
		position: relative;
		font-size: 0.8vh;
        text-align:center;
		color: white;
		text-shadow: 0 0px 6px black;
	}

	input[type='checkbox']{
		width:1vh;
		height:1vh;
	}

    form > div{
        display: inline-block;
        width: 50%;
    }

	.chart-max{
		opacity: 0.7;
		z-index: 1;
		font-size: 69%;
		margin-left: 10px;
	}

    .chart svg {
        border-top: 1px dotted gray;
    }

	.fixedFont{
		font-size:1vh;
	}

	.noselect{
	  -moz-user-select: none;
	  -khtml-user-select: none;
	  -webkit-user-select: none;
	  user-select: none;
	}
	.title {
		display: inline-block;
		position: absolute;
		white-space: nowrap;
		font-size:12px;
		left: 102%;
		pointer-events:none;
	}
	.bar-container{
		width:50%;
		border-right:1px solid #333;
		display:inline-block;
		cursor:crosshair;
		padding-top:30px;
		padding-bottom:20px;
		overflow-y:scroll;
	}
	.data-container{
		width:calc(50% - 8px);
		border-right:1px solid lightgray;
		display:inline-block;
		position: fixed;
		top: 0;
		bottom: 0;
		margin-left:8px;
		background:white;
	}
	.info{
		height:calc(60% - 1.2vh);
		overflow:auto;
	}


	.row-data{
		height:40%;
		overflow:auto;
		border-bottom:1px solid #333;
	}

	.row{
		width:calc(100%);
		position:relative;
		padding-top:1px;
		padding-bottom:1px;
	}

	.row-hover{
		background-color:#d8d8d8;/*#efefef;*/
	}

	.row-hover.stripe{
		background-color:#d8d8d8;
	}

		
	.bar {
	    position: relative;
		margin-top:1px;
		margin-bottom:1px;
		height:12px;
		min-width:1px;
	}

	.full-res .bar:after,
	.bar:hover:after{
		content: ' ';
		position: absolute;
		right: -" + secondAsPercentage / 2 + @"vw;
		left: " + secondAsPercentage / 2 + @"vw;
		height: 1px;
		background-color: #ff00007a;
		top: 5px;
	}

	.full-res .bar:before,
	.bar:hover:before{
		content: ' ';
		position: absolute;
		right: -" + secondAsPercentage / 2 + @"vw;
		left: -1px;
		height: 3px;
		background-color: #ff000044;
		top: 4px;
		border-radius:3px;
	}

	.event{
	    position: absolute;
		min-width: 1px;
		width: 1px;
	    height: calc(100% + 2px);
	    border-left: 1px solid gray;
	    font-size: 7px;
	    top: -1px;
		white-space: nowrap;
	}

	.event span{
		display:none;
		font-size:7px;
		transform:rotate(45deg);
		width: 1px;
		pointer-events: none;
		position: relative;
		z-index: 1;
		color: rgba(0,0,0,.4);
	}

	.event:hover span {
		color: Blue ;
		text-shadow: 0 0 4px white, 0 0 4px white, 0 0 4px white, 0 0 4px white, 0 0 1px white, 0 0 1px white, 0 0 1px white, 0 0 1px white, 0 0 1px white, 0 0 1px white;
	}

	.event:hover{
		z-index :100;
	}

	.row:hover .event span{
		display:block;
	}

	.full-res .event{
		display:block;
	}
	
	.full-res .bar:hover{
        margin-left:" + secondAsPercentage / 2 + @"vw;        
    }	
	.full-res .bar:hover:before{
		left: " + secondAsPercentage / 2 + @"vw;
		right: 0;    
    }    
	.full-res .bar:hover:after{        
		right: " + secondAsPercentage / 2 + @"vw;
		left: -" + secondAsPercentage / 2 + @"vw;
    }

	.url{		
		text-shadow:1px 0 1px white, 0 1px 1px white, 1px 1px 1px white, 0 0 1px white, 1px 0 1px white, 0 1px 1px white, 1px 1px 1px white, 0 0 1px white;
	}

	.url-left{
		left: initial;
		padding-right: 104%;
		right: 20px;
	}
	.url-center{
		left: 6px;
	}

	.line-description {
		position: absolute;
		right: 103%;
		font-size: 8px;
		padding-right: 5px;
		color:#666;
		padding-top: 1px;
		text-shadow:1px 0 1px #eee, 0 1px 1px #eee, 1px 1px 1px #eee, 0 0 1px #eee, 1px 0 1px #eee, 0 1px 1px #eee, 1px 1px 1px #eee, 0 0 1px #eee;
		white-space:nowrap;		
	}
	.line-description-left {
		left: 102%;
		right:initial;
		padding-left: 5px;
	}

	.stripe{
		background-color:#f7f7f7;
	}
	

	.flag.row-hover{
		background-color:#f9c7a5;
	}

	.hidden{
		display:none;
	}
	
	.json{
		white-space: pre;
		background-color: #ffffffe3;
		/* width: 50%; */
		padding: 5px 10px;
	}

	.left-shifted .bar{
		left:0px !important;
	}

	.mousebar{
		position:fixed;
		left:0;
		width:0px;
		top:0;
		bottom:0;
		border-right:1px dotted #333333;
		text-align:right;
		color:#333;
		pointer-events:none;
		white-space:nowrap;		
	}
	.mousebar span{
		background-color:white;
	}

	.dragbar{
		position:fixed;
		top:0;
		bottom:0;
		border-left:1px dotted #333333;
		border-right:1px dotted #333333;
		background-color:#33333333;
		padding-top:1.4vh;
		text-align:center;
		pointer-events:none;
	}

	.dragbarv{
		position:fixed;
		left:0;
		right:50%;
		border-top:1px dotted #FF9800;
		border-bottom:1px dotted #FF9800;
		pointer-events:none;
		background-color: #fff5ad33;
	}

	.dragdiag{
		background: linear-gradient(to top right, transparent calc(50% - 2px), #002a8ab0, transparent calc(50% + 2px));
		position:fixed;
		text-align:center;
		pointer-events:none;
		font-weight: bold;
		font-family: monospace;
	}

	.dragdiag.flip{
		background: linear-gradient(to top left, transparent calc(50% - 2px), #002a8ab0, transparent calc(50% + 2px));
	}

	.dragdiag p{
		display: flex;
		justify-content: center;
		align-items: center;
		color: #002a8a;
		height: 100%;
		width: 100%;
		text-shadow: 0 0 4px white, 0 0 4px white, 0 0 4px white, 0 0 4px white, 0 0 4px white, 0 0 4px white, 0 0 4px white, 0 0 4px white;
	}

	.status{
		position: fixed;
		left: 50%;
		right: 0;
		bottom: 0;
		padding: 2px;
		font-family: monospace;
		background-color: #333;
		color: white;
	}

	.status-text{
		cursor:pointer;
	}

	.copyable{
		cursor:copy;
	}	

	.flag .bar{
	}
	
	.flag {    
		background-color: #fbcbff;
	}

	.flag-UserFlag{
		background-color:#e1baf5;
	}

	.flag-UnusuallyLongRequest{
		background-color: #fdf6cb;
	}

	.flag-Clipped{
		border-left: 2px dotted black;
		border-right: 2px dotted black;
	}


	.flag-PotentialCauses{
		background-color: #bbf9e1;
	}

	.flag-HasError{
		background-color: #fbcbff;
	}

	.flag-LikelyCause{
		background-color: #ffc9ce !important;
		/*color: white !important;*/
	}

		.flag-LikelyCause .bar{
			/*border-left:1px solid white!important;
			border-right:1px solid white!important;
			background-color:white !important;*/
		}

	.flag-ByGuid{
		background-color: lightblue;
	}
	.flag-Fixed{
		background-color: #c5ffbe;
	}

	.click-flag{
		background-color: #ecffdd;    
		border-right: 7px Solid limegreen;
		border-radius: 8px;
	}
	.click-flag-text{
		display:none;
		width: 0px;
		height: 0px;
		font-size: 70%;
		color: #333333cc;		
	}
	/*.flag .click-flag-text,*/
	.click-flag .click-flag-text{
		display: inline-block;   
		z-index: 1;
		position: relative;
	}
	
	.flag-icon-container{
		height:14px;
		display:inline-block;
		position: absolute;
		right: 0px;
		top: 0px;
		text-align:right;
	}	
	.flag-icon{
		display: none;
		height: 10px;
		width: 10px;
		margin: 1px;
		/*float: right;*/
		border-radius:7px;
		cursor:help;
		opacity:.9;
		border:1px solid white;
	}

	.flag-PotentialCauses .flag-icon-PotentialCauses,
	.flag-UnusuallyLongRequest .flag-icon-UnusuallyLongRequest,
	.flag-UserFlag .flag-icon-UserFlag,
	.flag-LikelyCause .flag-icon-LikelyCause,
	.flag-ByGuid .flag-icon-ByGuid,
	.flag-HasError .flag-icon-HasError,
	.flag-Fixed .flag-icon-Fixed
	{
		display:inline-block;
		color:#ffffff11;
		font-size: 12px;
		line-height: 4px;
		text-align:right;
		/*direction: rtl;*/
	}

	.flag-icon-UserFlag{
		background-color:#bb23bd;
	}
	.flag-icon-UnusuallyLongRequest{
		background-color:#FFC107;
	}
	.flag-icon-PotentialCauses{
		background-color:#037a88;
	}
	.flag-icon-LikelyCause{
		background-color:red;
	}

	.flag-icon-ByGuid{
		background-color:#7676e0;
	}
	.flag-icon-HasError{
		background-color:deeppink;
	}

	.flag-icon-Fixed{
		background-color:lime;
	}
    .slice-container{
        pointer-events:none;    
        left:0px;
        width:50%; 
        position:fixed;
        top:0;
        bottom:0;
    }

    .slice-container .slice{
        position:relative;       
        border-left:1px dashed blue;
        height: 100%;   
        position: absolute;
    }
    .slice-container .slice:after{
		content:'';

	}
    .slice-container.green .slice{
        border-left:1px dashed green;
		color:green;
    }
    .slice-container.red .slice{
        border-left:1px dashed red;
		color:red;
    }


	.slice-container .slice .hover-hidden{
		display:none;
		position:fixed;
		bottom:0;
		right:0;
		width:50%;
		height:50%;
	}

	.slice-container .slice:hover .hover-hidden{
		display:block;
	}

    .slice-container .slice.non-zero{
        border-right:1px dashed blue;
    }
    .slice-container .slice span{
        position:absolute;
        bottom:0px;
        left:0px;
        color:blue;
        font-size:70%;
        background-color: #ffffff99;
    }

    .slice-container .slice.slice-hover span{
		color:deeppink !important;
	}
    .slice-container .slice.slice-hover{
		border-left:2px dashed deeppink !important;
		background-color: deeppink !important;
		display:block !important;
	}
	
	.slice-data-container{
		padding-bottom:220px;
	}

	.slice-data {
		padding: 10px;
		border: 1px solid blue;
		margin-top: 10px;
		white-space: pre-wrap;
		font-size: 12px;
	}

	.slice-data:hover {
		background-color:#ffeaee !important;
		border-color:deeppink !important;
	}

	.slice-data div{
		white-space: pre-wrap;
		font-size: 12px;
	}
	
    .full-res .slice-container{
        display:none;
    }

	.slice-search-hidden,
	.hidden{
		display:none;
	}

	#left-shift{
		position: fixed;
		left: 50%;
		top: 7px;
		z-index: 100;
	}


	.slice-container .slice .fill{
		width  :100%;
		height  :100%;
		display: block;
		opacity: .2;
	}

	@keyframes blinker {
	  50% {
		background-color: #f3c99f;
	  }
	}

</style>");
    }

    private static void AppendScripts(StreamWriter builder, DateTime start, DateTime end) {
      builder.Write("<script>");
      builder.Write("var start=" + start.ToUniversalTime().ToJsMs() + ";var scale = " + (end.ToJsMs() - start.ToJsMs()) + ";var origStart=start;var origScale = scale;");
      builder.Write(@"
var logFileVarName='logFile';

function hoverRow(row){
	if (!dragging){
		$('.line-data').hide();
		$('.row-hover').removeClass('row-hover');
		var id = $(row).attr('id');
		$('#'+id+'_data').show();
		$(row).addClass('row-hover');
	}
}

//Hovering over a row
$('.row').hover(function(){
	hoverRow(this);
},function(){
});

function rescale(){
	if ($('.dragbar').is(':visible')){
		var w = (document.body.clientWidth/2);
		start = start + parseFloat($('.dragbar').css('left'))/(w)*scale;
		scale = $('.dragbar').width()/w * scale;
	}else{
		if(scale == origScale && start == origStart){
			return; //exit early
		}

		scale = origScale;
		start = origStart;
	}

	$('.bar,.slice').each(function(){
		var lstart = $(this).data('start');
		var ldur = $(this).data('duration');
		var startOffset = (lstart - start)/scale*100;
		var width = ldur/scale*100;
		$(this).css('left',startOffset+'%');
		$(this).css('width',width+'%');
		var urlLoc = 'right';
		if (startOffset > 50) {
			urlLoc = 'left';
		}
		if (width > 50) {
			urlLoc = 'center';
		}
		$(this).find('.url').toggleClass('url-left',urlLoc=='left');
		$(this).find('.url').toggleClass('url-right',urlLoc=='right');
		$(this).find('.url').toggleClass('url-center',urlLoc=='center');
	});
	$('.dragbar').hide();
	$('.dragdiag').remove();
}

$(document).keydown(function(e){
	if (e.keyCode==38){
		hoverRow($('.row-hover').prev());
		e.preventDefault();
	}else if (e.keyCode == 40){
		hoverRow($('.row-hover').next());
		e.preventDefault();
	}
	
	var anyFocus = $(':focus').length>0;

	if (!anyFocus && e.keyCode == 82){
		rescale();
		e.preventDefault();
	}
});

$('.slice-data').hover(function(){
	var data = $(this).attr('guid');
	$('.slice-hover').removeClass('slice-hover');
	$('.'+data).addClass('slice-hover');
});

function toggleSlices(self){
	var color = $(self).attr('color');
	$('.slice-color-' + color).toggleClass('hidden');
	$('.slice-data-' + color).toggleClass('hidden');
}

function filterSlicesQ(element) {
	var value = $(element).val();

	var contents = value.split(' ');
	var required = contents.filter(function(x){return !x.startsWith('-');}).map(function(x){return x.toLowerCase();});
	var exclude = contents.filter(function(x){return x.startsWith('-');}).map(function(x){return x.substr(1).toLowerCase();});

	$('.slice-data').each(function() {
		var self = this;
		var show = true;
		var text  =$(self).text().toLowerCase();
		var anyExclude = exclude.some(function(x){ return text.search(x)!=-1; });
		var guid = $(this).attr('guid');
		if (anyExclude){
			$(this).hide();
			$('.'+guid).addClass('slice-search-hidden');

			return;
		}

		var allInclude = required.every(function(x) { return text.search(x)!=-1; });
		if (!allInclude){
			$(this).hide();
			$('.'+guid).addClass('slice-search-hidden');
			return;	
		}
		$(this).show();
		$('.'+guid).removeClass('slice-search-hidden');
	});
}

debounce = function(func, wait, immediate) {
	var timeout;
	return function() {
		var context = this, args = arguments;
		var later = function() {
			timeout = null;
			if (!immediate) func.apply(context, args);
		};
		var callNow = immediate && !timeout;
		clearTimeout(timeout);
		timeout = setTimeout(later, wait);
		if (callNow) func.apply(context, args);
	};
};

filterSlices = debounce(filterSlicesQ, 500, false);

function prettyDateDiff(ms){
	var DateDiff = {
		inSeconds: function(ms) {	return parseFloat((ms)/1000).toFixed(2);					},
		inMinutes: function(ms) {	return parseFloat((ms)/60000).toFixed(2);					},
		inHours:   function(ms)	{	return parseFloat((ms)/3600000).toFixed(2);					},
		inDays:    function(ms)	{	return parseFloat((ms)/(24*3600*1000)).toFixed(2);			},
		inWeeks:   function(ms)	{	return parseFloat((ms)/(24*3600*1000*7)).toFixed(2);		},
		inYears:   function(ms)	{	return parseFloat((ms)/(24*3600*1000*365.25)).toFixed(2);	},
	};
    
    var timeLaps = DateDiff.inSeconds(ms);
    var dateOutput = '';

    if (timeLaps<60) {
      dateOutput = timeLaps+'s';
    } else {
      timeLaps = DateDiff.inMinutes(ms);
      if (timeLaps<60) {
        dateOutput = timeLaps+' mins';
      } else {
			timeLaps = DateDiff.inHours(ms);
			if (timeLaps<24){
				dateOutput = timeLaps+' hrs';
			} else {
				timeLaps = DateDiff.inDays(ms);
				if (timeLaps<7) {
					dateOutput = timeLaps+' days';
				} else {
					timeLaps = DateDiff.inWeeks(ms);
					if (timeLaps<52) {
						dateOutput = timeLaps+' week';
					} else {                   
						timeLaps = DateDiff.inYears(ms);
						dateOutput = timeLaps+' years';
					}
				}
			}
		}
    }
	return dateOutput;
}

$('#left-shift').change(function() {
    $('body').toggleClass('left-shifted',this.checked);
});
    
$("".bar-container"").on('wheel', function(a,b,c){
	if (event.altKey){
		var w = ($(""#scaler"").data(""width"") || 1)*(1 + Math.sign(event.deltaY)/10);
		$(""#scaler"").data(""width"", w);
		$(""#scaler"").css(""width"", w*100+""%"");
		$("".bar-container"").css(""overflow-x"",""auto"");
	}
});

//Show vertical line and timestamp
$('body').mousemove(function(e){
	var time = start + e.pageX/(document.body.clientWidth/2) * scale;
	var date = new Date(time);


	$('.mousebar').css('width',e.pageX-1).find('span').html(date.toLocaleString()+' (local)');	

	if (dragging){
		$('.dragbar').css('left',Math.min(e.pageX,dragStartX));	
		$('.dragbar').css('width',Math.abs(e.pageX-dragStartX)-1);

		var sy = Math.round(Math.min(e.clientY,dragStartY)/16)*16;
		var ey = Math.round(Math.max(e.clientY,dragStartY)/16)*16;

		var selectedCount = (ey-sy)/16;

		$('.dragbarv').css('top',sy-2);
		$('.dragbarv').css('height',ey-sy-2);

		$('.dragdiag.current').css('top',sy-2);
		$('.dragdiag.current').css('height',ey-sy-2);
		$('.dragdiag.current').css('left',Math.min(e.pageX,dragStartX));
		$('.dragdiag.current').css('width',Math.abs(e.pageX-dragStartX)-1);

		var secs = Math.abs(Math.floor(dragStartTime-time)/1000);
		if (secs!=0 && selectedCount!=0){
			var ips = (Math.round(selectedCount / secs*1000)/1000)+'&nbsp;r/s';
			if (selectedCount / secs < 0.0001){
				ips = (Math.round(secs/selectedCount*1000)/1000)+'&nbsp;s/r';
				if (secs> 8640){
					ips = (Math.round(secs/86400/selectedCount*1000)/1000)+'&nbsp;d/r';
				}

			}

			var rot = Math.atan2(-e.clientY+dragStartY,e.pageX-dragStartX);
			var flip = !(rot >Math.PI/2 || (rot < 0 && rot > -Math.PI/2)); 
			$('.dragdiag.current').toggleClass('flip',flip);
			rot = -1*rot;
			if (Math.abs(rot)>Math.PI/2){
				rot += Math.PI;
			}

			$('.dragdiag.current p').html(ips).css('transform','rotate('+rot+'rad)');
		}

		


		$('.dragbar span').html(prettyDateDiff(secs*1000)+'<br/>'+selectedCount);
		
		var now = new Date();
		var t = now.getTime();
		var offset = now.getTimezoneOffset();
		offset = offset * 60000;
		

		$('.status-text').html(logFileVarName+'.FilterRange(new TimeRange('+Math.floor(Math.min(time,dragStartTime)- offset)+'.FromJsMs(),'+Math.floor(Math.max(time,dragStartTime)- offset)+'.FromJsMs(),DateTimeKind.Local));');
	}
});

//Flag right clicked row
$('.row').contextmenu(function(e){

		$(this).toggleClass('click-flag');
		if ($(this).hasClass('click-flag')){
			var status = $(this).data('guid');
			$('.status-text').html(logFileVarName+'.Flag(x=>x.Guid==""'+status+'"",FlagType.ByGuid);');
		}

		e.preventDefault();		
});

$('[name=\'chart\']').change(function(){
	$('.chart').addClass('hidden');
	$('[name=\'chart\']:checked').each(function(x){
		var chart = $(this).attr('value');
		$('.chart.chart-'+chart).removeClass('hidden');
	});
});

$('.status').click(function(){
	var text = $('.status-text').text();
	if (text.trim()!=''){
		copyToClipboard(text);
		$(this).flash();
	}
});
$('.copyable').click(function(){
	var text = $(this).text();
	if (text.trim()!=''){
		copyToClipboard(text);
	}
});

//Clear status
$(document).keyup(function(e){
	if (e.keyCode == 27) {
		$('.dragbar').hide();
		$('.dragbarv').hide();
		$('.dragdiag').remove();
		$('.status-text').html('&nbsp;');
		$('.chart').addClass('hidden');
		rescale();
		$('[name=""chart""]:checked').attr('checked',false);
		$('[name=""chart""]:checked').prop('checked',false);
    }

    var keyMaps = {
		48 : 9,
        81 : 10, 
        87 : 11, 
        69 : 12, 
        82 : 13, 
        84 : 14, 
        89 : 15, 
        85 : 16, 
        73 : 17, 
        79 : 18, 
        80 : 19, 
        219 :20, 
        221 :21, 
        220 : 22
    };       


	var anyFocus = $(':focus').length>0;
	if (!anyFocus){
		//show charts;
		if(e.keyCode >=49 && e.keyCode<=57 || e.keyCode >=97 && e.keyCode<=105 || e.keyCode in keyMaps){
			var id = e.keyCode-49;
			if(e.keyCode >=97 && e.keyCode<=105)
				id = e.keyCode-97;
			if (e.keyCode in keyMaps)
				id = keyMaps[e.keyCode];
			var selected = $('[name=\'chart\'][data-chart-num='+id+']');
			var shouldSelect = selected.is(':checked');
			selected.attr('checked',!shouldSelect);
			selected.prop('checked',!shouldSelect);
			selected.trigger('change');
		}

		if (e.keyCode == 192){
			//accent grave
			var allCharts = $('[name=\'chart\']').not(':checked');

			var setTo = true;
			if (allCharts.length==0){
				allCharts =$('[name=\'chart\']');
				allCharts.attr('checked',false);
				allCharts.prop('checked',false);
				allCharts.trigger('change');
				$('.chart').addClass('hidden');
			}else{
				allCharts.attr('checked',true);
				allCharts.prop('checked',true);
				allCharts.trigger('change');
				$('.chart').removeClass('hidden');
			}
		}
	}

	if(e.keyCode==16){
		//shift
		$('body').removeClass('full-res');		
		$('.dragdiag').hide();
	}
});
$(document).keydown(function(e){
	if(e.keyCode==16){
		$('body').addClass('full-res');
		$('.dragdiag').show();
	}
});


//Select a range
var dragStartX = 0;
var dragging = false;
var dragStartTime= 0;
$('.bar-container').mousedown(function(e) {
	if (event.which==1){
		$('body').addClass('noselect');
		dragStartTime = start + e.pageX/(document.body.clientWidth/2) * scale;
		dragStartX = e.pageX;
		dragStartY = e.clientY;
		$('.dragbar').show();
		$('.dragbarv').show();
		$('.dragdiag.current').removeClass('current');
		$('body').append('<div class=""dragdiag hidden fixedFont current""><p>-</p></div>');		
		$('.dragdiag.current').show();
		dragging = true;

		$('.row-hover').addClass('row-hover-off').removeClass('row-hover');
	}
});
$(document).mouseup(function(e) {
	if (event.which==1){
		$('body').removeClass('noselect');
		dragging = false;
		$('.dragdiag').hide();
		$('.dragdiag.current').removeClass('current');
		$('.row-hover-off').addClass('row-hover').removeClass('row-hover-off');
		//$('.dragbar').hide();
	}
});

//Copy to clipboard
function copyToClipboard(str){
  const el = document.createElement('textarea');
  el.value = str;
  el.setAttribute('readonly', '');
  el.style.position = 'absolute';
  el.style.left = '-9999px';
  document.body.appendChild(el);
  el.select();
  document.execCommand('copy');
  document.body.removeChild(el);
};

</script>");
    }
  }
}

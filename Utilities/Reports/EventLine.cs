using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Reports {
  public class EventErrorLine {
    public DateTime Date { get; set; }
    public string Env { get; set; }
    public string Instance { get; set; }
    public string Severity { get; set; }
    public string Error { get; set; }
    public string ErrorDetails { get; set; }
  }


  public class EventLine : ILogLineIO {
    public string Guid { get; set; }
    public string date { get; private set; }
    public string logName { get; private set; }
    public string severity { get; private set; }
    public string application { get; private set; }
    public string applicationCode { get; private set; }
    public string machine { get; private set; }
    public string message { get; private set; }




    public FlagType Flag { get; set; }
    public int GroupNumber { get; set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get { return StartTime; } }
    public string Title { get; set; }
    public string Description { get; set; }

    public EventErrorLine Error { get; private set; }

    private static string[] Break(string line) {
      var depth = 0;
      var result = new List<string>();
      var build = "";
      var building = false;
      foreach (var c in line) {
        if (c == '[') {
          depth += 1;
          if (depth == 1) {
            building = true;
            build = "";
            continue;
          }
        }
        if (c == ']') {
          depth -= 1;
          if (depth == 0) {
            building = false;
            result.Add(build);
            continue;
          }
        }
        build += c;
      }

      if (building == true) {
        result.Add(build);
      }
      return result.ToArray();
    }

    private static EventErrorLine GetError(string message) {
      //2019-04-22 16:43:32,408 [67] [awsenv_env4] [i-0f2bd31e6e2730825] ERROR NHibernate.Util.ADOExceptionReporter [(null)] - Fatal error encountered during command execution.
      if (message != null) {
        try {
          DateTime date;
          if (DateTime.TryParseExact(message.Substring(0, 23), "yyyy-MM-dd HH:mm:ss,fff", new CultureInfo("en-US"), DateTimeStyles.AssumeUniversal, out date)) {
            var remainder = message.Substring(24);
            var firstP = remainder.IndexOf('[');
            var _1 = remainder.Substring(firstP+1, remainder.IndexOf(']', firstP) - firstP-1);
            var secondP = remainder.IndexOf('[', firstP+1);
            var env = remainder.Substring(secondP+1, remainder.IndexOf(']', secondP) - secondP-1);
            var thirdP = remainder.IndexOf('[', secondP+1);
            var thirdPEnd = remainder.IndexOf(']', thirdP+1);
            var instance = remainder.Substring(thirdP+1, thirdPEnd - thirdP-1);
            var fourthP = remainder.IndexOf('[', thirdP+1);
            var fourthPEnd = remainder.IndexOf(']', fourthP+1);
            var _2 = remainder.Substring(fourthP+1, fourthPEnd - fourthP-1);
            var severityAndError = remainder.Substring(thirdPEnd+1, fourthP - thirdPEnd-1);
            var severity = severityAndError.TrimStart().Split(' ')[0];
            var error = string.Join(" ", severityAndError.TrimStart().Split(' ').Skip(1));
            var errorMessage = remainder.Substring(fourthPEnd+1).Substring(3);

            return new EventErrorLine() {
              Date = date,
              Env = env,
              Instance = instance,
              Severity = severity,
              Error = error,
              ErrorDetails = errorMessage,
            };


          }
        } catch (Exception e) {

        }
      }
      return null;
    }



    public ILogLine ConstructFromLine(string line) {
      try {
        var date = line.Substring(0, 23);
        var subline = line.Substring(25);

        var parts = Break(subline);

        EventLine ll = null;
        switch (parts.Length) {
          case 5:
            ll = Parse5Line(parts);
            break;
          case 6:
            ll = Parse6Line(parts);
            break;
          default:
            return null;
        }

        ll.Guid = Sha256.Hash(line);
        ll.StartTime = DateTime.SpecifyKind(DateTime.ParseExact(date, "yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture), DateTimeKind.Utc);

        return ll;
      } catch (Exception) {
        return null;
      }
    }

    private static EventLine Parse5Line(string[] parts) {

      var submessage = Break(parts[4]);

      return new EventLine {
        logName = parts[0],
        severity = parts[1],
        applicationCode = parts[2],
        machine = parts[3],
        message = parts[4],
        Error = GetError(parts[4])
      };
    }
    private static EventLine Parse6Line(string[] parts) {
      return new EventLine {
        logName = parts[0],
        severity = parts[1],
        applicationCode = parts[2],
        application = parts[3],
        machine = parts[4],
        message = parts[5],
        Error = GetError(parts[5])
      };
    }

    public string[] GetHeaders(bool additionalData) {
      var items = new List<string>{
          "logName",
          "severity",
          "applicationCode" ,
          "application",
          "machine",
          "message",
      };
      return items.ToArray();
    }

    public bool AnyFieldContains(string lookup) {
      lookup = lookup.ToLower();
      foreach (var f in GetLine(DateTime.MinValue, false)) {
        if (f.ToLower().Contains(lookup)) {
          return true;
        }
      }
      return false;
    }

    public string[] GetLine(DateTime startRange, bool additionalData) {
      var items = new List<string>{
          logName ,
          severity,
          applicationCode ,
          application,
          machine,
          message,
      };
      return new string[] { StartTime.ToString("yyyy-MM-ddTHH:mm:ss.fff") + "Z" }.Concat(items.Select(x => "[" + x + "]")).ToArray();
      //return string.Join(" ", items);
    }

    public IEnumerable<string> GetLines(string file) {
      var depth = 0;
      var result = new List<string>();
      var build = "";
      foreach (var c in file) {
        if (c == '[') {
          depth += 1;
        }
        if (c == ']') {
          depth -= 1;
        }
        if (c == '\n' && depth == 0) {
          yield return build;
          build = "";
          continue;
        }
        if (c != '\r') {
          build += c;
        }
      }
      yield return build;
      yield break;

    }
  }
}
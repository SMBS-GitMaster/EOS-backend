using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Core.Utilities.Reports {
  public class LogLine : ILogLineIO, ILogLine {

    public string Guid { get; set; }
    public string date { get; private set; }
    public string time { get; private set; }
    public string sIp { get; private set; }
    public string csMethod { get; private set; }
    public string csUriStem { get; private set; }
    public string csUriQuery { get; private set; }
    public string sPort { get; private set; }
    public string csUsername { get; private set; }
    public string cIp { get; private set; }
    public string csUserAgent { get; private set; }
    public string csReferer { get; private set; }
    public string scStatus { get; private set; }
    public string scSubstatus { get; private set; }
    public string scWin32Status { get; private set; }
    public string timeTaken { get; private set; }


    public FlagType Flag { get; set; }
    public int GroupNumber { get; set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public TimeSpan Duration { get; private set; }
    public string InstanceName { get; set; }
    public string Description { get; set; }

    public Dictionary<string, string> UriQuery {
      get {
        var dictionary = new Dictionary<string, string>();
        var a = HttpUtility.ParseQueryString(csUriQuery ?? "");
        foreach (var k in a.AllKeys) {
          if (k != null) {
            try {
              dictionary[k] = a[k];
            } catch (Exception) {
              dictionary[k] = "err";
            }
          }
        }
        return dictionary;
      }
    }


    public int? StatusCode {
      get {
        int status = 0;
        return int.TryParse(scStatus, out status) ? (int?)status : null;
      }
    }

    public string Title { get { return csUriStem; } }

    public LogLine ConstructFromInsightLine(string timestamp, string line) {
      try {
        var ll = (LogLine)ConstructFromLine(line);
        return ll;
      } catch (Exception) {
        return null;
      }
    }

    public void OverrideStartTime(DateTime startTime) {
      StartTime = startTime;
    }

    public void OverrideEndTime(DateTime endTime) {
      EndTime = endTime;
    }

    public ILogLine ConstructFromLine(string line) {
      try {
        var parts = line.Trim().Split(' ');

        LogLine ll = null;
        switch (parts.Length) {
          case 13:
            ll = ParseInsightLine(parts);
            break;
          case 14:
            ll = ParseCloudwatchLine(parts);
            break;
          case 15:
            ll = ParseLocalLine(parts);
            break;
          case 16:
            ll = ParseLineIIS10(parts);
            break;
          default:
            return null;
        }

        ll.EndTime = new DateTime(DateTime.Parse(ll.date + " " + ll.time).Ticks, DateTimeKind.Utc);
        ll.StartTime = ll.EndTime.AddMilliseconds(-int.Parse(ll.timeTaken));
        ll.Duration = ll.EndTime - ll.StartTime;
        ll.Guid = Sha256.Hash(line);

        return ll;
      } catch (Exception) {
        return null;
      }
    }

    public static LogLine ParseLine(string line) {
      return (LogLine)new LogLine().ConstructFromLine(line);
    }

    //		2019-10-24T14:01:11.868Z 2019-10-24 08:57:25 172.31.46.174 GET /signalr/ping _=1571868304762 80 chill@peoplespace.com 172.31.10.100 Mozilla/5.0+(Windows+NT+10.0;+Win64;+x64)+AppleWebKit/537.36+(KHTML,+like+Gecko)+Chrome/77.0.3865.120+Safari/537.36 https://traction.tools/L10/Meeting/8015 200 0 0 0
    //date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) cs(Referer) sc-status sc-substatus sc-win32-status time-taken
    private static LogLine ParseLineIIS10(string[] parts) {
      return new LogLine {
        date = parts[1],
        time = parts[2],
        sIp = parts[3],
        csMethod = parts[4],
        csUriStem = parts[5],
        csUriQuery = parts[6],
        sPort = parts[7],
        csUsername = parts[8],
        cIp = parts[9],
        csUserAgent = parts[10],
        csReferer = parts[11],
        scStatus = parts[12],
        scSubstatus = parts[13],
        scWin32Status = parts[14],
        timeTaken = parts[15],
      };
    }



    private static LogLine ParseLocalLine(string[] parts) {
      return new LogLine {
        date = parts[0],
        time = parts[1],
        sIp = parts[2],
        csMethod = parts[3],
        csUriStem = parts[4],
        csUriQuery = parts[5],
        sPort = parts[6],
        csUsername = parts[7],
        cIp = parts[8],
        csUserAgent = parts[9],
        csReferer = parts[10],
        scStatus = parts[11],
        scSubstatus = parts[12],
        scWin32Status = parts[13],
        timeTaken = parts[14],
      };
    }

    private static LogLine ParseCloudwatchLine(string[] parts) {
      var dateTime = parts[0].Split('T');

      return new LogLine {
        date = dateTime[0],
        time = dateTime[1],
        sIp = parts[1],
        csMethod = parts[2],
        csUriStem = parts[3],
        csUriQuery = parts[4],
        sPort = parts[5],
        csUsername = parts[6],
        cIp = parts[7],
        csUserAgent = parts[8],
        csReferer = parts[9],
        scStatus = parts[10],
        scSubstatus = parts[11],
        scWin32Status = parts[12],
        timeTaken = parts[13],
      };
    }

    private static LogLine ParseInsightLine(string[] parts) {

      return new LogLine {
        sIp = parts[0],
        csMethod = parts[1],
        csUriStem = parts[2],
        csUriQuery = parts[3],
        sPort = parts[4],
        csUsername = parts[5],
        cIp = parts[6],
        csUserAgent = parts[7],
        csReferer = parts[8],
        scStatus = parts[9],
        scSubstatus = parts[10],
        scWin32Status = parts[11],
        timeTaken = parts[12],
      };
    }
    public string[] GetHeaders(bool additionalData) {
      var items = new List<string>{
          "date"          ,
          "time"          ,
          "sIp"           ,
          "csMethod"      ,
          "csUriStem"     ,
          "csUriQuery"    ,
          "sPort"            ,
          "csUsername"    ,
          "cIp"           ,
          "csUserAgent"   ,
          "csReferer"     ,
          "scStatus"      ,
          "scSubstatus"   ,
          "scWin32Status" ,
          "timeTaken"     ,
      };
      if (additionalData) {
        items.AddRange(new[]{
          "StartDate","StartTime",
          "EndDate","EndTime",
          "RelativeStart",
          "Duration",
          "LocalStartDate","LocalStartTime",
        });
      }
      return items.ToArray();
      //return string.Join(" ", items);

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
          date          ,
          time          ,
          sIp           ,
          csMethod      ,
          csUriStem     ,
          csUriQuery    ,
          sPort         ,
          csUsername    ,
          cIp           ,
          csUserAgent   ,
          csReferer     ,
          scStatus      ,
          scSubstatus   ,
          scWin32Status ,
          timeTaken     ,
      };
      if (additionalData) {
        var additional = new List<string>(){
          StartTime.ToString("MM-dd-yyyy HH:mm:ss.FFFF"),
          EndTime.ToString("MM-dd-yyyy HH:mm:ss.FFFF"),
          (StartTime-startRange).ToString("hh':'mm':'ss'.'fff"),
          (Duration).ToString("hh':'mm':'ss'.'fff"),
          (StartTime.ToLocalTime()).ToString("MM-dd-yyyy HH:mm:ss.FFFF"),
        };
        items.AddRange(additional);
      }
      return items.ToArray();
      //return string.Join(" ", items);
    }

    public IEnumerable<string> GetLines(string file) {
      return file.Split('\n');
    }
  }


  public class Sha256 {
    public static string Hash(string str) {
      var crypt = new System.Security.Cryptography.SHA256Managed();
      var hash = new StringBuilder();
      byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(str));
      foreach (byte theByte in crypto) {
        hash.Append(theByte.ToString("x2"));
      }
      return hash.ToString();
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Reports {

  public class TimeSlice {

    public TimeSlice(DateTime time, DateTimeKind kind, string name, string description = null, string color = null)
      : this(ReportDateRange.Around(0, time, kind), name, description, color) { }


    public TimeSlice(ReportDateRange time, string name, string description = null, string color = null) {
      Name = name;
      Description = description;
      Range = time;
      GUID = "" + Guid.NewGuid();
      Color = color??"blue";
    }

    public ReportDateRange Range { get; set; }
    public String Name { get; set; }
    public String Description { get; set; }
    public String Color { get; set; }
    public string GUID { get; set; }
  }
}

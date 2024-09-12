using RadialReview.Utilities.DataTypes;
using System;
using TimeZoneConverter;
using TimeZoneNames;

namespace RadialReview.Models {
  public partial class OrganizationModel {
    [Obsolete("Use the user if possible.")]
    public virtual ITimeData GetTimeSettings() {
      return Settings.GetTimeSettings();
    }
    /// <summary>
    /// In minutes
    /// </summary>
    /// <returns></returns>
    public virtual int GetTimezoneOffset() {
      return Settings.GetTimezoneOffset();
    }

    public virtual DateTime ConvertFromUTC(DateTime utcTime) {
      var zone = Settings.TimeZoneId ?? "Central Standard Time";
      var tz = TZConvert.GetTimeZoneInfo(zone);
      return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
    }

    public virtual string GetTimeZoneId(DateTime? time = null) {

      time = time ?? DateTime.UtcNow;
      var id = Settings.TimeZoneId ?? "Central Standard Time";
      var tz = TZConvert.GetTimeZoneInfo(id);
      var abb = TZNames.GetAbbreviationsForTimeZone(id, "en-us");
      if (tz.IsDaylightSavingTime(time.Value)) {
        return abb.Daylight;
      }
      return abb.Standard;


    }

    public virtual DateTime ConvertToUTC(DateTime localTime) {
      var zone = Settings.TimeZoneId ?? "Central Standard Time";
      var tz = TZConvert.GetTimeZoneInfo(zone);
      return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }

    public virtual TimeSpan ConvertToUTC(TimeSpan localTimeSpan) {
      var zone = Settings.TimeZoneId ?? "Central Standard Time";
      var now = DateTime.UtcNow;
      return localTimeSpan - TZConvert.GetTimeZoneInfo(zone).GetUtcOffset(now);
    }
    public virtual TimeSpan ConvertFromUTC(TimeSpan localTimeSpan) {
      var zone = Settings.TimeZoneId ?? "Central Standard Time";
      var now = DateTime.UtcNow;
      return localTimeSpan + TZConvert.GetTimeZoneInfo(zone).GetUtcOffset(now);
    }
  }
}

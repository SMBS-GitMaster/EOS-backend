using System;

namespace RadialReview.Utilities.Types
{
  public class Timestamp
  {
    public static long ToUnixTimeStamp(DateTime date)
    {
      long timestamp = new DateTimeOffset(date).ToUnixTimeMilliseconds();
      return timestamp;
    }

    public static DateTime ToDateTime(long unixTimeSeconds)
    {
      var time = DateTime.UnixEpoch;
      var date = time.AddMilliseconds(unixTimeSeconds);
      return date;
    }
  }
}

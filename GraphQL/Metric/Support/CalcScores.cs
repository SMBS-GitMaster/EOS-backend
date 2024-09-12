using System;
using System.Collections.Generic;

namespace RadialReview.Repositories
{
  public class CalcScores
  {
    public class TinyScore
    {
      public DateTime ForWeek { get; set; }
      public decimal? Measured { get; set; }

    }
    public long MeasurableId { get; set; }
    public bool HasCumulative { get; set; }
    public bool HasProgressive { get; set; }
    public bool HasAverage { get; set; }
    public List<TinyScore> Scores { get; set; }
  }
}

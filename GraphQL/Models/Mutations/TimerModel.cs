namespace RadialReview.GraphQL.Models
{
  public class TimerModel
  {

    #region Properties

    public double TimeLastStarted { get; set; }

    public double? TimePreviouslySpentS { get; set; }

    public double? TimeLastPaused { get; set; }

    public double TimeSpentPausedS { get; set; }

    #endregion

  }
}

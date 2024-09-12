namespace RadialReview.GraphQL.Models
{
  public class NodeCompletionArgumentsQueryModel
  {

    #region Fields

    public long? RecurrenceId { get; set; }

    public double? StartDate { get; set; }

    public double EndDate { get; set; }

    public string GroupBy { get; set; }

    #endregion

  }
}

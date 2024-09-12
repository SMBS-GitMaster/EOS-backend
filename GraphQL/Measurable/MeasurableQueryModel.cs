namespace RadialReview.GraphQL.Models
{
  public class MeasurableQueryModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public string Title { get; set; }
    public UserQueryModel Assignee { get; set; }
    public UserQueryModel Owner { get; set; }

    public bool HasV3Config { get; set; }

    #endregion

    #region Subscription Data

    public static class Associations
    {
      public enum User
      {
        Assignee,
        Owner
      }
    }

    public static class Collections
    {
      public enum MetricScore
      {
        Scores
      }
    }

  }

  #endregion

}
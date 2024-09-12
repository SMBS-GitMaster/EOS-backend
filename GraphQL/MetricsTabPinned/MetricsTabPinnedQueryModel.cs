namespace RadialReview.GraphQL.Models
{
  using System;

  public class MetricsTabPinnedQueryModel
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual int Version { get; set; }

    public virtual DateTime CreatedTimestamp { get; set; }

    public virtual DateTime DateLastModified { get; set; }

    public virtual long UserId { get; set; }

    public virtual bool IsPinnedToTabBar { get; set; }

    public virtual long MetricsTabId { get; set; }

    #endregion

  }
}

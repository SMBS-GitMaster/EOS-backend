using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Issues
{
  public class MergedIssueData
  {
    public List<long> FromMergedIds { get; set; }

    public class Builder
    {
      private List<long> _FromMergedIssueIds;

      public Builder SetFromMergedIssueIds(List<long> issueIds)
      {
        _FromMergedIssueIds = issueIds;
        return this;
      }

      public MergedIssueData Build()
      {
        if (IsAllPropertiesNullOrEmpty())
        {
          return null;
        }

        var mergedIssue = new MergedIssueData
        {
          FromMergedIds = _FromMergedIssueIds,
        };

        return mergedIssue;
      }

      public bool IsAllPropertiesNullOrEmpty()
      {
        if (_FromMergedIssueIds is not null && _FromMergedIssueIds.Any())
        {
          return false;
        }

        return true;
      }
    }
  }
}

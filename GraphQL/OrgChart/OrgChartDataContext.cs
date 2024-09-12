namespace RadialReview.Repositories;

using System.Linq;
using System.Collections.Generic;
using System.Threading;
using RadialReview.GraphQL.Models;

public partial interface IDataContext
{
  IQueryable<(long UserId, OrgChartQueryModel OrgChart)> GetOrgChartsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);
}

public partial class DataContext
{
  public IQueryable<(long UserId, OrgChartQueryModel OrgChart)> GetOrgChartsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
  {
    return null;
  }
}
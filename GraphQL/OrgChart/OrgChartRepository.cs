namespace RadialReview.Repositories;

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using RadialReview.GraphQL.Models;
using RadialReview.Utilities;
using RadialReview.Core.Utilities.Extensions;
using RadialReview.Core.GraphQL;
using RadialReview.Models.Accountability;

public partial interface IRadialReviewRepository
{
  IQueryable<(long UserId, OrgChartQueryModel OrgChart)> GetOrgChartsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken, global::NHibernate.ISession session);
  OrgChartQueryModel GetOrgChartById (CancellationToken cancellationToken, long orgChartId);
}

public partial class RadialReviewRepository
{
  public IQueryable<(long UserId, OrgChartQueryModel OrgChart)> GetOrgChartsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken, global::NHibernate.ISession session)
  {
    // using var session = HibernateSession.GetCurrentSession();
    // using var tx = session.BeginTransaction();
    // dbContext.Database.SetDbConnection(session.Connection);

    IQueryable<(long UserId, OrgChartQueryModel OrgChart)>
      query =
        from uom in dbContext.UserOrganizationModels
        from orgChart in uom.ResponsibilityGroup.Organization.AccountabilityCharts
        where userIds.Contains(uom.ResponsibilityGroupModelId)
        select ValueTuple.Create(uom.ResponsibilityGroupModelId, new OrgChartQueryModel {
          Id = orgChart.Id,
          Name = orgChart.Name,
          CreateTime = orgChart.CreateTime.Value,
          DeleteTime = orgChart.DeleteTime,
        })
        ;

    var results =
        query
        .GroupedRedact(
          x => x.UserId,
          (userId, group) => {
            var userPerm = session.PermissionsForUser(userId);
            return group.Where(y => userPerm.PermissionsForOrgChart(y.OrgChart.Id).CanViewHierarchy);
          }
        )
        ;


    // tx.Commit();
    return results;
  }

  public OrgChartQueryModel GetOrgChartById(CancellationToken cancellationToken, long orgChartId)
  {
    try
    {
      var session = HibernateSession.GetCurrentSession();
      var perms = PermissionsUtility.Create(session, caller);
      var c = session.Query<AccountabilityChart>().FirstOrDefault(x => x.Id == orgChartId && x.DeleteTime == null);
      perms.ViewOrganization(c.OrganizationId);

      return c.Transform();
    } catch(Exception ex)
    {
      throw new Exception(ex.Message);
    }

  }
}
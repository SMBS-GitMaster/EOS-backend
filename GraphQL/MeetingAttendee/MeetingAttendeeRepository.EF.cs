using RadialReview.GraphQL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using PermItem = RadialReview.Models.PermItem;
using Convert = RadialReview.Core.Utilities.Extensions.RadialReviewDbContextExtensions;
using Microsoft.EntityFrameworkCore;
using RadialReview.Core.Utilities.Extensions;
using System.Threading;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    IEnumerable<(long recurrenceId, long uomId, MeetingPermissionsModel perm)> GetPermissionsForAttendeesOnMeetingAsync(IEnumerable<(long recurrenceId, long uomId)> keys, global::NHibernate.ISession session, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IEnumerable<(long recurrenceId, long uomId, MeetingPermissionsModel perm)> GetPermissionsForAttendeesOnMeetingAsync(IEnumerable<(long recurrenceId, long uomId)> keys, global::NHibernate.ISession session, CancellationToken cancellationToken)
    {

      var result =
        keys
        .GroupBy(x => x.uomId)
        .SelectMany(group => {
          var uomId = group.Key;
          var userperm = session.PermissionsForUser(uomId);

          return group.Select(x =>
            (
              recurrenceId: x.recurrenceId,
              uomId: x.uomId,
              perm: userperm.ResourcePermissions(
                PermItem.ResourceType.L10Recurrence,
                x.recurrenceId
              )
            )
          );
        })
        ;

      return result;
    }

    #endregion

    #region Mutations

    #endregion

  }
}

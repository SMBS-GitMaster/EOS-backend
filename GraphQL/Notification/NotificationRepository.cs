using RadialReview.Accessors;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<NotificationQueryModel> Notifications { get; }

    IQueryable<NotificationQueryModel> GetNotificationsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<NotificationQueryModel> Notifications
    {
      get
      {
        return DelayedQueryable.CreateFrom(null, () =>
        {
          Models.OrganizationModel orgAlias = null;
          var callerGuid = caller.User.Id;//We do want the guid here.. searching over all userorgs this users is attached to

          using (var s = HibernateSession.GetCurrentSession())
          {
            using (var tx = s.BeginTransaction())
            {

              //u.User.Id == callerGuid is the permission check..
              var availableCallerIds = s.QueryOver<Models.UserOrganizationModel>()
                              .JoinAlias(x => x.Organization, () => orgAlias)
                              .Where(u => u.User.Id == callerGuid && u.DeleteTime == null && orgAlias.DeleteTime == null)
                              .Select(x => x.Id)
                              .List<long>().ToList();

              var notifications = s.QueryOver<Models.Notifications.NotificationModel>()
                .Where(x => x.DeleteTime == null && x.Seen == null)
                .WhereRestrictionOn(x => x.UserId)
                .IsIn(availableCallerIds)
                .List().ToList();

              return notifications.Select(n => RepositoryTransformers.TransformNotification(n)).ToList();


            }
          }
        });
      }
    }

    /// <summary>
    /// !! This needs to be made threadsafe
    /// </summary>
    /// <param name="userIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public IQueryable<NotificationQueryModel> GetNotificationsForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom<NotificationQueryModel>(cancellationToken, () =>
      {
        //!!throw new Exception("need to find a way to make this awaitable. method.Result is not threadsafe");
        var results = new List<NotificationQueryModel>();

        foreach (var userId in userIds)
        {
          var notifications = (NotificationAccessor.GetNotificationsForUser(caller, userId).Result)
              .Select(notification => RepositoryTransformers.TransformNotification(notification));
          results.AddRange(notifications);
        }

        //throw new Exception("This method needs to check for permissions");
        //throw new Exception("Never ever rely on user supplied userIds");
        //var result =
        //  userIds.SelectMany<long,NotificationModel>(async userId => {
        //    //throw new Exception("ForceGetUser should basically never be used. Only when you know you are working with an authenticated cookie.");
        //    //var user = ForceGetUser(session, userId); not needed. using caller instead
        //    // How do we know the right userId to use?
        //    // Reference points have a GetUser().Id from the IdentityExtensions


        //    return notifications;
        //  });

        return results;
      });
    }

    #endregion

    #region Mutations

    #endregion

  }
}
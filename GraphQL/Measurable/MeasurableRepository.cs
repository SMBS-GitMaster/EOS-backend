using Amazon.DynamoDBv2;
using FluentNHibernate.Conventions;
using Humanizer;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Core.Models.L10;
using RadialReview.Core.Repositories;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Rocks;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MilestoneStatus = RadialReview.Models.Rocks.MilestoneStatus;
using ModelIssue = RadialReview.Models.Issues.IssueModel;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<MeasurableQueryModel> GetMeasurablesForUser(long userId, CancellationToken cancellationToken);

    IQueryable<MeasurableQueryModel> GetMeasurablesForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<MeasurableQueryModel> GetMeasurablesForUser(long userId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        //throw new Exception("Never rely on user supplied userIds");
        //throw new Exception("fix issues from obsolete tag");
        return GetMeasurablesForUsers(new[] { userId }, cancellationToken);
      });
    }

    public IQueryable<MeasurableQueryModel> GetMeasurablesForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        //throw new Exception("This method doesn't check permissions");
        //throw new Exception("EmailAtOrganization is unreliable");
        //throw new Exception("Should almost never need to get a user by their email address");
        //throw new Exception("fix issues from obsolete tag");
        //throw new Exception("the userGuid (userId) is not an email address");

        return userIds.SelectMany(userId => ScorecardAccessor.GetUserMeasurables(caller, userId))
            .Select(m => RepositoryTransformers.TransformMeasurable(m)).ToList();

        //var users = session.Query<RadialReview.Models.UserModel>();
        //var measurables = session.Query<RadialReview.Models.Scorecard.MeasurableModel>();
        //var query =
        //    measurables
        //      .Where(m => userIds.Contains(m.AccountableUser.EmailAtOrganization))
        //      .Select(m => new {
        //        Id = m.Id,
        //        Title = m.Title,
        //        AssigneeEmailAddress = m.AccountableUser.EmailAtOrganization,
        //        OwnerEmailAddress = m.AdminUser.EmailAtOrganization,
        //      })
        //      ;

        //var results =
        //    query
        //      .AsEnumerable()
        //      .Select(m => new MeasurableModel {
        //        Id = m.Id,
        //        Title = m.Title,
        //        Assignee = this.GetUserByEmail(m.AssigneeEmailAddress, cancellationToken),
        //        Owner = this.GetUserByEmail(m.OwnerEmailAddress, cancellationToken),
        //      })
        //      ;
        //return results;
        //return Enumerable.Empty<MeasurableModel>();
      });
    }

    #endregion

  }

}

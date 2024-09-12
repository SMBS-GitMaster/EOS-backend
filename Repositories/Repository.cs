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
  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Members
    [Obsolete("Should almost never be required")]
    protected readonly string callerGuid;
    protected readonly Models.UserOrganizationModel caller;

    private readonly INotesProvider _notesProvider;
    private readonly RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext;
    private readonly RedLockNet.IDistributedLockFactory redLockFactory;



    #endregion

    #region Constructors

    public RadialReviewRepository(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, Microsoft.AspNetCore.Http.IHttpContextAccessor accessor, INotesProvider notesProvider, RedLockNet.IDistributedLockFactory redLockFactory)
    {
      this.dbContext = dbContext;
      this.redLockFactory = redLockFactory;

      HibernateSession.InitializeSessionContextForRepository();

      _notesProvider = notesProvider;

      lock (accessor.HttpContext)
      {
        caller = RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems.GetUser(accessor.HttpContext);
      }

      if (caller._ClientTimestamp == null)
      {
        caller._ClientTimestamp = (long) (1000 * DateTime.Now.ToUnixTimeStamp());
      }

      callerGuid = caller.UserModelId;
    }

    public RadialReviewRepository(Models.UserOrganizationModel user, long clientTimestamp)
    {
      HibernateSession.InitializeSessionContextForRepository();

      _notesProvider = null;

      caller = user;
      caller._ClientTimestamp = clientTimestamp;
      callerGuid = caller.UserModelId;
    }

    #endregion

    #region (Generic) Public Methods

    public async Task<long> GetCallerId()
    {
      return caller.Id;
    }

    private static Func<object[], UserQueryModel> Unpackage = new Func<object[], UserQueryModel>(x =>
    {
      var fname = (string)x[0];
      var lname = (string)x[1];
      var email = (string)x[5];
      var uoId = (long)x[2];
      var imageGuid = (string)x[7];
      var createTime = (DateTime)x[8];
      var isUsingV3 = (bool?)x[10];
      //var timezoneOffset = (int)x[9];
      if (fname == null && lname == null)
      {
        fname = (string)x[3];
        lname = (string)x[4];
        email = (string)x[6];
        createTime = (DateTime)x[9];
      }

      var imageUrl = UserLookup.TransformImageSuffix(imageGuid.NotNull(x => "/" + x + ".png"), ImageSize._64);
      var fullName = ((fname ?? "").Trim() + " " + (lname ?? "").Trim()).Trim();

      return UserTransformer.TransformUser(uoId, imageUrl, fname, lname, fullName, email, createTime, isUsingV3);
    });

    #endregion

    #region Private Methods

    private void ErrorOnNonDefault<T, TProp>(T model, Expression<Func<T, TProp>> prop, TProp expected = default(TProp))
    {
      TProp found = prop.Compile()(model);
      if (!Object.Equals(found, expected))
      {
        MemberExpression member = prop.Body as MemberExpression;
        PropertyInfo propInfo = member.Member as PropertyInfo;
        throw new NotImplementedException("Property is not implemented yet:" + propInfo.Name + ". Please send:" + expected);
      }
    }

    #endregion

    #region Not finished

    public async Task<IdModel> SubmitFeedback(FeedbackSubmitModel feedbackSubmitModel)
    {
      throw new NotImplementedException();
    }

    public async Task<IncrementNumViewedNewFeaturesOutput> IncrementNumViewedNewFeatures(IncrementNumViewedNewFeaturesInput input, CancellationToken cancellationToken)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditUserDetails(input.UserId);

          var user = s.Get<RadialReview.Models.UserOrganizationModel>(input.UserId);
          user.NumViewedNewFeatures++;
          await s.UpdateAsync(user, cancellationToken);
          await tx.CommitAsync(cancellationToken);

          return new IncrementNumViewedNewFeaturesOutput { UserId = input.UserId, NumViewedNewFeatures = user.NumViewedNewFeatures };
        }
      }
    }
    public async Task<IdModel> UpdateVotingState(VotingUpdateStateModel votingUpdateStateModel)
    {
      throw new NotImplementedException();
    }


    public async Task<IdModel> StartStarVoting(StartStarVotingModel startStarVotingModel)
    {
      throw new NotImplementedException();
    }

    public async Task<IdModel> SubmitIssueStarVotes(SubmitIssueStarVotesModel submitIssueStarVotesModel)
    {
      throw new NotImplementedException();
    }

    #endregion

  }
}
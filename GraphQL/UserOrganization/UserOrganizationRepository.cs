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

    #endregion

    #region Mutations

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    #endregion

    #region Mutations

    #endregion

  }
}
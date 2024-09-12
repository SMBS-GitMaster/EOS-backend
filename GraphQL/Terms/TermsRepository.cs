using RadialReview.Accessors;
using RadialReview.Core.Accessors;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    TermsQueryModel GetTerms(CancellationToken cancellationToken);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public TermsQueryModel GetTerms( CancellationToken cancellationToken)
    {
      return RepositoryTransformers.TransformTerms(TermsAccessor.GetTermsCollection(caller, caller.Organization.Id));
    }

    #endregion
  }
}

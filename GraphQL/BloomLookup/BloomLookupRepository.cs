using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    BloomLookupModel GetBloomLookupNode(string id, object cancellationToken);

    Task<List<TimeZoneQueryModel>> GetTimeZoneLookup();

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    public BloomLookupModel GetBloomLookupNode(string id, object cancellationToken)
    {
      var result =
          new BloomLookupModel
          {
            Id = -1,
            ClassicCheckinTitle = "<Mock data>",
          };

      return result;
    }

    public async Task<List<TimeZoneQueryModel>> GetTimeZoneLookup()
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        return s.QueryOver<TimeZoneLookup>().List().Select(_ => BloomLookupTransformer.TransformTimeZone(_)).ToList();
      }
    }

  }

}

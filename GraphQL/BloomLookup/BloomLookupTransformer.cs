using RadialReview.GraphQL.Models;
using RadialReview.Models;

namespace RadialReview.Core.Repositories
{
  public static class BloomLookupTransformer
  {

    #region Public Methods

    public static TimeZoneQueryModel TransformTimeZone(TimeZoneLookup model)
    {
      return new TimeZoneQueryModel
      {
        CountryCode = model.CountryCode,
        IANA_Name = model.IANA_Name,
        Id = model.Id,
        DisplayName = model.IANA_Name.Replace("_", " ").Replace("-", " - ")
      };
    }

    #endregion

  }
}

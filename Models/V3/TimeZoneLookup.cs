using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models
{
  public class TimeZoneLookup : ILongIdentifiable
  {

    #region Properties

    public virtual long Id { get; set; }

    public virtual string CountryCode { get; set; }

    public virtual string IANA_Name { get; set; }

    #endregion

  }

  public class TimeZoneLookupMap : ClassMap<TimeZoneLookup>
  {

    #region Constructor

    public TimeZoneLookupMap()
    {
      Id(x => x.Id);
      Map(_ => _.CountryCode);
      Map(_ => _.IANA_Name);
    }

    #endregion

  }
}

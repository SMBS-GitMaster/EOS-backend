namespace RadialReview.GraphQL.Models
{
  public class TimeZoneQueryModel
  {

    #region Properties

    public long Id { get; set; }

    public string CountryCode { get; set; }

    public string IANA_Name { get; set; }

    public string DisplayName { get; set; }

    public int Version { get => 1; }

    public string Type { get => "timezone";  }

    public string LastUpdatedBy { get => string.Empty; }

    #endregion

  }
}

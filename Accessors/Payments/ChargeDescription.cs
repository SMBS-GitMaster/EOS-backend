using System;

namespace RadialReview.Accessors.Payments {


  public enum ChargeType {
    Default,
    ForceFree,
    ForceCharge //Overrides all others.
  }

  public enum ChargeProduct {
    WeeklyMeeting,
    PeopleTools,
  }

  public class ChargeDescription {

    public static string WEEKLY_MEETING = "Weekly Meeting";
    public static string WEEKLY_MEETING_INCLUDED_SEAT = "Weekly Meeting (Included)";
    public static string WEEKLY_MEETING_ALREADY_PAID = "Weekly Meeting (Already Paid)";
    public static string PEOPLE_TOOLS = "People Tools™";
    public static string PEOPLE_TOOLS_INCLUDED_SEAT = "People Tools™ (Included)";


    public ChargeDescription(string description, ChargeProduct product, ChargeType tagType) {
      Description=description;
      Type=tagType;
      Product = product;
    }

    public string Description { get; set; }
    public ChargeType Type { get; set; }
    public ChargeProduct Product { get; set; }


    private object EqualityObj() {
      return Tuple.Create(Description, Product, Type);
    }

    public override int GetHashCode() {
      return EqualityObj().GetHashCode();
    }

    public override bool Equals(object obj) {
      var other = obj as ChargeDescription;
      if (other == null)
        return false;
      return EqualityObj().Equals(other.EqualityObj());
    }
  }


}

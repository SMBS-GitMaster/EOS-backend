using System;
using System.Collections.Generic;

namespace RadialReview.GraphQL.Models {
  public class MeetingPermissionsModel
  {

    #region Properties

    public bool View { get; set; }

    public bool Edit { get; set; }

    public bool Admin { get; set; }

    #endregion

    public static MeetingPermissionsModel CreateDefault()
    {
      return new MeetingPermissionsModel()
      {
        View = true,
        Edit = false,
        Admin = false,
      };
    }

    public override bool Equals(object obj)
    {
      if (obj is MeetingPermissionsModel other)
      {
        return Admin == other.Admin && View == other.View && Edit == other.Edit;
      }

      return false;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Admin, View, Edit);
    }

  }
}
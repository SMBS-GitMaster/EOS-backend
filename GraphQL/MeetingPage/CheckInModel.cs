using System;

namespace RadialReview.GraphQL.Models
{
  public class CheckInModel
  {

    #region Properties

    public string IceBreaker { get; set; }

    public bool? IsAttendanceVisible { get; set; }

    public string CheckInType { get; set; }

    #endregion
  }
}
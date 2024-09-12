using NHibernate;
using RadialReview.Models;
using RadialReview.Core.Models.Scorecard;
using System.Threading.Tasks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces {

  public class PageCheckInUpdates {
    public string CheckInType { get; set; }
    public string IceBreaker { get; set; }
    public bool? IsAttendanceVisible { get; set; }
  }
  internal interface ICheckInHook : IHook {

    Task UpdateCheckIn(ISession s, UserOrganizationModel caller, L10Recurrence.L10Recurrence_Page page, PageCheckInUpdates updates);

  }
}

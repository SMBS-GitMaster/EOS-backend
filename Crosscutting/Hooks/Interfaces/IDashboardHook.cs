using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Dashboard;
using System.Threading.Tasks;


namespace RadialReview.Utilities.Hooks
{
  public class IDashboardHookUpdates
  {
  }


  public interface IDashboardHook : IHook
  {
    Task CreateDashboard(ISession s, UserOrganizationModel caller, Dashboard dashboard);
    Task UpdateDashboard(ISession s, UserOrganizationModel caller, Dashboard dashboard, IDashboardHookUpdates updates);
    Task DeleteDashboard(ISession s, UserOrganizationModel caller, Dashboard dashboard);
  }
}
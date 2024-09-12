using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces
{
  public interface IBusinessPlanHook : IHook
  {
    Task CreateBusinessPlan(BusinessPlanModel businessPlan);
    Task UpdateBusinessPlan(BusinessPlanModel businessPlan);
    Task CreateBusinessPlans(BusinessPlanModel businessPlan);
    Task UpdateBusinessPlans(BusinessPlanModel businessPlan);
  }
}

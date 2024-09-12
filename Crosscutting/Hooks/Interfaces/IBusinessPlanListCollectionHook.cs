
using RadialReview.BusinessPlan.Models;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces
{
  public interface IBusinessPlanListCollectionHook : IHook
  {
    Task CreateBusinessPlanListCollection(long businessPlanId,  Guid tileId,  BusinessPlanListCollection collection);
    Task UpdateBusinessPlanListCollection(long businessPlanId, BusinessPlanListCollection collection);
  }
}

using RadialReview.BusinessPlan.Core.Data.Models;
using RadialReview.BusinessPlan.Models;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces
{
  public interface IBusinessPlanTileHook: IHook
  {
    Task CreateBusinessPlanTile(long businessPlanId, BusinessPlanTile tile);
    Task UpdateBusinessPlanTile(long businessPlanId, BusinessPlanTile tile);
    Task DeleteBusinessPlanTile(long businessPlanId, BusinessPlanTile tile);
  }
}

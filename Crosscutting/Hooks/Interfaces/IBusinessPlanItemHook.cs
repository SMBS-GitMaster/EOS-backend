using RadialReview.BusinessPlan.Models;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Interfaces
{
  public interface IBusinessPlanListItemHook: IHook
  {
    Task CreateBusinessPlanListItem(long bizPlanId, Guid collectionId, BusinessPlanListItem itemSubscriptionModel);

    Task UpdateBusinessPlanListItem(long bizPlanId, Guid collectionId, BusinessPlanListItem itemSubscriptionModel);

    Task DeleteBusinessPlanListItem(long bizPlanId, Guid collectionId, BusinessPlanListItem itemSubscriptionModel);

  }
}

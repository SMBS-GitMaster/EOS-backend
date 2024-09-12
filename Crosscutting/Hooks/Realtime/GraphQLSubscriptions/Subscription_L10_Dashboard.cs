using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.Dashboard;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.UserQueryModel.Collections;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  internal class Subscription_L10_Dashboard : IDashboardHook
  {
    private readonly ITopicEventSender _eventSender;

    public Subscription_L10_Dashboard(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    public bool AbsorbErrors()
    {
      return false;
    }

    public bool CanRunRemotely()
    {
      return false;
    }

    public HookPriority GetHookPriority()
    {
      return HookPriority.UI;
    }

    public Task CreateDashboard(ISession s, UserOrganizationModel caller, Dashboard dashboard)
    {
      throw new NotImplementedException();
    }
    public async Task DeleteDashboard(ISession s, UserOrganizationModel caller, Dashboard dashboard)
    {
      var trasmformToQueryModel = dashboard.TransformDashboard();
      await _eventSender.SendChangeAsync(ResourceNames.User(caller.Id), Change<IMeetingChange>.Removed(Change.Target(caller.Id, UserQueryModel.Collections.Workspace1.Workspaces), dashboard.Id, trasmformToQueryModel)).ConfigureAwait(false);
    }

    public Task UpdateDashboard(ISession s, UserOrganizationModel caller, Dashboard dashboard, IDashboardHookUpdates updates)
    {
      throw new NotImplementedException();
    }
  }
}

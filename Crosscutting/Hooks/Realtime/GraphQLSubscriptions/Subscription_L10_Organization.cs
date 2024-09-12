using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Organization : IOrganizationHook
  {

    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_Organization(ITopicEventSender eventSender)
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

    public Task CreateOrganization(ISession s, UserOrganizationModel creator, OrganizationModel organization, OrgCreationData data, IOrganizationHookCreate meta)
    {
      throw new NotImplementedException();
    }


    public async Task UpdateOrganization(ISession s, long organizationId, IOrganizationHookUpdates updates, UserOrganizationModel user)
    {
      UserQueryModel userQueryModel = UserTransformer.TransformUser(user);
      var change = Change<IMeetingChange>.Updated(user.Id, userQueryModel, new[]{
      new ContainerTarget {
            Type = "user",
            Id = user.Id,
            Property = "USERS"
          }}
      );
      await _eventSender.SendChangeAsync(ResourceNames.User(user.Id), change).ConfigureAwait(false);
    }
  }
}

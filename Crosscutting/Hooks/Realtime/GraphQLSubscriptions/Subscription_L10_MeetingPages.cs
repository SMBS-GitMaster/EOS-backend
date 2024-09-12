using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.Repositories;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_MeetingPages : IMeetingPageHook
  {
    private readonly ITopicEventSender _eventSender;
    public Subscription_L10_MeetingPages(ITopicEventSender eventSender)
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

    public Task CreatePage(ISession s, L10Recurrence.L10Recurrence_Page recurPage)
    {
      return Task.CompletedTask;
    }

    public async Task UpdatePage(ISession s, L10Recurrence.L10Recurrence_Page recurPage)
    {
      var targets = new[]
      {
        new ContainerTarget
        {
          Type = "meeting",
          Id = recurPage.L10RecurrenceId,
          Property = "MEETING_PAGES"
        }
      };

      var page = recurPage.MeetingPageFromL10RecurrencePage();
      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurPage.L10RecurrenceId), Change<IMeetingChange>.Updated(recurPage.L10RecurrenceId, page, targets));
    }
  }
}

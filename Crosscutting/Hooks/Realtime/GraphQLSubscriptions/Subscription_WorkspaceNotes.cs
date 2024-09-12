using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_WorkspaceNotes : IWorkspaceNoteHook
  {


    #region Fields

    private readonly ITopicEventSender _eventSender;

    #endregion

    #region Constructor

    public Subscription_WorkspaceNotes(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    #endregion

    #region Base Hook Methods

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

    #endregion

    #region Workspace Tile Hook Methods

    public async Task InsertWorkspaceNote(ISession s, UserOrganizationModel caller, WorkspaceNoteQueryModel source)
    {
      var response = SubscriptionResponse<long>.Added(source.Id);
      await _eventSender.SendAsync(ResourceNames.WorkspaceNoteEvents, response);

      await _eventSender.SendChangeAsync(ResourceNames.WorkspaceNote(source.WorkspaceId), Change<IMeetingChange>.Created(source.Id, source));
      await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.Inserted(Change.Target(source.WorkspaceId, WorkspaceQueryModel.Collections.PersonalNotes.Notes), source.Id, source)).ConfigureAwait(false);
    }

    public async Task UpdateWorkspaceNote(ISession s, UserOrganizationModel caller, WorkspaceNoteQueryModel source)
    {
      var response = SubscriptionResponse<long>.Updated(source.Id);
      await _eventSender.SendAsync(ResourceNames.WorkspaceNoteEvents, response).ConfigureAwait(false);

      ContainerTarget[] workspaceTileTargets = new ContainerTarget[]
      {
            new ContainerTarget
            {
              Type = "workspaceNote",
              Id = source.Id,
              Property = "WORKSPACE_NOTES"
            },
            new ContainerTarget
            {
              Type = "workspace",
              Id = source.WorkspaceId,
              Property = "NOTES"
            },
      };

      await _eventSender.SendChangeAsync(ResourceNames.WorkspaceNote(source.Id), Change<IMeetingChange>.Updated(source.Id, source, workspaceTileTargets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.Updated(source.Id, source, workspaceTileTargets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(source.WorkspaceId, WorkspaceQueryModel.Associations.PersonalNotes2.Notes), source.Id, source)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(source.WorkspaceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(source.WorkspaceId, WorkspaceQueryModel.Collections.PersonalNotes.Notes), source.Id, source)).ConfigureAwait(false);

      if (source.Archived)
      {
        await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.Removed(Change.Target(source.WorkspaceId, WorkspaceQueryModel.Collections.PersonalNotes.Notes), source.Id, source)).ConfigureAwait(false);
      }

    }

    #endregion



  }
}

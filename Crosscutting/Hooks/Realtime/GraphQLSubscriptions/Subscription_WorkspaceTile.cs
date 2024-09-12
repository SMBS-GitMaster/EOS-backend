using DocumentFormat.OpenXml.Office2010.Excel;
using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.GraphQL.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Repositories;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System.Collections.Generic;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.IssueQueryModel.Associations;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_WorkspaceTile : IWorkspaceTileHook
  {

    #region Fields

    private readonly ITopicEventSender _eventSender;

    #endregion

    #region Constructor

    public Subscription_WorkspaceTile(ITopicEventSender eventSender)
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

    public async Task InsertWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel source, WorkspaceQueryModel workspace)
    {
      var response = SubscriptionResponse<long>.Added(source.Id);
      await _eventSender.SendAsync(ResourceNames.WorkspaceTileEvents, response);

      await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.Inserted(Change.Target(source.WorkspaceId, WorkspaceQueryModel.Collections.TileNodes.Tiles), source.Id, source)).ConfigureAwait(false);
    }

    public async Task RemoveWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel source, long workspaceId)
    {
      await RemoveWorkspaceTile(s, caller, source, RadialReviewRepository.GetWorkspaceWithFavorite(caller, workspaceId));
    }

    public async Task RemoveWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel source, WorkspaceQueryModel workspace)
    {
      var response = SubscriptionResponse<long>.Added(source.Id);
      await _eventSender.SendAsync(ResourceNames.WorkspaceTileEvents, response);

      await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.Removed(Change.Target(source.WorkspaceId, WorkspaceQueryModel.Collections.TileNodes.Tiles), source.Id, source)).ConfigureAwait(false);
    }

    public async Task UpdateWorkspaceTile(ISession s, UserOrganizationModel caller, WorkspaceTileQueryModel source)
    {
      var response = SubscriptionResponse<long>.Updated(source.Id);
      await _eventSender.SendAsync(ResourceNames.WorkspaceEvents, response).ConfigureAwait(false);
      long resourceId = source.MeetingId.HasValue ? source.MeetingId.Value : -1;

      ContainerTarget[] workspaceTileTargets = new ContainerTarget[]
      {
            new ContainerTarget
            {
              Type = "workspaceTile",
              Id = source.Id,
              Property = "WORKSPACE_TILES"
            },
            new ContainerTarget
            {
              Type = "workspace",
              Id = source.Id,
              Property = "TILES"
            },
      };

      await _eventSender.SendChangeAsync(ResourceNames.WorkspaceTile(source.Id), Change<IMeetingChange>.Updated(source.Id, source, workspaceTileTargets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.UpdatedAssociation(Change.Target(source.WorkspaceId, WorkspaceQueryModel.Associations.TileNodes2.Tiles), source.Id, source));
      await _eventSender.SendChangeAsync(ResourceNames.Workspace(source.WorkspaceId), Change<IMeetingChange>.Updated(source.Id, source, workspaceTileTargets)).ConfigureAwait(false);
    }

    #endregion

  }
}

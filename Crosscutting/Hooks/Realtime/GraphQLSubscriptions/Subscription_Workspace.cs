using DocumentFormat.OpenXml.Bibliography;
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
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_Workspace : IWorkspaceHook
  {

    #region Fields

    private readonly ITopicEventSender _eventSender;

    #endregion

    #region Constructor

    public Subscription_Workspace(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    #endregion

    #region Generic Hook Methods

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

    #region Workspace Hook Methods

    public async Task CreateWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace, long ownerId)
    {
      var response = SubscriptionResponse<long>.Updated(workspace.Id);
      await _eventSender.SendAsync(ResourceNames.Workspace(workspace.Id), response).ConfigureAwait(false);

      await _eventSender.SendChangeAsync(ResourceNames.Workspace(workspace.Id), Change<IMeetingChange>.Created(workspace.Id, workspace)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(ownerId), Change<IMeetingChange>.Inserted(Change.Target(ownerId, UserQueryModel.Collections.Workspace1.Workspaces), workspace.Id, workspace));

    }

    public async Task DeleteWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace, long ownerId)
    {
      var response = SubscriptionResponse<long>.Updated(workspace.Id);
      await _eventSender.SendAsync(ResourceNames.Workspace(workspace.Id), response).ConfigureAwait(false);

      await _eventSender.SendChangeAsync(ResourceNames.Workspace(workspace.Id), Change<IMeetingChange>.Deleted<WorkspaceQueryModel>(workspace.Id));
      await _eventSender.SendChangeAsync(ResourceNames.User(ownerId), Change<IMeetingChange>.Removed(Change.Target(workspace.Id, UserQueryModel.Collections.Workspace1.Workspaces), workspace.Id, workspace)).ConfigureAwait(false);
    }

    public async Task UpdateMeetingWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace, long recurrenceId)
    {
      var response = SubscriptionResponse<long>.Updated(workspace.Id);
      await _eventSender.SendAsync(ResourceNames.Workspace(workspace.Id), response).ConfigureAwait(false);

      ContainerTarget[] targets = new ContainerTarget[]
      {
            new ContainerTarget
            {
              Type = "workspace",
              Id = recurrenceId,
              Property = "WORKSPACE"
            },
      };

      await _eventSender.SendChangeAsync(ResourceNames.Meeting(recurrenceId), Change<IMeetingChange>.Updated(workspace.Id, workspace, targets)).ConfigureAwait(false);
    }

    public async Task UpdateWorkspace(ISession s, UserOrganizationModel caller, WorkspaceQueryModel workspace)
    {
      var response = SubscriptionResponse<long>.Updated(workspace.Id);
      await _eventSender.SendAsync(ResourceNames.Workspace(workspace.Id), response).ConfigureAwait(false);

      ContainerTarget[] targets = new ContainerTarget[]
      {
            new ContainerTarget
            {
              Type = "workspace",
              Id = workspace.Id,
              Property = "WORKSPACE"
            },
      };

      if (workspace.Tiles == null)
      {
        workspace.Tiles = new List<WorkspaceTileQueryModel>();
        var workspaceDA = DashboardAccessor.GetTilesAndDashboard(caller, workspace.Id);
        if (workspace != null)
        {
          foreach (var tile in workspaceDA.Tiles)
          {
            workspace.Tiles.Add(tile.Transform(caller));
          }
        }
      }

      await _eventSender.SendChangeAsync(ResourceNames.Workspace(workspace.Id), Change<IMeetingChange>.Updated(workspace.Id, workspace, targets)).ConfigureAwait(false);
      await _eventSender.SendChangeAsync(ResourceNames.User(workspace.UserId), Change<IMeetingChange>.Updated(workspace.Id, workspace, targets)).ConfigureAwait(false);

    }

    #endregion

  }
}

using HotChocolate.Subscriptions;
using NHibernate;
using GQL = RadialReview.GraphQL;
using RadialReview.Core.Repositories;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Accessors;
using RadialReview.GraphQL.Models;
using DocumentFormat.OpenXml.Bibliography;
using RadialReview.Crosscutting.Hooks;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;
using HotChocolate.Language;
using RadialReview.Models.Dashboard;

namespace RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions
{
  public class Subscription_L10_Favorites : IFavoriteHook
  {

    #region Fields

    private readonly ITopicEventSender _eventSender;

    #endregion

    #region Constructors

    public Subscription_L10_Favorites(ITopicEventSender eventSender)
    {
      _eventSender = eventSender;
    }

    #endregion

    #region Public Methods

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

    public async Task CreateFavorite(ISession s, UserOrganizationModel caller, FavoriteModel model)
    {
      switch (model.ParentType)
      {
        case FavoriteType.Meeting:
          var meeting = L10Accessor.GetL10Recurrence(caller, model.ParentId, new LoadMeeting { LoadUsers = true});
          await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateRecurrence(sess, caller, meeting));

          var recurrence = meeting;
          var targets = new[] {
            new ContainerTarget {
              Type = "user",
              Id = model.UserId,
              Property = "MEETINGS"
            }
          };

          var targetsMeetingListLookup = new[] {
            new ContainerTarget {
              Type = "user",
              Id = model.UserId,
              Property = "MEETINGS_LIST_LOOKUP",
            }
          };

          var favorite = FavoriteAccessor.GetFavoriteForUser(caller, RadialReview.Models.FavoriteType.Meeting, recurrence.Id);
          var settings = MeetingSettingsAccessor.GetSettingsForMeeting(caller, recurrence.Id);
          var m = meeting.MeetingFromRecurrence(caller, favorite, settings);
          var meetingListLookup = meeting.TransformMeetingListLookupFromRecurrence(caller.Id, favorite, settings);

          await _eventSender.SendChangeAsync(ResourceNames.User(model.UserId), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
          await _eventSender.SendChangeAsync(ResourceNames.User(model.UserId), Change<IMeetingChange>.Updated(m.Id, meetingListLookup, targetsMeetingListLookup)).ConfigureAwait(false);

          break;
        case FavoriteType.Workspace:
          var wsTargets = new[] {
            new ContainerTarget {
              Type = "user",
              Id = model.UserId,
              Property = "WORKSPACES"
            }
          };

          var w = WorkspaceFromDashboard(caller, model, DashboardAccessor.GetDashboard(caller, model.ParentId));
          await _eventSender.SendChangeAsync(ResourceNames.User(model.UserId), Change<IMeetingChange>.Updated(w.Id, w, wsTargets)).ConfigureAwait(false);
          break;
      }


    }

    public async Task UpdateFavorite(ISession s, UserOrganizationModel caller, FavoriteModel model, IFavoriteHookUpdates updates)
    {
      switch (model.ParentType)
      {
        case FavoriteType.Meeting:
          if (model.ParentType == FavoriteType.Meeting)
          {
            var meeting = L10Accessor.GetL10Recurrence(caller, model.ParentId, new LoadMeeting { LoadUsers = true });
            await HooksRegistry.Each<IMeetingEvents>((sess, x) => x.UpdateRecurrence(sess, caller, meeting));

            var recurrence = meeting;
            var targets = new[] {
              new ContainerTarget {
                Type = "user",
                Id = model.UserId,
                Property = "MEETINGS"
              }
            };

            var favorite = FavoriteAccessor.GetFavoriteForUser(caller, RadialReview.Models.FavoriteType.Meeting, recurrence.Id);
            var settings = MeetingSettingsAccessor.GetSettingsForMeeting(caller, recurrence.Id);
            var m = meeting.MeetingFromRecurrence(caller, favorite, settings);  
            var meetingListLookup = meeting.TransformMeetingListLookupFromRecurrence(caller.Id, favorite, settings);
            
            await _eventSender.SendChangeAsync(ResourceNames.User(model.UserId), Change<IMeetingChange>.Updated(m.Id, m, targets)).ConfigureAwait(false);
            
            var meetingListLookupTargets = new[] {
              new ContainerTarget {
                Type = "user",
                Id = model.UserId,
                Property = "MEETINGS_LIST_LOOKUP"
              }
            };

            await _eventSender.SendChangeAsync(ResourceNames.User(model.UserId), Change<IMeetingChange>.Updated(m.Id, meetingListLookup, meetingListLookupTargets)).ConfigureAwait(false);
          }
          break;
        case FavoriteType.Workspace:
          var wsTargets = new[] {
            new ContainerTarget {
              Type = "user",
              Id = model.UserId,
              Property = "WORKSPACES"
            }
          };

          var w = WorkspaceFromDashboard(caller, model, DashboardAccessor.GetDashboard(caller, model.ParentId));
          await _eventSender.SendChangeAsync(ResourceNames.User(model.UserId), Change<IMeetingChange>.Updated(w.Id, w, wsTargets)).ConfigureAwait(false);
          break;
      }

    }

    #endregion

    #region Private Methods

    private WorkspaceQueryModel WorkspaceFromDashboard(UserOrganizationModel caller, FavoriteModel model, Dashboard source)
    {
      var favorite = FavoriteAccessor.GetFavoriteForUser(caller, RadialReview.Models.FavoriteType.Workspace, model.ParentId);

      return new WorkspaceQueryModel
      {
        Archived = source.DeleteTime.HasValue,
        CreatedTimestamp = source.CreateTime.ToUnixTimeStamp(),
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        Favorited = favorite != null,
        FavoritedSortingPosition = favorite?.Position,
        FavoritedTimestamp = favorite?.CreatedDateTime.ToUnixTimeStamp(),
        FavoriteId = favorite?.Id,
        Id = source.Id,
        Name = source.PrimaryDashboard ? "Primary Workspace" : source.Title,
        IsPrimary = source.PrimaryDashboard,
      };
    }

    #endregion

  }
}
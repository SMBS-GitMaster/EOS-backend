using Amazon.DynamoDBv2;
using FluentNHibernate.Conventions;
using Humanizer;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Crosscutting.Hooks.Interfaces;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Models;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Core.GraphQL.Types.Mutations;
using RadialReview.Core.Models.L10;
using RadialReview.Core.Repositories;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Types;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Rocks;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Models.VTO;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MilestoneStatus = RadialReview.Models.Rocks.MilestoneStatus;
using ModelIssue = RadialReview.Models.Issues.IssueModel;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    string GetMeetingCurrentPage(long meetingId, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateMeetingPage(CreateMeetingPageModel createMeetingPageModel);

    Task<IdModel> EditMeetingPage(EditMeetingPageModel model);

    Task<IdModel> EditMeetingPageOrder(EditMeetingPageOrder model);

    Task<GraphQLResponseBase> RemoveMeetingPage(RemoveMeetingPageModel model);

    Task<IdModel> SetMeetingPage(SetMeetingPageModel setMeetingPageModel);

    Task<GraphQLResponseBase> SetMeetingPage(long meetingId, long newMeetingPageId, long currentMeetingPageId, double meetingPageStartTime);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public string GetMeetingCurrentPage(long meetingId, CancellationToken cancellationToken)
    {
      string result = L10Accessor.GetCurrentL10MeetingLeaderPage(caller, meetingId);
      if (result != null) return result.Replace("page-", "");
      return null;
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateMeetingPage(CreateMeetingPageModel model)
    {
      var page = RepositoryTransformers.L10RecurrencePageFromMeetingPage(createPage: model);

      var pageCreated = await L10Accessor.EditOrCreatePage(caller, page, false);
      return new IdModel(pageCreated.Id);
    }

    public async Task<IdModel> EditMeetingPage(EditMeetingPageModel model)
    {
      var pageCurrent = L10Accessor.GetPage(caller, model.MeetingPageId);
      var page = RepositoryTransformers.L10RecurrencePageFromMeetingPage(editPage: model, page: pageCurrent);

      int? checkInType = null;
      if (model.CheckIn?.CheckInType != null)
      {
        gqlCheckInType gqlCheckin = (gqlCheckInType)Enum.Parse(typeof(gqlCheckInType), model.CheckIn.CheckInType);
        checkInType = (int)gqlCheckin;

        var checkInData = new PageCheckInUpdates {
          CheckInType = model.CheckIn.CheckInType,
          IceBreaker =  model.CheckIn.IceBreaker,
          IsAttendanceVisible = model.CheckIn.IsAttendanceVisible
        };
        await HooksRegistry.Each<ICheckInHook>((ses, x) => x.UpdateCheckIn(ses, caller, page, checkInData));
      }

      var pageEdited = await L10Accessor.EditOrCreatePage(caller, page, false, checkInType, model.CheckIn?.IceBreaker, model.CheckIn?.IsAttendanceVisible);

      return new IdModel(pageEdited.Id);
    }

    public async Task<IdModel> EditMeetingPageOrder(EditMeetingPageOrder model)
    {
      await L10Accessor.ReorderPage(caller, model.MeetingPageId, model.OldIndex, model.NewIndex);
      return new IdModel(model.MeetingPageId);
    }

    public async Task<GraphQLResponseBase> RemoveMeetingPage(RemoveMeetingPageModel model)
    {
      try
      {
        var page = L10Accessor.GetPageInRecurrence(caller, model.MeetingPageId, model.RecurrenceId);
        page.DeleteTime = DateTime.UtcNow;
        await L10Accessor.EditOrCreatePage(caller, page, false);

        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    public async Task<IdModel> SetMeetingPage(SetMeetingPageModel model)
    {
      //page name might be wrong here..
      await L10Accessor.UpdatePage(caller, caller.Id, model.MeetingId, "page-" + model.NewPageId, null, model.MeetingPageStartTime.FromUnixTimeStamp());
      return null;
    }

    public async Task<GraphQLResponseBase> SetMeetingPage(long recurrenceId, long newMeetingPageId, long currentMeetingPageId, double meetingPageStartTime)
    {
      try
      {
        var storedCurrentPageId = L10Accessor.GetCurrentPageIdMeeting(caller, recurrenceId);
        bool isInvalidOrSameMeetingPage = storedCurrentPageId != currentMeetingPageId || newMeetingPageId == storedCurrentPageId;

        if (isInvalidOrSameMeetingPage)
          return GraphQLResponseBase.Error();

        var currentPage = L10Accessor.GetPageInRecurrence(caller, storedCurrentPageId, recurrenceId);

        var currentTimeSpent = meetingPageStartTime - currentPage.TimeLastStarted;

        // avoid TimePreviouslySpentS null value
        var timePreviouslySpentS = currentPage.TimePreviouslySpentS ?? 0;

        currentPage.TimePreviouslySpentS = timePreviouslySpentS + currentTimeSpent;

        await L10Accessor.EditOrCreatePage(caller, currentPage, false);

        var dateTimePage = meetingPageStartTime.FromUnixTimeStamp();
        var pageNameComplete = RepositoryTransformers.VerifyAndTransformPageId(newMeetingPageId.ToString());
        var page = await L10Accessor.UpdatePage(caller, caller.Id, recurrenceId, pageNameComplete, "", dateTimePage);

        var pageById = L10Accessor.GetPageInRecurrence(caller, newMeetingPageId, recurrenceId);

        pageById.TimeLastStarted = meetingPageStartTime;

        await L10Accessor.EditOrCreatePage(caller, pageById, false);

        return GraphQLResponseBase.Successfully();
      }
      catch (Exception ex)
      {
        return GraphQLResponseBase.Error(ex);
      }
    }

    private async Task UpdateSetMeetingPage(long recurrenceId, double startTime)
    {
      var loadPages = LoadMeeting.False();
      loadPages.LoadPages = true;
      var tempRecur = L10Accessor.GetL10Recurrence(caller, recurrenceId, loadPages);
      var page = L10Accessor.GetDefaultStartPage(tempRecur);
      page = page.NotNull(x => x.ToLower());

      var dateTimePage = startTime.FromUnixTimeStamp();
      await L10Accessor.UpdatePage(caller, caller.Id, recurrenceId, page, "", dateTimePage);
    }

    #endregion

  }
}
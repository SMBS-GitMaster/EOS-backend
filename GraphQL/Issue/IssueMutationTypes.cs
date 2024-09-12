using HotChocolate.Subscriptions;
using HotChocolate.Types;
using Microsoft.AspNetCore.Components.Forms;
using RadialReview.BusinessPlan.Core.Repositories.Interfaces;
using RadialReview.BusinessPlan.Models.Enums;
using RadialReview.BusinessPlan.Models.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Types;
using RadialReview.Repositories;
using System.Collections.Generic;
using TokenManager = RadialReview.Api.Authentication.TokenManager;

namespace RadialReview.GraphQL {

  public partial class IssueCreateMutationType : InputObjectType<IssueCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<IssueCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class IssueEditMutationType : InputObjectType<IssueEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<IssueEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class IssueCreateSentToMutationType : InputObjectType<IssueCreateSentToModel> {
    protected override void Configure(IInputObjectTypeDescriptor<IssueCreateSentToModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class IssueSubmitStarVotesType : InputObjectType<IssueSubmitStarVotesModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<IssueSubmitStarVotesModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

  public partial class IssueSubmitPriorityVotesType : InputObjectType<IssueSubmitPriorityVotesModel>
  {
    protected override void Configure(IInputObjectTypeDescriptor<IssueSubmitPriorityVotesModel> descriptor)
    {
      base.Configure(descriptor);
    }
  }

}


namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddIssueMutations(IObjectTypeDescriptor descriptor) {

      descriptor
      .Field("assignIssueToMeeting")
      .Argument("issueId", a => a.Type<NonNullType<LongType>>())
      .Argument("recurrenceId", a => a.Type<NonNullType<LongType>>())
      .Authorize()
      .Resolve((ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().AssignIssueToMeeting(
        ctx.ArgumentValue<long>("issueId"),
        ctx.ArgumentValue<long>("recurrenceId"),
        cancellationToken
      ));

      descriptor
      .Field("resetIssueStarVoting")
      .Argument("meetingId", a => a.Type<NonNullType<LongType>>())
      .Authorize()
      .Resolve((ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().ResetIssueStarVoting(
        ctx.ArgumentValue<long>("meetingId")
      ));

      descriptor
      .Field("submitIssueStarVotes")
      .Argument("input", a => a.Type<NonNullType<IssueSubmitStarVotesType>>())
      .Authorize()
      .Resolve((ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().SubmitIssueStarVotes(
        ctx.ArgumentValue<IssueSubmitStarVotesModel>("input")
      ));

      descriptor
      .Field("submitIssuePriorityVotes")
      .Argument("input", a => a.Type<NonNullType<IssueSubmitPriorityVotesType>>())
      .Authorize()
      .Resolve((ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().SubmitIssuePriorityVotes(
        ctx.ArgumentValue<IssueSubmitPriorityVotesModel>("input")
      ));

      descriptor
      .Field("reassignIssueToMeeting")
      .Argument("issueId", a => a.Type<NonNullType<LongType>>())
      .Argument("oldRecurrenceId", a => a.Type<NonNullType<LongType>>())
      .Argument("newRecurrenceId", a => a.Type<NonNullType<LongType>>())
      .Authorize()
      .Resolve((ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().ReassignIssueToMeeting(
        ctx.ArgumentValue<long>("issueId"),
        ctx.ArgumentValue<long>("oldRecurrenceId"),
        ctx.ArgumentValue<long>("newRecurrenceId"),
        cancellationToken
      ));

      descriptor
        .Field("SendIssueToMeeting")
        .Argument("input", a => a.Type<NonNullType<IssueCreateSentToMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateIssueSentTo(ctx.ArgumentValue<IssueCreateSentToModel>("input")));


      descriptor
        .Field("CreateIssue")
        .Argument("input", a => a.Type<NonNullType<IssueCreateMutationType>>())
        .Authorize()
        .Type<IssueQueryType>()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateIssue(ctx.ArgumentValue<IssueCreateModel>("input")));
        //.Resolve(async (ctx, cancellationToken) => {
        //  var inputData = ctx.ArgumentValue<IssueCreateModel>("input");
        //  var issueResponse = await ctx.Service<IRadialReviewRepository>().CreateIssue(inputData);

          // This code will be use later when we fire the 'createBusinessPlanForMeeting' mutation.
          //if (inputData.AddToDepartmentPlan == true)
          //{
          //  var goalOrIssuePayload = new GoalOrIssueInputModel
          //  {
          //    GoalOrIssueId = issueResponse.Id,
          //    DataType = ListItemType.ISSUE,
          //    MeetingDataList = new List<GoalOrIssueInputModel.MeetingData>()
          //    {
          //     new GoalOrIssueInputModel.MeetingData()
          //     {
          //        MeetingId = inputData.RecurrenceId,
          //        AddToBusinessPlan = true,
          //     }
          //    }
          //  };
          // // await ctx.Service<IBusinessPlanRepository>().CreateOrEditGoalOrIssue(goalOrIssuePayload);
          //}
        //  return issueResponse;
        //});

      descriptor
        .Field("EditIssue")
        .Argument("input", a => a.Type<NonNullType<IssueEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditIssue(ctx.ArgumentValue<IssueEditModel>("input")));
        //.Resolve(async (ctx, cancellationToken) => {
        //  var inputData = ctx.ArgumentValue<IssueEditModel>("input");
        //  var issueResponse = await ctx.Service<IRadialReviewRepository>().EditIssue(inputData);

          //// This code will be use later when we fire the 'createBusinessPlanForMeeting' mutation.
          // if (inputData.AddToDepartmentPlan != null || inputData.Archived != null)
          // {
          //   var goalOrIssuePayload = new GoalOrIssueInputModel
          //   {
          //     GoalOrIssueId = inputData.Id,
          //     DataType = ListItemType.ISSUE,
          //     MeetingDataList = new List<GoalOrIssueInputModel.MeetingData>()
          //     {
          //      new GoalOrIssueInputModel.MeetingData()
          //      {
          //         MeetingId = inputData.MeetingId ?? 0,
          //         AddToBusinessPlan = (bool)(inputData.AddToDepartmentPlan ?? !inputData.Archived),
          //      }
          //     }

          //   };
          ////   await ctx.Service<IBusinessPlanRepository>().CreateOrEditGoalOrIssue(goalOrIssuePayload);
          // }
        //  return issueResponse;
        //});

      descriptor
        .Field("ResetPriorityVoting")
        .Argument("meetingId", a => a.Type<NonNullType<LongType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().ResetPriorityVoting(ctx.ArgumentValue<long>("meetingId")));

    }
  }
}
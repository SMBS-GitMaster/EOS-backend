using HotChocolate.Types;
using RadialReview.BusinessPlan.Core.Repositories.Interfaces;
using RadialReview.BusinessPlan.Models.Enums;
using RadialReview.BusinessPlan.Models.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.GraphQL.Types.Mutations;
using RadialReview.Repositories;
using System.Collections.Generic;
using System.Linq;
using static RadialReview.BusinessPlan.Models.Models.GoalOrIssueInputModel;


namespace RadialReview.GraphQL.Types.Mutations {


  public partial class GoalCreateMutationType : InputObjectType<GoalCreateModel> {
    protected override void Configure(IInputObjectTypeDescriptor<GoalCreateModel> descriptor) {
      base.Configure(descriptor);
    }
  }

  public partial class GoalEditMutationType : InputObjectType<GoalEditModel> {
    protected override void Configure(IInputObjectTypeDescriptor<GoalEditModel> descriptor) {
      base.Configure(descriptor);
    }
  }
}

namespace RadialReview.GraphQL {
  public partial class MutationType {
    public void AddGoalMutations(IObjectTypeDescriptor descriptor) {

      descriptor
        .Field("CreateGoal")
        .Argument("input", a => a.Type<NonNullType<GoalCreateMutationType>>())
        .Authorize()
         .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().CreateGoal(ctx.ArgumentValue<GoalCreateModel>("input")));
        //.Resolve(async (ctx, cancellationToken) => {
        //  var inputData = ctx.ArgumentValue<GoalCreateModel>("input");
        //  var goalResponse = await ctx.Service<IRadialReviewRepository>().CreateGoal(inputData);

          // This code will be use later when we fire the 'createBusinessPlanForMeeting' mutation.
          //var meetingAndPlan = (inputData.MeetingsAndPlans ?? new List<GoalEditMeetingModel>());
          //if (meetingAndPlan.Any())
          //{
          //  var goalOrIssuePayload = new GoalOrIssueInputModel
          //  {
          //    GoalOrIssueId = goalResponse.Id,
          //    DataType = ListItemType.GOAL,
          //    MeetingDataList = meetingAndPlan.Select(x => new GoalOrIssueInputModel.MeetingData
          //    {
          //      MeetingId = x.MeetingId,
          //      AddToBusinessPlan = (bool)x.AddToDepartmentPlan,
          //    }).ToList()
          //  };

          //  //await ctx.Service<IBusinessPlanRepository>().CreateOrEditGoalOrIssue(goalOrIssuePayload);
          //}
        //  return goalResponse;
        //});

      descriptor
        .Field("EditGoal")
        .Argument("input", a => a.Type<NonNullType<GoalEditMutationType>>())
        .Authorize()
        .Resolve(async (ctx, cancellationToken) => await ctx.Service<IRadialReviewRepository>().EditGoal(ctx.ArgumentValue<GoalEditModel>("input")));
        //.Resolve(async (ctx, cancellationToken) => {
        //  var inputData = ctx.ArgumentValue<GoalEditModel>("input");
        //  var goalResponse = await ctx.Service<IRadialReviewRepository>().EditGoal(inputData);

          // This code will be use later when we fire the 'createBusinessPlanForMeeting' mutation.business plan logic
          //var meetingAndPlan = (inputData.MeetingsAndPlans ?? new List<GoalEditMeetingModel>());

          //if (meetingAndPlan.Any())
          //{
          //  var goalOrIssuePayload = new GoalOrIssueInputModel
          //  {
          //    GoalOrIssueId = goalResponse.Id,
          //    DataType = ListItemType.GOAL,
          //    MeetingDataList = meetingAndPlan.Select(x => new GoalOrIssueInputModel.MeetingData
          //    {
          //      MeetingId = x.MeetingId,
          //      AddToBusinessPlan = (bool)x.AddToDepartmentPlan,
          //    }).ToList()
          //  };
          //  //await ctx.Service<IBusinessPlanRepository>().CreateOrEditGoalOrIssue(goalOrIssuePayload);
          //}
        //  return goalResponse;
        //});

    }
  }
}



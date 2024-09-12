using HotChocolate.Types;
using RadialReview.BusinessPlan.Models;
using RadialReview.BusinessPlan.Models.Enums;
using RadialReview.GraphQL.Types;
using RadialReview.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;

namespace RadialReview.Core.GraphQL.BusinessPlan.Types.Queries
{
  public class ItemQueryType : ObjectType<BusinessPlanListItem>
  {
    protected readonly bool isSubscription;
    public ItemQueryType() : this(false) { }
    protected ItemQueryType(bool isSubscription) { this.isSubscription = isSubscription; }

    protected override void Configure(IObjectTypeDescriptor<BusinessPlanListItem> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Field(t => t.Date)
      .Resolve(r =>
      {
        DateTime? date = r.Parent<BusinessPlanListItem>().Date;
        return date.ToUnixTimeStamp();
      }).Type<FloatType>();

      descriptor
       .Field("type")
       .Type<StringType>()
       .Resolve(ctx => "businessPlanListItem")
       ;

      descriptor
      .Field("issue")
      .Type<SingleIssueType>()
      .Resolve((ctx, cancellationToken) =>
      {
        long? issueId = ctx.Parent<BusinessPlanListItem>().GoalOrIssueId;
        var isIssue = ctx.Parent<BusinessPlanListItem>().ListItemType == ListItemType.ISSUE.GetDisplayName();

        if (issueId != null && isIssue)
        {
          return ctx.Service<IDataContext>().GetIssueByIdAsync(issueId, cancellationToken);
        }
        return null;
      }
        )
      .Authorize()
      ;

      descriptor
        .Field("goal")
        .Type<GoalQueryType>()
        .Resolve((ctx, cancellationToken) =>
        {
          long? goalId = ctx.Parent<BusinessPlanListItem>().GoalOrIssueId;
          var isGoal = ctx.Parent<BusinessPlanListItem>().ListItemType == ListItemType.GOAL.GetDisplayName();

          if(goalId != null && isGoal)
          {
           return ctx.Service<IDataContext>().GetGoalByIdAsync((long)goalId, cancellationToken);
          }

          return null;

        })
        .UseProjection()
        .Authorize()
        ;

    }
  }
  public class BusinessPlanItemChangeType : ItemQueryType
  {
    public BusinessPlanItemChangeType() : base(true)
    {
    }
    protected override void Configure(IObjectTypeDescriptor<BusinessPlanListItem> descriptor)
    {
      base.Configure(descriptor);
      descriptor.Name("BusinessPlanItemModelChange");
    }
  }
}

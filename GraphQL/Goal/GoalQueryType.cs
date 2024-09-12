using System.Linq;
using HotChocolate.Types.Pagination;
using HotChocolate.Types;
using RadialReview.GraphQL.Models;
using RadialReview.Repositories;
using System;
using GreenDonut;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using static HotChocolate.Types.ProjectionObjectFieldDescriptorExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace RadialReview.GraphQL.Types {
  public class GoalChangeType : GoalQueryType
  {
    public GoalChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<GoalQueryModel> descriptor)
    {
      base.Configure(descriptor);
      
      descriptor.Name("GoalModelChange");
    }
  }

  public class GoalQueryType : ObjectType<GoalQueryModel> {
    protected readonly bool isSubscription;

    public GoalQueryType() 
      : this(false)
    {
    }

    protected GoalQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<GoalQueryModel> descriptor) {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "goal")
        ;

      descriptor
        .Field(t => t.Id).IsProjected(true)
        .Type<LongType>()
        ;

      descriptor
        .Field(t => t.Archived)
        .Type<BooleanType>()
        .IsProjected(true)
        ;

      //NOTE: We aim to ignore the 'x' field using the Ignore() method.
      descriptor
        .Field(f => f.DepartmentPlanRecords)
        .IsProjected(true)
        .Name("x")
        .Resolve(ctx => "x")
        //.Ignore()
        ;

      if (isSubscription)
      {
        descriptor
          .Field("meetings")
          .Type<ListType<GoalMeetingChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetGoalMeetingsAsync(new[] { ctx.Parent<GoalQueryModel>().Id }, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("milestones")
          .Type<ListType<MilestoneChangeType>>()
          .Resolve(async (ctx, ct) => {
            var dataCtx = ctx.Service<IDataContext>();
            var loader = ctx.GroupDataLoader<long, MilestoneQueryModel>(async (goalIds, ct) => {
              var milestones = (await dataCtx.GetMilestonesForGoalsAsync(goalIds, ct)).ToList();
              return milestones.ToLookup(x => x.GoalId);
            });
            var data = await loader.LoadAsync(ctx.Parent<GoalQueryModel>().Id, ct);
            return data;
          })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("departmentPlanRecords")
          .Type<ListType<DepartmentPlanRecordChangeType>>()
          .Resolve(ctx => ctx.Parent<GoalQueryModel>().DepartmentPlanRecords)
          .UseProjection()
          .UseFiltering()
          .UseSorting();
      }
      else
      {         
        descriptor
          .Field("meetings")
          .Type<ListType<GoalMeetingQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetGoalMeetingsAsync(new[] { ctx.Parent<GoalQueryModel>().Id }, cancellationToken))
          .UsePaging<GoalMeetingQueryType>(options: new PagingOptions { IncludeTotalCount = true }, connectionName: "GoalMeetings")
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

        descriptor
          .Field("milestones")
          .Type<ListType<MilestoneQueryType>>()
          .Resolve(async (ctx, ct) => {
            var dataCtx = ctx.Service<IDataContext>();
            var loader = ctx.GroupDataLoader<long, MilestoneQueryModel>(async (goalIds, ct) => {
              var milestones = (await dataCtx.GetMilestonesForGoalsAsync(goalIds, ct)).ToList();
              return milestones.ToLookup(x => x.GoalId);
            });
            var data = await loader.LoadAsync(ctx.Parent<GoalQueryModel>().Id, ct);
            return data;
          })
          .UsePaging<MilestoneQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("departmentPlanRecords")
          .Type<ListType<DepartmentPlanRecordQueryType>>()
          .Resolve(ctx => ctx.Parent<GoalQueryModel>().DepartmentPlanRecords)
          .UsePaging<DepartmentPlanRecordQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();
        ;
      }
    }
  }
}
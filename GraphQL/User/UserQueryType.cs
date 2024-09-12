namespace RadialReview.GraphQL.Types
{
  using System.Linq;
  using System.Threading.Tasks;
  using Microsoft.EntityFrameworkCore;
  using HotChocolate.Types;
  using HotChocolate.Types.Pagination;
  using RadialReview.Accessors;
  using RadialReview.Core.GraphQL.Common;
  using RadialReview.Core.GraphQL.MeetingListLookup;
  using RadialReview.Core.GraphQL.MetricFormulaLookup;
  using RadialReview.Core.GraphQL.Types;
  using RadialReview.Core.GraphQL.Types.Query;
  using RadialReview.Core.GraphQL.Models;
  using RadialReview.GraphQL.Models;
  using RadialReview.Models;
  using RadialReview.Repositories;

  public class UserChangeType : UserQueryType
  {
    public UserChangeType() : base(true)
    {
    }

    protected override void Configure(IObjectTypeDescriptor<UserQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor.Name("UserModelChange");
    }
  }

  public class UserQueryType : ObjectType<UserQueryModel>
  {
    protected static readonly System.Object dummy = new System.Object();
    protected readonly bool isSubscription;

    public UserQueryType()
      : this(false)
    {
    }

    protected UserQueryType(bool isSubscription)
    {
      this.isSubscription = isSubscription;
    }

    protected override void Configure(IObjectTypeDescriptor<UserQueryModel> descriptor)
    {
      base.Configure(descriptor);

      descriptor
        .Field("type")
        .Type<StringType>()
        .Resolve(ctx => "user");

      descriptor
        .Field(t => t.Id)
        .Type<LongType>()
        .IsProjected(true);

      descriptor
        .Field("createIssueMeetings")
        .Type<ListType<MeetingMetadataType>>()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
        .UsePaging<MeetingMetadataType>(options: new PagingOptions { IncludeTotalCount = true })
        .UseProjection()
        .UseFiltering()
        .UseSorting()
        ;

      descriptor
        .Field("coachToolsUrl")
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetCoachToolsURL(ct));

      descriptor
        .Field(t => t.SupportContactCode)
        .Type<StringType>()
        .Resolve(async (ctx, ct) =>
        {
          return await ctx.BatchDataLoader<long, string>(
            async (keys, ct) =>
            {
              var result = await ctx.Service<IDataContext>().GetSupportContactCodeForUsers(keys, ct);
              return result.ToDictionary(x => x.Key, x => x.Value);
            }).LoadAsync(ctx.Parent<UserQueryModel>().Id, ct);
        });

      descriptor
        .Field("orgSettings")
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetOrgSettings(ctx.Parent<UserQueryModel>().Id, ct));



      if (isSubscription)
      {
        descriptor
          .Field("notifications")
          .Type<ListType<NotificationChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, NotificationQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetNotificationsForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_notifications").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("todos")
          .Type<ListType<TodoChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetUserTodosAsync(ctx.Parent<UserQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetings")
          .Type<ListType<MeetingChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetMeetingsForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_meetings").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("MeetingListLookupChangeType")
          .Type<ListType<MeetingListLookupType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingListLookupModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetMeetingsListLookupForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_meetingsListLookup").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UseProjection()
          .UseSorting();

        descriptor
          .Field("workspaces")
          .Type<ListType<WorkspaceChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, WorkspaceQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetWorkspacesForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_workspaces").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
        .Field("metricFormulaLookup")
        .Type<ListType<MetricFormulaLookupChangeType>>()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetMetricFormulaLookup(ctx.Parent<UserQueryModel>().Id, ct))
        .UseProjection()
        .UseFiltering()
        .UseSorting();

        descriptor
        .Field("createHeadlineMeetings")
        .Type<ListType<MeetingMetadataChangeType>>()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
        .UseProjection()
        .UseFiltering()
        .UseSorting();

        descriptor
        .Field("createTodoMeetings")
        .Type<ListType<MeetingMetadataChangeType>>()
        .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
        .UseProjection()
        .UseFiltering()
        .UseSorting();

        descriptor
          .Field("editTodoMeetings")
          .Type<ListType<MeetingMetadataChangeType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("goals")
          .Type<ListType<GoalChangeType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetGoalsForUser(ct))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metrics")
          .Type<ListType<MetricChangeType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetMetricsForUserAsync(ct))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetingsListLookup")
          .Type<ListType<MeetingListLookupChangeType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingListLookupModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetMeetingsListLookupForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_meetingsListLookup").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("adminPermissionsMeetingLookups")
          .Type<ListType<SelectListChangeType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetAdminPermissionsMeetingsLookup(ctx.Parent<UserQueryModel>().Id, ct))
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("editPermissionsMeetingLookups")
          .Type<ListType<SelectListChangeType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetEditPermissionsMeetingsLookup(ctx.Parent<UserQueryModel>().Id, ct))
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("organizations")
          .Type<ListType<OrganizationChangeType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetOrganizationsForUser(ctx.Parent<UserQueryModel>().Id, cancellationToken))
          .UseProjection()
          .UseFiltering()
          .UseSorting();


        descriptor
          .Field("orgChart")
          .Type<ListType<OrgChartChangeType>>()
          .Resolve((ctx, ct) => {
            var session = RadialReview.Utilities.HibernateSession.GetCurrentSession();
            return ctx.Service<IRadialReviewRepository>().GetOrgChartsForUsers([ctx.Parent<UserQueryModel>().Id], ct, session).AsEnumerable().Select(x => x.OrgChart);
          })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;
      }
      else
      {
        descriptor
          .Field("notifications")
          .Type<ListType<NotificationQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, NotificationQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetNotificationsForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_notifications").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UsePaging<NotificationQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("todos")
          .Type<ListType<TodoQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetUserTodosAsync(ctx.Parent<UserQueryModel>().Id, cancellationToken))
          .UsePaging<TodoQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetings")
          .Type<ListType<MeetingQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetMeetingsForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_meetings").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UsePaging<MeetingQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("goals")
          .Type<ListType<GoalQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetGoalsForUser(cancellationToken))
          .UsePaging<GoalQueryType>(options: new PagingOptions { IncludeTotalCount = true }, connectionName: "UserGoals")
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metrics")
          .Type<ListType<MetricQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetMetricsForUserAsync(cancellationToken))
          .UsePaging<MetricQueryType>(options: new PagingOptions { IncludeTotalCount = true }, connectionName: "UserMetrics")
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("organizations")
          .Type<ListType<OrganizationQueryType>>()
          .Resolve(async (ctx, cancellationToken) => await ctx.Service<IDataContext>().GetOrganizationsForUser(ctx.Parent<UserQueryModel>().Id, cancellationToken))
          .UsePaging<OrganizationQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("meetingsListLookup")
          .Type<ListType<MeetingListLookupType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, MeetingListLookupModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetMeetingsListLookupForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_meetingsListLookup").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UsePaging<MeetingListLookupType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("adminPermissionsMeetingLookups")
          .Type<ListType<SelectListQueryType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetAdminPermissionsMeetingsLookup(ctx.Parent<UserQueryModel>().Id, ct))
          .UsePaging<SelectListQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("editPermissionsMeetingLookups")
          .Type<ListType<SelectListQueryType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetEditPermissionsMeetingsLookup(ctx.Parent<UserQueryModel>().Id, ct))
          .UsePaging<SelectListQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("createTodoMeetings")
          .Type<ListType<MeetingMetadataQueryType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
          .UsePaging<MeetingMetadataQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("editTodoMeetings")
          .Type<ListType<MeetingMetadataQueryType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
          .UsePaging<MeetingMetadataQueryType>(options: new PagingOptions { IncludeTotalCount = true, DefaultPageSize = 2000 })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        //!! Reinstate later
        //descriptor
        //  .Field("meetings")
        //  .Type<ListType<MeetingMetadataType>>()
        //  .Resolve(ctx => ctx.GroupDataLoader<long, MeetingMetadataModel>(async (keys, cancellationToken) => {
        //    var result = await ctx.Service<IDataContext>().GetMeetingMetadataForUsersAsync(keys, cancellationToken);
        //    return result.ToLookup(x => x.UserId);
        //  }, "user_meetings").LoadAsync(ctx.Parent<UserModel>().Id))
        //  .UsePaging<MeetingMetadataType>(options: new PagingOptions { IncludeTotalCount = true })
        //  .UseProjection()
        //  .UseFiltering()
        //  .UseSorting();

        descriptor
          .Field("workspaces")
          .Type<ListType<WorkspaceQueryType>>()
          .Resolve(ctx => ctx.GroupDataLoader<long, WorkspaceQueryModel>(async (keys, cancellationToken) =>
          {
            var result = await ctx.Service<IDataContext>().GetWorkspacesForUsersAsync(keys, cancellationToken);
            return result.ToLookup(x => x.UserId);
          }, "user_workspaces").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .UsePaging<WorkspaceQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("metricFormulaLookup")
          .Type<ListType<MetricFormulaLookupQueryType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetMetricFormulaLookup(ctx.Parent<UserQueryModel>().Id, ct))
          .UsePaging<MetricFormulaLookupQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("createHeadlineMeetings")
          .Type<ListType<MeetingMetadataQueryType>>()
          .Resolve(async (ctx, ct) => await ctx.Service<IDataContext>().GetPossibleMeetingsForUser(ct))
          .UsePaging<MeetingMetadataQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting();

        descriptor
          .Field("orgChartId")
          .Type<LongType>()
          .Resolve((ctx, ct) =>
          {
            return ctx.Service<IRadialReviewRepository>().GetCaller().Organization.AccountabilityChartId;
          });

        descriptor
          .Field("orgChart")
          .Type<ListType<OrgChartQueryType>>()
          // .Resolve(async ctx => await ctx.GroupDataLoader<long, OrgChartQueryModel>(async (keys, cancellationToken) =>
          // {
          //   var query = ctx.Service<IDataContext>().GetOrgChartsForUsers(keys, cancellationToken);
          //   var result = await query.ToListAsync(cancellationToken);
          //   return result.ToLookup(x => x.UserId, x => x.OrgChart);
          // }, "user_orgcharts").LoadAsync(ctx.Parent<UserQueryModel>().Id))
          .Resolve((ctx, ct) => {
            var session = RadialReview.Utilities.HibernateSession.GetCurrentSession();
            return ctx.Service<IRadialReviewRepository>().GetOrgChartsForUsers([ctx.Parent<UserQueryModel>().Id], ct, session).AsEnumerable().Select(x => x.OrgChart);
          })
          // .UsePaging<OrgChartQueryType>(options: new PagingOptions { IncludeTotalCount = true })
          .UseProjection()
          .UseFiltering()
          .UseSorting()
          ;

      }

      descriptor
      .Field("settings")
      .Type<UserSettingsType>()
      .Resolve((ctx, ct) => ctx.Service<IRadialReviewRepository>().GetSettings(ctx.Parent<UserQueryModel>().Id, ct))
      .UseProjection()
        .UseFiltering()
        .UseSorting();
    }
  }
}
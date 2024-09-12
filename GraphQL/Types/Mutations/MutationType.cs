using HotChocolate.Subscriptions;
using HotChocolate.Types;
using RadialReview.Core.GraphQL.BusinessPlan;
using RadialReview.GraphQL.Types;
using RadialReview.Repositories;
using TokenManager = RadialReview.Api.Authentication.TokenManager;

namespace RadialReview.GraphQL {

  public partial class MutationType : ObjectType {
    protected override void Configure(IObjectTypeDescriptor descriptor) {
      base.Configure(descriptor);

      AddAuthenticationMutations(descriptor);
      AddCommentMutations(descriptor);
      AddFavoriteMutations(descriptor);
      AddGoalMutations(descriptor);
      AddHeadlineMutations(descriptor);
      AddIssueMutations(descriptor);
      AddMeetingActionMutations(descriptor);
      AddMeetingMutations(descriptor);
      AddMeetingInstanceAttendeeMutations(descriptor);
      AddMeetingNotesMutations(descriptor);
      AddMeetingPageMutations(descriptor);
      AddMetricsMutations(descriptor);
      AddMetricsTabMutations(descriptor);
      AddMilestoneMutations(descriptor);
      AddTodoMutations(descriptor);
      AddFeedbackMutations(descriptor);
      AddVotingMutations(descriptor);
      AddWorkspaceMutations(descriptor);
      AddUserMutations(descriptor);
      AddIframeEmbedMutations(descriptor);
      AddNotepadMutations(descriptor);
      AddCustomGoalMutations(descriptor);
      AddMetricScoreMutations(descriptor);
      AddUserSettingsMutations(descriptor);
      OrganizationSettingsMutations(descriptor);
      AddMetricDividerMutations(descriptor);
      AddWorkspaceNoteMutations(descriptor);
      AddOrgChartSeatMutations(descriptor);
      MutationTypeExtension.BusinessPlanMutations(descriptor);
    }

    private static void AddAuthenticationMutations(IObjectTypeDescriptor descriptor) {
      descriptor
              .Field("authenticate")
              .Argument("username", a => a.Type<NonNullType<StringType>>())
              .Argument("password", a => a.Type<NonNullType<StringType>>())
              .Type<TokenResultType>()
              .Resolve((ctx, cancellationToken) => ctx.Service<TokenManager>().GetToken(ctx.ArgumentValue<string>("username"), ctx.ArgumentValue<string>("password"), cancellationToken));

      descriptor
        .Field("getAuthenticatedUserId")
        .Type<LongType>()
        .Resolve((ctx, cancellationToken) => ctx.Service<IRadialReviewRepository>().GetCallerId())
        .Authorize();
    }
  }
}
using RadialReview.Crosscutting.Hooks.CrossCutting;
using RadialReview.Crosscutting.Hooks.CrossCutting.Formula;
using RadialReview.Crosscutting.Hooks.Integrations;
using RadialReview.Crosscutting.Hooks.Payment;
using RadialReview.Crosscutting.Hooks.QuarterlyConversation;
using RadialReview.Crosscutting.Hooks.CrossCutting.Payment;
using RadialReview.Crosscutting.Hooks.Meeting;
using RadialReview.Crosscutting.Hooks.Realtime;
using RadialReview.Crosscutting.Hooks.Realtime.Dashboard;
using RadialReview.Crosscutting.Hooks.Realtime.L10;
using RadialReview.Crosscutting.Hooks.UserRegistration;
using RadialReview.Utilities;
using System;
using RadialReview.Crosscutting.Hooks.CrossCutting.Zapier;
using RadialReview.Hooks;
using RadialReview.Hooks.CrossCutting.AgileCrm;
using log4net;
using RadialReview.Crosscutting.Hooks.Internal;
using RadialReview.Crosscutting.Hooks.Notifications;
using RadialReview.Crosscutting.Hooks.Realtime.Process;
using RadialReview.Crosscutting.Hooks.Realtime.AccountabilityChart;
using RadialReview.Core.Crosscutting.Hooks.Meeting;
using Mandrill.Models;
using HotChocolate.Subscriptions;
using RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions;
using RadialReview.Core.Crosscutting.Hooks.Realtime.L10;

namespace RadialReview.Crosscutting.Hooks {

	public class HookConfig {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public static void RegisterHooks(ITopicEventSender eventSender) {

      HooksRegistry.RegisterHook(new UpdateUserModel_TeamNames());
      HooksRegistry.RegisterHook(new UpdateUserCache());


      HooksRegistry.RegisterHook(new EnterpriseHook(Config.EnterpriseAboveUserCount()));

      HooksRegistry.RegisterHook(new ZapierEventSubscription());

      //try {
      //	HooksRegistry.RegisterHook(new AgileCrmOrgEventHook());
      //	HooksRegistry.RegisterHook(new AgileCrmUserEventHooks());
      //	HooksRegistry.RegisterHook(new AgileCrmMeetings());
      //} catch (Exception e) {
      //	log.Error(e);
      //}

      HooksRegistry.RegisterHook(new InternalZapierHooks());

      HooksRegistry.RegisterHook(new DepristineHooks());
      HooksRegistry.RegisterHook(new MeetingRockCompletion());
      HooksRegistry.RegisterHook(new AuditLogHooks());

      HooksRegistry.RegisterHook(new RealTime_L10_Todo());
      HooksRegistry.RegisterHook(new RealTime_Dashboard_Todo());
      HooksRegistry.RegisterHook(new RealTime_L10_Issues());

      HooksRegistry.RegisterHook(new Realtime_L10Scorecard());
      HooksRegistry.RegisterHook(new RealTime_L10_UpdateRocks());
      HooksRegistry.RegisterHook(new RealTime_VTO_UpdateRocks());
      HooksRegistry.RegisterHook(new RealTime_Dashboard_UpdateL10Rocks());
      HooksRegistry.RegisterHook(new RealTime_Dashboard_Scorecard());
      HooksRegistry.RegisterHook(new RealTime_L10_Headline());
      HooksRegistry.RegisterHook(new RealTime_Dashboard_Headline());
      HooksRegistry.RegisterHook(new RealTime_Process());
      HooksRegistry.RegisterHook(new RealTimeUpdateRoles());
      HooksRegistry.RegisterHook(new RealTime_L10_Rating());

      // Show Tangent Integrations
      HooksRegistry.RegisterHook(new Realtime_L10_Tangent());

      HooksRegistry.RegisterHook(new CalculateCumulative());
      HooksRegistry.RegisterHook(new AttendeeHooks());
      HooksRegistry.RegisterHook(new SwapScorecardOnRegister());

      HooksRegistry.RegisterHook(new CreateFinancialPermItems());

      HooksRegistry.RegisterHook(new UpdatePlaceholder());
      HooksRegistry.RegisterHook(new RealTime_L10_Milestone());
      HooksRegistry.RegisterHook(new CascadeScorecardFormulaUpdates());

      HooksRegistry.RegisterHook(new ExecutePaymentCardUpdate());
      HooksRegistry.RegisterHook(new FirstPaymentEmail());
      HooksRegistry.RegisterHook(new SetDelinquentFlag());
      HooksRegistry.RegisterHook(new CardExpireEmail());
      HooksRegistry.RegisterHook(new UnlockOnCard());
      HooksRegistry.RegisterHook(new SubmitTaxJarOrder());

      HooksRegistry.RegisterHook(new CoachPermissions());

      HooksRegistry.RegisterHook(new QuarterlyConversationCreationNotifications());
      HooksRegistry.RegisterHook(new SetPeopleToolsTrial());

      HooksRegistry.RegisterHook(new NotificationOnNewQuarterHooks());

      //Registration hooks
      HooksRegistry.RegisterHook(new SendVerification());

      //First & default meeting setup
      HooksRegistry.RegisterHook(new FirstMeetingInjectedDataHooks());

      //Todo Integrations
      HooksRegistry.RegisterHook(new AsanaTodoHook());

      RegisterGraphQLHooks(eventSender);
    }

    public static void RegisterGraphQLHooks(ITopicEventSender eventSender)
    {
      //Trigger Subscription 
      HooksRegistry.RegisterHook(new MeetingSubscriptionHooks(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Goals(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Headline(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Todos(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Issues(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Measurables(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Metrics(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Milestone(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_RateMeeting(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Tangent(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_MeetingMeasurable(eventSender));
      HooksRegistry.RegisterHook(new Subscription_MeetingAttendee(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Score(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Notes(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_MeetingPages(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Favorites(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Organization(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_Dashboard(eventSender));
      HooksRegistry.RegisterHook(new Subscription_Workspace(eventSender));
      HooksRegistry.RegisterHook(new Subscription_WorkspaceTile(eventSender));
      HooksRegistry.RegisterHook(new Subscription_WorkspaceNotes(eventSender));
      HooksRegistry.RegisterHook(new Subscription_L10_OrgChartSeat(eventSender));

      #region BusinessPlan

      HooksRegistry.RegisterHook(new Subscription_BusinessPlan(eventSender));
      HooksRegistry.RegisterHook(new Subscription_BusinessPlanTile(eventSender));
      HooksRegistry.RegisterHook(new Subscription_BusinessPlanListCollection(eventSender));

      HooksRegistry.RegisterHook(new Subscription_BusinessPlanListItem(eventSender));
      #endregion
    }
  }
}

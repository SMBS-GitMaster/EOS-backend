namespace RadialReview.Hangfire {
  public static class HangfireQueues {



    ///<summary>
    ///Immediate fire jobs should update their queue. If we change the code, we do not want to run on the wrong verions
    ///</summary>
    public static class Immediate {


      public const string BUILD_TIME = "07/10/2019 03:00:00";

      public const string CRITICAL = "critical_w_dv2";
      public const string ETHERPAD = "etherpad_w_dv3";
      public const string CONCLUSION_EMAIL = "conclusionemail_w_dv5";
      public const string GENERATE_QC = "generateqc_w_dv3";
      public const string CHARGE_ACCOUNT_VIA_HANGFIRE = "chargeaccount_w_dv6";

      public const string INVOICE_ACCOUNT_VIA_HANGFIRE = "invoice_account_via_hangfire_dv7";

      public const string EXECUTE_TASKS = "executetasks_w_dv3";
      public const string EXECUTE_TASK = "executetasks_w_dv3";
      public const string DAILY_TASKS = "dailytasks_w_dv2";
      public const string SCHEDULED_QUARTERLY_EMAIL = "scheduledquarterlyemail_w_dv4";
      public const string NOTIFY_MEETING_START = "notifymeetingstart_w_dv2";
      public const string EXECUTE_EVENT_ANALYZERS = "executeeventanalyzers_w_dv2";
      public const string GENERATE_ALL_DAILY_EVENTS = "generate_all_daily_events_w_dv3";
      public const string DAILY_ORGANIZATION_EVENTS = "daily_organization_events_w_dv2";
      public const string EMAILER = "emailer_w_dv2";
      public const string REMINDER_EMAILS = "reminder_emails_w_dv4";
      public const string SEND_SLACK_MESSAGE = "send_slack_message_w_dv2";

      public const string ASANA_EVENTS = "asana_w_dv1";
      public const string GENERATE_SCORECARD = "generate_scorecard_w_dv3";
      public const string GENERATE_USER_SCORECARD = "generate_user_scorecard_w_dv3";

      public const string ZAPIER_SEND_INTERNAL = "zapier_send_internal_w_dv2";
      public const string FIRE_NOTIFICATION = "fire_notification_w_dv2";
      public const string AUTOGENERATE_NOTIFICATIONS = "autogenerate_notifications_w_dv2";

      public const string CLOSE_MEETING = "close_meeting_dv3";

      public const string ZAPIER_EVENTS = "zapier_w_dv4";
      public const string AUDIO = "audio_w_dv5";
      public const string TEMPORARY_FILES = "temporary_files_w_dv4";

      public const string ALPHA = "alpha_w_dv2";

      public const string GENERATE_VTO = "generate_vto_dv3";
      public const string GENERATE_QUARTERLY_PRINTOUT = "generate_quarterly_printout_dv5";

      public const string GENERATE_AC_PRINTOUT = "generate_ac_printout_dv3";

      public const string SAVE_WHITEBOARD = "save_whiteboard_dv3";
      public const string CRON = "cron_dv3";

      public const string MARK_VTO_UPDATED = "mark_vto_updated_v1";


      public const string MIGRATION = "migration_dv1";
    }


    public static readonly string[] OrderedQueues = new[]{
      Immediate.CRITICAL,
      Immediate.ETHERPAD,
      Immediate.EMAILER,
      Immediate.CONCLUSION_EMAIL,
      Immediate.GENERATE_QC,
      Immediate.CHARGE_ACCOUNT_VIA_HANGFIRE,
      Immediate.INVOICE_ACCOUNT_VIA_HANGFIRE,
      Immediate.EXECUTE_TASKS,
      Immediate.EXECUTE_TASK,
      Immediate.DAILY_TASKS,
      Immediate.NOTIFY_MEETING_START,
      Immediate.SCHEDULED_QUARTERLY_EMAIL,
      Immediate.EXECUTE_EVENT_ANALYZERS,
      Immediate.GENERATE_SCORECARD,
      Immediate.GENERATE_USER_SCORECARD,

      Immediate.CLOSE_MEETING,
      Immediate.ASANA_EVENTS,
      Immediate.REMINDER_EMAILS,
      Immediate.DAILY_ORGANIZATION_EVENTS,
      Immediate.GENERATE_ALL_DAILY_EVENTS,
      Immediate.SEND_SLACK_MESSAGE,
      Immediate.ZAPIER_SEND_INTERNAL,
      Immediate.FIRE_NOTIFICATION,
      Immediate.AUTOGENERATE_NOTIFICATIONS,
      Immediate.ZAPIER_EVENTS,
      Immediate.AUDIO,
      Immediate.TEMPORARY_FILES,
      Immediate.MARK_VTO_UPDATED,

      Immediate.GENERATE_VTO,
      Immediate.GENERATE_QUARTERLY_PRINTOUT,
      Immediate.GENERATE_AC_PRINTOUT,
      Immediate.SAVE_WHITEBOARD,

      Immediate.CRON,
      Immediate.MIGRATION,

      DEFAULT,
      Immediate.ALPHA
    };


    ///<summary>
    ///I think we want it to run the jobs even if they are (incorrectly) unmarked
    ///</summary>
    public const string DEFAULT = "default";
  }
}


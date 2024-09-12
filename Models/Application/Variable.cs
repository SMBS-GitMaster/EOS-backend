using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Utilities;
using System;

namespace RadialReview {
  public class Variable {

    public static class Names {
      //Do not change these strings!! They are DB constants
      public static readonly string LAST_CAMUNDA_UPDATE_TIME = "LAST_CAMUNDA_UPDATE_TIME";
      public static readonly string USER_RADIAL_DATA_IDS = "USER_RADIAL_DATA_IDS";
      public static readonly string CONSENT_MESSAGE = "CONSENT_MESSAGE";
      public static readonly string WHALE_CONSENT_MESSAGE = "WHALE_CONSENT_MESSAGE";
      public static readonly string PRIVACY_URL = "PRIVACY_URL";
      public static readonly string TOS_URL = "TOS_URL";
      public static readonly string DELINQUENT_MESSAGE_MEETING = "DELINQUENT_MESSAGE_MEETING";
      public static readonly string UPDATE_CARD_SUBJECT = "UPDATE_CARD_SUBJECT";
      public static readonly string TODO_DIVISOR = "TODO_DIVISOR";
      public static readonly string INJECTED_SCRIPTS = "INJECTED_SCRIPTS";
      public static readonly string INJECTED_SCRIPTS_GET_STARTED = "INJECTED_SCRIPTS_GET_STARTED";
      public static readonly string LOG_ERRORS = "LOG_ERRORS";
      public static readonly string LAYOUT_WEIGHTS = "LAYOUT_WEIGHTS";
      public static readonly string LAYOUT_SETTINGS = "LAYOUT_SETTINGS";
      public static readonly string EOSI_REFERRAL_EMAIL = "EOSI_REFERRAL_EMAIL";
      public static readonly string CLIENT_REFERRAL_EMAIL = "CLIENT_REFERRAL_EMAIL";
      public static readonly string JOIN_ORGANIZATION_UNDER_MANAGER_BODY = "JOIN_ORGANIZATION_UNDER_MANAGER_BODY";

      public static readonly string MEETING_HUB_SETTINGS = "MEETING_HUB_SETTINGS";
      public static readonly string SOFTWARE_VERSION = "SOFTWARE_VERSION";
      public static readonly string READ_ONLY_MODE = "READ_ONLY_MODE";

      public static readonly string SHOULD_AUTOGENERATE_NOTIFICATION = "SHOULD_AUTOGENERATE_NOTIFICATION";

      public static readonly string V2_LANDING_INJECT = "V2_LANDING_INJECT";
      public static readonly string V2_LANDING_VIDEOS = "V2_LANDING_VIDEOS";
      public static readonly string V2_LANDING_BOTTOMVIDEOS = "V2_LANDING_BOTTOMVIDEOS";
      public static readonly string V2_LOGIN_URL = "V2_LOGIN_URL";
      public static readonly string V2_LOGIN_REDIRECT = "V2_LOGIN_REDIRECT";

      public static readonly string V2_PORT_SCRIPT = "V2_PORT_SCRIPT";
      public static readonly string V2_MIGRATIONDONE_BODY = "V2_MIGRATIONDONE_BODY";

      public static readonly string CLOSE_MEETING_AFTER = "CLOSE_MEETING_AFTER";

      public static readonly string ISSUE_SUGGESTIONS = "ISSUE_SUGGESTIONS";
      public static readonly string SOFTWARE_UPDATES = "SOFTWARE_UPDATES";

      public static readonly string DEFFAULT_GET_STARTED_PARAMETERS = "DEFFAULT_GET_STARTED_PARAMETERS";
      public static readonly string ACCOUNT_CREATION_USER_ID = "ACCOUNT_CREATION_USER_ID";

      public static readonly string KB_URL = "KB_URL";
      public static readonly string MEETING_MODE_FORMATS = "MEETING_MODE_FORMATS";

      public static readonly string ZAPIER_ENABLED = "ZAPIER_ENABLED";

      public static readonly string RECORDING_URL = "RECORDING_URL";

      public static readonly string COACH_LINK_HTML = "COACH_LINK_HTML";
      public static readonly string COACH_LINKED_EMAIL = "COACH_LINKED_EMAIL";

      public static readonly string INTERNAL_META_ENDPOINT = "INTERNAL_META_ENDPOINT";

      public static readonly string NON_COACH_LINK_HTML = "NON_COACH_LINK_HTML";
      public static readonly string VERIFICATION_EMAIL_TEMPLATE = "VERIFICATION_EMAIL_TEMPLATE";

      public static readonly string TAXJAR_TRANSFORMER = "TAXJAR_TRANSFORMER";
      public static readonly string APPLY_SALESTAX = "APPLY_SALESTAX";

      public static readonly string READ_ONLY_MESSAGE = "READ_ONLY_MESSAGE";
      public static readonly string WHALE_IFRAME_SRC = "WHALE_IFRAME_SRC";
      public static readonly string WhaleIO_Flag = "WhaleIO_Flag";

      public static readonly string GET_STARTED_CONTROLLER_REDIRECT = "GET_STARTED_CONTROLLER_REDIRECT";
      public static readonly string GET_STARTED_PAGE_REDIRECT = "GET_STARTED_PAGE_REDIRECT";

      public static readonly string FOLDER_ROOT = "FOLDER_ROOT";
      public static readonly string FOLDER_MEETING = "FOLDER_MEETING";
      public static readonly string FOLDER_TRACTIONTOOLS = "FOLDER_TRACTIONTOOLS";
      public static readonly string FOLDER_ROOTCOMPANY = "FOLDER_ROOTCOMPANY";
      public static readonly string FOLDER_ROOTCOMPANYTEAM = "FOLDER_ROOTCOMPANYTEAM";

      public static readonly string FOLDER_EOSTOOLS = "FOLDER_EOSTOOLS";
      public static readonly string FOLDER_EOSTOOLS_FILES = "FOLDER_EOSTOOLS_FILES";
      public static readonly string FOLDER_EOSTOOLS_FILES_OVERRIDE_DISABLE = "FOLDER_EOSTOOLS_FILES_OVERRIDE";
      public static readonly string GET_STARTED_MESSAGES = "GET_STARTED_MESSAGES";

      public static readonly string V3_SHOW_FEATURES = "V3_SHOW_FEATURES";
      public static readonly string V3_BANNER_URL = "V3_BANNER_URL"; 
      public static readonly string V3_FEATURES_MODAL_URL = "V3_FEATURES_MODAL_URL";
      public static readonly string V3_STAR_VOTING_MODAL_URL = "V3_STAR_VOTING_MODAL_URL";
      public static readonly string V3_URL = "V3_URL";
      public static readonly string REALTIME_SETTINGS = "REALTIME_SETTINGS";


      //Marketing Form URL's
      public static readonly string ReferralFriendFormUrl = "REFERRAL_FRIEND_FORM_URL";
      public static readonly string ReferralCoachFormUrl = "REFERRAL_COACH_FORM_URL";
      public static readonly string DataRequestFormUrl = "DATA_REQUEST_FORM_URL";

      public static readonly string DATABASE_HASH = "DATABASE_HASH";

      public static readonly string SEND_EMAIL_IMMEDIATELY_DEFAULT = "SEND_EMAIL_IMMEDIATELY_DEFAULT";

      public static readonly string MAX_SYNC_KEYS = "MAX_SYNC_KEYS";

    }



    public virtual string K { get; set; }
    public virtual string V { get; set; }
    public virtual DateTime LastUpdate { get; set; }

    public Variable() {
      LastUpdate = DateTime.UtcNow;
    }

    public class Map : ClassMap<Variable> {
      public Map() {
        Id(x => x.K).GeneratedBy.Assigned();
        Map(x => x.V).Length(1024);
        Map(x => x.LastUpdate);
      }
    }
  }

}
namespace RadialReview.Variables {

  public class VariableAccessor {
    public static string Get(string key, Func<string> defaultValue) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var v = s.GetSettingOrDefault(key, () => defaultValue());
          tx.Commit();
          s.Flush();
          return v;
        }
      }
    }

    public static T Get<T>(string key, Func<T> defaultValue) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var v = s.GetSettingOrDefault(key, defaultValue);
          tx.Commit();
          s.Flush();
          return v;
        }
      }
    }

    public static T Get<T>(string key, T defaultValue) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var v = s.GetSettingOrDefault(key, () => defaultValue);
          tx.Commit();
          s.Flush();
          return v;
        }
      }
    }
    public static string Get(string key, string defaultValue) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var v = s.GetSettingOrDefault(key, defaultValue);
          tx.Commit();
          s.Flush();
          return v;
        }
      }
    }


  }

  public static class VariableExtensions {
    #region Session
    private static Variable _GetSettingOrDefault(this ISession s, string key, Func<string> defaultValue = null) {
      var found = s.Get<Variable>(key);
      if (found == null) {
        found = new Variable() {
          K = key,
          V = defaultValue == null ? null : defaultValue()
        };
        s.Save(found);
      }
      return found;
    }

    public static string GetSettingOrDefault(this ISession s, string key, Func<string> defaultValue = null) {
      return _GetSettingOrDefault(s, key, defaultValue).V;
    }
    public static string GetSettingOrDefault(this ISession s, string key, string defaultValue) {
      return _GetSettingOrDefault(s, key, () => defaultValue).V;
    }

    public static T GetSettingOrDefault<T>(this ISession s, string key, Func<T> defaultValue) {
      return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue(), Formatting.Indented)).V);
    }
    public static T GetSettingOrDefault<T>(this ISession s, string key, T defaultValue) {
      return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue, Formatting.Indented)).V);
    }
    public static Variable UpdateSetting<T>(this ISession s, string key, T newValue) {
      return UpdateSetting(s, key, JsonConvert.SerializeObject(newValue, Formatting.Indented));
    }
    public static Variable UpdateSetting(this ISession s, string key, string newValue) {
      var found = _GetSettingOrDefault(s, key, () => newValue);
      if (found.V != newValue) {
        found.V = newValue;
        found.LastUpdate = DateTime.UtcNow;
        s.Update(found);
      }
      return found;
    }
    #endregion
    #region StatelessSession
    private static Variable _GetSettingOrDefault(this IStatelessSession s, string key, Func<string> defaultValue = null) {
      var found = s.Get<Variable>(key);
      if (found == null) {
        found = new Variable() {
          K = key,
          V = defaultValue == null ? null : defaultValue()
        };
        s.Insert(found);
      }
      return found;
    }
    public static T GetSettingOrDefault<T>(this IStatelessSession s, string key, T defaultValue) {
      return JsonConvert.DeserializeObject<T>(_GetSettingOrDefault(s, key, () => JsonConvert.SerializeObject(defaultValue)).V);
    }
    #endregion
  }
}

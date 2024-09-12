using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Request.HttpContextExtensions.Prefetch;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public class SettingsAccessor {
    private static AmazonDynamoDBClient client = new AmazonDynamoDBClient(RegionEndpoint.USWest2);

    public class SettingsKey {
      private SettingsKey(string key) {
        Key = key;
      }

      private string Key { get; set; }

      public static SettingsKey DatabaseUpdate(string applicationVersion, string method) {
        return new SettingsKey("DatabaseUpdate-" + applicationVersion + "-" + method);
      }

      public override string ToString() {
        return Key;
      }
    }

    public static async Task<string> GetProductionSetting(SettingsKey key) {
      Table settings = Table.LoadTable(client, "TT-Settings");
      GetItemOperationConfig config = new GetItemOperationConfig {
        AttributesToGet = new List<string> { "Key", "Value" },
        ConsistentRead = true
      };
      Document document = await settings.GetItemAsync(key.ToString(), config);
      if (document == null) {
        return null;
      }
      return document["Value"].AsString();
    }

    public static async Task SetProductionSetting(SettingsKey key, string value) {
      Table settings = Table.LoadTable(client, "TT-Settings");
      var item = new Document();
      item["Key"] = key.ToString();
      item["Value"] = value;
      await settings.PutItemAsync(item);
    }

    private static string GetRealtimeSettings()
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var settings = s.GetSettingOrDefault("REALTIME_SETTINGS", "{\"WebSockets\":1}");
          tx.Commit();
          s.Flush();
          return settings;
        }
      }
    }

    public static SettingsViewModel GenerateViewSettings(UserOrganizationModel oneUser, string nameStr, bool isManager, bool superAdmin, PrefetchData prefetchData, TermsCollection terms) {
      var settings = new SettingsViewModel();
      var isEosw = true;

      try {
        settings = new SettingsViewModel() {
          user = new SettingsViewModel.User() {
            isOrganizationAdmin = oneUser.NotNull(x => x.ManagingOrganization),
            userName = oneUser.NotNull(x => x.GetEmail()),
            userId = oneUser.NotNull(x => x.Id),
            isSupervisor = isManager,
            isSuperAdmin = superAdmin,
            name = nameStr,
          },
        };
      } catch (Exception e) {
      }
      try {
        if (oneUser != null && oneUser.Organization != null) {
          isEosw = oneUser.Organization.Id.IsEOSW();

          settings.organization = new SettingsViewModel.Org() {
            name = oneUser.Organization.GetName(),
            organizationId = oneUser.Organization.Id,
            accountType = Enum.GetName(typeof(AccountType), oneUser.Organization.AccountType),
            eosw = isEosw,
          };
        }
      } catch (Exception) {
      }
      try {
        var signalrEndpoint = Config.GetSignalrEndpoint();
        settings.signalr.endpoint_count = signalrEndpoint.EndpointCount;
        settings.signalr.endpoint_pattern = signalrEndpoint.EndpointPattern;
        settings.signalr.settings = GetRealtimeSettings();
      } catch (Exception) {
      }

      try {
        if (prefetchData != null) {
          settings.service.knowledgebase_url = prefetchData.KnowledgeBaseUrl;
        }
      } catch (Exception) {
      }

      try {
        settings.flags.enable_shotclock = !isEosw;
      } catch (Exception) {
      }

      try {
        settings.localization = new SettingsViewModel.Localization() {
          lang = terms.LanguageCode,
          terms = terms.Terms,
        };
        if (oneUser!=null) {
          settings.localization.timezone_offset = oneUser.GetTimezoneOffset();
        }

      } catch (Exception) {
      }

      return settings;
    }

  }
}
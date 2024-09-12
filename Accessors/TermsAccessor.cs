using NHibernate;
using RadialReview.Core.Exceptions;
using RadialReview.Core.Models.Angular.Terms;
using RadialReview.Core.Models.Terms;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview;
using static RadialReview.Models.L10.L10Recurrence;
using System.Diagnostics;
using System.Text.RegularExpressions;
using RadialReview.Accessors;
using Newtonsoft.Json;
using RadialReview.Models.Application;
using System.Threading.Tasks;

namespace RadialReview.Core.Accessors {
  public class TermsAccessor {

    public static TermsCollection GetTermsCollection(UserOrganizationModel caller, long organizationId) {
      try {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var perms = PermissionsUtility.Create(s, caller);
            return GetTermsCollection(s, perms, organizationId);
          }
        }
      } catch (Exception e) {
        return TermsCollection.DEFAULT;
      }
    }

    public static TermsCollection GetTermsCollection(ISession s, PermissionsUtility perms, long organizationId) {
      try {
        perms.ViewOrganization(organizationId);
        return GetTermsCollection_Unsafe(s, organizationId);
      } catch (Exception e) {
        return TermsCollection.DEFAULT;
      }
    }

    public static TermsCollection GetTermsCollection_Unsafe(ISession s, long organizationId) {
      try {
        var termsId = s.Get<OrganizationModel>(organizationId).TermsId;
        var model = s.Get<TermsModel>(termsId);
        return TermsCollection.GetTerms(model);
      } catch (Exception e) {
        return TermsCollection.DEFAULT;
      }
    }

    public static void ResetTerms(UserOrganizationModel caller, long orgId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ManagingOrganization(orgId);
          var org = s.Get<OrganizationModel>(orgId);
          org.TermsId = null;
          s.Update(org);
          tx.Commit();
          s.Flush();
        }
      }
    }

    public class ProcessResults {
      public List<String> Errors { get; set; }
      public bool Success { get; set; }
      public long Ms { get; set; }
      public List<string> Unprocessed { get; set; }
      public int UpdateCount { get; set; }
    }

    public static ProcessResults ProcessUpdate(UserOrganizationModel caller, long orgId, Dictionary<string, string> data) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var sw = Stopwatch.StartNew();
          var perms = PermissionsUtility.Create(s, caller);

          var terms = GetOrCreateTermsModel(s, perms, orgId);

          var errors = new List<UploadException>();
          var used = new HashSet<string>();
          ProcessMeetingPages(s, perms, terms, data, errors, used);
          ProcessUpdateFutureMeeting(s, perms, terms, data, errors, used);
          ProcessOtherTerms(s, perms, terms, data, errors, used);
          ProcessLanguageCode(s, perms, terms, data, errors, used);

          var missing = SetUtility.AddRemove(data.Keys, used).RemovedValues;
          //if (missing.Any()) {
          //  errors.Add(new UploadException("Keys were not processed: " + string.Join(",", missing)));
          //}

          s.Update(terms);

          tx.Commit();
          s.Flush();

          return new ProcessResults {
            UpdateCount = used.Count(),
            Errors = errors.Select(x => x.Message).ToList(),
            Unprocessed = missing.ToList(),
            Success = !errors.Any(x => x.Status == UploadExpectionStatus.Error || x.Status == UploadExpectionStatus.Warning),
            Ms = sw.ElapsedMilliseconds
          };
        }
      }

    }

    private static TermsModel GetOrCreateTermsModel(ISession s, PermissionsUtility perms, long orgId) {
      perms.ManagingOrganization(orgId);
      var org = s.Get<OrganizationModel>(orgId);
      TermsModel terms;
      if (org.TermsId == null) {
        terms = new TermsModel() {
          OrgId = orgId,
          LanguageCode = "en-us"
        };
        s.Save(terms);
        org.TermsId = terms.Id;
        s.Update(org);
      } else {
        terms = s.Get<TermsModel>(org.TermsId);
      }

      return terms;
    }

    private static void ProcessLanguageCode(ISession s, PermissionsUtility perms, TermsModel terms, Dictionary<string, string> data, List<UploadException> errors, HashSet<string> used) {
      if (data.ContainsKey("lang")) {
        terms.LanguageCode = CleanTitle(data["lang"]);
        used.Add("lang");
      }
    }

    public static TermsExportFormat ExportTerms(UserOrganizationModel caller, long orgId) {
      PermissionsAccessor.EnsurePermitted(caller, x => x.ManagingOrganization(orgId));
      var terms = TermsAccessor.GetTermsCollection(caller, orgId);
      return new TermsExportFormat() {
        createdAt = DateTime.UtcNow.ToJsMs(),
        lang = terms.LanguageCode,
        terms = terms.Terms.Select(x => new TermsExportFormat.TermExportFormat() {
          key = x.KeyString,
          value = x.Value
        }).ToList(),
        version = 1
      };
    }

    [Obsolete("Do not use", true)]
    public static List<UploadException> ImportTerms(UserOrganizationModel caller, long orgId, string jsonContents) {
      PermissionsAccessor.EnsurePermitted(caller, x => x.ManagingOrganization(orgId));
      var obj = JsonConvert.DeserializeObject<TermsExportFormat>(jsonContents);
      var errors = new List<UploadException>();
      if (obj == null) {
        errors.Add(new UploadException(UploadExpectionStatus.Error, "File could not be read"));
        return errors;
      }
      if (obj.version!=1) {
        errors.Add(new UploadException(UploadExpectionStatus.Error, "File version is unsupported"));
        return errors;
      }
      if (obj.lang == null && obj.terms.Count == 0) {
        errors.Add(new UploadException(UploadExpectionStatus.Warning, "File contains no modifications."));
        return errors;
      }


      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ManagingOrganization(orgId);
          TermsModel terms = GetOrCreateTermsModel(s, perms, orgId);


          if (obj.lang!=null) {
            terms.LanguageCode = obj.lang;
          }

          if (obj.terms!=null) {
            foreach (var t in obj.terms) {
              try {
                var k = t.key.Parse<TermKey>();
                TermKeyToTermsModel.SetValue(terms, k, t.value);
              } catch (Exception e) {
                errors.Add(new UploadException(UploadExpectionStatus.Warning, "Failed to set value for "+t.key));
              }
            }
          }

          tx.Commit();
          s.Flush();
          return errors;
        }
      }
    }

    public static TermsPageVM GetTermsPageVM(UserOrganizationModel caller, long orgId) {

      var terms = TermsAccessor.GetTermsCollection(caller, orgId);
      var meetings = L10Accessor.GetAllL10RecurrenceAtOrganization(caller,orgId);

      var meetingNameIds = meetings.Select(x => {
        var name = string.IsNullOrWhiteSpace(x.Name) ? "-unnamed-" : x.Name;
        return new NameId(name, x.Id);
      }).ToList();

      return new TermsPageVM() {
        TermCollection = terms,
        Meetings = meetingNameIds
      };
    }

    public static TermsPageVM GetImportTermsPageVM(UserOrganizationModel caller,long orgId, string jsonContents) {
      var obj = JsonConvert.DeserializeObject<TermsExportFormat>(jsonContents);
      var errors = new List<UploadException>();
      if (obj == null) {
        throw new UploadException(UploadExpectionStatus.Error, "File could not be read");
      }
      if (obj.version!=1) {
        throw new UploadException(UploadExpectionStatus.Error, "File version is unsupported");
      }
      if (obj.lang == null && obj.terms.Count == 0) {
        throw new UploadException(UploadExpectionStatus.Warning, "File contains no modifications.");
      }

      var res = GetTermsPageVM(caller, orgId);
      res.ImportMode = true;

      //if (obj.lang!=null) {
      //  terms.LanguageCode = obj.lang;
      //}

      if (obj.terms!=null) {
        foreach (var t in obj.terms) {
          try {
            var k = t.key.Parse<TermKey>();
            res.ImportValues[k] = CleanTitle(t.value);
          } catch (Exception e) {
            errors.Add(new UploadException(UploadExpectionStatus.Warning, "Failed to set value for "+t.key));
          }
        }
      }
      return res;

    }

    public static void TryApplyTermsPluginByCode(UserOrganizationModel caller, long organizationId, string referralSource, string referralCode)
    {
      try
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            var perms = PermissionsUtility.Create(s, caller);

            perms.ManagingOrganization(organizationId);
            var referralPlugin = s.Get<ReferralPluginModel>(new ReferralPluginIdentifier { ReferralSource = referralSource, ReferralCode = referralCode });

            if (referralPlugin == null)
              return;

            var termsPlugin = s.Get<TermsPluginModel>(referralPlugin.TermsPluginId);
            var terms = GetOrCreateTermsModel(s, perms, organizationId);
            termsPlugin.ApplyToTermsModel(terms);
            terms.TermsPluginId = termsPlugin.Id;

            s.Update(terms);

            tx.Commit();
            s.Flush();
          }
        }

      }
      catch (Exception e)
      {
        //We want this to silently fail
      }
    }

    public static async Task ApplyTermsPlugin(UserOrganizationModel caller, long organizationId, long termsPluginId)
    {
      try
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            var perms = PermissionsUtility.Create(s, caller);

            perms.ManagingOrganization(organizationId);
            var termsPlugin = s.Get<TermsPluginModel>(termsPluginId);
            var terms = GetOrCreateTermsModel(s, perms, organizationId);
            termsPlugin.ApplyToTermsModel(terms);

            s.Update(terms);

            tx.Commit();
            s.Flush();
          }
        }

      }
      catch (Exception e)
      {

      }
    }

    private static void ProcessOtherTerms(ISession s, PermissionsUtility perms, TermsModel terms, Dictionary<string, string> data, List<UploadException> errors, HashSet<string> used) {
      var mItems = data.Where(x => x.Key.StartsWith("std-"))
        .SelectNoException(x => new {
          term = x.Key.Split("-")[1],
          title = CleanTitle(x.Value),
          key = x.Key
        });


      foreach (var m in mItems) {
        try {
          terms[m.term.Parse<TermKey>()] = m.title;
          used.Add(m.key);
        } catch (Exception e) {
          errors.Add(new UploadException(UploadExpectionStatus.Error, "Failed to set " + m.term));
        }
      }

    }

    private static void ProcessUpdateFutureMeeting(ISession s, PermissionsUtility perms, TermsModel terms, Dictionary<string, string> data, List<UploadException> errors, HashSet<string> used) {
      var mItems = data.Where(x => x.Key.StartsWith("applyfuture-") && x.Value == "true")
        .SelectNoException(x => new {
          term = x.Key.Split("-")[1],
          title = CleanTitle(data[x.Key.Split("-")[1]]),
          applyFutureKey = x.Key,
          valueKey = x.Key.Split("-")[1]
        });


      foreach (var m in mItems) {
        try {
          terms[m.term.Parse<TermKey>()] = m.title;
          used.Add(m.applyFutureKey);
          used.Add(m.valueKey);
        } catch (Exception e) {
          errors.Add(new UploadException(UploadExpectionStatus.Error, "Failed to set " + m.term));
        }
      }
    }

    private static void ProcessMeetingPages(ISession s, PermissionsUtility perms, TermsModel terms, Dictionary<string, string> data, List<UploadException> errors, HashSet<string> used) {
      var mItems = data.Where(x => x.Key.StartsWith("meeting-") && x.Value == "true").Select(x => new {
        key = x.Key,
        term = x.Key.Split("-")[1],
        recurId = long.Parse(x.Key.Split("-")[2]),
        title = CleanTitle(data[x.Key.Split("-")[1]])
      });

      var keyMatch = new Dictionary<string, L10PageType>();
      keyMatch["" + TermKey.CheckIn] = L10PageType.Segue;
      keyMatch["" + TermKey.Metrics] = L10PageType.Scorecard;
      keyMatch["" + TermKey.Goals] = L10PageType.Rocks;
      keyMatch["" + TermKey.Headlines] = L10PageType.Headlines;
      keyMatch["" + TermKey.ToDos] = L10PageType.Todo;
      keyMatch["" + TermKey.Issues] = L10PageType.IDS;
      keyMatch["" + TermKey.WrapUp] = L10PageType.Conclude;

      foreach (var m in mItems) {
        try {
          perms.AdminL10Recurrence(m.recurId);

          if (keyMatch.ContainsKey(m.term)) {
            var pages = s.QueryOver<L10Recurrence_Page>()
              .Where(x => x.DeleteTime == null && x.PageType == keyMatch[m.term] && x.L10RecurrenceId == m.recurId)
              .List().ToList();

            if (!pages.Any())
              errors.Add(new UploadException(UploadExpectionStatus.Info, "Info: A meeting (" + m.recurId + ") did not have a " + m.title + " page."));

            foreach (var p in pages) {
              p.Title = m.title;
              s.Update(p);
            }
            used.Add(m.key);
          } else {
            errors.Add(new UploadException(UploadExpectionStatus.Error, "Unhandled meeting page:" + m.term));
          }
        } catch (Exception e) {
          errors.Add(new UploadException(UploadExpectionStatus.Error, "Error updating meeting (" + m.recurId + ")  page " + m.term));
        }
      }
    }

    //Excludes < > ' : " 
    private static Regex FilterRegex = new Regex("[^a-zA-Z0-9_#%\\s^&\\*\\(\\)\\+!\\@\\${}|\\[\\]\\\\\\?\\.\\,;\\-\\=\\:]");

    private static string CleanTitle(string title) {
      var replaced = FilterRegex.Replace(title, String.Empty).Trim();
      if (title.Trim().Length != 0 && replaced.Length == 0) {
        return "--invalid title--";
      }
      return replaced;

    }
  }
}

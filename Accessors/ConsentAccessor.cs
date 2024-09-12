using NHibernate;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public class ConsentAccessor {

    const string DEFAULT_CONSENT_MESSAGE = "To create your experience, we record information that you or your organization has supplied. This information includes your name, profile picture, phone number, and corporate email address. " +
            "<br/>We use this information for your login and to send you meeting summaries, to-dos, feature updates, best practices, and other information relevant to your use of Bloom Growth (you can shut these off). " +
            "<br/>Bloom Growth uses cookies to store your login. We need to collect your billing address. We log all IP addresses to protect the service from non-user actors. " +
            "<br/>It should go without saying: We will never give away or sell your data. Your data is yours. " +
            "<br/><br/>Please see our <a href='/privacy'>Privacy Policy</a> and <a href='/tos'>Terms of Service </a> for a complete list of your data privacy rights." +
            "<br/><br/>Our service can't work without this data. So in order to use Bloom Growth you must agree to us storing it.";


    public class ConsentMessage {
      public string Message { get; set; }
      public string Guid { get; set; }
    }

    public static ConsentMessage GetConsentMessage(UserOrganizationModel caller) {
      using(var s = HibernateSession.GetCurrentSession()) {
        using(var tx = s.BeginTransaction()) {
          string message;
          TermsVM terms;
          GetConsentMessage(caller.User, s, out message, out terms);
          tx.Commit();
          s.Flush();
          return new ConsentMessage {
            Message = message,
            Guid = terms.Id
          };
        }
      }
    }

    public static ConsentModel GetConsentMessage(UserModel callerUser, ISession s, out string message, out TermsVM terms) {
      var found = s.QueryOver<ConsentModel>().Where(x => x.UserId == callerUser.Id).Take(1).SingleOrDefault();

      if(found == null) {
        found = new ConsentModel() {
          UserId = callerUser.Id,
        };
        s.Save(found);
      }

      message = s.GetSettingOrDefault(Variable.Names.CONSENT_MESSAGE, DEFAULT_CONSENT_MESSAGE);
      terms = LegalAccessor.CreateTerms(s, callerUser, message, Variable.Names.CONSENT_MESSAGE);
      return found;
    }

    public static ConsentMessage GetConsentMessage() {
      using(var s = HibernateSession.GetCurrentSession()) {
        var message = s.GetSettingOrDefault(Variable.Names.CONSENT_MESSAGE, DEFAULT_CONSENT_MESSAGE);

        return new ConsentMessage {
          Message = message
        };
      }
    }
    public static ConsentMessage GetWhaleConsentMessage(UserOrganizationModel caller) {
      using(var s = HibernateSession.GetCurrentSession()) {
        using(var tx = s.BeginTransaction()) {

          var found = s.QueryOver<ConsentModel>().Where(x => x.UserId == caller.User.Id).Take(1).SingleOrDefault();

          if(found == null) {
            found = new ConsentModel() {
              UserId = caller.User.Id,
            };
            s.Save(found);
          }

          var message = s.GetSettingOrDefault(Variable.Names.WHALE_CONSENT_MESSAGE, DEFAULT_CONSENT_MESSAGE);

          var terms = LegalAccessor.CreateTerms(s, caller.User, message, Variable.Names.WHALE_CONSENT_MESSAGE);
          tx.Commit();
          s.Flush();
          return new ConsentMessage {
            Message = message,
            Guid = terms.Id
          };
        }
      }
    }

    public static async Task ApplyConsent(UserOrganizationModel caller, Guid termsId, bool affirmative) {
      using(var s = HibernateSession.GetCurrentSession()) {
        using(var tx = s.BeginTransaction()) {

          var found = s.QueryOver<ConsentModel>().Where(x => x.UserId == caller.User.Id).Take(1).SingleOrDefault();

          if(found == null) {
            found = new ConsentModel() { UserId = caller.User.Id };
            s.Save(found);
          }
          await ApplyConsent(caller.User, termsId, affirmative, s, found);

          tx.Commit();
          s.Flush();


        }
      }
    }

    public static async Task ApplyConsent(UserModel callerUser, Guid termsId, bool affirmative, ISession s, ConsentModel found) {
      if(affirmative) {
        found.ConsentTime = DateTime.UtcNow;
        found.DenyTime = null;
      } else {
        found.DenyTime = DateTime.UtcNow;
      }
      s.Update(found);

      await LegalAccessor.SubmitTerms(s, callerUser, termsId, affirmative);
    }

    public static bool HasConsented(UserOrganizationModel caller) {

      if(caller.User == null) {
        //hack for when the User is null
        return true;
      }

      using(var s = HibernateSession.GetCurrentSession()) {
        using(var tx = s.BeginTransaction()) {
          var found = s.QueryOver<ConsentModel>().Where(x => x.UserId == caller.User.Id && x.ConsentTime != null).Take(1).SingleOrDefault();
          return found != null;
        }
      }
    }
  }
}

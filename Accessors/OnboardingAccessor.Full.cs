using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Onboard;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Accessors.AccountabilityAccessor;
using static RadialReview.Accessors.TeamAccessor;
using static RadialReview.Utilities.PaymentSpringUtil;

namespace RadialReview.Accessors {
  public partial class OnboardingAccessor {

    public static async Task<FullSelfOnboardResult> FullSelfOnboard(Controller ctrl, UserManager<UserModel> userManager, GetStartedDTO data) {
      UserModel user;
      OnboardingUser onboardingUser;
      DefaultOrg deflt;
      long onboardingUserId;
      var now = DateTime.UtcNow;
      var request = ctrl.Request;
      var selfOnboarding = true;


      //OPEN A NEW SESSION.
      using(var s = HibernateSession.GetCurrentSession(false)) {
        using(var tx = s.BeginTransaction()) {
          //Create Onboarding User
          var ou = await _CreateOnboardingUser(data, request, s);
          onboardingUserId = ou.Id;

          //Create UserModel
          user = await _CreateUserModel(userManager, data);

          //Get default settings
          deflt = s.GetSettingOrDefault(Variable.Names.DEFFAULT_GET_STARTED_PARAMETERS, () => new DefaultOrg(true));

          tx.Commit();
          s.Flush();
        }

        //Create organization (has its own transactions)
        var orgResult = await _CreateOrganization(data, user, deflt, now, selfOnboarding, s);

        //Update OnboardingUser
        _UpdateOnboardingUser(onboardingUserId, s, orgResult);

        //Consent
        await _ApplyConsent(user, s);

        onboardingUser = _RegisterOrganization(onboardingUserId, now, s, orgResult);

      }

      return new FullSelfOnboardResult() {
        User = user,
        OnboardingUser = onboardingUser
      };
    }

    private static OnboardingUser _RegisterOrganization(long onboardingUserId, DateTime now, global::NHibernate.ISession s, OrganizationAccessor.CreateOrganizationOutput orgResult) {
      OnboardingUser onboardingUser;
      using(var tx = s.BeginTransaction()) {
        var org = s.Get<OrganizationModel>(orgResult.organization.Id);
        onboardingUser = s.Get<OnboardingUser>(onboardingUserId);

        if(org.DeleteTime == new DateTime(1, 1, 1)) {
          org.DeleteTime = null;
          s.Update(org);
          onboardingUser.DeleteTime = now;
          s.Update(onboardingUser);
        }

        tx.Commit();
        s.Flush();
      }

      return onboardingUser;
    }

    private static async Task _ApplyConsent(UserModel user, global::NHibernate.ISession s) {
      using(var tx = s.BeginTransaction()) {
        string _message;
        TermsVM _terms;
        var consentModel = ConsentAccessor.GetConsentMessage(user, s, out _message, out _terms);
        await ConsentAccessor.ApplyConsent(user, Guid.Parse(_terms.Id), true, s, consentModel);

        tx.Commit();
        s.Flush();
      }
    }

    private static void _UpdateOnboardingUser(long onboardingUserId, global::NHibernate.ISession s, OrganizationAccessor.CreateOrganizationOutput orgResult) {
      using(var tx = s.BeginTransaction()) {
        var onboardingUser = s.Get<OnboardingUser>(onboardingUserId);
        onboardingUser.OrganizationId = orgResult.organization.Id;
        onboardingUser.UserId = orgResult.NewUser.Id;
        onboardingUser.CurrentPage = "Registered";
        s.Update(onboardingUser);
        tx.Commit();
        s.Flush();
      }
    }

    private static async Task<OrganizationAccessor.CreateOrganizationOutput> _CreateOrganization(GetStartedDTO data, UserModel user, DefaultOrg deflt, DateTime now, bool selfOnboarding, global::NHibernate.ISession s) {
      //Create Organization
      OrganizationAccessor.CreateOrganizationOutput result;
      EosUserType guessUserType = EosUserType.Unknown;//GuessEosUserType(data.Position, deflt);
      var dataOrg = new OrgCreationData() {
        Name = data.CompanyName,
        StartDeactivated = true,
        AccountType = AccountType.Demo,
        HasCoach = (!string.IsNullOrWhiteSpace(data.ImplementerName) || data.HaveImplementer == true) ? HasCoach.Yes : ((data.HaveImplementer == false) ? HasCoach.No : HasCoach.Unknown),
        ContactFN = data.FirstName,
        ContactLN = data.LastName,
        ContactEmail = data.Email,
        EnableL10 = deflt.EnableL10,
        EnableAC = deflt.EnablelAC,
        EnablePeople = deflt.EnablePeopleTools,
        EnableProcess = deflt.EnableProcess,
        EnableReview = deflt.EnableOldReview,
        EnableDocs = deflt.EnableDocs,
        ReferralSource = data.ReferralSource,
        ReferralData = data.ReferralCode,
        ContactEosUserType = guessUserType,
        AssignedTo = null,
        CoachId = null,
        ContactPosition = "",//data.Position,
        EnableZapier = deflt.EnableZapier,
        EosStartDate = null,//onboardingUser.EosStartTime,
        EnableWhale = false,
        TrialEnd = DateTime.UtcNow.AddDays(Math.Max(0, deflt.TrialDays))
      };
      var paymentPlanType = PaymentAccessor.GetPlanType("professional");
      using(var usersToUpdate = new UserCacheUpdater()) {
        result = await OrganizationAccessor.CreateOrganization(s, user, paymentPlanType, now, dataOrg, selfOnboarding, usersToUpdate);
        s.Flush();
        return result;
      }

    }

    private static async Task<UserModel> _CreateUserModel(UserManager<UserModel> userManager, GetStartedDTO data) {
      UserModel user = new UserModel() {
        UserName = data.Email,
        FirstName = data.FirstName,
        LastName = data.LastName,
        EmailNotVerified = true,
        _TimezoneOffset = 0
      };
      var userResult = await UserAccessor.CreateUser(userManager, user, data.Password);
      if(!userResult.Succeeded) {
        string errorMessage = "";
        //This could be broken..
        foreach(string error in userResult.Errors.Select(x => x.Description)) {
          if(error.StartsWith("Username") && error.EndsWith("is already taken.") && await _passwordMatch(userManager, data.Email, data.Password)) {
            errorMessage += "An account with this email already exists. If you'd like a second organization using the same email, please contact support.";
          } else {
            errorMessage += error + " ";
          }
        }
        throw new PermissionsException(errorMessage);
      }
      return user;
    }
    private static async Task<OnboardingUser> _CreateOnboardingUser(GetStartedDTO data, HttpRequest request, global::NHibernate.ISession s) {
      OnboardingUser onboardingUser = new OnboardingUser() {
        ContactCompleteTime = DateTime.UtcNow,
        FirstName = data.FirstName,
        LastName = data.LastName,
        Email = data.Email,
        CompanyName = data.CompanyName,
        ImplementerName = data.ImplementerName,
        HaveImpl = data.HaveImplementer,
        ReferralSource = data.ReferralSource,
        Referral = data.Referral,
        Phone = data.Phone,
        ReferralCode = data.ReferralCode,
        CurrentPage = "Login",
        Agree = data.Agree,
        Guid = Guid.NewGuid().ToString(),
        StartTime = DateTime.UtcNow,
        UserAgent = request.NotNull(x => x.Headers[HeaderNames.UserAgent]),
        Languages = request.NotNull(x => x.Headers[HeaderNames.AcceptLanguage])
      };
      s.SaveOrUpdate(onboardingUser);
      await EventUtil.Trigger(x => x.Create(s, EventType.SignupStep, null, onboardingUser, onboardingUser.CurrentPage));
      return onboardingUser;
    }
    private static async Task<bool> _passwordMatch(UserManager<UserModel> userManager, string username, string password) {
      if(username == null || password == null)
        return false;
      var user = await userManager.FindByNameAsync(username.ToLower().Trim());
      return user != null && await userManager.CheckPasswordAsync(user, password);
    }
    private static EosUserType GuessEosUserType(string position, DefaultOrg deflt) {
      var types = deflt.GuessPairs;
      var stdPos = (position ?? "").ToLower().Trim();
      foreach(var t in types) {
        if(DistanceUtility.DamerauLevenshteinDistance(stdPos, t.Item1, deflt.GuessThreshold) <= (t.Item3 ?? deflt.MaxGuessDistance)) {
          return t.Item2;
        }
      }

      return EosUserType.Unknown;
    }
  }

  public class FullSelfOnboardResult {
    public FullSelfOnboardResult() {
    }

    public UserModel User { get; set; }
    public OnboardingUser OnboardingUser { get; set; }
  }
}

using RadialReview.Utilities.Hooks;
using System;
using NHibernate;
using RadialReview.Models;
using System.Threading.Tasks;
using static RadialReview.Accessors.PaymentAccessor;
using RadialReview.Utilities.DataTypes;
using RadialReview.Accessors.Payments;
using RadialReview.Accessors;
using RadialReview.Nhibernate;
using System.Linq;

namespace RadialReview.Crosscutting.Hooks {
  public class EnterpriseHook : ICreateUserOrganizationHook, IDeleteUserOrganizationHook {
    private int EnterpriseGreaterThanUsers;

    public HookPriority GetHookPriority() {
      return HookPriority.Database;
    }
    public bool AbsorbErrors() {
      return false;
    }

    public bool CanRunRemotely() {
      return true;
    }

    public EnterpriseHook(int greaterThanN) {
      EnterpriseGreaterThanUsers = greaterThanN;
    }

    public async Task CreateUserOrganization(ISession s, UserOrganizationModel user, CreateUserOrganizationData data) {
      //data will be null since other methods call this one.
      await UpdateCalculation(s, user);
    }

    private async Task UpdateCalculation(ISession s, UserOrganizationModel user) {
      var calcOrg = UserCount(s, user);
      //Is null when autocalculate is off
      if (calcOrg != null) {
        var calc = calcOrg.Item1;
        var org = calcOrg.Item2;
        if (calc.GetMeetingUsers().Count() >= EnterpriseGreaterThanUsers + 1) {
          //calc.Plan.BaselinePrice = 499m;
          //calc.Plan.FirstNUsersFree = 45;
          //calc.Plan.MeetingPricePerPerson = 2m;
          //org.PaymentPlan = calc.Plan;
          var p = org.PaymentPlan;
          var _ = p.Description;
          p = s.Unproxy(p);
          var pp = p as PaymentPlan_Monthly;
          if (pp != null) {
            pp.BaselinePrice = 499m;
            pp.FirstN_Users_Free = 45;
            pp.L10PricePerPerson = 2m;
            org.PaymentPlan = pp;
            s.Update(org);
          }
        }
      }
    }

    public async Task UndeleteUser(ISession s, UserOrganizationModel user, DateTime deleteTime) {
      await UpdateCalculation(s, user);
    }
    public async Task DeleteUser(ISession s, UserOrganizationModel user, DateTime deleteTime) {
      var calcOrg = UserCount(s, user);
      //Is null when autocalculate is off
      if (calcOrg != null) {
        var calc = calcOrg.Item1;
        var org = calcOrg.Item2;
        if (calc.GetMeetingUsers().Count() <= EnterpriseGreaterThanUsers) {
          //calc.Plan.BaselinePrice = 149m;
          //calc.Plan.FirstNUsersFree = 10;
          //calc.Plan.MeetingPricePerPerson = 10m;
          //org.PaymentPlan = calc.Plan;
          var p = org.PaymentPlan;
          var _ = p.Description;
          p = s.Unproxy(p);
          var pp = p as PaymentPlan_Monthly;
          if (pp != null) {

            pp.BaselinePrice = 149m;
            pp.FirstN_Users_Free = 10;
            pp.L10PricePerPerson = 10m;
            org.PaymentPlan = pp;
          }
          s.Update(org);
        }
      }
    }
    public async Task OnUserOrganizationAttach(ISession s, UserOrganizationModel user, OnUserOrganizationAttachData data) {
      await UpdateCalculation(s, user);
    }
    public async Task OnUserRegister(ISession s, UserModel user, OnUserRegisterData data) {
      //Do nothing.
    }


    public Tuple<UserCalculator, OrganizationModel> UserCount(ISession s, UserOrganizationModel user) {
      if (user.Organization.Settings.AutoUpgradePayment) {
        var orgId = user.Organization.Id;
        var org = s.Get<OrganizationModel>(orgId);
        var paymentPlan = org.PaymentPlan;

        var _ = paymentPlan.Description;
        paymentPlan = s.Unproxy(paymentPlan);

        //var paymentPlan = s.GetSessionImplementation().PersistenceContext.Unproxy(org.PaymentPlan) as PaymentPlan_Monthly;

        var qc = PaymentAccessor.GetUQUsersForOrganization_Unsafe(s, orgId, org.PaymentPlan, new DateRange(DateTime.MaxValue, DateTime.MaxValue));
        var simplePlan = new SimplePlan(orgId, org.GetName(), org.AccountType, DateTime.MaxValue, (PaymentPlan_Monthly)paymentPlan, org.Settings.EnableL10, org.Settings.EnablePeople);
        var calc = new UserCalculator(qc, simplePlan, new Dedup());
        return Tuple.Create(calc, org);
      }
      return null;
    }
  }
}

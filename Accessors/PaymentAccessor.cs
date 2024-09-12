using Hangfire;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Payments;
using RadialReview.Models.Tasks;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.Calculators;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Extensions;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static RadialReview.Utilities.PaymentSpringUtil;
using RadialReview.Nhibernate;
using RadialReview.Hangfire.Activator;
using Microsoft.Extensions.Logging;
using Hangfire.Batches;
using Hangfire.States;
using EmailStrings = RadialReview.Core.Properties.EmailStrings;
using RadialReview.Core.Accessors;
using RadialReview.Accessors.Payments;
using RadialReview.Models.UserModels;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;

namespace RadialReview.Accessors {

  public class ChargeMetaData {
    public string Guid { get; set; }
    public bool IsTest { get; set; }
    public long OrganizationId { get; set; }
    public long TaskId { get; set; }
    public bool SendReceipt { get; set; }
    public DateTime ServiceStart { get; set; }
    public bool FirstAttempt { get; set; }
    public string BatchId { get; set; }

    [Obsolete("do not use")]
    public ChargeMetaData() {
      FirstAttempt = true;
    }

    public ChargeMetaData(string guid, bool isTest, long organizationId, long taskId, bool sendReceipt, DateTime serviceStart, bool firstAttempt) {
      Guid = guid;
      IsTest = isTest;
      OrganizationId = organizationId;
      TaskId = taskId;
      SendReceipt = sendReceipt;
      ServiceStart = serviceStart;
      FirstAttempt = firstAttempt;
    }
  }

  public partial class PaymentAccessor : BaseAccessor {

    /// <summary>
    /// Returns true if delinquent by certain number of days
    /// </summary>
    /// <param name="caller"></param>
    /// <param name="orgId"></param>
    /// <param name="daysOverdue"></param>
    /// <returns></returns>
    public static bool ShowDelinquent(UserOrganizationModel caller, long orgId, int daysOverdue) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          try {
            perms.EditCompanyPayment(orgId);
            var cards = s.QueryOver<PaymentSpringsToken>()
              .Where(x => x.OrganizationId == orgId && x.DeleteTime == null && x.Active == true)
              .List().ToList();

            if (!cards.Any()) {
              var org = s.Get<OrganizationModel>(orgId);
              if (org.AccountType != AccountType.Demo) {
                return false;
              }

              if (org == null) {
                throw new NullReferenceException("Organization does not exist");
              }

              if (org.DeleteTime != null) {
                throw new FallthroughException("Organization was deleted.");
              }

              var plan = org.PaymentPlan;
              if (plan.FreeUntil.AddDays(daysOverdue) < DateTime.UtcNow) {
                return true;
              }
            }
            return false;
          } catch (Exception) {
            return false;
          }
        }
      }
    }

    #region Payment Information
    public static List<PaymentMethodVM> GetCards(UserOrganizationModel caller, long organizationId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          if (!caller.User.IsRadialAdmin && organizationId != caller.Organization.Id) {
            throw new PermissionsException("Organization Ids do not match");
          }

          PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
          var cards = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.DeleteTime == null).List().ToList();
          return cards.Select(x => new PaymentMethodVM(x)).ToList();
        }
      }
    }

    public static async Task<PaymentMethodVM> SetCard(UserOrganizationModel caller, long orgId, PaymentTokenVM token) {
      return await SetCard(caller, orgId, token.id, token.@class, token.card_type, token.card_owner_name, token.last_4, token.card_exp_month, token.card_exp_year, null, null, null, null, null, null, null, null, null, true);
    }

    public static async Task<PaymentMethodVM> SetACH(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
      string token_type, string account_type, string firstName, string lastName, string accountLast4, string routingNumber, String address_1, String address_2,
      String city, String state, string zip, string phone, string website, string country, string email, bool active) {
      if (token_type != "bank_account") {
        throw new PermissionsException("ACH requires token_type = 'bank_account'");
      }

      return await SetToken(caller, organizationId, tokenId, @class, null, null, null, 0, 0, address_1, address_2, city, state, zip, phone, website, country, email, active, accountLast4, routingNumber, firstName, lastName, account_type, PaymentSpringTokenType.BankAccount);
    }

    public static async Task<PaymentMethodVM> SetCard(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
      string cardType, string cardOwnerName, string last4, int expireMonth, int expireYear, String address_1, String address_2,
      String city, String state, string zip, string phone, string website, string country, string email, bool active) {

      return await SetToken(caller, organizationId, tokenId, @class, cardType, cardOwnerName, last4, expireMonth, expireYear, address_1, address_2, city, state, zip, phone, website, country, email, active, null, null, null, null, null, PaymentSpringTokenType.CreditCard);

    }

    private static async Task<PaymentMethodVM> SetToken(UserOrganizationModel caller, long organizationId, string tokenId, string @class,
      string cardType, string cardOwnerName, string cardLast4, int cardExpireMonth, int cardExpireYear, String address_1, String address_2,
      String city, String state, string zip, string phone, string website, string country, string email, bool active,
      string bankLast4, string bankRouting, string bankFirstName, string bankLastName, string bankAccountType, PaymentSpringTokenType tokenType) {

      PaymentSpringsToken token;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          if (@class != "token") {
            throw new PermissionsException("Id must be a token");
          }

          if (String.IsNullOrWhiteSpace(tokenId)) {
            throw new PermissionsException("Token was empty");
          }

          if (organizationId != caller.Organization.Id) {
            throw new PermissionsException("Organization Ids do not match");
          }

          PermissionsUtility.Create(s, caller).EditCompanyPayment(organizationId);
          if (active) {
            var previous = s.QueryOver<PaymentSpringsToken>().Where(x => x.OrganizationId == organizationId && x.Active == true && x.DeleteTime == null).List().ToList();
            foreach (var p in previous) {
              p.Active = false;
              s.Update(p);
            }
          }

          //CURL
          var client = new HttpClient();

          var keys = new List<KeyValuePair<string, string>>();
          keys.Add(new KeyValuePair<string, string>("token", tokenId));
          keys.Add(new KeyValuePair<string, string>("first_name", caller.GetFirstName()));
          keys.Add(new KeyValuePair<string, string>("last_name", caller.GetLastName()));
          keys.Add(new KeyValuePair<string, string>("company", caller.Organization.GetName()));
          if (address_1 != null) {
            keys.Add(new KeyValuePair<string, string>("address_1", address_1));
          }

          if (address_2 != null) {
            keys.Add(new KeyValuePair<string, string>("address_2", address_2));
          }

          if (city != null) {
            keys.Add(new KeyValuePair<string, string>("city", city));
          }

          if (state != null) {
            keys.Add(new KeyValuePair<string, string>("state", state));
          }

          if (zip != null) {
            keys.Add(new KeyValuePair<string, string>("zip", zip));
          }

          if (phone != null) {
            keys.Add(new KeyValuePair<string, string>("phone", phone));
          }
          //if (fax != null)
          //    keys.Add(new KeyValuePair<string, string>("fax", fax));
          if (website != null) {
            keys.Add(new KeyValuePair<string, string>("website", website));
          }

          if (country != null) {
            keys.Add(new KeyValuePair<string, string>("country", country));
          }

          if (email != null) {
            keys.Add(new KeyValuePair<string, string>("email", email));
          }


          // Create the HttpContent for the form to be posted.
          var requestContent = new FormUrlEncodedContent(keys.ToArray());
          try {

            //Do not supress
            var privateApi = Config.PaymentSpring_PrivateKey();

            var byteArray = new UTF8Encoding().GetBytes(privateApi + ":");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

            //added
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            HttpResponseMessage response = await client.PostAsync("https://api.paymentspring.com/api/v1/customers", requestContent);
            HttpContent responseContent = response.Content;
            using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
              var result = await reader.ReadToEndAsync();
              if (JsonConvert.DeserializeObject<dynamic>(result).errors != null) {
                var builder = new List<string>();
                for (var i = 0; i < JsonConvert.DeserializeObject<dynamic>(result).errors.Length; i++) {
                  builder.Add(JsonConvert.DeserializeObject<dynamic>(result).errors[i].message + " (" + JsonConvert.DeserializeObject<dynamic>(result).errors[i].code + ").");
                }
                throw new PermissionsException(String.Join(" ", builder));
              }
              if (JsonConvert.DeserializeObject<dynamic>(result).@class != "customer") {
                throw new PermissionsException("Expected class: 'Customer'");
              }

              token = new PaymentSpringsToken() {
                CustomerToken = JsonConvert.DeserializeObject<dynamic>(result).id,
                CardLast4 = cardLast4,
                CardOwner = cardOwnerName,
                CardType = cardType,
                MonthExpire = cardExpireMonth,
                YearExpire = cardExpireYear,
                OrganizationId = organizationId,
                Active = active,
                ReceiptEmail = email,
                CreatedBy = caller.Id,

                TokenType = tokenType,
                BankAccountLast4 = bankLast4,
                BankRouting = bankRouting,
                BankFirstName = bankFirstName,
                BankLastName = bankLastName,
                BankAccountType = bankAccountType,

                Address_1 = address_1,
                Address_2 = address_2,
                City = city,
                State = state,
                Zip = zip,
                Phone = phone,
                Website = website,
                Country = country,

              };
              s.Save(token);
              tx.Commit();
              s.Flush();
            }
          } catch (Exception) {
            throw;
          }
        }
        using (var ss = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            await EventUtil.Trigger(x => x.Create(ss, EventType.PaymentEntered, caller, token, "Added " + tokenType));
            tx.Commit();
            s.Flush();
          }
        }
        using (var ss = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            await HooksRegistry.Each<IPaymentHook>((ses, x) => x.UpdateCard(ses, token));
            tx.Commit();
            s.Flush();
          }
        }

        return new PaymentMethodVM(token);
      }
    }

    public static IEnumerable<PaymentCredit> GetCredits(UserOrganizationModel caller, long organizationId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return GetCredits(s, perms, organizationId);
        }
      }
    }

    public static IEnumerable<PaymentCredit> GetCredits(ISession s, PermissionsUtility perms, long organizationId) {
      perms.CanView(PermItem.ResourceType.UpdatePaymentForOrganization, organizationId);
      return s.QueryOver<PaymentCredit>().Where(x => x.OrgId == organizationId && x.DeleteTime == null).List().ToList();
    }

    #endregion
    #region Charge
    public static async Task<string> EnqueueChargeOrganizationFromTask(long organizationId, long taskId, bool forceUseTest = false, bool sendReceipt = true, DateTime? executeTime = null) {
      string invoiceJobId = "not-started";
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var org = s.Get<OrganizationModel>(organizationId);
          if (org == null) {
            throw new NullReferenceException("Organization does not exist");
          }

          if (org.DeleteTime != null) {
            throw new FallthroughException("Organization was deleted.");
          }

          var plan = org.PaymentPlan;
          if (plan.Task == null) {
            throw new PermissionsException("Task was null.");
          }

          if (plan.Task.OriginalTaskId == 0) {
            throw new PermissionsException("PaymentPlan OriginalTaskId was 0.");
          }

          var task = s.Get<ScheduledTask>(taskId);
          if (task.Executed != null) {
            throw new PermissionsException("Task was already executed.");
          }

          if (task.DeleteTime != null) {
            throw new PermissionsException("Task was deleted.");
          }

          if (task.OriginalTaskId == 0) {
            throw new PermissionsException("ScheduledTask OriginalTaskId was 0.");
          }

          if (plan.Task.OriginalTaskId != task.OriginalTaskId) {
            throw new PermissionsException("ScheduledTask and PaymentPlan do not have the same task.");
          }

          if (task.Started == null) {
            throw new PermissionsException("Task was not started.");
          }

          executeTime = executeTime ?? DateTime.UtcNow.Date;
#pragma warning disable CS0618 // Type or member is obsolete

          var guid = "" + Guid.NewGuid();
          var data = new ChargeMetaData(guid, forceUseTest, organizationId, taskId, sendReceipt, executeTime.Value, true);

          invoiceJobId = new Unsafe().ScheduleInvoiceAndExecution(data);



          //invoiceJobId = Scheduler.Enqueue(() => new Unsafe().InvoiceViaHangfire(organizationId, taskId, forceUseTest, sendReceipt, executeTime.Value));
          //Scheduler.Advanced().ContinueJobWith(invoiceJobId, () => new Unsafe().ExecuteInvoiceViaHangfire());


#pragma warning restore CS0618 // Type or member is obsolete
        }
      }
      return invoiceJobId;
    }

    #endregion

    #region Invoicing/Calculators
    public static async Task<InvoiceModel> CreateInvoice(ISession s, OrganizationModel org, string guid, DateRange serviceRange, ItemizedCharge items, bool useTest) {
      var invoice = new InvoiceModel() {
        BatchGuid = guid,
        Organization = org,
        InvoiceDueDate = serviceRange.StartTime.Add(TimespanExtensions.OneMonth()).Date,
        ServiceStart = serviceRange.StartTime,
        ServiceEnd = serviceRange.EndTime,
        IsTest = useTest,
      };

      s.Save(invoice);

      var invoiceItems = items.ItemizedList.Select(x => new InvoiceItemModel() {
        AmountDue = x.Total(),
        Currency = Currency.USD,
        PricePerItem = x.Price,
        Quantity = x.Quantity,
        Name = x.Name,
        Description = x.Description,
        ForInvoice = invoice,
        BusinessUnit = x.BusinessUnit
      }).ToList();

      foreach (var i in invoiceItems) {
        s.Save(i);
      }

      foreach (var user in items.ChargedFor) {
        s.Save(new InvoiceUserItemModel() {
          Email = user.Email,
          InvoiceId = invoice.Id,
          Name = user.Name,
          OrgId = org.Id,
          UserAttachTime = user.AttachTime,
          UserOrganizationId = user.UserOrgId,
          Description = string.Join(", ", user.ChargedFor),
          //Description =
        });
      }

      invoice.InvoiceItems = invoiceItems;
      invoice.AmountDue = invoice.InvoiceItems.Sum(x => x.AmountDue);
      invoice.Subtotal = invoice.AmountDue; //Havent calculated tax yet.

      s.Update(invoice);
      await HooksRegistry.Each<IInvoiceHook>((ses, x) => x.InvoiceCreated(ses, invoice));

      return invoice;
    }

    [Obsolete("Public for testing only")]
    public static decimal _ApplyCreditsToInvoice(ISession s, OrganizationModel org, List<Itemized> itemized, List<PaymentCredit> credits, bool isTest) {
      //Apply credits

      var total = itemized.Sum(x => x.Total());
      var totalCreditsApplied = 0m;


      if (credits.Any(x => x.AmountRemaining > 0) && total > 0) {
        var adjTotal = total;
        foreach (var c in credits) {
          if (adjTotal > 0 && c.AmountRemaining > 0) {
            if (c.AmountRemaining >= adjTotal) {
              c.AmountRemaining -= adjTotal;
              totalCreditsApplied += adjTotal;
              if (!isTest) {
                s.Update(c);
              }
              adjTotal = 0;
              break;
            } else {
              adjTotal -= c.AmountRemaining;
              totalCreditsApplied += c.AmountRemaining;
              c.AmountRemaining = 0;
              if (!isTest) {
                s.Update(c);
              }
            }
          }
        }
        itemized.Add(new Itemized() {
          Name = "Credit",
          Price = -totalCreditsApplied,
          Quantity = 1,
          Type = ItemizedType.Credit,
        });
      }

      return totalCreditsApplied;
    }

    public static async Task RefundInvoice(UserOrganizationModel caller, long invoiceId, decimal subtotalRefund, bool useTest) {

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.RadialAdmin(true);

          var invoice = s.Get<InvoiceModel>(invoiceId, LockMode.Upgrade);
          useTest = useTest || invoice.IsTest || Config.IsLocal();

          if (!invoice.WasAutomaticallyPaid()) {
            throw new PermissionsException("Cannot refund invoice. Invoice was not paid automatically.");
          }
          if (invoice.TransactionId == null) {
            throw new PermissionsException("Cannot refund invoice. Could not find PaymentSpring transaction.");
          }

          var refundableAmount = invoice.GetSubtotal() - invoice.SubtotalRefunded;


          if (subtotalRefund > refundableAmount) {
            if (refundableAmount == 0 && invoice.GetSubtotal() > 0) {
              throw new PermissionsException("Cannot refund invoice. Already refunded.");
            }
            throw new PermissionsException("Cannot refund invoice. Refund exceeded maximum refundable amount. (max:" + refundableAmount + ")");
          }



          ////////
          var orgId = invoice.Organization.Id;
          var psToken = PaymentSpringUtil.GetToken(s, orgId);
          var taxLocation = TaxJarUtility.GetValidTaxLocation(s, psToken);

          var newSubtotal = (invoice.GetSubtotal()) - (invoice.SubtotalRefunded ?? 0);

          var newTaxAndRate = await TaxJarUtility.CalculateSaleTax(invoiceId, taxLocation, newSubtotal, invoice.GetDiscount() ?? 0, useTest);
          if (newTaxAndRate == null && psToken.LastSalesTaxRate != null) {
            newTaxAndRate = TaxJarUtility.CalculateFallbackSalesTax(newSubtotal, psToken.LastSalesTaxRate.Value);
          }
          var oldTaxPaidAfterRefunds = (invoice.TaxDue ?? 0) - (invoice.TaxRefunded ?? 0);
          var taxRefund = oldTaxPaidAfterRefunds - (newTaxAndRate.NotNull(x => x.TaxAmount));

          if (taxRefund < 0) {
            taxRefund = 0;
            //throw new PermissionsException("Cannot refund a negative tax amount");
          }
          var totalRefund = subtotalRefund + taxRefund;

          var refundMeta = new IPaymentHookRefundMetaData(invoice.Organization.Id, invoice.PaidTime ?? DateTime.UtcNow, DateTime.UtcNow, invoiceId, invoice.TransactionId, subtotalRefund, taxRefund, taxLocation, useTest, invoice.TaxExempt);
          var result = await PaymentSpringUtil.RefundTransaction(invoice.Organization, invoice.TransactionId, totalRefund, useTest);

          //Record keeping
          invoice.SubtotalRefunded = (invoice.SubtotalRefunded ?? 0) + subtotalRefund;
          invoice.TaxRefunded = (invoice.TaxRefunded ?? 0) + taxRefund;

          s.Update(invoice);
          tx.Commit();
          s.Flush();

          await HooksRegistry.Each<IInvoiceHook>((ses, x) => x.UpdateInvoice(ses, invoice, new IInvoiceUpdates() { ChargeRefunded = true }));
          await HooksRegistry.Each<IPaymentHook>((ses, x) => x.RefundApplied(ses, refundMeta));

        }
      }
    }

    public class ItemizedCharge {
      private UserCalculator calculator;

      public ItemizedCharge() {
        ItemizedList = new List<Itemized>();
        ChargedFor = new List<UserCharge>();
      }

      public List<Itemized> ItemizedList { get; set; }
      public List<UserCharge> ChargedFor { get; set; }
      public decimal DiscountApplied { get; set; }
      public bool ShouldDedupMeetingUsers { get; set; }
      public bool ShouldDedupPeopleToolsUsers { get; set; }

      public class UserCharge {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public long UserOrgId { get; set; }
        public long OrgId { get; set; }
        public DateTime? AttachTime { get; set; }
        public DateTime? DeleteTime { get; set; }
        public HashSet<string> ChargedFor { get; set; }
      }

      public decimal CalculateTotal() {
        return ItemizedList.Sum(x => x.Total()) - DiscountApplied;
      }

      internal void Add(ItemizedCharge itemizedCharge) {
        ItemizedList.AddRange(itemizedCharge.ItemizedList);
        ChargedFor.AddRange(itemizedCharge.ChargedFor);
        DiscountApplied+=itemizedCharge.DiscountApplied;
      }
    }

    //public class ChargeTypes {
    //  public static string L10 = "Weekly Meeting";
    //  public static string PeopleTools = "People Tools™";
    //}

    public class UsersAndPlan {
      public UsersAndPlan(List<UQ> users, SimplePlan plan) {
        Plan=plan;
        Users=users;
      }

      public SimplePlan Plan { get; set; }
      public List<UQ> Users { get; set; }
    }


    public static UsersAndPlan GetUsersAndPlanForOrganization_Unsafe(ISession s, long orgId, DateTime executionTime) {
      var org = s.Get<OrganizationModel>(orgId);
      var paymentPlan = org.PaymentPlan;

      if (NHibernateUtil.GetClass(paymentPlan) != typeof(PaymentPlan_Monthly)) {
        throw new PermissionsException("Unhandled Payment Plan");
      }
      var plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(paymentPlan);
      var rangeStart = executionTime.Subtract(plan.SchedulerPeriod());// TimespanExtensions.OneMonth());
      var rangeEnd = executionTime;
      var range = new DateRange(rangeStart, rangeEnd);
      var durationMult = plan.DurationMultiplier();
      var durationDesc = plan.MultiplierDesc();

      var allRevisions = s.AuditReader().GetRevisionsBetween<OrganizationModel>(s, org.Id, rangeStart, rangeEnd).ToList();
      var peopleToolsEnabled = /*org.Settings.EnableReview;//*/allRevisions.Any(x => x.Object.Settings.EnableReview || x.Object.Settings.EnablePeople);
      var meetingEnabled = /*org.Settings.EnableL10; //*/allRevisions.Any(x => x.Object.Settings.EnableL10);

      var accountType = org.AccountType;

      //In case clocks are off.
      var executionCalculationDate = executionTime.AddDays(1).Date;


      //Gather data for calculator
      var uqs = GetUQUsersForOrganization_Unsafe(s, org.Id, plan, range);
      var simplePlan = new SimplePlan(org.Id, org.GetName(), org.AccountType, executionCalculationDate, plan, meetingEnabled, peopleToolsEnabled);

      return new UsersAndPlan(uqs, simplePlan);

    }





    [Obsolete("Be careful about saving after this. It will commit the discount")]
    public static ItemizedCharge CalculateChargeLessTaxAndCreditsPerOrganization(UsersAndPlan usersAndPlan, Dedup dedup) {
      var users = usersAndPlan.Users;
      var simplePlan = usersAndPlan.Plan;
      var calc = new UserCalculator(users, simplePlan, dedup);
      var invoice = calc.GenerateInvoiceAndUpdateDedup();
      return invoice;
      //dedup = dedup ?? new Dedup();

      ////var itemized = new List<Itemized>();
      ////var chargedFor = new List<ItemizedCharge.UserCharge>();

      //if (NHibernateUtil.GetClass(paymentPlan) == typeof(PaymentPlan_Monthly)) {
      //  var plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(paymentPlan);
      //  var rangeStart = executeTime.Subtract(plan.SchedulerPeriod());// TimespanExtensions.OneMonth());
      //  var rangeEnd = executeTime;
      //  var range = new DateRange(rangeStart, rangeEnd);
      //  var durationMult = plan.DurationMultiplier();
      //  var durationDesc = plan.MultiplierDesc();

      //  var allRevisions = s.AuditReader().GetRevisionsBetween<OrganizationModel>(s, org.Id, rangeStart, rangeEnd).ToList();
      //  var peopleToolsEnabled = /*org.Settings.EnableReview;//*/allRevisions.Any(x => x.Object.Settings.EnableReview || x.Object.Settings.EnablePeople);
      //  var meetingEnabled = /*org.Settings.EnableL10; //*/allRevisions.Any(x => x.Object.Settings.EnableL10);

      //  var accountType = org.AccountType;

      //  //In case clocks are off.
      //  var executionCalculationDate = executeTime.AddDays(1).Date;


      //  //Gather data for calculator
      //  var uqs = GetUQUsersForOrganization_Unsafe(s, org.Id, plan, range);
      //  var simplePlan = new SimplePlan(org.Id, org.GetName(), org.AccountType, executionCalculationDate, plan, meetingEnabled, peopleToolsEnabled);


      //HEAVY LIFTING
      //var users = usersAndPlan.Users;
      //var simplePlan = usersAndPlan.Plan;
      //var calc = new UserCalculator(users, simplePlan, dedup);
      //var invoice = calc.GenerateInvoiceAndUpdateDedup();
      //return invoice;
      //} else {
      //  throw new PermissionsException("Unhandled Payment Plan");
      //}



      //HEAVY LIFTING
      //var calc = UserCalculator.Create(s, org.Id, org.GetName(), plan, , meetingEnabled, peopleToolsEnabled, executionCalculationDate, accountType, deduplicatedUsers);



      //var buisinessUnit = org.GetName()+" ["+org.Id+"]";

      //if (plan.BaselinePrice > 0) {
      //  var reviewItem = new Itemized() {
      //    BusinessUnit = buisinessUnit,
      //    Name = "Bloom Growth™" + durationDesc,
      //    Price = plan.BaselinePrice * durationMult,
      //    Quantity = 1,
      //  };
      //  itemized.Add(reviewItem);
      //}

      //bool dedupL10Users = false;
      //bool dedupPeopleToolsUsers = false;

      //if (meetingEnabled) {

      //var l10Users = calc.GetL10UsersWithTags();

      //var l10Item = new Itemized() {
      //  BusinessUnit = buisinessUnit,
      //  Name = "Weekly Meeting Software" + durationDesc,
      //  Price = plan.L10PricePerPerson * durationMult,
      //  Quantity = l10Users.GetNumberOfUsersChargedFor() //calc.NumberL10UsersToChargeFor,
      //};


      //l10Users.UsersWithTags.ToList().ForEach(x=>x.

      //calc.L10Users.ToList().ForEach(x => x.ChargedFor.Add(ChargeTypes.L10/*, plan.L10PricePerPerson * durationMult*/));

      //if (l10Item.Quantity != 0) {
      //  itemized.Add(l10Item);
      //  if (!(plan.L10FreeUntil == null || !(plan.L10FreeUntil.Value.Date > executionCalculationDate))) {
      //    //Discount it since it is free
      //    itemized.Add(l10Item.GenerateDiscount(buisinessUnit));
      //  } else {
      //dedupL10Users=true;
      //  }
      //}
      //}

      //if (qcEnabled) {
      //  var reviewItem = new Itemized() {
      //    BusinessUnit = buisinessUnit,
      //    Name = "People Tools™" + durationDesc,
      //    Price = plan.ReviewPricePerPerson * durationMult,
      //    Quantity = calc.NumberQCUsersToChargeFor//allPeopleList.Where(x => !x.IsClient).Count()
      //  };
      //  calc.QCUsers.ToList().ForEach(x => x.ChargedFor.Add(ChargeTypes.PeopleTools/*, plan.L10PricePerPerson * durationMult*/));
      //  if (reviewItem.Quantity != 0) {
      //    itemized.Add(reviewItem);
      //    if (!(plan.ReviewFreeUntil == null || !(plan.ReviewFreeUntil.Value.Date > executionCalculationDate))) {
      //      //Discount it since it is free
      //      itemized.Add(reviewItem.GenerateDiscount(buisinessUnit));
      //    } else {
      //dedupPeopleToolsUsers=true;
      //    }
      //  }
      //}

      //if ((plan.FreeUntil.Date > executionCalculationDate)) {
      //  //Discount it since it is free
      //  var total = itemized.Sum(x => x.Total());
      //  itemized.Add(new Itemized() {
      //    BusinessUnit = org.GetName()+" ["+org.Id+"]",
      //    Name = "Discount",
      //    Price = -1 * total,
      //    Quantity = 1,
      //  });
      //  dedupPeopleToolsUsers=false;
      //  dedupL10Users=false;

      //}

      //var discountLookup = new Dictionary<AccountType, string>() {
      //            //{AccountType.Cancelled,"Inactive Account" },
      //            {AccountType.Implementer,"Discount (Bloom Growth Guide)" },
      //            {AccountType.Dormant,"Inactive Account" },
      //            {AccountType.SwanServices, "Demo Account (Swan Services)" },
      //            {AccountType.Other, "Discount (Special Account)" },
      //            {AccountType.UserGroup, "Discount (User Group)" },
      //            {AccountType.Coach, "Discount (Coach)" },
      //            {AccountType.FreeForever, "Discount (Free)" },
      //};

      //if (org != null && discountLookup.ContainsKey(org.AccountType) && itemized.Sum(x => x.Total()) != 0) {
      //  var total = itemized.Sum(x => x.Total());
      //  itemized.Add(new Itemized() {
      //    BusinessUnit = org.GetName()+" ["+org.Id+"]",
      //    Name = discountLookup[org.AccountType],//"Discount (Bloom Growth Guide)",
      //    Price = -1 * total,
      //    Quantity = 1,
      //  });
      //  dedupPeopleToolsUsers=false;
      //  dedupL10Users=false;
      //}




      //chargedFor.AddRange(calc.AllPeopleList.Select(x => new ItemizedCharge.UserCharge() {
      //  AttachTime = x.AttachTime,
      //  DeleteTime = x.DeleteTime,
      //  ChargedFor = new HashSet<string>(x.ChargedFor.ToList()),
      //  Email = x.Email,
      //  Name = string.Join(" ", new[] { x.FirstName, x.LastName }),
      //  FirstName = x.FirstName,
      //  LastName = x.LastName,
      //  UserOrgId = x.UserOrgId,
      //}));

      //if (org != null && org.AccountType == AccountType.Cancelled) {
      //  itemized = new List<Itemized>();
      //  chargedFor = new List<ItemizedCharge.UserCharge>();
      //  dedupPeopleToolsUsers=false;
      //  dedupL10Users=false;
      //}

      //Add users to dedup lists for joint accounts
      //if (dedupL10Users) {
      //  foreach (var u in calc.L10Users) {
      //    deduplicatedUsers.SetL10UserAsExcluded(u.Email);
      //  }
      //}

      //if (dedupPeopleToolsUsers) {
      //  foreach (var u in calc.QCUsers) {
      //    deduplicatedUsers.SetQCUserAsExcluded(u.Email);
      //  }
      //}

      //return new ItemizedCharge() {
      //  ItemizedList = itemized,
      //  ChargedFor = chargedFor,
      //  DiscountApplied = 0.0m,

      //};
    }

    public static List<UQ> GetUQUsersForOrganization_Unsafe(ISession s, long orgId, PaymentPlanModel planModel, DateRange range) {
      //throw new NotImplementedException("Use other UserCalculator");
      if (NHibernateUtil.GetClass(planModel) != typeof(PaymentPlan_Monthly)) {
        throw new PermissionsException("Unhandled Payment Plan");
      }

      var plan = (PaymentPlan_Monthly)s.GetSessionImplementation().PersistenceContext.Unproxy(planModel);

      if (plan.OrgId != orgId) {
        throw new Exception("Org Id do not match");
      }

      var rangeStart = range.StartTime;
      var rangeEnd = range.EndTime;


      UserModel uAlias = null;
      TempUserModel tuAlias = null;
      UserOrganizationModel uoAlias = null;
      var allPeopleList = s.QueryOver<UserOrganizationModel>(() => uoAlias)
                .Left.JoinAlias(() => uoAlias.User, () => uAlias)                 //Existed any time during this range.
                .Left.JoinAlias(() => uoAlias.TempUser, () => tuAlias)
                //.Where(() => uo.Organization.Id == orgId && uo.CreateTime <= rangeEnd && (uo.DeleteTime == null || uo.DeleteTime > rangeStart) && !uo.IsRadialAdmin && !uo.IsFreeUser)
                .Where(() => uoAlias.Organization.Id == orgId && uoAlias.CreateTime <= rangeEnd && (uoAlias.DeleteTime == null || uoAlias.DeleteTime > rangeStart) && !uoAlias.IsRadialAdmin)
                  //.Select(x => x.Id, x => u.IsRadialAdmin, x => x.IsClient, x => x.User.Id, x => x.EvalOnly, x => u.FirstName, x => u.LastName, x => u.UserName, x => x.EmailAtOrganization, x => x.AttachTime, x => tu.FirstName, x => tu.LastName, x => tu.Email)
                  .Select(x =>
                          x.Id,                           //0
                          x => uAlias.IsRadialAdmin,      //1
                          x => x.IsClient,                //2
                          x => x.User.Id,                 //3
                          x => x.EvalOnly,                //4
                          x => uAlias.FirstName,          //5
                          x => uAlias.LastName,           //6
                          x => uAlias.UserName,           //7
                          x => x.EmailAtOrganization,     //8
                          x => x.AttachTime,              //9
                          x => tuAlias.FirstName,         //10
                          x => tuAlias.LastName,          //11
                          x => tuAlias.Email,             //12
                          x => uoAlias.IsFreeUser,        //13
                          x => uoAlias.DeleteTime,        //14
                          x => x.Organization.Id          //15
                 )
                .List<object[]>()
                .Select(x => new UQ {
                  UserOrgId = (long)x[0],
                  IsRadialAdmin = (bool?)x[1],
                  IsClient = (bool)x[2],
                  UserId = (string)x[3],
                  IsRegistered = x[3] != null,
                  EvalOnly = (bool?)x[4] ?? false,
                  FirstName = (string)x[5] ?? (string)x[10],
                  LastName = (string)x[6] ?? (string)x[11],
                  Email = ((string)x[7]) ?? (string)x[12] ?? ((string)x[8]),
                  AttachTime = (x[3] == null) ? ((DateTime?)null) : ((DateTime?)x[9]),
                  IsFreeUser = (bool)(x[13] ?? false),
                  DeleteTime = (x[3] == null) ? ((DateTime?)null) : ((DateTime?)x[14]),
                  OrgId = (long)x[15]
                })
                .Where(x => x.IsRadialAdmin == null || (bool)x.IsRadialAdmin == false)
                .Where(x => !x.IsFreeUser)
                .ToList();
      if (plan.NoChargeForClients) {
        allPeopleList = allPeopleList.Where(x => x.IsClient == false).ToList();
      }
      if (plan.NoChargeForUnregisteredUsers) {
        allPeopleList = allPeopleList.Where(x => x.IsRegistered).ToList();
      }
      return allPeopleList;
    }

    //private static decimal _CalculateSalesTax(PaymentSpringsToken token, List<Itemized> itemized) {
    //	var total = itemized.Sum(x => x.Total());
    //	return TaxJarUtility.CalculateSaleTax(token, total);
    //}

    #endregion

    #region Plan
    public PaymentPlanModel BasicPaymentPlan() {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PaymentPlanModel basicPlan = null;
          try {
            basicPlan = s.QueryOver<PaymentPlanModel>().Where(x => x.IsDefault).SingleOrDefault();
          } catch (Exception e) {
            log.Error(e);
          }
          if (basicPlan == null) {
            basicPlan = new PaymentPlanModel() {
              Description = "Employee count model",
              IsDefault = true,
              PlanCreated = DateTime.UtcNow
            };
            s.Save(basicPlan);
            tx.Commit();
            s.Flush();
          }
          return basicPlan;
        }
      }
    }

    [Obsolete("Dont forget to attach to send this through AttachPlan")]
    public static PaymentPlan_Monthly GeneratePlan(PaymentPlanType type, DateTime? now = null, DateTime? trialEnd = null) {
      var now1 = now ?? DateTime.UtcNow;
      var day30 = now1.AddDays(30);
      var day90 = now1.AddDays(90);
      var basePlan = new PaymentPlan_Monthly() {
        FreeUntil = trialEnd ?? day30,
        L10FreeUntil = trialEnd ?? day30,
        ReviewFreeUntil = Math2.Max(day90, trialEnd ?? DateTime.MinValue),
        PlanCreated = now1,
        NoChargeForUnregisteredUsers = true,
      };
      switch (type) {
        case PaymentPlanType.Enterprise_Monthly_March2016:
          basePlan.Description = "Bloom Growth™ for Enterprise";
          basePlan.BaselinePrice = 500;
          basePlan.L10PricePerPerson = 2;
          basePlan.ReviewPricePerPerson = 0;
          basePlan.FirstN_Users_Free = 45;
          break;
        case PaymentPlanType.Professional_Monthly_March2016:
          basePlan.Description = "Bloom Growth™ Professional";
          basePlan.BaselinePrice = 149;
          basePlan.L10PricePerPerson = 10;
          basePlan.ReviewPricePerPerson = 0;
          basePlan.FirstN_Users_Free = 10;
          break;
        case PaymentPlanType.SelfImplementer_Monthly_March2016:
          basePlan.Description = "Bloom Growth™ Self-Onboard Guide";
          basePlan.BaselinePrice = 199;
          basePlan.L10PricePerPerson = 12;
          basePlan.ReviewPricePerPerson = 0;
          basePlan.FirstN_Users_Free = 10;
          break;
        case PaymentPlanType.BloomGrowthCoachMembership:
          basePlan.Description = "Bloom Growth™ Coaching Membership";
          basePlan.BaselinePrice = 700;
          basePlan.L10PricePerPerson = 5;
          basePlan.ReviewPricePerPerson = 0;
          basePlan.FirstN_Users_Free = 5;
          break;
        default:
          throw new ArgumentOutOfRangeException("type", "PaymentPlanType not implemented " + type);
      }
      return basePlan;
    }

    public static PaymentPlanModel AttachPlan(ISession s, OrganizationModel organization, PaymentPlanModel plan) {
      var task = new ScheduledTask() {
        MaxException = 1,
        Url = "/Scheduler/ChargeAccount/" + organization.Id,
        NextSchedule = plan.SchedulerPeriod(),
        Fire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date.AddHours(3)),
        FirstFire = Math2.Max(DateTime.UtcNow.Date, plan.FreeUntil.Date.AddHours(3)),
        TaskName = plan.TaskName(),
        EmailOnException = true,
      };
      s.Save(task);
      task.OriginalTaskId = task.Id;
      s.Update(task);
      if (plan is PaymentPlan_Monthly) {
        var ppm = (PaymentPlan_Monthly)plan;
        ppm.OrgId = organization.Id;
      }

      plan.Task = task;
      s.Save(plan);
      return plan;
    }

    public static PaymentPlanModel GetPlan(UserOrganizationModel caller, long organizationId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).Or(x => x.ManagingOrganization(organizationId), x => x.CanView(PermItem.ResourceType.UpdatePaymentForOrganization, organizationId));
          var org = s.Get<OrganizationModel>(organizationId);

          var plan = s.Get<PaymentPlanModel>(org.PaymentPlan.Id);

          if (plan != null && plan.Task != null) {
            plan._CurrentTask = s.QueryOver<ScheduledTask>()
              .Where(x => x.OriginalTaskId == plan.Task.OriginalTaskId && x.Executed == null)
              .List().FirstOrDefault();
          }


          return (PaymentPlanModel)s.GetSessionImplementation().PersistenceContext.Unproxy(plan);

        }
      }
    }

    public static PaymentPlanType GetPlanType(string planType) {
      switch (planType.Replace("-", "").ToLower()) {
        case "professional":
          return PaymentPlanType.Professional_Monthly_March2016;
        case "enterprise":
          return PaymentPlanType.Enterprise_Monthly_March2016;
        case "selfimplementer":
          return PaymentPlanType.SelfImplementer_Monthly_March2016;
        case "bloomgrowthcoachmembership":
          return PaymentPlanType.BloomGrowthCoachMembership;
        default:
          throw new ArgumentOutOfRangeException("Cannot create Payment Plan (" + planType + ")");
      }
    }

    #endregion

    #region Unsafe Methods
    [Obsolete("Unsafe")]
    public partial class Unsafe : IChargeViaHangfire {

      public static DateRange GetServiceRange(ISession s, DateTime executionTime, PaymentPlanModel paymentPlan) {
        if (NHibernateUtil.GetClass(paymentPlan) == typeof(PaymentPlan_Monthly)) {
          var plan = (PaymentPlan_Monthly)s.Unproxy(paymentPlan);
          return new DateRange(executionTime, executionTime.Add(plan.SchedulerPeriod()).Date);
        } else {
          throw new PermissionsException("Unhandled Payment Plan");
        }
      }

      public class OrgAndPlan {
        public OrgAndPlan(OrganizationModel org, PaymentPlanModel plan) {
          Org=org;
          Plan=plan;
        }

        public OrganizationModel Org { get; set; }
        public PaymentPlanModel Plan { get; set; }
      }


      public static async Task<InvoiceModel> GenerateInvoiceModelsForAllJointAccounts(ISession s, OrganizationModel org, PaymentPlanModel plan, string guid, DateTime executeTime, bool forceUseTest) {

        //Get organizations we pay for
        var ownedOrgsTree = JointAccountAccessor.GetOwnedOrgs_Unsafe(s, org.Id, true);
        var ownedOrgIds = ownedOrgsTree.Select(x => x.ChildOrgId).Distinct().ToList();

        List<OrgAndPlan> ownedOrgsAndPaymentPlans;
        if (ownedOrgIds.Count()==1 && ownedOrgIds.First() == org.Id) {
          //Skip the query if it's just one org
          ownedOrgsAndPaymentPlans = new List<OrgAndPlan>() { new OrgAndPlan(org, plan) };
        } else {
          //Fetch orgs and payment plans
          ownedOrgsAndPaymentPlans = s.QueryOver<OrganizationModel>()
            .Fetch(x => x.PaymentPlan).Eager
            .Where(x => x.DeleteTime==null)
            .WhereRestrictionOn(x => x.Id).IsIn(ownedOrgIds)
            .List().ToList()
            .Select(x => {
              return new OrgAndPlan(x, x.PaymentPlan);
            }).ToList();

          //Precalculate the estimated per-user fee across all owned organizations
          UserModel uAlias = null;
          var allUsers = s.QueryOver<UserOrganizationModel>()
                        .JoinAlias(x => x.User, () => uAlias)
                        .Where(x => x.DeleteTime==null && x.User!=null)
                        .WhereRestrictionOn(x => x.Organization.Id).IsIn(ownedOrgIds)
                        .Select(x => x.Organization.Id, x => uAlias.UserName)
                        .List<object[]>().Select(x => new {
                          OrgId = (long)x[0],
                          Email = (string)x[1]
                        }).ToList();
        }

        //Generate User and Plan data
        var userPlansList = new List<UsersAndPlan>();
        foreach (var oopp in ownedOrgsAndPaymentPlans) {
          var up = GetUsersAndPlanForOrganization_Unsafe(s, oopp.Org.Id, executeTime);
          userPlansList.Add(up);
        }

        //Calculate charge for each organization we pay for.
        ItemizedCharge mergedItemizedCharge = CalculateChargeAcrossJointOrganizations(userPlansList);

        //Add discount as calculated from parent's credits.
        var credits = s.QueryOver<PaymentCredit>().Where(x => x.DeleteTime == null && x.OrgId == org.Id && x.AmountRemaining > 0).List().ToList();
        mergedItemizedCharge.DiscountApplied = _ApplyCreditsToInvoice(s, org, mergedItemizedCharge.ItemizedList, credits, forceUseTest);

        //Calculate range for invoice
        var serviceRange = GetServiceRange(s, executeTime, plan);
        //Save invoice
        var invoice = await CreateInvoice(s, org, guid, serviceRange, mergedItemizedCharge, forceUseTest);
        //Add tax to invoice
        await AddTaxToInvoice(s, org, invoice, forceUseTest);
        return invoice;
      }

      public static ItemizedCharge CalculateChargeAcrossJointOrganizations(List<UsersAndPlan> userPlansList) {

        var dedup = new Dedup();

        //Precache dedup 
        foreach (var usersPlan in userPlansList) {
          var plan = usersPlan.Plan;
          var users = usersPlan.Users;
          foreach (var user in users) {
            dedup.PreAddUser(user, plan);
          }
        }

        //Order by Seat Price
        var orderedPlans = userPlansList.OrderByDescending(x => {
          var seatPrice = 0.0m;
          if (x.Plan.MeetingEnabled)
            seatPrice += x.Plan.MeetingPricePerPerson;
          if (x.Plan.PeopleToolsEnabled)
            seatPrice += x.Plan.PeopleToolsPricePerPerson;
          return seatPrice;
        }).ToList();

        //Merge Calculations Together
        var mergedItemizedCharge = new ItemizedCharge();
        foreach (var o in orderedPlans) {
          var itemizedCharge = CalculateChargeLessTaxAndCreditsPerOrganization(o, dedup);
          mergedItemizedCharge.Add(itemizedCharge);
        }
        return mergedItemizedCharge;
      }

      private static async Task AddTaxToInvoice(ISession s, OrganizationModel org, InvoiceModel invoice, bool useTest) {
        try {
          if (org == null || org.PaymentPlan == null) {
            return;
          }

          if (!TaxJarUtility.SaleTaxEnabled(s)) {
            return; //Disabled
          }

          if (invoice.TaxDue != null) {
            return; //Already Calculated
          }

          var psToken = PaymentSpringUtil.GetToken(s, org.Id);
          var beforeDiscount = invoice.InvoiceItems.Where(x => x.AmountDue > 0).Sum(x => x.AmountDue);
          var discount = invoice.GetDiscount() ?? 0;
          var taxLocation = TaxJarUtility.GetValidTaxLocation(s, psToken);
          invoice._TaxLocation = taxLocation; //Not saved. Just a hint.

          //Short circuit if tax exempt..
          if (org.PaymentPlan.TaxExempt) {
            invoice.TaxExempt = org.PaymentPlan.TaxExempt;
            s.Update(invoice);
            return;
          }


          var taxAndRate = await TaxJarUtility.CalculateSaleTax(invoice.Id, taxLocation, beforeDiscount, discount, useTest);

          if (taxAndRate == null && psToken != null && psToken.LastSalesTaxRate != null) {
            taxAndRate = TaxJarUtility.CalculateFallbackSalesTax(invoice.GetSubtotal(), psToken.LastSalesTaxRate.Value);
          }

          if (taxAndRate != null) {
            if (!useTest && psToken.LastSalesTaxRate != taxAndRate.TaxRate) {
              psToken.LastSalesTaxRate = taxAndRate.TaxRate;
              psToken.LastSalesTaxRateDate = DateTime.UtcNow;
              s.Update(psToken);
            }
            invoice.TaxRate = taxAndRate.TaxRate;
            invoice.TaxDue = taxAndRate.TaxAmount;
            invoice.Subtotal = invoice.AmountDue;
            invoice.AmountDue = invoice.AmountDue + (invoice.TaxDue ?? 0);
            s.Update(invoice);
          }
        } catch (Exception e) {
          log.Error("TaxJar Exception:", e);
        }
      }


      //[Obsolete("Unsafe")]
      public static async Task<bool> EmailInvoice(string emailAddress, InvoiceModel invoice, DateTime chargeTime) {
        var ProductName = Config.ProductName(invoice.Organization);
        var SupportEmail = ProductStrings.SupportEmail;
        var OrgName = invoice.Organization.GetName();
        var Charged = invoice.AmountDue;
        //var CardLast4 = result.card_number ?? "NA";
        //var TransactionId = result.id ?? "NA";
        var ChargeTime = chargeTime;
        var ServiceThroughDate = invoice.ServiceEnd.ToString("yyyy-MM-dd");
        var Address = ProductStrings.Address;

        var localChargeTime = invoice.Organization.ConvertFromUTC(ChargeTime);
        var lctStr = localChargeTime.ToString("dd MMM yyyy hh:mmtt");
        try {
          lctStr += " " + invoice.Organization.GetTimeZoneId(localChargeTime);
        } catch (Exception e) {
          lctStr += " " + (int)(invoice.Organization.GetTimezoneOffset() / 60);
        }

        var email = Mail.Bcc(EmailTypes.Receipt, ProductStrings.PaymentReceiptEmail);
        if (emailAddress != null) {
          email = email.AddBcc(emailAddress);
        }
        var toSend = email.SubjectPlainText("[" + ProductName + "] Invoice for " + invoice.Organization.GetName())
          //[ProductName, SupportEmail, OrgName, Charged, CardLast4, TransactionId, ChargeTime, ServiceThroughDate, Address]
          .Body(EmailStrings.PaymentReceipt_Body, ProductName, SupportEmail, OrgName, String.Format("${0:f2}", Charged), "", "", lctStr, ServiceThroughDate, Address);
        await Emailer.SendEmail(toSend);
        return true;
      }

      public static async Task<PaymentResult> ExecuteInvoice(ISession s, InvoiceModel invoice, bool useTest = false) {

        if (invoice.PaidTime != null) {
          throw new FallthroughException("Invoice was already paid");
        }

        if (invoice.ForgivenBy != null) {
          throw new FallthroughException("Invoice was forgiven");
        }

        var subtotal = invoice.GetSubtotal();

        var data = new ChargeOrganizationData(
                    invoice.Organization.Id,
                    invoice.Id,
                    subtotal,
                    invoice.TaxDue,
                    invoice.TaxRate,
                    invoice.GetDiscount() ?? 0,
                    invoice._TaxLocation,
                    invoice.TaxExempt
                );

        var result = await ChargeOrganizationAmount(s, data, useTest || invoice.IsTest);

        invoice.TransactionId = result.id;
        invoice.PaidTime = DateTime.UtcNow;
        invoice.ChargeStatus = InvoiceModel.STATUS_COMPLETE;

        s.Update(invoice);
        await HooksRegistry.Each<IInvoiceHook>((ses, x) => x.UpdateInvoice(ses, invoice, new IInvoiceUpdates() { PaidStatusChanged = true }));

        return result;
      }


      public class ChargeOrganizationData {

        /// <summary>
        /// subtotal should include any discounts
        /// </summary>
        public ChargeOrganizationData(long organizationId, long invoiceId, decimal subtotal, decimal? taxToCollect, decimal? taxRate, decimal discountAlreadyApplied, ValidTaxLocation taxLocation, bool taxExempt) {
          OrganizationId = organizationId;
          InvoiceId = invoiceId;
          Subtotal = subtotal;
          TaxToCollect = taxToCollect;
          TaxRate = taxRate;
          DiscountAlreadyApplied = discountAlreadyApplied;
          TaxLocation = taxLocation;
          TaxExempt = taxExempt;
        }

        public long OrganizationId { get; set; }
        public long InvoiceId { get; set; }
        public decimal? TaxRate { get; set; }
        /// <summary>
        /// Subtotal should include discounts. This fields is purely informational
        /// </summary>
        public decimal DiscountAlreadyApplied { get; set; }


        public decimal Subtotal { get; set; }
        public decimal? TaxToCollect { get; set; }
        public decimal AmountDue { get { return Subtotal + (TaxToCollect ?? 0); } }

        public ValidTaxLocation TaxLocation { get; set; }
        public bool TaxExempt { get; set; }
      }


      [Obsolete("Use ExecuteInvoice instead.")]
      public static async Task<PaymentResult> ChargeOrganizationAmount(ISession s, ChargeOrganizationData data, bool forceTest = false) {
        var organizationId = data.OrganizationId;
        var amount = data.AmountDue;

        if (amount == 0) {
          await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFree, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "No Charge", arg1: 0m));
          return new PaymentResult() {
            amount_settled = 0,
          };
        }

        var token = PaymentSpringUtil.GetToken(s, organizationId);


        var org2 = s.Get<OrganizationModel>(organizationId);
        //Test for Bloom Growth Guide (Bloom Growth Guide)
        if (org2 != null && org2.AccountType == AccountType.Implementer) {
          await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFree, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Bloom Growth Guide", arg1: 0));
          throw new FallthroughException("Failed to charge Bloom Growth Guide account (" + org2.Id + ") " + org2.GetName());
        }
        //test for dormant
        if (org2 != null && org2.AccountType == AccountType.Dormant) {
          await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Dormant", arg1: 0));
          throw new FallthroughException("Failed to charge dormant account (" + org2.Id + ") " + org2.GetName());
        }
        //Test for no token
        if (token == null) {
          await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "MissingToken", arg1: amount));
          throw new PaymentException(s.Get<OrganizationModel>(organizationId), amount, PaymentExceptionType.MissingToken, "Token missing for " + org2.GetName() + " (" + organizationId + ")");
        }


        PaymentResult pr = null;
        try {
          //Heavy lifting -- Charge token
          pr = await PaymentSpringUtil.ChargeToken(org2, token, amount, forceTest);
          await EventUtil.Trigger(x => x.Create(s, EventType.PaymentReceived, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Charged", arg1: amount));
        } catch (PaymentException e) {
          await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "" + e.Type, arg1: amount));
          throw e;
        } catch (Exception e) {
          await EventUtil.Trigger(x => x.Create(s, EventType.PaymentFailed, null, organizationId, ForModel.Create<OrganizationModel>(organizationId), message: "Unhandled:" + e.Message, arg1: amount));
          throw;
        }

        var org = s.Get<OrganizationModel>(organizationId);
        if (org.AccountType == AccountType.Demo) {
          org.AccountType = AccountType.Paying;
        }

        if (org.PaymentPlan != null) {

          var taxLocation = data.TaxLocation;
          try {
            if (taxLocation == null) {
              taxLocation = TaxJarUtility.GetValidTaxLocation(s, token);
            }
          } catch (Exception e) {
            int a = 0;
          }

          var paymentMeta = new IPaymentHookChargeMetaData(
                            data.InvoiceId,
                            data.Subtotal,
                            data.TaxToCollect ?? 0,
                            amount,
                            pr.created_at,
                            taxLocation,
                            forceTest,
                            pr.id,
                            data.DiscountAlreadyApplied,
                            data.TaxExempt
                          );

          if (org.PaymentPlan.LastExecuted == null) {
            await HooksRegistry.Each<IPaymentHook>((ses, x) => x.FirstSuccessfulCharge(ses, token, paymentMeta));
          }
          await HooksRegistry.Each<IPaymentHook>((ses, x) => x.SuccessfulCharge(ses, token, amount, paymentMeta));
          org.PaymentPlan.LastExecuted = DateTime.UtcNow;
        }

        return pr;
      }


      [Obsolete("Do not use. Use ExecuteInvoice instead.")]
      public static async Task<PaymentResult> ChargeOrganizationAmountWithoutInvoice(long organizationId, decimal amount, string title, bool useTest) {
        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var executeTime = DateTime.UtcNow;
            var org = s.Get<OrganizationModel>(organizationId);

            var itemized = new ItemizedCharge() {
              ItemizedList = new List<Itemized>() {
                new Itemized(){
                  Name = title,
                  Price = amount,
                  Quantity = 1,
                  Type = ItemizedType.Default
                },
              },
              ChargedFor = new List<ItemizedCharge.UserCharge>(),
            };

            var guid = "" + Guid.NewGuid();
            var invoice = await CreateInvoice(s, org, guid, new DateRange(DateTime.UtcNow, DateTime.UtcNow), itemized, useTest);
            await AddTaxToInvoice(s, org, invoice, useTest);
            var result = await ExecuteInvoice(s, invoice, useTest);

            //var charged = await ChargeOrganizationAmount(s, organizationId, amount, useTest);
            tx.Commit();
            s.Flush();
            return result;
          }
        }
      }

      //[Obsolete("Unsafe", false)]
      public static List<long> GetPayingOrganizations(ISession s) {
        var scheduledToPay = s.QueryOver<ScheduledTask>().Where(x => x.TaskName == ScheduledTask.MonthlyPaymentPlan && x.DeleteTime == null && x.Executed == null)
          .List().ToList().Select(x => x.Url.Split('/').Last().ToLong());
        var hasTokens = s.QueryOver<PaymentSpringsToken>().Where(x => x.Active && x.DeleteTime == null).List().ToList();

        var hasTokens_scheduledToPay = hasTokens.Select(x => x.OrganizationId).Intersect(scheduledToPay);
        return hasTokens_scheduledToPay.ToList();
      }
      //[Obsolete("Unsafe", false)]
      public static decimal CalculateTotalCharge(ISession s, List<long> orgIds) {
        var orgs = s.QueryOver<OrganizationModel>().WhereRestrictionOn(x => x.Organization.Id).IsIn(orgIds.Distinct().ToList()).List().ToList();

        var dedup = new Dedup();
        return orgs.Sum(o => {
          var usersAndPlan = GetUsersAndPlanForOrganization_Unsafe(s, o.Id, DateTime.UtcNow);
          var invoice = CalculateChargeLessTaxAndCreditsPerOrganization(usersAndPlan, dedup);//s, o, o.PaymentPlan, DateTime.UtcNow, dedup)
          var total = invoice.ItemizedList.Sum(x => x.Total());
          return total;
        });
      }
      public static async Task RecordCapturedPaymentException(PaymentException capturedPaymentException, long taskId) {
        try {
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              s.Save(PaymentErrorLog.Create(capturedPaymentException, taskId));
              tx.Commit();
              s.Flush();
            }
          }
        } catch (Exception e) {
          log.Error("FatalPaymentException [A]~(task:" + taskId + ")", e);
        }
        log.Error("PaymentException~(task:" + taskId + ")", capturedPaymentException);

        try {
          var orgName = capturedPaymentException.OrganizationName + "(" + capturedPaymentException.OrganizationId + ")";
          var trace = capturedPaymentException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
          var email = Mail.To(EmailTypes.PaymentException, ProductStrings.PaymentExceptionEmail)
            .Subject(EmailStrings.PaymentException_Subject, orgName)
            .Body(EmailStrings.PaymentException_Body,
              capturedPaymentException.Message,
              "<b>" + capturedPaymentException.Type + "</b> for '" + orgName + "'  ($" + capturedPaymentException.ChargeAmount + ") at " + capturedPaymentException.OccurredAt + " [TaskId=" + taskId + "]",
              trace,
              "");

          await Emailer.SendEmail(email, true);
        } catch (Exception e) {
          log.Error("FatalPaymentException [B]~(task:" + taskId + ")", e);
        }
      }
      public static async Task RecordUnknownPaymentException(Exception capturedException, long orgId, long taskId) {
        log.Error("Exception during Payment~(org:" + orgId + ", task:" + taskId + ")", capturedException);
        try {
          var trace = capturedException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
          var email = Mail.To(EmailTypes.PaymentException, ProductStrings.ErrorEmail)
            .Subject(EmailStrings.PaymentException_Subject, "{Non-payment exception}")
            .Body(EmailStrings.PaymentException_Body, capturedException.NotNull(x => x.Message), "{Non-payment}", trace, "[OrgId=" + orgId + "] --  [TaskId=" + taskId + "]");
          await Emailer.SendEmail(email, true);
        } catch (Exception e) {
          log.Error("FatalPaymentException [C]~(org:" + orgId + ", task:" + taskId + ")", e);
        }
      }


      public string ScheduleInvoiceAndExecution(ChargeMetaData data) {
        var rnd = new Random().NextDouble();
        var startDelay = TimeSpan.FromSeconds(10).Add(rnd * TimeSpan.FromMinutes(10));

        if (data.IsTest) {
          startDelay = TimeSpan.FromSeconds(5);
        }


        var paymentBatchId = BatchJob.StartNew(batch => { }, "Invoicing and Executing Payment. Org: " + data.OrganizationId);
        data.BatchId = paymentBatchId;

        BatchJob.Attach(paymentBatchId, batch => {
          var invoiceJobId = batch.Enqueue(() => new Unsafe().InvoiceViaHangfire(data, default(ILogger<PaymentAccessor>)));
          //Receipt sent inside of execute.
          var executionJobId = batch.ContinueJobWith(invoiceJobId, () => new Unsafe().ExecuteInvoiceViaHangfire(data, default(ILogger<PaymentAccessor>)));
        });//, " - Scheduling invoice and execute:" + data.OrganizationId, BatchContinuationOptions.OnAnyFinishedState);
        return paymentBatchId;
      }

      [Queue(HangfireQueues.Immediate.INVOICE_ACCOUNT_VIA_HANGFIRE)]
      [AutomaticRetry(Attempts = 0)]
      public async Task<InvoiceModel> InvoiceViaHangfire(ChargeMetaData data, [ActivateParameter] ILogger<PaymentAccessor> logger) {
        var guid = data.Guid;
        var organizationId = data.OrganizationId;
        var executeTime = data.ServiceStart;
        var forceUseTest = data.IsTest;
        var firstAttempt = data.FirstAttempt;

        if (string.IsNullOrWhiteSpace(guid))
          throw new Exception("guid cannot be empty");

        logger.LogInformation("Generating Invoice for Organization " + organizationId);
        return await WrapExceptionHandling(data, logger, async () => {
          InvoiceModel invoice = null;
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              var org = s.Get<OrganizationModel>(organizationId);
              var plan = org.PaymentPlan;

              var existingInvoice = s.QueryOver<InvoiceModel>()
                          .Where(x => x.Organization.Id == organizationId && x.ServiceStart == executeTime)
                          .Take(1)
                          .RowCountInt64();

              if (existingInvoice > 0)
                throw new PaymentException(org, 0, PaymentExceptionType.Fallthrough, "Invoice already exists.");

              invoice = await GenerateInvoiceModelsForAllJointAccounts(s, org, plan, guid, executeTime, forceUseTest);
              tx.Commit();
              s.Flush();
            }
          }
          logger.LogInformation("Invoice Generated for Organization " + organizationId);

          return invoice;
        });
      }


      [Queue(HangfireQueues.Immediate.INVOICE_ACCOUNT_VIA_HANGFIRE)]
      [AutomaticRetry(Attempts = 0)]
      public async Task<PaymentResult> ExecuteInvoiceViaHangfire(ChargeMetaData data, [ActivateParameter] ILogger<PaymentAccessor> logger) {
        var guid = data.Guid;
        var organizationId = data.OrganizationId;
        var executeTime = data.ServiceStart;
        var forceUseTest = data.IsTest;
        var firstAttempt = data.FirstAttempt;
        var sendReceipt = data.SendReceipt;

        if (string.IsNullOrWhiteSpace(guid))
          throw new Exception("guid cannot be empty");

        return await WrapExceptionHandling(data, logger, async () => {
          long invoiceId;
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              var org = s.Get<OrganizationModel>(organizationId);
              var invoices = s.QueryOver<InvoiceModel>().Where(x => x.BatchGuid == guid).List().ToList();

              if (invoices.Count() == 0) {
                logger.LogError("Cannot charge. Invoice not found: " + guid);
                throw new PaymentException(org, 0, PaymentExceptionType.InvoiceNotFound, "Cannot charge. Invoice not found: " + guid);
              } else if (invoices.Count() > 1) {
                logger.LogError("Cannot charge. Duplicate invoice not found: " + guid);
                throw new PaymentException(org, 0, PaymentExceptionType.InvoiceNotFound, "Cannot charge. Duplicate invoice found: " + guid);
              }
              var invoice = invoices.Single();

              if (invoice.WasPaid())
                throw new FallthroughException("Invoice was already paid");

              invoice.ChargeStatus = InvoiceModel.STATUS_IN_PROGRESS;
              s.Update(invoice);
              invoiceId = invoice.Id;
            }
          }
          PaymentResult result;
          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              var invoice = s.Get<InvoiceModel>(invoiceId);
              result = await ExecuteInvoice(s, invoice, invoice.IsTest);
              tx.Commit();
              s.Flush();
            }
          }

          if (data.SendReceipt) {
            BatchJob.Attach(data.BatchId, batch => {
              batch.Enqueue(() => ScheduleReceipt(data.BatchId, result, invoiceId));
            });//, " -- Generating Invoice Receipt:" + organizationId, BatchContinuationOptions.OnAnyFinishedState);
          }

          return result;
        });
      }

      [Queue(HangfireQueues.Immediate.INVOICE_ACCOUNT_VIA_HANGFIRE)]
      [AutomaticRetry(Attempts = 0)]
      public static async Task<bool> ScheduleReceipt(string batchId, PaymentResult result, long invoiceId) {
        if (string.IsNullOrWhiteSpace(batchId))
          throw new ArgumentNullException(nameof(batchId));

        using (var s = HibernateSession.GetCurrentSession()) {
          using (var tx = s.BeginTransaction()) {
            var invoice = s.Get<InvoiceModel>(invoiceId);

            if (invoice.PaidTime != null) {
              var ProductName = Config.ProductName(invoice.Organization);
              var SupportEmail = ProductStrings.SupportEmail;
              var OrgName = invoice.Organization.GetName();
              var Charged = invoice.AmountDue;
              var CardLast4 = result.card_number ?? "NA";
              var TransactionId = result.id ?? "NA";
              var ChargeTime = invoice.PaidTime;
              var ServiceThroughDate = invoice.ServiceEnd.ToString("yyyy-MM-dd");
              var Address = ProductStrings.Address;

              var localChargeTime = invoice.Organization.ConvertFromUTC(ChargeTime.Value);
              var lctStr = localChargeTime.ToString("dd MMM yyyy hh:mmtt");
              try {
                lctStr += " " + invoice.Organization.GetTimeZoneId(localChargeTime);
              } catch (Exception e) {
                lctStr += " " + (int)(invoice.Organization.GetTimezoneOffset() / 60);
              }

              var email = Mail.Bcc(EmailTypes.Receipt, ProductStrings.PaymentReceiptEmail);
              if (result.email != null) {
                email = email.AddBcc(result.email);
              }
              var toSend = email.SubjectPlainText("[" + ProductName + "] Payment Receipt for " + invoice.Organization.GetName())
                .Body(EmailStrings.PaymentReceipt_Body, ProductName, SupportEmail, OrgName, String.Format("${0:f2}", Charged), CardLast4, TransactionId, lctStr, ServiceThroughDate, Address);

              //BatchJob.Attach(batchId, batch => {
              Scheduler.Enqueue(() => Emailer.SendEmail(toSend, false, true));
              //});//, " -- Sending Invoice Receipt:" + invoice.Organization.Id, BatchContinuationOptions.OnAnyFinishedState);
              //await Emailer.SendEmail(toSend);
              return true;
            }
            return false;
          }
        }
      }


      private async Task<T> WrapExceptionHandling<T>(ChargeMetaData data, ILogger<PaymentAccessor> logger, Func<Task<T>> action) {
        try {
          try {
            return await action();
          } catch (PaymentException e) {
            await HooksRegistry.Each<IPaymentHook>((ses, x) => x.PaymentFailedCaptured(ses, data.OrganizationId, data.ServiceStart, e, data.FirstAttempt));
            logger.LogError("PaymentAccessor.cs (1)", e);
            throw;
          } catch (Exception e) {
            if (!(e is FallthroughException)) {
              await HooksRegistry.Each<IPaymentHook>((ses, x) => x.PaymentFailedUncaptured(ses, data.OrganizationId, data.ServiceStart, e.Message, data.FirstAttempt));
            }
            logger.LogError("PaymentAccessor.cs (2)", e);
            throw;
          }
        } catch (PaymentException capturedPaymentException) {
          await RecordCapturedPaymentException(capturedPaymentException, data.TaskId);
          //if (capturedPaymentException.Type == PaymentExceptionType.Fallthrough)
          //	return default(T);
          //Saved exception.. stop execution
          throw;
          //return new HangfireChargeResult(null, false, false, true, true, "" + capturedPaymentException.Type);
        } catch (FallthroughException e) {
          log.Error("FallthroughCaptured", e);
          //It's a fallthrough, stop execution
          return default(T);
        } catch (Exception capturedException) {
          await RecordUnknownPaymentException(capturedException, data.OrganizationId, data.TaskId);
          //Email send.. stop execution.
          throw;
          //return new HangfireChargeResult(null, false, false, true, false, capturedException.NotNull(x => x.Message) ?? "-no message-");
        }
      }







    }
    #endregion
    #region Test Methods
    public class Test {
      public enum TestCardType : long {
        Visa = 4111111111111111L,
        Amex = 345829002709133L,
        Discover = 6011010948700474L,
        Mastercard = 5499740000000057L,
      }
      public static async Task<PaymentTokenVM> GenerateFakeCard(string owner = "John Doe", TestCardType cardType = TestCardType.Visa) {

        var url = "https://api.paymentspring.com/api/v1/tokens";
        //var  reqparm = new NameValueCollection();
        //reqparm.Add("card_number", @"""4111111111111111""");
        //reqparm.Add("card_exp_month", @"""1""");
        //reqparm.Add("card_exp_year", @"""2020""");
        //reqparm.Add("csc", @"""1234""");
        //reqparm.Add("card_owner_name", "\""+owner+"\"");
        //var cc = new CredentialCache();
        //cc.Add(new Uri(url), "NTLM", new NetworkCredential(Config.PaymentSpring_PublicKey(true),""));
        //client.Credentials = cc;
        //byte[] responsebytes = await client.UploadValuesTaskAsync(url, "POST", reqparm);
        //string responsebody = Encoding.UTF8.GetString(responsebytes);
        //PaymentTokenVM r = JsonConvert.DeserializeObject<PaymentTokenVM>(responsebody);

        //if (r.@class != "token")
        //    throw new PermissionsException("Id must be a token");
        //if (String.IsNullOrWhiteSpace(r.id))
        //    throw new PermissionsException("Token was empty");
        //if (r.card_owner_name!=owner)
        //    throw new PermissionsException("Owner incorrect");

        var csc = "999";
        if (cardType == TestCardType.Amex) {
          csc = csc + "7";
        }

        var client = new HttpClient();

        var keys = new List<KeyValuePair<string, string>>();
        keys.Add(new KeyValuePair<string, string>("card_number", "" + (long)cardType));
        keys.Add(new KeyValuePair<string, string>("card_exp_month", "08"));
        keys.Add(new KeyValuePair<string, string>("card_exp_year", "2022"));
        keys.Add(new KeyValuePair<string, string>("csc", csc));

        keys.Add(new KeyValuePair<string, string>("card_owner_name", owner));


        // Create the HttpContent for the form to be posted.
        var requestContent = new FormUrlEncodedContent(keys.ToArray());

        var publicApi = Config.PaymentSpring_PublicKey(true);
        var byteArray = new UTF8Encoding().GetBytes(publicApi + ":");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

        //added
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        HttpResponseMessage response = await client.PostAsync(url, requestContent);
        HttpContent responseContent = response.Content;
        using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync())) {
          var result = await reader.ReadToEndAsync();
          if (JsonConvert.DeserializeObject<dynamic>(result).errors != null) {
            var builder = new List<string>();
            for (var i = 0; i < JsonConvert.DeserializeObject<dynamic>(result).errors.Length; i++) {
              builder.Add(JsonConvert.DeserializeObject<dynamic>(result).errors[i].message + " (" + JsonConvert.DeserializeObject<dynamic>(result).errors[i].code + ").");
            }
            throw new PermissionsException(String.Join(" ", builder));
          }
          var r = JsonConvert.DeserializeObject<PaymentTokenVM>(result);
          if (r.@class != "token") {
            throw new PermissionsException("Id must be a token");
          }

          if (String.IsNullOrWhiteSpace(r.id)) {
            throw new PermissionsException("Token was empty");
          }

          if (r.card_owner_name != owner) {
            throw new PermissionsException("Owner incorrect");
          }

          return r;
        }
      }
    }

    public interface IChargeViaHangfire {
      //Change inteface with care. this will break outstanding jobs.
      Task<HangfireChargeResult> ChargeViaHangfire(long organizationId, long unchecked_taskId, bool forceUseTest, bool sendReceipt, DateTime executeTime);
    }
    #endregion

  }

}

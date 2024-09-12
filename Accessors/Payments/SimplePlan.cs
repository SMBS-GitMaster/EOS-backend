using RadialReview.Models;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Accessors.Payments {
  public class SimplePlan {
    public SimplePlan(long orgId, string orgName,AccountType accountType,DateTime executionTime, PaymentPlan_Monthly plan, bool meetingsEnabled,bool peopleEnabled) {
      if (plan==null) {
        throw new ArgumentNullException(nameof(plan));
      }
      FirstNUsersFree = plan.FirstN_Users_Free;
      DurationDescription = plan.MultiplierDesc();
      DurationMultiplier = plan.DurationMultiplier();
      BaselinePrice = plan.BaselinePrice;
      MeetingEnabled = meetingsEnabled;
      PeopleToolsEnabled = peopleEnabled;
      OrganizationName = orgName;
      OrganizationId = orgId;
      MeetingPricePerPerson = plan.L10PricePerPerson;
      PeopleToolsPricePerPerson = plan.ReviewPricePerPerson;
      MeetingFreeUntil = plan.L10FreeUntil ?? DateTime.MinValue;
      PeopleToolsFreeUntil = plan.ReviewFreeUntil ?? DateTime.MinValue;
      FreeUntil = plan.FreeUntil;
      AccountType = accountType;
      ExecutionTime = executionTime;
    }

    public SimplePlan() {
      DurationMultiplier=1;
      MeetingFreeUntil = DateTime.MinValue;
      PeopleToolsFreeUntil = DateTime.MinValue;
      FreeUntil = DateTime.MinValue;
      ExecutionTime = DateTime.UtcNow;
    }

    public string OrganizationName { get; set; }
    public long OrganizationId { get; set; }
    public int FirstNUsersFree { get; set; }
    public bool MeetingEnabled { get; set; }
    public bool PeopleToolsEnabled { get; set; }
    public decimal BaselinePrice { get; set; }
    public string DurationDescription { get; set; }
    public decimal DurationMultiplier { get; set; }
    public decimal MeetingPricePerPerson { get; set; }
    public DateTime MeetingFreeUntil { get; set; }
    public decimal PeopleToolsPricePerPerson { get; set; }
    public DateTime PeopleToolsFreeUntil { get; set; }
    public DateTime FreeUntil { get; set; }
    public AccountType AccountType { get; set; }
    public DateTime ExecutionTime { get; set; }
  }
}

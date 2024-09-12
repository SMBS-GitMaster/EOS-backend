using System;
using System.Collections.Generic;

namespace RadialReview.Accessors.Payments {
  public class UQ {
    public long OrgId { get; set; }
    public long UserOrgId { get; set; }
    public bool? IsRadialAdmin { get; set; }
    public bool IsClient { get; set; }
    public String UserId { get; set; }
    public bool IsRegistered { get; set; }
    public bool EvalOnly { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public DateTime? AttachTime { get; set; }
    //public HashSet<string> ChargedFor { get; set; }

    public List<ChargeDescription> ChargeDescription { get; set; }

    public bool IsFreeUser { get; set; }
    public DateTime? DeleteTime { get; set; }
    public bool UsingWeeklyMeeting { get; set; }
    public bool UsingPeopleTools { get; internal set; }

    public UQ() {
      //ChargedFor = new HashSet<string>();
      ChargeDescription = new List<ChargeDescription>();
    }

    public override string ToString() {
      return Email+" - " +OrgId;
    }
  }


}

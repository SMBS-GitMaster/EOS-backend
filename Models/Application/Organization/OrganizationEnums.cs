using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Models {
  public enum CoachType {
    Unknown = 0,
    CertifiedOrProfessionalEOSi = 1,
    BaseCamp = 2,
    BusinessCoach = 3,
    Other = 4,
    BloomGrowthCoach = 5
  }

  public enum HasCoach {
    Unknown = 0,
    Yes = 1,
    No = 2,
    Other = 3
  }

  public enum EosUserType {
    Unknown = 0,
    Visionary = 1,
    Integrator = 2,
    HR = 4,
    Ops = 5,
    SystemAdmin = 6,
    Other = 7,
    SalesOrMarketing = 8,
    Finance = 9,
    Implementor = 10,
  }

  public enum LockoutType {
    NoLockout = 0,
    Payment,
    MigratedToV2
  }

  public enum ShareVtoPages {
    [Display(Name = "Future Focus Only")]
    FutureFocusOnly = 0,
    [Display(Name = "Both Future Focus and Short-Term Focus")]
    BothFFAndSTF = 2,
    [Display(Name = "Both Future Focus and Short-Term Focus excluding Issues")]
    BothFFAndSTFNoIssues = 3,
  }
}

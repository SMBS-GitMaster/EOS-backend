using System.ComponentModel.DataAnnotations;

namespace RadialReview.Core.Models.Enums {
  public enum ClientSuccessTag {
    None = 0,
    [Display(Name = "Franchise Corporate")]
    FranchiseCorporate = 10,
    [Display(Name = "Franchisor Brand")]
    FranchisorBrand = 20,
    Franchisee = 30,
    [Display(Name = "Bloom Client")]
    BloomClient = 40,
    [Display(Name = "Bloom Coach")]
    BloomCoach = 50,
    [Display(Name = "Umbrella Org")]
    UmbrellaOrg = 60,
    Spam = 70,
    [Display(Name = "Duplicate Accounts")]
    DuplicateAccounts = 80,
    Partner = 90
  }
}

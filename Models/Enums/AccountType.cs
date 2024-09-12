using System.ComponentModel.DataAnnotations;

namespace RadialReview.Models.Enums {
	public enum AccountType {
		Paying = -20,
		Coach = -12,
		UserGroup = -11,
		Implementer = -10,
    BloomGrowthCoach = -13,
		Invalid = -5,
		[Display(Name = "Trial")]
		Demo = 0,
		Other = 10,
		SwanServices = 11,
		FreeForever = 12,
		Dormant = 20,
		Cancelled = 30,
	}

	public static class AccountTypeExtensions {
		public static bool IsImplementerOrCoach(this AccountType accountType) {
			return accountType == AccountType.Coach || accountType == AccountType.Implementer || accountType == AccountType.BloomGrowthCoach;
		}

	}

}
using System;

namespace RadialReview.Models.UserModels {
	public class HangfireCaller {
		public HangfireCaller() {

		}
		public HangfireCaller(UserOrganizationModel caller) {
			UserOrganizationId = caller.Id;
			TimezoneOffset = caller.GetTimezoneOffset();
			ConnectionId = caller.GetConnectionId();
		}

		public long UserOrganizationId { get; set; }
		public int TimezoneOffset { get; set; }
		public string ConnectionId { get; set; }
		public DateTime GetCallerLocalTime() {
			try {
				return DateTime.UtcNow.AddMinutes(TimezoneOffset);
			} catch (ArgumentOutOfRangeException) {
				if (TimezoneOffset > 0) {
					return DateTime.MaxValue;
				}
				return DateTime.MinValue;
			}
		}
	}
}

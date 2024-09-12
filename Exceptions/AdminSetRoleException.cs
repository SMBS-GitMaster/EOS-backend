﻿using RadialReview.Models.Admin;

namespace RadialReview.Exceptions {
	public class AdminSetRoleException : RedirectException, ISafeExceptionMessage {
		public long RequestedRoleId { get; set; }
		public string RequestedEmail { get; set; }

		public AdminAccessLevel AccessLevel { get; set; }

		public AdminSetRoleException(long requestedRoleId) : base("Admin session does not exist.") {
			RequestedRoleId = requestedRoleId;
			AccessLevel =AdminAccessLevel.View;
		}
		public AdminSetRoleException(string requestedEmail) : base("Admin session does not exist.") {
			RequestedEmail = requestedEmail;
			AccessLevel = AdminAccessLevel.SetAs;
		}
	}
}
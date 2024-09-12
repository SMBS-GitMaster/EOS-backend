using RadialReview.Models.Enums;

namespace RadialReview.Models.Variables {
	public class GetStartedMessages {
		public string VerificationEmailAlertMessage { get; set; }

		public string[] VisionaryRoles { get; set; }
		public string[] IntegratorRoles { get; set; }
		public string[] SalesMarketingRoles { get; set; }
		public string[] OperationsRoles { get; set; }
		public string[] FinanceRoles { get; set; }

		public bool EnableMeetingItemCreation { get; set; }

		public MeetingItem[] AllRocks { get; set; }
		public MeetingItem[] AllTodo { get; set; }
		public MeetingItem[] ContactsTodos { get; set; }
		public MeetingItem[] ContactsHeadlines { get; set; }
		public MeetingItem[] ContactsIssue { get; set; }
		public MeetingItem[] ContactsMeasurables { get; set; }

		public string L10EnrollmentUrl_Fallback { get; set; }
		public string L10EnrollmentUrl_Inexperienced { get; set; }
		public string L10EnrollmentUrl_Experienced { get; set; }
		public double L10EnrollmentUrl_ExperiencedYears { get; set; }


		public GetStartedMessages() {
			VerificationEmailAlertMessage = "Please check your email, we've sent you a verification link!";

			VisionaryRoles = new[] { "20 Ideas", "Creative/Problem Solving", "Big Relationships", "Culture", "R&D" };
			IntegratorRoles = new[] { "Lead and manage", "Profit & Loss / Business Plan", "Remove Obstacles & Barriers", "Special Projects / Management" };
			SalesMarketingRoles = new[] { "Lead and manage", "Sales / Revenue Goal", "Selling", "Marketing", "Sales & Marketing Processes" };
			OperationsRoles = new[] { "Lead and manage", "Customer Service", "Process Management", "Make the Product", "Providing the Service" };
			FinanceRoles = new[] { "Lead and manage", "AR / AP", "Budgeting", "Reporting", "HR/Admin", "IT", "Office Management", };

			EnableMeetingItemCreation = true;
			AllRocks = new[] { new MeetingItem("Read the book Traction by Gino Wickman", "This example goal was auto-generated.") };
			AllTodo = new[] { new MeetingItem("Watch the Bloom Growth Weekly Meeting video", "https://app.bloomgrowth.com/t/L10") };
			ContactsHeadlines = new[] { new MeetingItem("Congratulations on your first Bloom Growth weekly meeting!", "") };
			ContactsIssue = new[] { new MeetingItem("Generate a list of all your team's issues", "Expertly allocate your team's energy by solving your most important issues. Not the easiest, nor the quickest, but the most important.\n\nIdentify the issue, Discuss the issue, and Solve the issue in a permanent and inspiring way. Take to-dos that capture this solve and hold each other accountable to their completion. You’re well on your way to maximizing the effectiveness of your team.") };
			ContactsMeasurables = new[] {
				new MeetingItem("Revenue",1000,LessGreater.GreaterThanNotEqual, UnitType.Dollar),
				new MeetingItem("New Leads",5,LessGreater.GreaterThan, UnitType.None),
				new MeetingItem("Customer Rating",7,LessGreater.GreaterThan, UnitType.None)
			};

			L10EnrollmentUrl_Fallback = "https://www.youtube.com/watch?v=HmV6_fH5NkU";
			L10EnrollmentUrl_Inexperienced = "https://www.youtube.com/watch?v=HmV6_fH5NkU";
			L10EnrollmentUrl_Experienced = "https://www.youtube.com/watch?v=HmV6_fH5NkU";
			L10EnrollmentUrl_ExperiencedYears = 0.5;

		}

		public class MeetingItem {
			public MeetingItem() {
			}

			public MeetingItem(string name, string details) {
				Name = name;
				Details = details;
			}

			public MeetingItem(string name, decimal goal, LessGreater goalDirection, UnitType units) {
				Name = name;
				Goal = goal;
				GoalDirection = goalDirection;
				Units = units;
			}

			public string Details { get; set; }
			public string Name { get; set; }
			public decimal? Goal { get; set; }
			public LessGreater? GoalDirection { get; set; }
			public UnitType? Units { get; set; }
		}
	}
}
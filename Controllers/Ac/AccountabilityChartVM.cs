namespace RadialReview.Core.Controllers
{
  public partial class AccountabilityController
  {
    public class AccountabilityChartVM {
			public string CompanyImageUrl { get; set; }

			public long UserId;
			public long OrganizationId;
			public long ChartId;
			public long? FocusNode { get; internal set; }

			public bool ExpandAll { get; set; }

			public string Json { get; set; }

			public string CompanyName { get; set; }

			public bool CanEditHierarchy { get; set; }

			public bool IsVerified { get; set; }
		}
	}
}

using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Interfaces;
using System.Threading.Tasks;

namespace RadialReview.Models.Scorecard {
	public partial class ScoreModel : IIssue {

		public virtual async Task<string> GetIssueMessage() {
			return Measurable.Title;
		}

        public virtual async Task<string> GetIssueDetails(INotesProvider notesProvider) {

            string week = ForWeek.AddDays(-7).ToString("d");
            string accountable = Measurable.AccountableUser.NotNull(x => x.GetName());
            string admin = Measurable.AdminUser.NotNull(x => x.GetName());

            if (admin != accountable) {
                accountable += "/" + admin;
            }

            string footer = $"Week: {week} \nOwner: {accountable} \n\n {GetMeasurableState()}";

            if (!Measured.HasValue)
                return footer;

			string recorded = "RECORDED: " + Measurable.UnitType.Format(Measured.Value);
            return GetGoalDescription() + "\n" + recorded + "\n\n" + footer;
        }

	}
}
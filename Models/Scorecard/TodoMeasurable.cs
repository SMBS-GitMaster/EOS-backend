using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.Interfaces;
using System.Threading.Tasks;

namespace RadialReview.Models.Scorecard {

	public partial class ScoreModel : ITodo {

		public virtual async Task<string> GetTodoMessage() {
			return string.Empty;
		}
		public virtual async Task<string> GetTodoDetails(INotesProvider notesProvider) {

			string week = ForWeek.AddDays(-7).ToString("d");
			string accountable = Measurable.AccountableUser.NotNull(x => x.GetName());
			string admin = Measurable.AdminUser.NotNull(x => x.GetName());

			if (admin != accountable) {
				accountable += "/" + admin;
			}

			string footer = $"{GetMeasurableState()} \n\nWeek: {week} \nOwner: {accountable}";
			if (!Measured.HasValue)
				return footer;

			string recorded = $"RECORDED: {Measurable.UnitType.Format(Measured.Value)}";
			return GetGoalDescription() + "\n" + recorded + "\n\n" + footer;
		}

	}
}
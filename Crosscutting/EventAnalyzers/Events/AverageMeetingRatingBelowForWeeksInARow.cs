using System.Collections.Generic;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using System.Threading.Tasks;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using RadialReview.Models.Frontend;
using System.ComponentModel.DataAnnotations;
using NHibernate;
using RadialReview.Models.L10;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class AverageMeetingRatingBelowForWeeksInARow : IEventAnalyzer, IEventAnalyzerGenerator, IRecurrenceEventAnalyerGenerator {


		[Display(Name = "Meeting")]
		public long RecurrenceId { get; set; }
		[Display(Prompt = "Enter threshold")]
		public decimal RatingTheshold { get; set; }
		[Display(Description = "Number of weeks in a row before firing")]
		public int WeeksInARow { get; set; }
		public LessGreater Direction { get; set; }

		public string EventType { get { return "AverageMeetingRatingBelowForWeeksInARow"; } }

		public AverageMeetingRatingBelowForWeeksInARow(long recurrenceId) {
			RecurrenceId = recurrenceId;
			RatingTheshold = 7;
			WeeksInARow = 2;
			Direction = LessGreater.LessThanOrEqual;
		}


		public bool IsEnabled(IEventSettings settings) {
			return true;
		}

		public IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(Direction, RatingTheshold);
		}

		public EventFrequency GetExecutionFrequency() {
			return EventFrequency.Weekly;
		}

		public int GetNumberOfFailsToTrigger(IEventSettings settings) {
			return WeeksInARow;
		}

		public int GetNumberOfPassesToReset(IEventSettings settings) {
			return 1;
		}

		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var meetings = await settings.Lookup(new SearchRealL10Meeting(RecurrenceId));
			return EventHelper.ToBinnedEventsFromRatio(EventFrequency.Weekly, meetings, x => x.StartTime, x => x.AverageMeetingRating);
		}

		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
			return new[] { this };
		}

		public async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.RatingTheshold),
				EditorField.FromProperty(this,x=>x.Direction),
				EditorField.FromProperty(this,x=>x.WeeksInARow),
			};
		}

		private string _MeetingName { get; set; }
		public async Task PreSaveOrUpdate(ISession s) {
			_MeetingName = s.Get<L10Recurrence>(RecurrenceId).Name;
		}

		public string Name {
			get {
				return "Average consecutive meeting rating";
			}
		}
		public string Description {
			get {
				return string.Format("{0} for {1} weeks in a row{2}", Direction.ToDescription(RatingTheshold), WeeksInARow, _MeetingName.NotNull(x => " for " + x) ?? "");
			}
		}










	}
}

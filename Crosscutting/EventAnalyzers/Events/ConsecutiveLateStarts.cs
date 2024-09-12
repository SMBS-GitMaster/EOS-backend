using System.Collections.Generic;
using System.Linq;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Models.Enums;
using RadialReview.Crosscutting.EventAnalyzers.Searchers;
using System.Threading.Tasks;
using RadialReview.Models.Frontend;
using System.ComponentModel.DataAnnotations;
using NHibernate;
using RadialReview.Models.L10;

namespace RadialReview.Crosscutting.EventAnalyzers.Events {
	public class ConsecutiveLateStarts : IEventAnalyzer, IEventAnalyzerGenerator, IRecurrenceEventAnalyerGenerator {
		public ConsecutiveLateStarts(long recurrenceId) {
			RecurrenceId = recurrenceId;
			WeeksInARow = 2;
			MinutesLate = 5;
		}

		[Display(Name = "Meeting")]
		public long RecurrenceId { get; set; }
		public int WeeksInARow { get; set; }
		public decimal MinutesLate { get; set; }

		public string EventType { get { return "ConsecutiveLateStarts"; } }
		public IThreshold GetFireThreshold(IEventSettings settings) {
			return new EventThreshold(LessGreater.GreaterThan, MinutesLate);
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

		public bool IsEnabled(IEventSettings settings) {
			return true;
		}

		public async Task<IEnumerable<IEventAnalyzer>> GenerateAnalyzers(IEventSettings settings) {
			return new[] { this };
		}

		public async Task<IEnumerable<IEvent>> GenerateEvents(IEventSettings settings) {
			var l10Meetings = await settings.Lookup(new SearchRealL10Meeting(RecurrenceId));

			var divisor = 15m;


			var evts = EventHelper.ToBinnedEvents(
				EventFrequency.Weekly,
				l10Meetings,
				x => x.StartTime,
				x => {
					if (!x.Any())
						return null;
					var minutes = x.Max(y => ((y.StartTime.Value.Minute + divisor / 2.0m) % divisor) - divisor / 2.0m);
					return new BaseEvent(minutes, x.Date);
				}
			);

			return evts;
		}

		public async Task<IEnumerable<EditorField>> GetSettingsFields(IEventGeneratorSettings settings) {
			//todo EditorField
			return new[] {
				EditorField.DropdownFromProperty(this,x=>x.RecurrenceId,settings.VisibleRecurrences),
				EditorField.FromProperty(this,x=>x.MinutesLate),
				EditorField.FromProperty(this,x=>x.WeeksInARow),
			};
		}
		private string _MeetingName { get; set; }
		public async Task PreSaveOrUpdate(ISession s) {
			_MeetingName = s.Get<L10Recurrence>(RecurrenceId).Name;
		}

		public string Name {
			get {
				return "Consecutive late meeting starts";
			}
		}
		
		public string Description {
			get {
				return string.Format("{0} minutes for {1} weeks in a row{2}", LessGreater.GreaterThan.ToDescription(MinutesLate), WeeksInARow, _MeetingName.NotNull(x => " for " + x) ?? "");
			}
		}
	}
}

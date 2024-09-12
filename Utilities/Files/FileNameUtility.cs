using RadialReview.Utilities.DataTypes;

namespace RadialReview.Utilities.Files {
	public class FileNameUtility {

		public class NameDuplication {

			private DefaultDictionary<string, int> Count = new DefaultDictionary<string, int>(x => 0);

			public string AdjustName(string name) {
				var shouldAdjust = (Count[name] > 0);
				Count[name] += 1;
				if (shouldAdjust) {
					name += " (" + Count[name] + ")";
				}
				return name;
			}
		}


		public static NameDuplication CreateNameDeduplicator() {
			return new NameDuplication();
		}


	}
}
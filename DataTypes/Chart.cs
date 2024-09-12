using RadialReview.Models.Angular.Base;

namespace RadialReview.Utilities.DataTypes {
	public class Chart<T> : BaseAngular {

		public Chart(long id) : base(id) {
		}

		public Chart() {
		}

		public string height { get; set; }
		public string width { get; set; }
		public T data { get; set; }
	}
}
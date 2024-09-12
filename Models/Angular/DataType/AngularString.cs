using RadialReview.Models.Angular.Base;

namespace RadialReview.Models.Angular.DataType {
	public class AngularString : BaseAngular {

		public AngularString() {
		}

		public AngularString(long id) : base(id) { }

		public AngularString(long id, string str) : base(id) {
			Data = str;
		}

		public string Data { get; set; }
	}
}

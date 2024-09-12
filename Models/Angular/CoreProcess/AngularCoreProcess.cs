using RadialReview.Models.Angular.Base;

namespace RadialReview.Models.Angular.CoreProcess {
	public class AngularCoreProcess : BaseAngular {
		public AngularCoreProcess(long id) : base(id) {
		}
		public AngularCoreProcess() {
		}

		public string Name { get; set; }
	}
}
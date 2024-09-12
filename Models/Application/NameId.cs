using RadialReview.Models.Angular.Base;
using System;
using System.Diagnostics;

namespace RadialReview.Models.Application {
	[DebuggerDisplay("{Id},{Name}")]
	public class NameId : BaseAngular {
		public string Name { get; set; }

		[Obsolete("do not use")]
		public NameId() {
		}

		public NameId(string name, long id) {
			Name = name;
			Id = id;
		}

		public NameId(long id) : base(id) {
			Id = id;
		}
	}
}
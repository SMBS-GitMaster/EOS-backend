using Newtonsoft.Json;
using System;

namespace RadialReview.Models.Angular.Base {
	public class AngularPointer {
		public string Key {
			get { return Reference.GetKey(); }
		}
		[JsonIgnore]
		public DateTime LastUpdate { get; set; }

		public int _P { get { return 1; } }

		[JsonIgnore]
		public IAngularId Reference { get; set; }

		public AngularPointer(IAngularId reference, DateTime time) {
			Reference = reference;
			LastUpdate = time;
		}
	}
}

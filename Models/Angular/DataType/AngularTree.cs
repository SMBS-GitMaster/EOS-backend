using Newtonsoft.Json;
using RadialReview.Models.Angular.Base;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace RadialReview.Models.Angular {
	public class AngularTreeNode<T> : BaseAngular where T : AngularTreeNode<T> {

		public AngularTreeNode() {
		}

		public AngularTreeNode(long id) : base(id) { }
		[IgnoreDataMember]
		public bool collapsed { get; set; }
		[IgnoreDataMember]
		public int? order { get; set; }
		[IgnoreDataMember]
		public bool? Editable { get; set; }
		[IgnoreDataMember]
		public bool? Me { get; set; }

		protected IEnumerable<T> __children { get; set; }

		[JsonProperty(PropertyName = "Children")]
		public IEnumerable<T> children { get { return !collapsed ? __children : null; } set { collapsed = false; __children = value; } }
		[IgnoreDataMember]
		public IEnumerable<T> _children { get { return collapsed ? __children : null; } set { collapsed = true; __children = value; } }

		public void SetChildren(IEnumerable<T> children, bool? collapse = null) {
			__children = children;
			collapsed = collapse ?? collapsed;
		}

		public List<T> GetDirectChildren() {
			if (__children == null)
				return new List<T>();
			return __children.ToList();
		}
	}
}

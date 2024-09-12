using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Angular.Base {
	public interface IAngular {
	}

	public interface IAngularId : IAngular {
		object GetAngularId();
		string GetAngularType();
	}

	public interface IAngularItem : IAngularId, ILongIdentifiable {
		long Id { get; set; }
		string Type { get; }
		bool Hide { get; }
	}

	public interface IAngularItemString : IAngularId {
		string Id { get; set; }
		string Type { get; }
		bool Hide { get; }
	}

	public interface IAngularUpdate : IAngular {
	}

	public static class IAngularExtensions {
		public static string GetKey(this IAngularId self) { return self.GetAngularType() + "_" + self.GetAngularId(); }
	}

	public interface IAngularIgnore {

	}

	public static class AngularIgnore {
		public static AngularIgnore<T> Create<T>(T item) {
			return new AngularIgnore<T>(item);
		}
	}

	public class AngularIgnore<T> : IAngularIgnore {
		public T Item { get; set; }
		public AngularIgnore(T item) {
			Item = item;
		}
	}
}

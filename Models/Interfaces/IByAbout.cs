namespace RadialReview.Models.Interfaces {
    public interface IForModel {
        long ModelId { get; }
        string ModelType { get; }
        bool Is<T>();
		string ToPrettyString();
	}
    public interface IByAbout {
        IForModel GetBy();
        IForModel GetAbout();
    }
}

namespace RadialReview.Models.Interfaces
{
    public interface ICompletable
    {
        ICompletionModel GetCompletion(bool split=false);
    }
}

using System.Collections.Generic;

namespace RadialReview.Models.Interfaces
{
    public interface ICompletionModel
    {
        bool Started { get; }
        bool FullyComplete { get; }
        List<CompletionModel> GetCompletions();
	    decimal GetPercentage();
        bool Illegal { get; }
    }
}

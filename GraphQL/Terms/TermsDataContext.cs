using RadialReview.GraphQL.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {
    Task<TermsQueryModel> GetTermsAsync(CancellationToken cancellationToken);
  }

  public partial class DataContext : IDataContext
  {
    public async Task<TermsQueryModel> GetTermsAsync(CancellationToken cancellationToken)
    {
      return repository.GetTerms(cancellationToken);
    }

  }
}

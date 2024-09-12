using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<string> GetMeetingCurrentPageAsync(long meetingId, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public async Task<string> GetMeetingCurrentPageAsync(long meetingId, CancellationToken cancellationToken)
    {
      return (repository.GetMeetingCurrentPage(meetingId, cancellationToken));
    }

  }
}

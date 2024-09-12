using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadialReview.Core.GraphQL;
using RadialReview.GraphQL.Models;

namespace RadialReview.Repositories
{
  public partial interface IDataContext
  {

    Task<string> GetNoteText(string padId);

    Task<string> GetNoteHTML(string padId);

    IQueryable<long> GetL10MeetingNotes(long meetingId, CancellationToken cancellationToken);

  }

  public partial class DataContext : IDataContext
  {

    public Task<string> GetNoteText(string padId)
    {
      return repository.GetNoteText(padId);
    }

    public Task<string> GetNoteHTML(string padId)
    {
      return repository.GetNoteHTML(padId);
    }

    public IQueryable<long> GetL10MeetingNotes(long meetingId, CancellationToken cancellationToken)
    {
      return repository.GetL10MeetingNotes(meetingId, cancellationToken);
    }

  }
}

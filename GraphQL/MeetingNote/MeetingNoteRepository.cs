using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{

  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<long> GetL10MeetingNotes(long meetingId, CancellationToken cancellationToken);

    IQueryable<MeetingNoteQueryModel> GetNotesForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateMeetingNote(NoteCreateModel note);

    Task<IdModel> EditMeetingNote(EditNoteModel editNoteModel);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public IQueryable<long> GetL10MeetingNotes(long meetingId, CancellationToken cancellationToken)
    {
      return L10Accessor.GetL10MeetingNotes(caller, meetingId).AsQueryable();
    }

    public IQueryable<MeetingNoteQueryModel> GetNotesForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken)
    {
      var results =
          recurrenceIds.SelectMany(recurrenceId =>
          {
            var notes = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long> { recurrenceId });
            var meetingNoteModels = notes.Select(note => RepositoryTransformers.MeetingNoteFromL10Note(note));
            return meetingNoteModels;
          });

      return results.AsQueryable();
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateMeetingNote(NoteCreateModel note)
    {
      //archived, archivedTimestamp and owner parameters have been ignored.
      var padId = await L10Accessor.CreateNote(caller, note.MeetingId, note.Title, note.NotesId);
      long noteId = L10Accessor.GetVisibleL10Notes_Unsafe(new List<long>{note.MeetingId}).Where(x => x.PadId == padId).Select(x => x.Id).FirstOrDefault();

      return new IdModel(noteId);
    }

    public async Task<IdModel> EditMeetingNote(EditNoteModel model)
    {
      double? deleteTimestamp = model.Archived != null && model.Archived.Value ? DateTime.UtcNow.ToUnixTimeStamp() : null;
      await L10Accessor.EditNoteInRecurrence(caller, model.MeetingNoteId, model.MeetingId, model.Title, delete: model.Archived, padId: model.NotesId, deleteTimestamp: deleteTimestamp);

      return new IdModel(model.MeetingNoteId);
    }

    #endregion

  }
}
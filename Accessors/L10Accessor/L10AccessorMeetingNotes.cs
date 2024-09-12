using RadialReview.Accessors;
using RadialReview.Core.Models.L10;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Accessors
{
  public partial class L10Accessor : BaseAccessor
  {
    public static async Task CreateL10MeetingNotes(UserOrganizationModel caller, long meetingId, List<string> notesIds)
    {
      using (var session = HibernateSession.GetCurrentSession())
      {
        using (var transaction = session.BeginTransaction())
        {
          PermissionsUtility.Create(session, caller).ViewL10Meeting(meetingId);
          foreach (var notesId in notesIds)
          {
            var meetingNote = new L10MeetingNote {
              NotesId = notesId,
              MeetingId = meetingId
            };
            session.Save(meetingNote);
          }
          transaction.Commit();
          session.Flush();
        }
      }
    }

    public static List<long> GetL10MeetingNotes(UserOrganizationModel caller, long meetingId)
    {
      using(var session = HibernateSession.GetCurrentSession())
      {
        using(var tx = session.BeginTransaction())
        {
          PermissionsUtility.Create(session, caller).ViewL10Meeting(meetingId);
          RadialReview.Models.L10.L10Note note = null;
          var notes = session.QueryOver<L10MeetingNote>()
          .JoinAlias(x => x.L10Note, () => note)
          .Where(m => m.MeetingId == meetingId && m.DeleteTime == null)
          .List()
          .Select(n => n.L10Note.Id)
          .ToList();

          tx.Commit();
          return notes;
        }
      }
    }

    public static async Task DeleteL10MeetingNote(UserOrganizationModel caller, long instanceId, List<string> notesId)
    {
      var session = HibernateSession.GetCurrentSession();
      var transaction = session.BeginTransaction();

      PermissionsUtility.Create(session, caller).ViewL10Meeting(instanceId);

      var l10meetingNotes = session.QueryOver<L10MeetingNote>()
        .WhereRestrictionOn(x => x.NotesId).IsIn(notesId).And(x => x.MeetingId == instanceId).List().ToList();

      
      foreach(var note in l10meetingNotes)
      {
        note.DeleteTime = DateTime.UtcNow;
        session.Update(note);
      }

      transaction.Commit();
      session.Flush();
    }
  }
}

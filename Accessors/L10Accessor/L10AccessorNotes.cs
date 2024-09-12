using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
//using ListExtensions = WebGrease.Css.Extensions.ListExtensions;
//using System.Web.WebPages.Html;

namespace RadialReview.Accessors {
	public partial class L10Accessor : BaseAccessor {

		#region Notes
		public static L10Note GetNote(UserOrganizationModel caller, long noteId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewL10Note(noteId);
					return s.Get<L10Note>(noteId);
				}
			}
		}

    public static L10Note GetNoteInRecurrence(UserOrganizationModel caller, long noteId, long recurrenceId)
    {
      var note = GetNote(caller, noteId);
      if (note.Recurrence.Id != recurrenceId)
        throw new PermissionsException("Note does not exist.");
      return note;
    }
    public static List<L10Note> GetVisibleL10Notes_Unsafe(List<long> recurrences) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					List<L10Note> notes = GetVisibleL10Notes_Unsafe(s, recurrences);
					return notes;
				}
			}
		}

		public static List<L10Note> GetVisibleL10Notes_Unsafe(ISession s, List<long> recurrences) {
			return s.QueryOver<L10Note>().Where(x => x.DeleteTime == null)
									.WhereRestrictionOn(x => x.Recurrence).IsIn(recurrences.ToArray())
									.List().ToList();
		}

      public static async Task<string> CreateNote(UserOrganizationModel caller, long recurrenceId,
                                                      string name, string notesId = null)
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {
            PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
            var note = new L10Note()
            {
              Name = name,
              Contents = "",
              Recurrence = s.Load<L10Recurrence>(recurrenceId)
            };
            if (!string.IsNullOrEmpty(notesId))
            {
              note.PadId = notesId;
            }
            s.Save(note);
            await using (var rt = RealTimeUtility.Create())
            {
              var group = rt.UpdateRecurrences(recurrenceId);
              group.Call("createNote", note.Id, name);
              var rec = new AngularRecurrence(recurrenceId)
              {
                Notes = new List<AngularMeetingNotes>(){
                  new AngularMeetingNotes(note)
                }
              };
              group.Update(rec);
            }

            await Audit.L10Log(s, caller, recurrenceId, "CreateNote", ForModel.Create(note), name);
            await HooksRegistry.Each<INoteHook>((sess, x) => x.CreateNote(sess, caller, note));
            tx.Commit();
            s.Flush();
            return note.PadId;
          }
        }
      }


    public static async Task EditNote(UserOrganizationModel caller, long noteId, /*string contents = null,*/ string name = null, string connectionId = null, bool? delete = null, string padId = null, double? deleteTimestamp = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var note = s.Get<L10Note>(noteId);
					PermissionsUtility.Create(s, caller).EditL10Recurrence(note.Recurrence.Id);
					await using (var rt = RealTimeUtility.Create(connectionId)) {
						var now = DateTime.UtcNow;
            //if (contents != null) {
            //    note.Contents = contents;
            //    hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteContents(noteId, contents, now.ToJavascriptMilliseconds());
            //}

            if (padId != null && note.PadId != padId)
            {
              note.PadId = padId;
            }

						if (name != null) {
							note.Name = name;
							rt.UpdateRecurrences(note.Recurrence.Id).Call("updateNoteName", noteId, name);
						}
					}
					_ProcessDeleted(s, note, delete, deleteTimestamp);
					s.Update(note);
					await Audit.L10Log(s, caller, note.Recurrence.Id, "EditNote", ForModel.Create(note), note.Name + ":\n" + note.Contents);
          await HooksRegistry.Each<INoteHook>((sess, x) => x.UpdateNote(sess, caller, note, null));
          tx.Commit();
					s.Flush();
				}
			}
		}

    public static async Task EditNoteInRecurrence(UserOrganizationModel caller, long noteId, long recurrenceId, string name = null, string connectionId = null, bool? delete = null, string padId = null, double? deleteTimestamp = null)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var note = GetNoteInRecurrence(caller, noteId, recurrenceId);
          await using (var rt = RealTimeUtility.Create(connectionId))
          {
            var now = DateTime.UtcNow;
            //if (contents != null) {
            //    note.Contents = contents;
            //    hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(note.Recurrence.Id), connectionId).updateNoteContents(noteId, contents, now.ToJavascriptMilliseconds());
            //}

            if (padId != null && note.PadId != padId)
            {
              note.PadId = padId;
            }

            if (name != null)
            {
              note.Name = name;
              rt.UpdateRecurrences(note.Recurrence.Id).Call("updateNoteName", noteId, name);
            }
          }
          _ProcessDeleted(s, note, delete, deleteTimestamp);
          s.Update(note);
          await Audit.L10Log(s, caller, note.Recurrence.Id, "EditNote", ForModel.Create(note), note.Name + ":\n" + note.Contents);
          await HooksRegistry.Each<INoteHook>((sess, x) => x.UpdateNote(sess, caller, note, null));
          tx.Commit();
          s.Flush();
        }
      }
    }
    #endregion
  }
}
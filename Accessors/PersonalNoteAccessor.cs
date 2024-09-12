using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Repositories;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors
{
  public class PersonalNoteAccessor
  {

    public static async Task<PersonalNote> CreatePersonalNote(UserOrganizationModel caller, long workspaceId, string title)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditDashboard(Models.Enums.DashboardType.Standard, workspaceId);

          await PadAccessor.CreatePad(title);

          PersonalNote note = new PersonalNote()
          {
            Title = title,
            WorkspaceId = workspaceId,
            DateCreated = DateTime.UtcNow,
            DateLastModified = DateTime.UtcNow
          };

          s.Save(note);
          s.Flush();
          tx.Commit();

          return note;
        }
      }

    }

    public static async Task<PersonalNote> EditPersonalNote(UserOrganizationModel caller, long noteId, string title = null, bool? archived = false)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {

          var note = s.Get<PersonalNote>(noteId);

          var perms = PermissionsUtility.Create(s, caller);
          perms.EditDashboard(Models.Enums.DashboardType.Standard, note.WorkspaceId);

          if (title != null)
          {
            note.Title = title;
          }

          if (archived != null)
          {
            if (archived.Value)
            {
              note.DeleteTime = DateTime.UtcNow;
            }
            else
            {
              note.DeleteTime = null;
            }
          }

          if (note.DateCreated == null) note.DateCreated = DateTime.UtcNow;
          note.DateLastModified = DateTime.UtcNow;

          s.Save(note);
          s.Flush();
          tx.Commit();

          await HooksRegistry.Each<IWorkspaceNoteHook>((ses, x) => x.UpdateWorkspaceNote(ses, caller, note.Transform()));

          return note;
        }
      }

    }

    public static PersonalNote GetPersonalNote(UserOrganizationModel caller, long personalNoteId)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          return s.Get<PersonalNote>(personalNoteId);
        }
      }
    }

    public static List<PersonalNote> GetPersonalNotesForWorkspace(UserOrganizationModel caller, long workspaceId, bool includeArchived = false)
    {
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          //var perms = PermissionsUtility.Create(s, caller).ViewDashboard(Models.Enums.DashboardType.Standard, workspaceId);
          var query = s.QueryOver<PersonalNote>().Where(_ => _.WorkspaceId == workspaceId);
          if (!includeArchived)
          {
            query.Where(_ => _.DeleteTime == null);
          }

          return query.List().ToList();
        }
      }
    }

  }
}
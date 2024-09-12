using NHibernate;
using RadialReview;
using RadialReview.Exceptions;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.Synchronize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Utilities.RealTime;
using RadialReview.Utilities.DataTypes;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Utilities.Types;
using RadialReview.Core.GraphQL.Types;

namespace RadialReview.Accessors {
  public class HeadlineAccessor : BaseAccessor {

    public static async Task<bool> CreateHeadline(ISession s, PermissionsUtility perms, PeopleHeadline headline) {
      if (headline.Id != 0)
        throw new PermissionsException("Id was not zero");

      if (headline.CreatedDuringMeetingId == -1)
        headline.CreatedDuringMeetingId = null;

      if (headline.CreatedDuringMeetingId != null)
        perms.ViewL10Meeting(headline.CreatedDuringMeetingId.Value);

      perms.ViewOrganization(headline.OrganizationId);

      OrganizationModel om = s.Load<OrganizationModel>(headline.OrganizationId);

      bool shareHeadlines = om._Settings.UsersCanSharePHToAnyMeeting;
      if (!shareHeadlines)
        perms.EditL10Recurrence(headline.RecurrenceId);

      headline.CreatedBy = perms.GetCaller().Id;

      if (headline.OwnerId == 0 && headline.Owner == null) {
        headline.OwnerId = perms.GetCaller().Id;
        headline.Owner = perms.GetCaller();
      } else if (headline.OwnerId == 0 && headline.Owner != null) {
        headline.OwnerId = headline.OwnerId;
      } else if (headline.OwnerId != 0 && headline.Owner == null) {
        headline.Owner = s.Load<UserOrganizationModel>(headline.OwnerId);
      }

      perms.ViewUserOrganization(headline.OwnerId, false);

      if (headline.AboutId != null)
        perms.ViewRGM(headline.AboutId.Value);


      L10Recurrence r = null;
      var recurrenceId = headline.RecurrenceId;

      if (recurrenceId > 0) {
        r = s.Get<L10Recurrence>(recurrenceId);
        await L10Accessor.Depristine_Unsafe(s, perms.GetCaller(), r);
        s.Update(r);
      }


      s.Save(headline);
      headline.Ordering = -headline.Id;

      if (headline.AboutId.HasValue)
        headline.About = s.Get<ResponsibilityGroupModel>(headline.AboutId.Value);

      headline.Owner = s.Get<UserOrganizationModel>(headline.OwnerId);

      s.Update(headline);

      if (!string.IsNullOrWhiteSpace(headline._Details))
        await PadAccessor.CreatePad(headline.HeadlinePadId, headline._Details);

      var cc = perms.GetCaller();
      await HooksRegistry.Each<IHeadlineHook>((ses, x) => x.CreateHeadline(ses, cc, headline));

      return true;
    }

    public static async Task<bool> CreateHeadline(UserOrganizationModel caller, PeopleHeadline headline) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          var created = await CreateHeadline(s, perms, headline);
          tx.Commit();
          s.Flush();
          return created;
        }
      }
    }

    public static async Task UpdateHeadline(UserOrganizationModel caller, HeadlineEditModel model)
    {
      await UpdateHeadline(caller, model.HeadlineId, model.Title, accountableUser: model.Assignee, archiveTimestamp: model.ArchivedTimestamp, noteId: model.NotesId);
    }

    [Untested("Test EnsureAfter")]
    public static async Task UpdateHeadline(UserOrganizationModel caller, long headlineId, string message, long? aboutId = null, string aboutName = null, long? accountableUser = null, double? archiveTimestamp = null, string noteId = null) {
      await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateHeadlineMessage(headlineId), async (s, perms) => {
        var headline = s.Get<PeopleHeadline>(headlineId);
        perms.EditL10Recurrence(headline.RecurrenceId);
        if (accountableUser != null && headline.OwnerId != accountableUser) {
          perms.ViewUserOrganization(headline.OwnerId, false);
        }
      }, async s => {
        //var perms = PermissionsUtility.Create(s, caller);
        var headline = s.Get<PeopleHeadline>(headlineId);

        var updates = new IHeadlineHookUpdates();

        if(noteId != null && headline.HeadlinePadId != noteId)
        {
          headline.HeadlinePadId = noteId;
        }

        if (message != null && headline.Message != message) {
          headline.Message = message;
          updates.MessageChanged = true;
        }
        if (accountableUser != null && headline.OwnerId != accountableUser) {
          //tested above. perms.ViewUserOrganization(headline.OwnerId, false);
          var user = s.Get<UserOrganizationModel>(accountableUser.Value);
          headline.OwnerId = accountableUser.Value;
          headline.Owner = user;
          updates.OwnerChanged = true;
        }
        if (aboutId.HasValue) {
          headline.AboutId = aboutId.Value;
          headline.AboutName = aboutName;
          updates.MessageChanged = true;
          headline.About = s.Get<ResponsibilityGroupModel>(headline.AboutId);
        }

        if (archiveTimestamp.HasValue)
          headline.DeleteTime = archiveTimestamp.Value.FromUnixTimeStamp();
        else
          headline.DeleteTime = null;

        s.Update(headline);
        await HooksRegistry.Each<IHeadlineHook>((ses, x) => x.UpdateHeadline(ses, caller, headline, updates));
      }, null);
    }


    public static PeopleHeadline GetHeadline(UserOrganizationModel caller, long headlineId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.ViewHeadline(headlineId);
          var h = s.Get<PeopleHeadline>(headlineId);

          if (h.Owner != null) {
            h.Owner.GetName();
            h.Owner.GetImageUrl();
          }
          if (h.About != null) {
            h.About.GetName();
            h.About.GetImageUrl();
          }

          return h;
        }
      }
    }

    public static List<NameId> GetRecurrencesWithHeadlines(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          List<long> attendee_recurrences;
          List<long> _nil;
          var uniqueL10NameIds = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, userId, out attendee_recurrences, out _nil);
          var uniqueL10Ids = uniqueL10NameIds.Select(x => x.Id).ToList();


          var recurs = s.QueryOver<L10Recurrence>()
            .Where(x => x.DeleteTime == null && x.HeadlineType == Models.Enums.PeopleHeadlineType.HeadlinesList)
            .WhereRestrictionOn(x => x.Id).IsIn(uniqueL10Ids)
            .Select(x => x.Id, x => x.Name)
            .List<object[]>().Select(x => new NameId((string)x[1], (long)x[0])).ToList();

          var lookup = L10Accessor.GetStarredMeetingsLookup_Unsafe(s, userId);
          return recurs.OrderBy(x => lookup[x.Id] ?? DateTime.MaxValue).ToList();
        }
      }
    }

    public class CopyHeadlineResult {
      public int Success { get; set; }
      public List<string> Errors { get; set; }
      public bool HasError {
        get { return Errors != null && Errors.Count > 0; }
      }
    }

    public static async Task<CopyHeadlineResult> CopyHeadline(UserOrganizationModel caller, INotesProvider notesProvider, long headlineId, long[] candidateChildRecurrenceIds) {
      if (!candidateChildRecurrenceIds.Any()) {
        return new CopyHeadlineResult() {
          Success = 0,
          Errors = new List<string>()
        };
      }

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var now = DateTime.UtcNow;

          var parent = s.Get<PeopleHeadline>(headlineId);

          var perms = PermissionsUtility.Create(s, caller)
            .ViewL10Recurrence(parent.RecurrenceId)
            .ViewHeadline(parent.Id);
          var details = await notesProvider.GetTextForPad(parent.HeadlinePadId);

          var parentMeeting = L10Accessor._GetCurrentL10Meeting(s, perms, parent.RecurrenceId, true, false, false);
          var possible = L10Accessor._GetAllConnectedL10Recurrence(s, caller, parent.RecurrenceId, false, false);

          var childRecurrenceIds = new List<long>();
          var errors = new DefaultDictionary<string, int>(x => 0);
          foreach (var childRecurrenceId in candidateChildRecurrenceIds) {
            var childRecur = s.Get<L10Recurrence>(childRecurrenceId);
            if (childRecur.Organization.Id != caller.Organization.Id) {
              errors["You cannot copy an issue into this meeting."] += 1;
              continue;
            }
            if (parent.DeleteTime != null) {
              errors["Issue does not exist."] += 1;
              continue;
            }

            if (possible.All(x => x.Id != childRecurrenceId)) {
              errors["You do not have permission to copy this issue."] += 1;
              continue;
            }
            childRecurrenceIds.Add(childRecurrenceId);
          }

          var count = childRecurrenceIds.Count();
          int successCount = 0;
          await using (var rt = RealTimeUtility.Create(count > 1)) {
            var statusUpdater = rt.UpdateCaller(caller).GetStatusUpdater();

            await statusUpdater.UpdateStatus(x => x.SetMessage("Sending headlines").SetPercentage(0, childRecurrenceIds.Count()));

            int i = 0;
            foreach (var childRecurrenceId in childRecurrenceIds) {
              try {
                i++;
                var childRecur = s.Get<L10Recurrence>(childRecurrenceId);
                var newHeadline = new PeopleHeadline() {
                  About = parent.About,
                  AboutId = parent.AboutId,
                  AboutName = parent.AboutName,
                  CloseDuringMeetingId = null,
                  CloseTime = null,
                  CreatedDuringMeetingId = parentMeeting.NotNull(x => x.Id),
                  Message = parent.Message,
                  _Details = details,
                  _Notes = details,
                  OrganizationId = childRecur.OrganizationId,
                  Owner = parent.Owner,
                  OwnerId = parent.OwnerId,
                  RecurrenceId = childRecur.Id,
                  CreateTime = now,
                  CreatedBy = caller.Id,
                  HeadlinePadId = Guid.NewGuid().ToString(),
                };

                await CreateHeadline(s, perms, newHeadline);
                await statusUpdater.UpdateStatus(x => x.SetMessage("Sending headlines " + i + "/" + count).SetPercentage(i, childRecurrenceIds.Count()));

                await Audit.L10Log(s, caller, parent.RecurrenceId, "CopyHeadline", ForModel.Create(newHeadline), newHeadline.NotNull(x => x.Message) + " copied " + childRecur.NotNull(x => "into" + x.Name));
                successCount += 1;
              } catch (Exception e) {
                if (e is ISafeExceptionMessage) {
                  errors[e.Message ?? "Unknown error"] += 1;
                } else {
                  errors["Unknown error"] += 1;
                }
              }
            }

            var status = "Sent " + count + " headlines.";
            if (errors.Any()) {
              status += " Failed to send " + errors.Count + ".";
            }
            await statusUpdater.UpdateStatus(x => x.SetMessage(status));

          }

          tx.Commit();
          s.Flush();

          return new CopyHeadlineResult() {
            Success = successCount,
            Errors = errors.ToList().OrderByDescending(x => x.Value).Select(x => {
              return x.Value > 1 ? (x.Key + " (x" + x.Value + ")") : x.Key;
            }).ToList()
          };
        }
      }
    }

    public static List<PeopleHeadline> GetHeadlinesForUser(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);
          var foundQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.OwnerId == userId);
          var found = foundQ.Fetch(x => x.Owner).Eager
                    .Fetch(x => x.About).Eager
                    .List().ToList();
          foreach (var f in found) {
            if (f.Owner != null) {
              var a = f.Owner.GetName();
              var b = f.Owner.ImageUrl(true, ImageSize._32);
            }
            if (f.About != null) {
              var a = f.About.GetName();
              var b = f.About.GetImageUrl();
            }
          }
          return found;
        }
      }
    }

    public static List<PeopleHeadline> GetRecurrenceHeadlinesForUser(UserOrganizationModel caller, long userId, long recurrenceId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).ViewL10Recurrence(recurrenceId);
          var foundQ = s.QueryOver<PeopleHeadline>().Where(x => x.DeleteTime == null && x.RecurrenceId == recurrenceId && x.OwnerId == userId);
          var found = foundQ.Fetch(x => x.Owner).Eager
                    .Fetch(x => x.About).Eager
                    .List().ToList();
          foreach (var f in found) {
            if (f.Owner != null) {
              var a = f.Owner.GetName();
              var b = f.Owner.ImageUrl(true, ImageSize._32);
            }
            if (f.About != null) {
              var a = f.About.GetName();
              var b = f.About.GetImageUrl();
            }
          }
          return found;
        }
      }
    }

    public static async Task CopyHeadlineToMeetings(UserOrganizationModel caller, long headlineId, List<NoteMeeting> noteMeetings)
    {
      var s = HibernateSession.GetCurrentSession();
      var tx = s.BeginTransaction();
      var perms = PermissionsUtility.Create(s, caller);

      var headline = s.Get<PeopleHeadline>(headlineId);

      foreach (var noteMeeting in noteMeetings)
      {
        var newHeadline = new PeopleHeadline()
        {
          Message = headline.Message,
          OwnerId = headline.OwnerId,
          RecurrenceId = noteMeeting.MeetingId,
          HeadlinePadId = noteMeeting.NotePadId,
          OrganizationId = headline.OrganizationId
        };

        await CreateHeadline(s, perms, newHeadline);
      }

      tx.Commit();
      s.Flush();
    }
  }
}

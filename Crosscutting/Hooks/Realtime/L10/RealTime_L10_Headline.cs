using HotChocolate.Subscriptions;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Accessors;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Common.DTO.Subscription;
using RadialReview.Core.Models.Terms;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using RadialReview.Utilities.RealTime;
using System;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.Realtime.L10 {
  public class RealTime_L10_Headline : IHeadlineHook {
    public bool AbsorbErrors() {
      return false;
    }
    public bool CanRunRemotely() {
      return false;
    }
    public HookPriority GetHookPriority() {
      return HookPriority.UI;
    }

    public async Task CreateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline) {
      var recurrenceId = headline.RecurrenceId;
      if (recurrenceId <= 0)
         return;

        await using (var rt = RealTimeUtility.Create()) {
          var meetingHub = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(recurrenceId));

          TermsCollection terms = TermsCollection.DEFAULT;
          try {
            terms= TermsAccessor.GetTermsCollection(s, PermissionsUtility.Create(s, caller), caller.Organization.Id);
          } catch (Exception e) {

          }

          if (headline.CreatedDuringMeetingId == null) {
            headline.CreatedDuringMeetingId = L10Accessor._GetCurrentL10Meeting(s, PermissionsUtility.CreateAdmin(s), recurrenceId, true, false, false).NotNull(x => (long?)x.Id);
          }
          var aHeadline = new AngularHeadline(headline);
          meetingHub.Call("appendHeadline", ".headlines-list", headline.ToRow());
          meetingHub.Call("showAlert", $@"Created {terms.GetTermSingular(TermKey.Headlines)}.", 1500);
          var updates = new AngularRecurrence(recurrenceId);
          updates.Headlines = AngularList.CreateFrom(AngularListType.Add, aHeadline);
          meetingHub.Update(updates);
        }
    }

    public async Task UpdateHeadline(ISession s, UserOrganizationModel caller, PeopleHeadline headline, IHeadlineHookUpdates updates) {
      await using (var rt = RealTimeUtility.Create(RealTimeHelpers.GetConnectionString())) {
        var group = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(headline.RecurrenceId));
        if (updates.MessageChanged) {
          group.Call("updateHeadlineMessage", headline.Id, headline.Message);

          AngularPicture about = null;
          try {
            if (headline.About != null) {
              about = new AngularPicture(headline.About);
            }
          } catch (Exception) {
          }

          group.Update(new AngularHeadline(headline.Id) {
            Name = headline.Message,
            About = headline.About.NotNull(x => new AngularPicture(x))
          });
        }
        if (updates.OwnerChanged)
          group.Call("updateHeadlineOwner", headline.Id, headline.Owner.Id, headline.Owner.GetName(), headline.Owner.ImageUrl(true, ImageSize._32));

      }
    }

    public async Task ArchiveHeadline(ISession s, PeopleHeadline headline) {
      await using (var rt = RealTimeUtility.Create()) {
        rt.UpdateRecurrences(headline.RecurrenceId)
          .Update(new AngularRecurrence(headline.RecurrenceId) {
            Headlines = AngularList.CreateFrom(AngularListType.Remove, new AngularHeadline(headline.Id))
          });

        var meetingHub = rt.UpdateGroup(RealTimeHub.Keys.GenerateMeetingGroupId(headline.RecurrenceId));
        meetingHub.Call("removeHeadlineTr", headline.Id);
      }
    }
    public async Task UnArchiveHeadline(ISession s, PeopleHeadline headline) {
      await using (var rt = RealTimeUtility.Create()) {
        rt.UpdateRecurrences(headline.RecurrenceId)
          .Update(new AngularRecurrence(headline.RecurrenceId) {
            Headlines = AngularList.CreateFrom(AngularListType.ReplaceIfNewer, new AngularHeadline(headline))
          });
      }
    }
  }
}

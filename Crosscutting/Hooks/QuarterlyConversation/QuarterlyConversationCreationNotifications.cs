using RadialReview.Crosscutting.Hooks.Interfaces;
using System.Collections.Generic;
using NHibernate;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Areas.People.Angular.Survey;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Json;
using RadialReview.Utilities.RealTime;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Crosscutting.Hooks.QuarterlyConversation {
  public class QuarterlyConversationCreationNotifications : IQuarterlyConversationHook {
    public bool CanRunRemotely() {
      return false;
    }
    public bool AbsorbErrors() {
      return false;
    }

    public HookPriority GetHookPriority() {
      return HookPriority.UI;
    }

    public async Task QuarterlyConversationCreated(ISession s, long qcId) {
      var sc = s.Get<SurveyContainer>(qcId);
      if (sc.CreatedBy.ModelType == ForModel.GetModelType<UserOrganizationModel>()) {
        var user = s.Get<UserOrganizationModel>(sc.CreatedBy.ModelId);

        var terms = TermsAccessor.GetTermsCollection_Unsafe(s, user.Organization.Id);

        await using (var rt = RealTimeUtility.Create()) {
          rt.UpdateUsers(user.Id).Call("addQC", terms.GetTerm(TermKey.Quarterly1_1)+" issued!", new AngularSurveyContainer(sc, false, AngularUser.CreateUser(user)) {
            Ordering = -1,
          });

        }
      }
    }

    public async Task QuarterlyConversationError(ISession s, IForModel creator, QuarterlyConversationErrorType failureType, List<string> errors) {
      if (creator.Is<UserOrganizationModel>()) {
        var user = s.Get<UserOrganizationModel>(creator.ModelId);
        var terms = TermsAccessor.GetTermsCollection_Unsafe(s, user.Organization.Id);
        await using (var rt = RealTimeUtility.Create()) {
          var message = "An error occurred issuing the "+ terms.GetTerm(TermKey.Quarterly1_1)+".";
          switch (failureType) {
            case QuarterlyConversationErrorType.EmailsFailed:
              message = terms.GetTerm(TermKey.Quarterly1_1)+ " was generated, but email notifications failed";
              break;
            default:
              break;
          }
          rt.UpdateUsers(user.Id).Call("showAlert", ResultObject.CreateError(message));
        }
      }
    }

    public async Task QuarterlyConversationEmailsSent(ISession s, long qcId) {
      //Noop
    }

  }
}

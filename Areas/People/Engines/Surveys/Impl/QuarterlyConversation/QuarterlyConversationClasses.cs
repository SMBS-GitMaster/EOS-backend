using RadialReview.Areas.People.Engines.Surveys.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models.Interfaces;
using RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation.Sections;
using RadialReview.Models.Accountability;
using RadialReview.Utilities.DataTypes;
using RadialReview.Core.Models.Terms;

namespace RadialReview.Areas.People.Engines.Surveys.Impl.QuarterlyConversation {
  public class QuarterlyConversationInitializer : ISurveyInitializer {

    public IForModel CreatedBy { get; set; }
    public String Name { get; set; }
    public DateTime DueDate { get; set; }
    public DateRange QuarterRange { get; set; }
    public long OrgId { get; set; }
    public List<SurveyUserNode> SelectedUsers { get; set; }
    public TermsCollection Terms { get; set; }

    public QuarterlyConversationInitializer(IForModel createdBy, string name, long orgId, DateRange quarterRange, DateTime dueDate, List<SurveyUserNode> selectedUsers, TermsCollection terms) {
      CreatedBy = createdBy;
      DueDate = dueDate;
      Name = name;
      OrgId = orgId;
      QuarterRange = quarterRange;
      SelectedUsers = selectedUsers;
      Terms = terms;
    }

    private IEnumerable<ISectionInitializer> _sectionBuilders() {
      yield return new ValueSection(Terms);
      yield return new RoleSection(Terms);
      yield return new RockSection(QuarterRange, Terms);// new DateRange(QuarterRange.AddDays(-7),QuarterRange.AddDays(65)));
                                                 //yield return new RockCompletionSection();
      yield return new LeadershipAssessmentSection(false,Terms);
      yield return new ManagementAssessmentSection(false, Terms);
      yield return new GeneralCommentsSection();
    }

    #region Standard Customization
    public ISurveyContainer BuildSurveyContainer() {
      return new SurveyContainer(CreatedBy, Name, OrgId, SurveyType.QuarterlyConversation, null, DueDate);
    }

    public IEnumerable<ISectionInitializer> GetAllPossibleSectionBuilders(IEnumerable<IByAbout> byAbouts) {
      return _sectionBuilders();
    }

    public IEnumerable<ISectionInitializer> GetSectionBuilders(ISectionInitializerData data) {
      return _sectionBuilders();
    }

    public void Prelookup(IInitializerLookupData data) {
      //nothing to do.

      var nodeIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<AccountabilityNode>()).Select(x => x.ModelId).ToArray();
      if (nodeIds.Any()) {
        data.Lookup.AddList(data.Session.QueryOver<AccountabilityNode>().WhereRestrictionOn(x => x.Id).IsIn(nodeIds).Future());
      }

      var surveyUserIds = data.ByAbouts.SelectMany(x => new[] { x.GetBy(), x.GetAbout() }).Where(x => x.Is<SurveyUserNode>()).Select(x => x.ModelId).ToArray();
      if (surveyUserIds.Any()) {
        data.Lookup.AddList(data.Session.QueryOver<SurveyUserNode>().WhereRestrictionOn(x => x.Id).IsIn(surveyUserIds).Future());
      }

    }

    [Todo]
    public ISurvey InitializeSurvey(ISurveyInitializerData data) {
      var name = data.About.ToPrettyString();

      if (name == null && data.About.ModelType == ForModel.GetModelType<AccountabilityNode>()) {
        throw new NotImplementedException("No longer supporting AccountabilityNode");
      } else if (name == null && data.About.ModelType == ForModel.GetModelType<SurveyUserNode>()) {
        name = data.Lookup.GetList<SurveyUserNode>().FirstOrDefault(x => x.Id == data.About.ModelId).NotNull(x => x.ToPrettyString());
      }

      bool sendEmail = false;

      if (data.About is SurveyUserNode) {
        SurveyUserNode userNode = (SurveyUserNode)data.About;
        sendEmail = SelectedUsers.Any(x => x.UserOrganizationId == (userNode).UserOrganizationId);
      }

      return new Survey(name, DueDate, sendEmail, data);
    }

    #endregion
  }

}

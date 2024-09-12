namespace RadialReview.Models.ViewModels.Application {
  public class NavBarViewModel {
    public bool ShowL10 { get; set; }
    public bool ShowEvals { get; set; }
    public bool NoAccessCode { get; set; }
    public int OrganizationCount { get; set; }
    public int TaskCount { get; set; }
    public long? PrimaryVtoL10 { get; set; }
    public bool ShowAC { get; set; }
    public bool ShowPeople { get; set; }
    public bool ShowSurvey { get; set; }
    public bool ShowCoreProcess { get; set; }
    public bool IsLogin { get; set; }
    public string ReturnUrl { get; set; }
    public bool ShowDocs { get; set; }
    public bool FullLogo { get; set; }
    public bool ShowWhale { get; set; }
    public bool ShowCoach { get; set; }
    public bool ShowBetaButton {  get; set; }

    public NavBarViewModel() {
      ShowL10 = false;
      ShowEvals = false;
      OrganizationCount = 0;
      TaskCount = 0;
      PrimaryVtoL10 = null;
      ShowSurvey = false;
      ShowAC = false;
      ShowPeople = false;
      NoAccessCode = false;
      ShowCoreProcess = false;
      IsLogin = false;
      ReturnUrl = null;
      ShowDocs = false;
      FullLogo = false;
      ShowWhale = false;
      ShowCoach = false;
      ShowBetaButton = false;
    }

    public static NavBarViewModel CreateErrorNav() {
      return new NavBarViewModel() {
        NoAccessCode = true,
        FullLogo = true
      };
    }
  }
}


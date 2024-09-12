using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Middleware.Request.HttpContextExtensions;
using RadialReview.Middleware.Request.HttpContextExtensions.Navbar;
using RadialReview.Middleware.Request.HttpContextExtensions.Permissions;
using RadialReview.Middleware.Request.HttpContextExtensions.Prefetch;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.ViewModels.Application;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;
using RadialReview.Variables;
using RadialReview.Extensions;
using RadialReview.Core.Utilities.Request;

namespace RadialReview.Middleware.Request.ActionFilters {
  public class ViewBagFinalizeFilter : IAsyncActionFilter {
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
      var httpCtx = context.HttpContext;
      var prefetch = httpCtx.GetPrefetchData();
      var viewBag = context.Controller is Controller ctrl ? ctrl.ViewBag : new ExpandoObject();
      var isRadialAdmin = httpCtx.IsRadialAdmin();
      var absoluteUri = httpCtx.Request.GetDisplayUrl();

      if (httpCtx.IsLoggedIn()) {
        UserOrganizationModel oneUser = null;
        try {
          oneUser = httpCtx.GetUser();
        } catch (OrganizationIdException) {// should we be catching this one?
        } catch (NoUserOrganizationException) {
          //No big deal if there is no user organization, let the Account creation logic handle
          await next();
          return;
        }

        if (oneUser != null) {
          var navbar = httpCtx.GetNavBar();
          var terms = TermsAccessor.GetTermsCollection(oneUser, oneUser.Organization.Id);
          OneUserViewBagSetup(context, oneUser, prefetch, navbar, terms);
          SetupToolTips(context, prefetch);
        } else {
          var user = httpCtx.GetUserModel();
          viewBag.Hints = user.Hints;
          viewBag.UserName = user.Name() ?? MessageStrings.User;
          viewBag.UserColor = user.GetUserHashCode();
          SetupAccessibilty(viewBag, user);
        }
        viewBag.IsRadialAdmin = isRadialAdmin;
        viewBag.Organizations = prefetch.UserOrganizationCount;
        try {
          if (oneUser._PermissionsOverrides.Admin.IsRadialAdmin) {
            viewBag.Organizations+=2;
          }
        } catch (Exception e) {
        }

      }

      viewBag.SoftwareVersion = prefetch.GetSettingOrDefault(Variable.Names.SOFTWARE_VERSION, 1);
      viewBag.SwitchStyles = SwitchesAccessor.GetSwitchStyles(absoluteUri, isRadialAdmin);

      try {
        var noHeading = context.HttpContext.Request.Query.FirstOrDefault(x => x.Key.ToLower() == "noheading");
        if (noHeading.Value.Any(x => x.ToLower() == "true")) {
          var viewData = context.Controller is Controller ctrl1 ? ctrl1.ViewData : null;
          viewBag.NoTitleBar = true;

          if (viewData != null) {
            ViewSettingsUtility.RemoveTitleBar(viewData, true);
          }
          
        }
      } catch { }
      

      await next();

      ViewBagSetupAfterExecution(context);

      //Forwards TempData from previous request to current request.
      if (context.Controller is Controller) {
        var c = context.Controller as Controller;
        if (c.TempData["Message"] != null) {
          c.ViewBag.Message = c.TempData["Message"];
        }
        if (c.TempData["InfoAlert"] != null) {
          c.ViewBag.InfoAlert = c.TempData["InfoAlert"];
        }
      }
    }

    #region Helpers

    private static void ViewBagSetupAfterExecution(ActionExecutingContext context) {
      if (context.Controller is Controller) {
        var viewBag = context.Controller is Controller ctrl ? ctrl.ViewBag : new ExpandoObject();
        if (viewBag.Settings is SettingsViewModel settings) {
          if (viewBag.SettingsModifiers is List<Action<SettingsViewModel>> modifiers) {
            foreach (var mod in modifiers) {
              mod(settings);
            }
          }
        }
      }
    }

    private static void OneUserViewBagSetup(ActionExecutingContext context, UserOrganizationModel oneUser, PrefetchData prefetchData, NavBarViewModel navBar, TermsCollection terms) {

      if (context.Controller is Controller) {
        var viewBag = context.Controller is Controller ctrl ? ctrl.ViewBag : new ExpandoObject();

        var userOrgCount = prefetchData.UserOrganizationCount;
        var nameStr = oneUser.GetName();
        var name = new HtmlString(nameStr);
        //Global WhaleIO Integration flag
        bool WhaleIO_Flag = prefetchData.GetSettingOrDefault(Variable.Names.WhaleIO_Flag, false);
        //Get refer a friend forms
        string referralFriendFormUrl = prefetchData.GetSettingOrDefault(Variable.Names.ReferralFriendFormUrl, "https://share.hsforms.com/1ak39h6QQRHGSOUqJMJHk0A5f3td");
        string referralCoachFormUrl = prefetchData.GetSettingOrDefault(Variable.Names.ReferralCoachFormUrl, "https://share.hsforms.com/1aiSeZR2DQ9KsColq5ZI3rw5f3td?__hstc=3737839.3e55e67d54de039fc2d2fdc7477f90cb.1610137192326.1619536372446.1619542825861.288&__hssc=3737839.3.1619542825861&__hsfp=1539332063");
        string dataRequestFormUrl = prefetchData.GetSettingOrDefault(Variable.Names.DataRequestFormUrl, "https://www.bloomgrowth.com/upload-data/");

        try {
          name = new HtmlString("<span class=\"CustomNavbar-user-info-name\">" + name + "</span><p class=\"CustomNavbar-user-info-organization\">" + oneUser.Organization.Name + "</p>");
        } catch (Exception e) {
          //log.Error(e);
        }

        viewBag.UserImage = oneUser.ImageUrl(true, ImageSize._img);
        viewBag.UserInitials = oneUser.GetInitials();
        viewBag.Email = oneUser.GetEmail();
        viewBag.UserColor = oneUser.GeUserHashCode();
        viewBag.UsersName = oneUser.GetName();
        viewBag.SupportContactCode = UserAccessor.GetSupportSecretCodes(oneUser, oneUser.Id).FirstOrDefault();
        viewBag.v3ShowFeatures = VariableAccessor.Get(Variable.Names.V3_SHOW_FEATURES, () => false);
        viewBag.UserOrganization = oneUser;
        viewBag.ConsoleLog = oneUser.User.NotNull(x => x.ConsoleLog);
        SetupAccessibilty(viewBag, oneUser.User);
        navBar.TaskCount = 0;
        viewBag.TaskCount = 0;
        viewBag.UserName = name;

        viewBag.EvalOnly = oneUser.EvalOnly;
        viewBag.UserStyles = prefetchData.UserStyles;

        //referral urls in the nav bar
        viewBag.referralFriendFormUrl = referralFriendFormUrl;
        viewBag.referralCoachFormUrl = referralCoachFormUrl;
        viewBag.dataRequestFormUrl = dataRequestFormUrl;

        //VTO
        try {
          viewBag.PrimaryVto = null;
          navBar.PrimaryVtoL10 = L10Accessor.GetSharedVTOVision(oneUser, oneUser.Organization.Id);
          viewBag.PrimaryVto = navBar.PrimaryVtoL10;
        } catch (Exception) {
          //Eat it.
        }
        //Navbar
        navBar.ShowL10 = oneUser.Organization.Settings.EnableL10 && !oneUser.EvalOnly;
        navBar.ShowEvals = oneUser.Organization.Settings.EnableReview && !oneUser.IsClient;
        navBar.ShowSurvey = oneUser.Organization.Settings.EnableSurvey && oneUser.IsManager() && !oneUser.EvalOnly;
        navBar.ShowPeople = oneUser.Organization.Settings.EnablePeople;
        navBar.ShowCoreProcess = oneUser.Organization.Settings.EnableCoreProcess && !oneUser.EvalOnly;
        navBar.ShowBetaButton = oneUser.Organization.Settings.EnableBetaButton;
        navBar.ShowDocs = oneUser.Organization.Settings.EnableDocs;
        navBar.ShowWhale = WhaleIO_Flag && oneUser.Organization.Settings.EnableWhale;

        navBar.ShowCoach = oneUser.Organization.AccountType.IsImplementerOrCoach() && oneUser.Organization.Settings.EnableDocs;

        if (!navBar.ShowCoach && oneUser.Organization.CoachDocumentsFolderId!=null && oneUser.Organization.HasCoachDocuments && oneUser.Organization.Settings.EnableDocs) {
          navBar.ShowCoach = PermissionsAccessor.CanView(oneUser, PermItem.ResourceType.DocumentsFolder, oneUser.Organization.CoachDocumentsFolderId.Value);
        }



        //Try to deprecate these
          viewBag.ShowL10 = navBar.ShowL10;
        viewBag.ShowReview = navBar.ShowEvals;
        viewBag.ShowSurvey = navBar.ShowSurvey;
        viewBag.ShowPeople = navBar.ShowPeople;
        viewBag.ShowCoreProcess = navBar.ShowCoreProcess;
        viewBag.ShowDocs = navBar.ShowDocs;

        viewBag.ShowCoach = navBar.ShowCoach;
        viewBag.ShowWhale = navBar.ShowWhale;

        viewBag.WhaleEnabled = WhaleIO_Flag;
        viewBag.WhaleAccepted = WhaleIO_Flag && oneUser.Organization.Settings.EnableWhale && oneUser.EnableWhale;


        var isManager = oneUser.ManagerAtOrganization || oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
        var superAdmin = oneUser.IsRadialAdmin || (viewBag.IsRadialAdmin ?? false);

        viewBag.LimitFiveState = oneUser.Organization.Settings.LimitFiveState;
        viewBag.IsRadialAdmin = superAdmin;
        viewBag.IsManager = isManager;
        viewBag.ManagingOrganization = oneUser.ManagingOrganization || oneUser.IsRadialAdmin;
        viewBag.UserId = oneUser.Id;
        viewBag.OrganizationId = oneUser.Organization.Id;
        viewBag.Organization = oneUser.Organization;
        viewBag.Hints = oneUser.User.NotNull(x => x.Hints);

        var settings = SettingsAccessor.GenerateViewSettings(oneUser, nameStr, isManager, superAdmin, prefetchData, terms);

        viewBag.Settings = settings;
        var forceGetLayoutData = false;
        var isJsonResult = false;
        var isPartialResult = false;
        var isContentResult = false;
        var isFileResult = false;
        try {
          var action = ((ControllerActionDescriptor)context.ActionDescriptor).MethodInfo;
          isJsonResult = typeof(JsonResult).IsAssignableFrom(action.ReturnType) || typeof(Task<JsonResult>).IsAssignableFrom(action.ReturnType);
          isPartialResult = typeof(PartialViewResult).IsAssignableFrom(action.ReturnType) || typeof(Task<PartialViewResult>).IsAssignableFrom(action.ReturnType);
          isContentResult = typeof(ContentResult).IsAssignableFrom(action.ReturnType) || typeof(Task<ContentResult>).IsAssignableFrom(action.ReturnType);
          isFileResult = typeof(FileResult).IsAssignableFrom(action.ReturnType) || typeof(Task<FileResult>).IsAssignableFrom(action.ReturnType);
        } catch {
          forceGetLayoutData = true;
        }

        //Only for pages, not partials
        if (forceGetLayoutData || (!isJsonResult && !isPartialResult && !isContentResult && !isFileResult)) {

          using (var s = HibernateSession.GetCurrentSession()) {
            using (var tx = s.BeginTransaction()) {
              navBar.ShowAC = PermissionsAccessor.IsPermitted(s, oneUser, x => x.CanView(PermItem.ResourceType.AccountabilityHierarchy, oneUser.Organization.AccountabilityChartId)); // oneUser.Organization.acc && oneUser.IsManager();
            }
          }
          viewBag.ShowAC = navBar.ShowAC;
          viewBag.InjectedScripts = prefetchData.InjectedScript;
        }
      }
    }

    private static void SetupAccessibilty(dynamic viewbag, UserModel user) {
      try {
        if (user != null) {
          viewbag.AccessibilityClasses = user.ColorMode.ToClassName();
          if (user.DarkMode != null) {
            viewbag.DarkModeClasses = user.DarkMode.Value ? "dark-mode" : "light-mode";
          } else {
            viewbag.DarkModeClasses = "dark-mode";
          }
        }
      } catch (Exception) {
        //ops
      }
    }

    private void SetupToolTips(dynamic ViewBag, PrefetchData prefetch) {
      try {
        ViewBag.TooltipsEnabled = prefetch.TooltipsEnabled;
        ViewBag.Tooltips = prefetch.Tooltips;
      } catch (Exception) {
      }
    }

    #endregion
  }
}

using RadialReview.Models;
using Microsoft.AspNetCore.Mvc;
using RadialReview.Core.Models.Terms;
using RadialReview.Core.Middleware.Request.HttpContextExtensions;

namespace RadialReview.Controllers {
  public partial class BaseController : Controller {




    protected TermsCollection GetTermsCollection() {     
      return HttpContext.GetTermsCollection();
    }

    protected string GetTerm(TermKey key, TermModification modification = TermModification.None) {
      return GetTermsCollection().GetTerm(key, modification);
    }
    protected string GetTermSingular(TermKey key) {
      return GetTermsCollection().GetTermSingular(key);
    }
    protected string GetTermPlural(TermKey key) {
      return GetTermsCollection().GetTermPlural(key);
    }

  }
}

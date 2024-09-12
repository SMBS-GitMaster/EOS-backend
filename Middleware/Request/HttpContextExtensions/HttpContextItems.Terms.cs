using Microsoft.AspNetCore.Http;
using RadialReview.Core.Accessors;
using RadialReview.Core.Models.Terms;
using RadialReview.Middleware.Request.HttpContextExtensions;
using System;
using static RadialReview.Middleware.Request.HttpContextExtensions.HttpContextItems;

namespace RadialReview.Core.Middleware.Request.HttpContextExtensions {
  public static partial class HttpContextItems {

    public static HttpContextItemKey TERMS_COLLECTION = new HttpContextItemKey("TermsCollection");

    public static TermsCollection GetTermsCollection(this HttpContext ctx) {
      return ctx.GetOrCreateRequestItem(TERMS_COLLECTION, x => {
        try {
          return TermsAccessor.GetTermsCollection(x.GetUser(), x.GetUser().Organization.Id);
        } catch (Exception e) {
          return TermsCollection.DEFAULT;
        }
       });
    }

  }
}

using System;
using RadialReview.Utilities;
using Microsoft.AspNetCore.Html;
using System.Collections.Generic;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.ViewModels {
	public class OrgReviewsViewModel : IPagination {
		public List<ReviewsViewModel> Reviews { get; set; }
		public List<TemplateViewModel> Templates { get; set; }
		public double NumPages { get; set; }
		public int Page { get; set; }
		public bool AllowEdit { get; set; }

		public HtmlString CreateLabel(ReviewsViewModel review) {
			var clzz = "label label-";
			var txt = "";
			var hover = "";
			if (review.IsPrereview) {
				clzz += "info";
				hover = "Complete pre-" + Config.ReviewName().ToLower() + ".";
				txt = "Pre-" + Config.ReviewName().ToLower();
				return new HtmlString("<span class='" + clzz + "' title='" + hover + "' style='display: block; top: 1px;position: relative;'>" + txt + "</span>");
			}

			if (review.UserReview != null) {
				if (review.Review.DueDate > DateTime.UtcNow) {
					if (review.UserReview.Complete) {
						clzz += "success";
						txt = "complete";
						hover = "Evals completed.";
					} else if (review.UserReview.Started) {
						clzz += "warning";
						txt = "started";
						hover = "Evals incomplete.";
					} else {
						clzz += "default";
						txt = "start";
						hover = "Evals unstarted.";
					}
				} else {
					clzz += "success";
					txt = "concluded";
					hover = "Evals have concluded.";
				}

				return new HtmlString("<span class='" + clzz + "' title='" + hover + "' style='display: block; top: 1px;position: relative;'>" + txt + "</span>");
			} else {
				clzz += "default";
				txt = "";
				return new HtmlString("");
			}
		}
	}
}

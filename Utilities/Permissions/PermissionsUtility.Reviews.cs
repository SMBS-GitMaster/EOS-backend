using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Documents;
using RadialReview.Models.Enums;
using RadialReview.Models.Integrations;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Prereview;
using RadialReview.Models.Process;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Survey;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.VTO;
using RadialReview.Reflection;
using RadialReview.Utilities.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace RadialReview.Utilities {
  public partial class PermissionsUtility {

    public PermissionsUtility ViewPrereview(long prereviewId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var prereview = session.Get<PrereviewModel>(prereviewId);
      var prereviewOrgId = session.Get<ReviewsModel>(prereview.ReviewContainerId).OrganizationId;

      if (IsManagingOrganization(prereviewOrgId)) {
        return this;
      }

      if (IsOwnedBelowOrEqual(caller, prereview.ManagerId)) {
        return this;
      }

      throw new PermissionsException();
    }

    public PermissionsUtility EditQuestion(long questionId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var question = session.Get<QuestionModel>(questionId);
      if (question.OriginType != OriginType.Invalid) {
        return EditOrigin(question.OriginType, question.OriginId, true);
      }


      throw new PermissionsException();
    }

    public PermissionsUtility ViewQuestion(long questionId) {

      if (IsRadialAdmin(caller)) {
        return this;
      }

      var question = session.Get<QuestionModel>(questionId);

      switch (question.OriginType) {


        case OriginType.Organization:
          if (caller.Organization.Id != question.OriginId) {
            throw new PermissionsException();
          }

          break;
        case OriginType.Industry:
          break;
        case OriginType.Application:
          break;
        case OriginType.Invalid:
          throw new PermissionsException();
        default:
          throw new PermissionsException();
      }
      return this;
    }


    public PermissionsUtility EditQuestionForUser(long forUserId) {
      return EditUserDetails(forUserId);
    }

    public PermissionsUtility EditOrganizationQuestions(long orgId) {
      return EditOrganization(orgId);
    }

    public PermissionsUtility ViewCategory(long id) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var category = session.Get<QuestionCategoryModel>(id);
      if (category.OriginType == OriginType.Application) {
        return this;
      }

      if (category.OriginType == OriginType.Organization && IsOwnedBelowOrEqualOrganizational(caller.Organization, new Origin(category.OriginType, category.OriginId))) {
        return this;
      }

      throw new PermissionsException();
    }
    public PermissionsUtility PairCategoryToQuestion(long categoryId, long questionId) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var category = session.Get<QuestionCategoryModel>(categoryId);
      if (questionId == 0 && category.OriginType == OriginType.Organization) {
        return this;
      }

      var question = session.Get<QuestionModel>(questionId);

      //Cant attach questions to application categories
      if (category.OriginType == OriginType.Application && !caller.IsRadialAdmin) {
        throw new PermissionsException();
      }
      //Belongs to the same organization
      if (category.OriginType == OriginType.Organization && question.OriginType == OriginType.Organization && question.OriginId == category.OriginId) {
        return this;
      }

      //TODO any other special permissions here.

      throw new PermissionsException();
    }


    public PermissionsUtility AdminReviewContainer(long reviewContainerId) {
      //TODO more permissions here?
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var review = session.Get<ReviewsModel>(reviewContainerId);
      if (review.CreatedById == caller.Id) {
        return this;
      }

      var team = session.Get<OrganizationTeamModel>(review.ForTeamId);
      if (team.ManagedBy == caller.Id) {
        return this;
      }

      ManagingOrganization(caller.Organization.Id);

      return this;

    }

    public PermissionsUtility EditReview(long reviewId) {
      return CheckCacheFirst("EditReview", reviewId).Execute(() => {
        //TODO more permissions here?
        if (IsRadialAdmin(caller)) {
          return this;
        }

        var review = session.Get<ReviewModel>(reviewId);
        if (review.DueDate < DateTime.UtcNow) {
          throw new PermissionsException("Review period has expired.");
        }

        if (review.ReviewerUserId == caller.Id) {
          return this;
        }

        throw new PermissionsException();
      });
    }

    public PermissionsUtility ViewReviews(long reviewContainerId, bool sensitive) {
      if (IsRadialAdmin(caller)) {
        return this;
      }

      var review = session.Get<ReviewsModel>(reviewContainerId);
      var orgId = review.Organization.Id;
      if (sensitive) {
        ManagerAtOrganization(caller.Id, orgId);
      }

      if (orgId == caller.Organization.Id) {
        return this;
      }




      throw new PermissionsException();
    }

    public PermissionsUtility ViewReview(long reviewId) {
      return TryWithOverrides(y => {
        if (IsRadialAdmin(caller)) {
          return this;
        }

        var review = session.Get<ReviewModel>(reviewId);
        var reviewUserId = review.ReviewerUserId;

        //Is this correct?
        if (IsManagingOrganization(review.ReviewerUser.Organization.Id)) {
          return this;
        }

        //Cannot be viewed by the user
        if (reviewUserId == caller.Id) {
          return this;
        }

        if (DeepAccessor.Users.ManagesUser(session, this, caller.Id, reviewUserId)) {
          return this;
        }


        throw new PermissionsException();
      });

    }

    public PermissionsUtility ManageUserReview(long reviewId, bool userCanManageOwnReview) {
      ViewReview(reviewId);
      var review = session.Get<ReviewModel>(reviewId);
      var userId = review.ReviewerUserId;

      if (userCanManageOwnReview && review.ReviewerUserId == caller.Id) {
        return this;
      }

      return ManagesUserOrganization(userId, false);
    }
    public PermissionsUtility ManageUserReview_Answer(long answerId, bool userCanManageOwnReview) {
      var answer = session.Get<AnswerModel>(answerId);

      if (answer == null) {
        throw new PermissionsException("Answer does not exist");
      }

      var reviews = session.QueryOver<ReviewModel>()
        .Where(x => x.DeleteTime == null && x.ForReviewContainerId == answer.ForReviewContainerId && x.ReviewerUserId == answer.RevieweeUserId)
        .List();
      if (!reviews.Any()) {
        throw new PermissionsException("Review does not exist");
      }

      foreach (var review in reviews) {
        ManageUserReview(review.Id, userCanManageOwnReview);
      }


      return this;
    }
  }
}

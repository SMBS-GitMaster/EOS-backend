using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;

namespace RadialReview.Accessors {
  public class CommentAccessor : BaseAccessor {

    #region Public Methods

    public static async Task<long> AddComment(UserOrganizationModel caller, ParentType parentType, long parentId, string body, DateTime postedTimestamp, long authorId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          //Check updated permissions
          var perms = PermissionsUtility.Create(s, caller)
            .CanViewParentType(parentType, parentId)
            .ViewUserOrganization(authorId, false);

          var comment = new CommentModel() {
            PostedDateTime = postedTimestamp,
            DateLastModified = postedTimestamp,
            Body = body,
            CommentParentType = parentType,
            ParentId = parentId,
            AuthorId = authorId
          };
          s.Save(comment);

          await HooksRegistry.Each<ICommentHook>((sess, x) => x.CreateComment(sess, caller, comment));

          tx.Commit();
          s.Flush();

          return comment.Id;
        }
      }
    }

    public static async Task<long> EditComment(UserOrganizationModel caller, long commentId, ParentType? parentType, long? parentId, string body, long? authorId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          var comment = s.Get<CommentModel>(commentId);
          var perms = PermissionsUtility.Create(s, caller)
            //Check existing permissions
            .CanViewParentType(comment.CommentParentType, comment.ParentId);

          if ((parentType!=null && parentType!=comment.CommentParentType) || (parentId !=null && parentId != comment.ParentId)) {
            //Check updated permissions
            perms.CanViewParentType(parentType ?? comment.CommentParentType, parentId ?? comment.ParentId);
          }

          if (authorId!=null && authorId != comment.AuthorId) {
            perms.ViewUserOrganization(authorId.Value, false);
          }


          if (parentType != null)
            comment.CommentParentType = parentType.Value;
          if (parentId != null)
            comment.ParentId = (long)parentId;
          comment.Body = body;
          if (authorId != null)
            comment.AuthorId = (long)authorId;
          comment.DateLastModified = DateTime.UtcNow;
          s.Update(comment);

          await HooksRegistry.Each<ICommentHook>((sess, x) => x.UpdateComment(sess, caller, comment, null));

          tx.Commit();
          s.Flush();

          return comment.Id;
        }
      }
    }

    public static async Task<long> DeleteComment(UserOrganizationModel caller, long commentId, ParentType parentType /*is this needed?*/) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          var comment = s.Get<CommentModel>(commentId);

          PermissionsUtility.Create(s, caller)
            .CanViewParentType(comment.CommentParentType, comment.ParentId);

          comment.CommentParentType = parentType; //should this be here?
          comment.DeleteTime = DateTime.UtcNow;
          s.Update(comment);

          await HooksRegistry.Each<ICommentHook>((sess, x) => x.UpdateComment(sess, caller, comment, null));

          tx.Commit();
          s.Flush();

          return comment.Id;
        }
      }
    }

    public static List<CommentModel> GetUserComments(UserOrganizationModel caller, long forUserId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          PermissionsUtility.Create(s, caller)
            //.ViewOrganization(caller.Organization.Id) not restrictive enough?
            .ViewUserOrganization(forUserId, true); //may be too restricted?

          return s.QueryOver<CommentModel>()
                      .Where(x => x.AuthorId == forUserId && x.DeleteTime==null)
                      .List().ToList();
        }
      }
    }

    public static List<CommentModel> GetComments(UserOrganizationModel caller, ParentType parentType, long parentId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller)
            //.ViewOrganization(caller.Organization.Id)
            .CanViewParentType(parentType, parentId);

          return s.QueryOver<CommentModel>()
            .Where(x => x.CommentParentType == parentType && x.ParentId == parentId && x.DeleteTime == null)
            .List().ToList();
        }
      }
    }

    #endregion

  }
}

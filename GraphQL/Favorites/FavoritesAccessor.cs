using NHibernate;
using RadialReview.Core.Crosscutting.Hooks.Realtime.GraphQLSubscriptions;
using RadialReview.Core.GraphQL.Common.Constants;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Exceptions;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
  public class FavoriteAccessor : BaseAccessor {

    #region Public Methods

    public static async Task<long> AddFavorite(UserOrganizationModel caller, long userId, FavoriteType parentType, long parentId, int position, DateTime createdTimestamp) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          PermissionsUtility.Create(s, caller).CreateFavorite(userId, parentType, parentId);

          // Check if favorite already exists
          var existing = s.Query<FavoriteModel>().Where(_ => _.ParentId == parentId && _.ParentType == parentType && _.UserId == userId && _.DeleteTime == null).FirstOrDefault();
          if (existing != null) {
            await EditFavorite(caller, existing.Id, parentType, parentId, position, userId);
            return existing.Id;
          }

          var favorite = new FavoriteModel() {
            CreatedDateTime = createdTimestamp,
            DateLastModified = createdTimestamp,
            Position = position,
            ParentType = parentType,
            ParentId = parentId,
            UserId = userId
          };
          s.Save(favorite);

          await UpdateFavoritesPosition(caller, userId, favorite.Id, s, position, parentType);

          await HooksRegistry.Each<IFavoriteHook>((sess, x) => x.CreateFavorite(sess, caller, favorite));

          tx.Commit();
          s.Flush();

          return favorite.Id;
        }
      }
    }

    public static async Task<List<FavoriteModel>> UpdateFavoritesPosition(UserOrganizationModel caller, long userId, long favoriteId, ISession s, long position, FavoriteType parentType)
    {
      var existFavoriteWithPosition = s.Query<FavoriteModel>().Where((x) =>
        x.DeleteTime == null &&
        x.UserId == userId &&
        x.Position == position &&
        x.Id != favoriteId &&
        x.ParentType == parentType
      ).OrderBy(x => x.CreatedDateTime).FirstOrDefault();

      if (existFavoriteWithPosition == null) return new List<FavoriteModel>();

      var favorites = s.QueryOver<FavoriteModel>().Where((x) =>
        x.DeleteTime == null &&
        x.UserId == userId &&
        x.Id != favoriteId &&
        x.Position >= position &&
        x.ParentType == parentType
      ).List().ToList();

      foreach ( var favorite in favorites )
      {
        favorite.Position += 1;
        s.Update(favorite);
      }

      return favorites;
    }

    public static async Task<long> EditFavorite(UserOrganizationModel caller, long favoriteId, FavoriteType? parentType, long? parentId, int? position, long? userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          var favorite = s.Get<FavoriteModel>(favoriteId);
          List<FavoriteModel> favorites = new List<FavoriteModel>();
          var uid = userId ?? favorite.UserId;
          var pid = parentId ?? favorite.ParentId;
          var pt = parentType ?? favorite.ParentType;


          PermissionsUtility.Create(s, caller)
            .EditFavorite(favoriteId)
            .CreateFavorite(uid,pt,pid);


          favorite.ParentType = pt;
          if (parentId != null)
            favorite.ParentId = (long)parentId;
          if (position != null)
          {
            favorite.Position = (int)position;
            favorites = await UpdateFavoritesPosition(caller, uid, favoriteId, s, favorite.Position, pt);
          }
          if (userId != null)
            favorite.UserId = (long)userId;
          favorite.DateLastModified = DateTime.Now;
          s.Update(favorite);

         favorites.Add(favorite);
         foreach(var f in favorites)
         {
            await HooksRegistry.Each<IFavoriteHook>((sess, x) => x.UpdateFavorite(sess, caller, f, new IFavoriteHookUpdates(){}));
         }

          tx.Commit();
          s.Flush();

          return favorite.Id;
        }
      }
    }

    public static async Task<long> DeleteFavorite(UserOrganizationModel caller, long favoriteId, FavoriteType parentType) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          PermissionsUtility.Create(s, caller).EditFavorite(favoriteId);

          var favorite = s.Get<FavoriteModel>(favoriteId);
          favorite.ParentType = parentType;
          favorite.DeleteTime = DateTime.Now;
          s.Update(favorite);

          await HooksRegistry.Each<IFavoriteHook>((sess, x) => x.UpdateFavorite(sess, caller, favorite, null));

          tx.Commit();
          s.Flush();

          return favorite.Id;
        }
      }
    }

    //public static List<FavoriteModel> GetFavorites(UserOrganizationModel caller, FavoriteType parentType, long parentId, bool includeArchived = true) {
    //  using (var s = HibernateSession.GetCurrentSession()) {
    //    using (var tx = s.BeginTransaction()) {

    //      PermissionsUtility.Create(s, caller).ViewFavorite(caller.Id,parentType,parentId);

    //      if (includeArchived) {
    //        return s.QueryOver<FavoriteModel>()
    //          .Where(x => x.ParentType == parentType && x.ParentId == parentId)
    //          .List().ToList();
    //      } else {
    //        return s.QueryOver<FavoriteModel>()
    //          .Where(x => x.ParentType == parentType && x.ParentId == parentId && x.DeleteTime == null)
    //          .List().ToList();
    //      }
    //    }
    //  }
    //}

    public static FavoriteModel GetFavoriteForUser(UserOrganizationModel caller, FavoriteType parentType, long parentId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          PermissionsUtility.Create(s, caller).ViewFavorite(caller.Id, parentType, parentId);

          return s.QueryOver<FavoriteModel>()
              .Where(x => x.ParentType == parentType && x.ParentId == parentId && x.UserId == caller.Id && x.DeleteTime == null)
              .List().FirstOrDefault();
        }
      }
    }

    //public static FavoriteModel GetFavoriteForUser_unsafe(UserOrganizationModel caller, FavoriteType parentType, long parentId) {
    //  using (var s = HibernateSession.GetCurrentSession()) {
    //    using (var tx = s.BeginTransaction()) {
    //      return s.QueryOver<FavoriteModel>()
    //          .Where(x => x.ParentType == parentType && x.ParentId == parentId && x.UserId == caller.Id && x.DeleteTime == null)
    //          .List().FirstOrDefault();
    //    }
    //  }
    //}

    public static IEnumerable<FavoriteModel> GetFavoriteForMeetingsUserQuery_Unsafe(ISession s, long forUserId, FavoriteType parentType, List<long> parentIds) {
      return s.QueryOver<FavoriteModel>()
          .Where(x => x.ParentType == parentType && x.UserId == forUserId && x.DeleteTime == null)
          .WhereRestrictionOn(x => x.ParentId).IsIn(parentIds)
          .Future();
    }

    public static List<FavoriteModel> GetFavoritesForUser(UserOrganizationModel caller, long userId, FavoriteType parentType, bool archived = false) {

      using (var session = HibernateSession.GetCurrentSession()) {
        using (var tx = session.BeginTransaction()) {
          PermissionsUtility.Create(session, caller).Self(userId);
          var favoritesQuery = session.QueryOver<FavoriteModel>().Where(x => x.ParentType == parentType && x.UserId == userId);

          if (!archived)
            favoritesQuery.Where(x => x.DeleteTime == null);

          return favoritesQuery.List().ToList();
        }
      }
    }

    #endregion

  }
}

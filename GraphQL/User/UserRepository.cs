using RadialReview.Accessors;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadialReview.Core.Utilities.PermisionMessages;
using static RadialReview.Core.Utilities.PermisionMessages.PermissionMessages;
using RadialReview.Models.Documents;
using RadialReview.Models.Enums;
using RadialReview.Models;
using RadialReview.Core.Models.Terms;
using Taxjar;
using RadialReview.Core.Accessors;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<UserQueryModel> GetUsers(CancellationToken cancellationToken);

    UserQueryModel GetUserById(long? userId, CancellationToken cancellationToken);
    IQueryable<UserQueryModel> GetTrackedMetricCreators(List<long> userIds, CancellationToken cancellationToken);

    UserSettingsQueryModel GetSettings(long userId, CancellationToken cancellationToken);

    IQueryable<MeetingMetadataModel> GetPossibleMeetingsForUser(CancellationToken cancellationToken);

    IQueryable<MeetingLookupModel> GetMeetingsForUserByAdminStatus(CancellationToken cancellationToken);

    IQueryable<UserOrganizationQueryModel> GetUserOrganizationForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken);

    IQueryable<SelectListQueryModel> GetAdminPermissionsMeetingsLookup(long userId, CancellationToken ct);

    Task<string> GetCoachToolsURL(CancellationToken cancellationToken);

    IQueryable<SelectListQueryModel> GetEditPermissionsMeetingsLookup(long userId, CancellationToken ct);

    IQueryable<KeyValuePair<long, string>> GetSupportContactCodeForUsers(IEnumerable<long> userIds, CancellationToken ct);

    OrgSettingsModel GetOrgSettings(long userId, CancellationToken ct);

    TermsCollection GetCustomTerms();

    IQueryable<TodoQueryModel> GetUserTodos(long userId, CancellationToken cancellationToken);

    IQueryable<OrganizationQueryModel> GetOrganizationsForUser(long userId, CancellationToken cancellationToken);
    RadialReview.Models.UserOrganizationModel GetCaller();

    IQueryable<GoalQueryModel> GetGoalsForUser(CancellationToken cancellationToken);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public UserQueryModel GetUserById(long? userId, CancellationToken cancellationToken)
    {
      if (userId == null) return null;

      //!! There do not seem to be any permissions associated with returning a user
      //var perms = PermissionsUtility.Create(session, caller);
      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var perms = PermissionsUtility.Create(s, caller);
          var user = s.Get<RadialReview.Models.UserOrganizationModel>((long)userId);
          perms.ViewOrganization(user.Organization.Id);
          return UserTransformer.TransformUser(user);
        }
      }
    }

    public IQueryable<UserQueryModel> GetTrackedMetricCreators(List<long> userIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        var session = HibernateSession.GetCurrentSession();
        List<RadialReview.Models.UserOrganizationModel> creators = UserAccessor.GetUsersByIdsUnsafe(userIds, session);
        return creators.Select(x => UserTransformer.TransformUser(x)).AsQueryable();
      });
    }

    public UserSettingsQueryModel GetSettings(long userId, CancellationToken cancellationToken)
    {
      return UserAccessor.GetSettings(caller, userId);
    }
    
    public IQueryable<OrganizationQueryModel> GetOrganizationsForUser(long userId, CancellationToken cancellationToken)
    {
      var session = HibernateSession.GetCurrentSession();
      Models.UserOrganizationModel user = session.Get<Models.UserOrganizationModel>((long)userId);
      List<Models.UserOrganizationModel> userOrgs = UserAccessor.GetUserOrganizationsForUser(user.User, session);
      IEnumerable<OrganizationQueryModel> orgQueryModel = userOrgs.Where(x => x.DeleteTime == null && x.Organization.DeleteTime == null && x.Organization.AccountType != AccountType.Cancelled).Select(x =>
        RepositoryTransformers.TransformToOrganization(x)
      );
      return orgQueryModel.AsQueryable();
    }

    public IQueryable<UserQueryModel> GetUsers(CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        using (var s = HibernateSession.GetCurrentSession())
        {
          using (var tx = s.BeginTransaction())
          {

            TempUserModel tempUserAlias = null;
            Models.UserOrganizationModel userOrgAlias = null;
            Models.UserModel userAlias = null;

            var q = s.QueryOver<Models.UserOrganizationModel>(() => userOrgAlias)
              .Left.JoinAlias(x => x.User, () => userAlias)
              .Left.JoinAlias(x => x.TempUser, () => tempUserAlias)
              .Where(x => x.Organization.Id == caller.Organization.Id)
              .Where(x => x.DeleteTime == null)
              .Select(
                    x => userAlias.FirstName,      //0
                    x => userAlias.LastName,       //1
                    x => x.Id,                     //2
                    x => tempUserAlias.FirstName,  //3
                    x => tempUserAlias.LastName,   //4
                    x => userAlias.UserName,       //5
                    x => tempUserAlias.Email,      //6
                    x => userAlias.ImageGuid,      //7
                    x => x.CreateTime,             //8
                    x => tempUserAlias.Created,    //9
                    x => userAlias.IsUsingV3      //10
                    //x => x.GetTimezoneOffset
                 )
                .List<object[]>()
                .Select(Unpackage)
                .ToList();

            q.ForEach(x =>
            {
              SecretCode code = UserAccessor.GetSupportSecretCodes(caller, x.Id)?.FirstOrDefault();
              x.SupportContactCode = code?.Code;
            });

            return q;
          }
        }
      });
    }

    public IQueryable<MeetingMetadataModel> GetPossibleMeetingsForUser(CancellationToken cancellationToken)
    {
      return L10Accessor.GetVisibleL10Recurrences(caller, caller.Id)
        .Select(x => RepositoryTransformers.MeetingMetadataFromTinyRecurrence(x, caller.Id))
        .AsQueryable()
        ;
    }

    public IQueryable<MeetingLookupModel> GetMeetingsForUserByAdminStatus(CancellationToken cancellationToken)
    {
      return SelectListAccessor.GetL10RecurrenceAdminable(caller, caller.Id)
        .Select(x => RepositoryTransformers.ToMeetingLookup(x))
        .AsQueryable();
    }

    public IQueryable<UserOrganizationQueryModel> GetUserOrganizationForUsers(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {

        return userIds.Select(uid => UserAccessor.GetUserOrganization(caller, uid, false, false))
                  .Select(x => RepositoryTransformers.TransformUserOrganization(x))
                  .ToList();

        //throw new Exception("Never rely on user supplied userIds");
        //throw new Exception("No permissions checked");
        //throw new Exception("EmailAtOrganization is unreliable");
        //var query =
        //    session
        //      .Query<RadialReview.Models.UserOrganizationModel>()
        //      .Where(m => userIds.Contains(m.Id))
        //      .Select(x => )
        //      ;
        //return query;
      });
    }

    public IQueryable<SelectListQueryModel> GetAdminPermissionsMeetingsLookup(long userId, CancellationToken ct)
    {
      var result = SelectListAccessor.GetL10RecurrenceAdminable(caller, userId, displayNonAdmin: true, allowHtmlText: false).Select(x => new SelectListQueryModel
      {
        Id = long.Parse(x.Value),
        Name = x.Text,
        Disabled = x.Disabled,
        DisabledText = x.Disabled ? PermissionMessages.GetMessage(MeetingPermissionMessage.AdminPermissionRequired) : null,
      }).AsQueryable();
      return result;
    }

    public async Task<string> GetCoachToolsURL(CancellationToken cancellationToken)
    {
      var orgId = caller.Organization.Id;
      if (caller.Organization.AccountType.IsImplementerOrCoach())
      {
        var fid = await DocumentsAccessor.GetCoachingTemplateFolder(caller, orgId);
        return "/documents/folder/" + fid;
      }

      var coachDocsFolder = caller.Organization.CoachDocumentsFolderId;
      if (coachDocsFolder == null)
      {
        return null;
      }

      using (var s = HibernateSession.GetCurrentSession())
      {
        using (var tx = s.BeginTransaction())
        {
          var folderId = s.Get<DocumentsFolder>(coachDocsFolder).LookupId;
          return "/documents/folder/" + folderId;
        }
      }
    }

    public IQueryable<SelectListQueryModel> GetEditPermissionsMeetingsLookup(long userId, CancellationToken ct)
    {
      var result = SelectListAccessor.GetL10RecurrenceEditable(caller, userId, displayNonEditable: true, allowHtmlText: false).Select(x => new SelectListQueryModel
      {
        Id = long.Parse(x.Value),
        Name = x.Text,
        Disabled = x.Disabled,
        DisabledText = x.Disabled ? PermissionMessages.GetMessage(MeetingPermissionMessage.EditPermissionRequired) : null,
      }).AsQueryable();
      return result;
    }

    public IQueryable<KeyValuePair<long, string>> GetSupportContactCodeForUsers(IEnumerable<long> userIds, CancellationToken ct)
    {
      var suportContactCodes = UserAccessor.GetSupportSecretCodeForUsers(caller, userIds);
      return suportContactCodes
        .Select(x => new KeyValuePair<long, string>(x.Key, x.Value.Code))
        .AsQueryable();
    }

    public OrgSettingsModel GetOrgSettings(long userId, CancellationToken ct)
    {
      var session = HibernateSession.GetCurrentSession();
      var businessPlanId = L10Accessor.GetSharedVTOVision(caller, caller.Organization.Id);
      UserOrganizationModel user = session.Get<UserOrganizationModel>(userId);
      var userOrg = user.GetOrganizationSettings();

      var _orgSettings = new OrgSettingsModel
      {
        Id = caller.Organization.Id,
        BusinessPlanId = businessPlanId.HasValue ? businessPlanId.Value : 0,
        V3BusinessPlanId = userOrg.V3BusinessPlanId,
        WeekStart = userOrg.WeekStart.ToString(),
        IsCoreProcessEnabled = userOrg.EnableCoreProcess,
      };

      return _orgSettings;
    }

    public TermsCollection GetCustomTerms()
    {
      var session = HibernateSession.GetCurrentSession();
      var tx = session.BeginTransaction();
      var perms = PermissionsUtility.Create(session, caller);
      var terms = TermsAccessor.GetTermsCollection(session, perms, caller.Organization.Id);

      return terms;
    }

    public IQueryable<TodoQueryModel> GetUserTodos(long userId, CancellationToken ct)
    {
      return TodoAccessor.GetTodosForUser(caller, userId, false).Select(_ => RepositoryTransformers.TransformTodo(_)).AsQueryable();
    }

    public RadialReview.Models.UserOrganizationModel GetCaller()
    {
      return caller;
    }

    public IQueryable<GoalQueryModel> GetGoalsForUser(CancellationToken cancellationToken)
    {
      return RockAccessor.GetRocksForUser(caller, caller.Id).Select(_ => _.TransformRock(null)).AsQueryable();
    }

    #endregion

  }

}
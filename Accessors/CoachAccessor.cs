using FluentNHibernate.Mapping;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Documents.Interceptors;
using RadialReview.Models.L10;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {


  public class CoachVM {
    public long OrgId { get; set; }
    public string CoachId { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public string OrganizationName { get; set; }
    public string CoachOrImplementer { get; set; }
    public string Title { get; set; }
    public string Email { get; set; }
    public UserRoleType RoleType { get; set; }
  }

  public class CoachOrg {
    public virtual long Id { get; set; }
    public virtual string CoachId { get; set; }
    public virtual long CoachUserId { get; set; }
    public virtual long OrgId { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual bool ViewEverything { get; set; }
    public virtual bool AdminEverything { get; set; }
    public virtual long? AdminSingleMeeting { get; set; }
    // public virtual string CoachesSharedFolderId { get; set; }

    public CoachOrg() {
      CreateTime = DateTime.UtcNow;
    }
    public class Map : ClassMap<CoachOrg> {
      public Map() {
        Id(x => x.Id);
        Map(x => x.CreateTime);
        Map(x => x.DeleteTime);
        Map(x => x.OrgId);
        Map(x => x.CoachId);
        Map(x => x.ViewEverything);
        Map(x => x.AdminEverything);
        Map(x => x.AdminSingleMeeting);
        Map(x => x.CoachUserId);
        //Map(x => x.CoachesSharedFolderId);
      }
    }
  }


  public class CoachAccessor {

    public class CoachSettings {
      public long? AdminRecurrenceId { get; set; }
      public bool ViewEverything { get; set; }
      public bool AdminEverything { get; set; }
      public bool AllowNonCoach { get; set; }
    }

    public static async Task<CoachVM> AddCoachToAccount(UserOrganizationModel caller, long orgId, string coachId, CoachSettings settings) {
      CoachVM coach;
      CoachEmail email;
      string orgName;
      string implName;
      string linkingUserName;
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.Or(x => x.ManagerAtOrganization(caller.Id, orgId), x => x.ManagingOrganization(orgId));

          if (AlreadyAttached(s, perms, orgId, coachId)) {
            throw new PermissionsException("Coach already linked.");
          }

          coach = GetCoach(coachId, settings.AllowNonCoach);
          if (coach == null) {
            throw new PermissionsException("Coach does not exist");
          }

          if (settings.AdminRecurrenceId != null) {
            perms.AdminL10Recurrence(settings.AdminRecurrenceId.Value);
          }

          var org = s.Get<OrganizationModel>(orgId);
          if (org.DeleteTime != null) {
            throw new PermissionsException("Organization does not exist.");
          }

          linkingUserName = caller.GetName();
          orgName = org.GetName();




          var isManager = settings.AdminEverything;
          var isOrgAdmin = settings.AdminEverything;

          var user = await JoinOrganizationAccessor.AttachUserModelToOrganization_Unsafe(s, orgId, coach.CoachId, isOrgAdmin, false, isManager, true, coach.RoleType);

          implName = user.GetFirstName();

          var warnings = new List<string>();


          if (settings.ViewEverything || settings.AdminEverything) {
            var l10s = s.QueryOver<L10Recurrence>()
              .Where(x => x.DeleteTime == null && x.OrganizationId == orgId)
              .List().ToList();

            int noAdmin = 0;
            foreach (var m in l10s) {
              try {
                PermissionsAccessor.AddPermItems(s, perms, caller, PermItem.ResourceType.L10Recurrence, m.Id, PermTiny.RGM(user.Id, settings.ViewEverything || settings.AdminEverything, settings.AdminEverything, settings.AdminEverything));
              } catch (PermissionsException e) {
                noAdmin += 1;
              }
            }

            if (noAdmin > 0) {
              warnings.Add("Some meetings cannot be shared. You are not a meeting admin.");
            }
          }

          if (settings.AdminRecurrenceId != null) {
            try {
              perms.AdminL10Recurrence(settings.AdminRecurrenceId.Value);
              PermissionsAccessor.AddPermItems(s, perms, caller, PermItem.ResourceType.L10Recurrence, settings.AdminRecurrenceId.Value, PermTiny.RGM(user.Id, true, true, true));
            } catch (PermissionsException) {
              warnings.Add("Cannot share the specified meeting. You are not a meeting admin.");
            }
          }

          s.Save(new CoachOrg() {
            AdminEverything = settings.AdminEverything,
            CoachId = coachId,
            OrgId = orgId,
            ViewEverything = settings.ViewEverything,
            AdminSingleMeeting = settings.AdminRecurrenceId,
            CoachUserId = user.Id
          });

          try {
            PermissionsAccessor.AddPermItems(s, perms, caller, PermItem.ResourceType.AccountabilityHierarchy, org.AccountabilityChartId, PermTiny.RGM(user.Id, true, settings.AdminEverything, settings.AdminEverything));
          } catch (PermissionsException) {
            warnings.Add("Cannot share the Organizational Chart. You are not a chart admin.");

          }


          email = s.GetSettingOrDefault(Variable.Names.COACH_LINKED_EMAIL, () => new CoachEmail {
            Subject = "You have been linked to {0} on {3}",
            Body = @"<center><img src=""{4}"" width=""300"" height=""auto"" alt=""{3}"" /></center><p>Hello {1},</p><p>{2} has linked you to {0}.</p><p>To view this team, log in to your account and click 'Change Organization' from the top-right dropdown and choose {0}. </p><p> If you have any questions reach out to the support team.</p><p>Thank you!</p><p>The {3}</p>",
          });

          await CoachTemplateFolderInterceptor.LinkAllFilesToAccount_Unsafe(s, coachId, orgId);

          tx.Commit();
          s.Flush();
        }
      }

      var mail = Mail.To("CoachLinkMessage", coach.Email)
              .Subject(email.Subject, orgName, implName, linkingUserName, Config.ProductName(), Config.ProductLogoUrl())
              .Body(email.Body, orgName, implName, linkingUserName, Config.ProductName(), Config.ProductLogoUrl());

      await Emailer.EnqueueEmail(mail, true);
      return coach;
    }

    public class CoachEmail {
      public string Subject { get; set; }
      public string Body { get; set; }
    }

    public static bool AlreadyAttached(UserOrganizationModel caller, long orgId, string coachId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          return AlreadyAttached(s, perms, orgId, coachId);

        }
      }
    }

    public static bool AlreadyAttached(ISession s, PermissionsUtility perms, long orgId, string coachId) {
      perms.ViewOrganization(orgId);

      return s.QueryOver<CoachOrg>()
        .Where(x => x.DeleteTime == null && x.OrgId == orgId && coachId == x.CoachId)
        .List().Any();
    }

    public static CoachVM GetCoach(string coachId, bool allowNonCoach) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          return GetCoach(s, coachId, allowNonCoach);
        }
      }
    }

    private static CoachVM GetCoach(ISession s, string coachId, bool allowNonCoach = false) {
      var user = s.Get<UserModel>(coachId);

      if (user.DeleteTime != null) {
        throw new PermissionsException("Coach does not exist anymore.");
      }

      OrganizationModel org = null;

      var validCoachesQ = s.QueryOver<UserOrganizationModel>()
        .JoinAlias(x => x.Organization, () => org)
        .Where(x => x.User.Id == coachId && x.DeleteTime == null && org.DeleteTime == null);
      if (!allowNonCoach) {
        validCoachesQ.Where(x => org.AccountType == Models.Enums.AccountType.Coach || org.AccountType == Models.Enums.AccountType.Implementer || org.AccountType == Models.Enums.AccountType.BloomGrowthCoach);
      }
      var validCoaches = validCoachesQ.List().ToList();

      if (!validCoaches.Any()) {
        throw new PermissionsException("Coach doesn't exist.");
      }

      var output = new CoachVM() {
        CoachId = coachId,
        CoachOrImplementer = "Coach",
        ImageUrl = validCoaches.First().ImageUrl(),
        Name = validCoaches.First().GetName(),
        OrganizationName = validCoaches.First().Organization.GetName(),
        Title = "Coach",//validCoaches.First().GetTitles(),
        OrgId = validCoaches.First().Organization.Id,
        RoleType = UserRoleType.Coach,
        Email = validCoaches.First().GetEmail()
      };

      var asImpl = validCoaches.FirstOrDefault(x => x.Organization.AccountType == Models.Enums.AccountType.Implementer);

      if (asImpl != null) {
        output.CoachOrImplementer = "Bloom Growth Guide";
        output.Title = "Bloom Growth Guide";//asImpl.GetTitles();
        output.OrganizationName = asImpl.Organization.GetName();
        output.OrgId = asImpl.Organization.Id;
        output.RoleType = UserRoleType.Implementer;
        output.Email = asImpl.GetEmail();
      }

      return output;
    }

    public static bool IsCoach(UserOrganizationModel caller, long userId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.Self(userId);
          var user = s.Get<UserOrganizationModel>(userId);

          if (user.UserIds.Length == 0)
            return false;

          return s.QueryOver<UserRole>()
                .Where(x => x.DeleteTime == null && (x.RoleType == UserRoleType.Coach || x.RoleType == UserRoleType.Implementer))
                .WhereRestrictionOn(x => x.UserId)
                .IsIn(user.UserIds)
                .Take(1).List().ToList().Any();

        }
      }
    }
  }
}

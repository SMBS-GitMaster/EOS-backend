using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Core.Properties;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using TimeZoneConverter;
using TimeZoneNames;
using RadialReview.Core.Models.Enums;

namespace RadialReview.Models {

  [DebuggerDisplay("Organization")]
  public partial class OrganizationModel : ResponsibilityGroupModel, IOrigin, IDeletable, TimeSettings {



    public virtual long? PrimaryContactUserId { get; set; }
    public virtual long? AgileOrganizationId { get; set; }



    [Obsolete("deprecated", false)]
    public virtual LocalizedStringModel NameDeprecated { get; set; }

    [Display(Name = "organizationName", ResourceType = typeof(DisplayNameStrings))]
    public virtual string Name { get; set; }

    [Display(Name = "imageUrl", ResourceType = typeof(DisplayNameStrings))]
    public virtual ImageModel Image { get; set; }

    [Display(Name = "managerCanAddQuestions", ResourceType = typeof(DisplayNameStrings))]
    public virtual Boolean ManagersCanEdit { get; set; }
    [Obsolete("Do not use, use PermItem (EditDeleteUserDataForOrganization) instead.")]
    public virtual Boolean ManagersCanRemoveUsers { get; set; }
    public virtual bool StrictHierarchy { get; set; }

    [Obsolete("Use Settings instead.")]
    public virtual OrganizationSettings _Settings { get; set; }
    public virtual OrganizationSettings Settings {
      get {
        if (_Settings == null)
          _Settings = new OrganizationSettings();
        return _Settings;
      }
    }

    public virtual LockoutType Lockout { get; set; }

    public virtual AccountType AccountType { get; set; }
    public virtual ClientSuccessTag ClientSuccessTag { get; set; }
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<UserOrganizationModel> Members { get; set; }
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<PaymentModel> Payments { get; set; }
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<InvoiceModel> Invoices { get; set; }
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<QuestionModel> CustomQuestions { get; set; }
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<QuestionCategoryModel> QuestionCategories { get; set; }
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<GroupModel> Groups { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual DateTime CreationTime { get; set; }
    public virtual bool SendEmailImmediately { get; set; }
    public virtual long AccountabilityChartId { get; set; }

    //public virtual bool IsCoachAccount { get; set; }
    //public virtual bool HasCoach { get; set; }
    public virtual bool HasCoachDocuments { get; set; }
    public virtual long? CoachDocumentsFolderId { get; set; }
    public virtual long? CoachTemplateDocumentsFolderId { get; set; }
    public virtual long? TermsId { get; set; }



    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<ReviewsModel> Reviews { get; set; }

    public override OriginType GetOrigin() {
      return OriginType.Organization;
    }
    public virtual OriginType GetOriginType() {
      return OriginType.Organization;
    }

    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual PaymentPlanModel PaymentPlan { get; set; }

    public virtual String GetSpecificNameForOrigin() {
      return Name;
    }

    public virtual bool ManagersCanEditPositions { get; set; }
    public virtual string ImplementerEmail { get; set; }
    public virtual long? ProcessMainFolderId { get; set; }
    public virtual long? DocumentsMainFolderId { get; set; }

    public OrganizationModel() {
      Groups = new List<GroupModel>();
      Payments = new List<PaymentModel>();
      Invoices = new List<InvoiceModel>();
      CustomQuestions = new List<QuestionModel>();
      Members = new List<UserOrganizationModel>();
      QuestionCategories = new List<QuestionCategoryModel>();
      Reviews = new List<ReviewsModel>();
      ManagersCanEditPositions = true;
      ManagersCanEdit = false;
      _Settings = new OrganizationSettings();
      AccountType = AccountType.Demo;
      ClientSuccessTag = ClientSuccessTag.None;
      Lockout = LockoutType.NoLockout;

    }

    public virtual List<IOrigin> OwnsOrigins() {
      var owns = new List<IOrigin>();
      owns.AddRange(CustomQuestions.Cast<IOrigin>().ToList());
      owns.AddRange(QuestionCategories.Cast<IOrigin>().ToList());
      owns.AddRange(Groups.Cast<IOrigin>().ToList());
      owns.AddRange(Members.Cast<IOrigin>().ToList());
      owns.AddRange(Members.Cast<IOrigin>().ToList());

      return owns;
    }

    public virtual List<IOrigin> OwnedByOrigins() {
      var ownedBy = new List<IOrigin>();
      return ownedBy;
    }

    public override string GetName(GivenNameFormat format = GivenNameFormat.FirstAndLast) {
      return Name;
    }
    public override string GetImageUrl() {
      return Settings.GetImageUrl() ?? base.GetImageUrl();
    }

    public override string GetGroupType() {
      return DisplayNameStrings.organization;
    }




    public class OrganizationModelMap : SubclassMap<OrganizationModel> {
      public OrganizationModelMap() {
        Map(x => x.AccountType);
        Map(x => x.ClientSuccessTag);
        Map(x => x.ManagersCanEdit);
        Map(x => x.DeleteTime);
        Map(x => x.CreationTime);
        Map(x => x.StrictHierarchy);
        Map(x => x.ManagersCanEditPositions);
        Map(x => x.ManagersCanRemoveUsers);
        Map(x => x.AccountabilityChartId);

        Map(x => x.PrimaryContactUserId);
        Map(x => x.AgileOrganizationId);

        Map(x => x.Lockout).CustomType<LockoutType>();

        Map(x => x.SendEmailImmediately);

        Map(x => x.ImplementerEmail);
        Map(x => x.ProcessMainFolderId);
        Map(x => x.DocumentsMainFolderId);
        Map(x => x.TermsId);


        //Map(x => x.HasCoach);
        Map(x => x.HasCoachDocuments);
        Map(x => x.CoachDocumentsFolderId);
        Map(x => x.CoachTemplateDocumentsFolderId);


        Map(x => x.Name).Column("NameStr");

        Component(x => x._Settings).ColumnPrefix("Settings_");

        References(x => x.Image)
          .Not.LazyLoad()
          .Cascade.SaveUpdate();

        References(x => x.NameDeprecated)
          .Column("Name_id")
          .LazyLoad()
          .Fetch.Join()
          .Cascade.SaveUpdate();


        References(x => x.PaymentPlan)
          .LazyLoad()
          .Cascade.SaveUpdate();

        HasMany(x => x.Reviews)
          .LazyLoad()
          .Cascade.SaveUpdate();
        HasMany(x => x.Members)
          .KeyColumn("Organization_Id")
          .LazyLoad()
          .Cascade.SaveUpdate();
        HasMany(x => x.Payments)
          .LazyLoad()
          .Cascade.SaveUpdate();
        HasMany(x => x.Invoices)
          .LazyLoad()
          .Cascade.SaveUpdate();
      }
    }

  }
}

using FluentNHibernate.Mapping;
using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;
using RadialReview.Models.UserModels;
using RadialReview.Core.Properties;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace RadialReview.Models {



  [DebuggerDisplay("User: {EmailAtOrganization}")]
  [DataContract]
  public class UserOrganizationModel : ResponsibilityGroupModel, IOrigin, IHistorical, TimeSettings, IForModel {
    protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public static long ADMIN_ID = -7231398885982031L;

    public static UserOrganizationModel ADMIN = new UserOrganizationModel() {
      IsRadialAdmin = true,
      Id = UserOrganizationModel.ADMIN_ID,
    };

    public static UserOrganizationModel CreateAdmin() {

      return new UserOrganizationModel() {
        IsRadialAdmin = true,
        Id = UserOrganizationModel.ADMIN_ID,
      };
    }


    public virtual DateTime? _MethodStart { get; set; }

    public virtual string GetClientRequestId() {
      if (string.IsNullOrEmpty(_ClientRequestId)) {
        return "" + User.NotNull(x => x.Id.ToString().Replace("-", "")) ?? ("" + CreateTime.ToJsMs() + Id);
      }
      return _ClientRequestId;
    }

    public virtual void SetClientRequestId(string id) {
      _ClientRequestId = id;
    }

    public virtual UserOrganizationModel SetClientTimeStamp(long timestamp) {
      _ClientTimestamp = timestamp;
      return this;
    }
    public virtual void IncrementClientTimestamp() {
      _ClientTimestamp = (_ClientTimestamp ?? DateTime.UtcNow.ToJsMs()) + 1;
    }

    public virtual bool IsEmailVerified() {
      try {
        if (User == null) {
          return false;
        }
        return !User.EmailNotVerified;
      } catch (Exception e) {
        return false;
      }

    }

    public virtual string _ClientRequestId { get; set; }
    public virtual long? _ClientTimestamp { get; set; }
    public virtual int? _ClientOffset { get; set; }
    protected virtual TimeData _timeData { get; set; }

    public class PermissionsOverrides {
      public AdminShortCircuit Admin { get; set; }
      public bool IgnorePaymentLockout { get; set; }
      public PermissionsOverrides() {
        Admin = new AdminShortCircuit();
      }

    }
    public class AdminShortCircuit {
      public bool IsMocking { get; internal set; }
      public bool IsRadialAdmin { get; set; }
      public string ActualUserId { get; set; }
      public string EmulatedUserId { get; set; }
      public bool AllowAdminWithoutAudit { get; set; }
    }
    public virtual PermissionsOverrides _PermissionsOverrides { get; set; }
    public virtual bool _IsRadialAdmin { get; set; }
    [Obsolete("For testing only")]
    public virtual bool _IsTestAdmin { get; set; }

    public virtual ITimeData GetTimeSettings() {
      if (_timeData == null) {
        var orgSettings = GetOrganizationSettings();
        _timeData = new TimeData() {
          Now = _MethodStart ?? DateTime.UtcNow,
          Period = orgSettings.ScorecardPeriod,
          TimezoneOffset = _ClientOffset ?? orgSettings.GetTimezoneOffset(),
          WeekStart = orgSettings.WeekStart,
          YearStart = orgSettings.YearStart,
          DateFormat = orgSettings.GetDateFormat()
        };
      }
      return _timeData;
    }

    [DataMember]
    public virtual string Name { get { return GetName(); } }
    [DataMember]
    public virtual string UserName {
      get {
        try {
          return GetUsername();
        } catch (Exception) {
          return null;
        }
      }
    }

    [JsonIgnore]
    [IgnoreDataMember]
    public virtual TempUserModel TempUser { get; set; }

    [Obsolete("This is property unreliable. uom.GetEmail() is correct.")]
    public virtual String EmailAtOrganization { get; set; }
    public virtual int NumViewedNewFeatures { get; set; }

    public virtual Boolean ManagerAtOrganization { get; set; }
    public virtual Boolean ManagingOrganization { get; set; }
    public virtual Boolean IsRadialAdmin { get; set; }
    public virtual bool IsImplementer { get; set; }
    public virtual bool IsFreeUser { get; set; }
    public virtual bool EnableWhale { get; set; }
    public virtual DateTime AttachTime { get; set; }
    public virtual DateTime? DetachTime { get; set; }

    public virtual DateTime? LastSupportCodeReset { get; set; }

    [JsonIgnore]
    [IgnoreDataMember]
    public virtual UserModel User { get; set; }

    [JsonIgnore]
    [IgnoreDataMember]
    public virtual long? AgileUserId { get; set; }

    public virtual long[] UserIds {
      get {
        if (User == null) {
          return new long[] { Id };
        }

        return User.UserOrganizationIds;
      }
    }

    [DebuggerDisplay("Cache")]
    public virtual UserLookup Cache { get; set; }

    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerDisplay("ManagingUsers")]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<ManagerDuration> ManagingUsers { get; set; }
    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerDisplay("ManagedBy")]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<ManagerDuration> ManagedBy { get; set; }
    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerDisplay("Groups")]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<GroupModel> Groups { get; set; }
    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerDisplay("ManagingGroups")]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<GroupModel> ManagingGroups { get; set; }
    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<ReviewModel> Reviews { get; set; }
    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual List<ReviewsModel> CreatedReviews { get; set; }
    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [Obsolete("do not use", true)]
    private IList<PositionDurationModel> Positions { get; set; }
    [JsonIgnore]
    [IgnoreDataMember]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IList<TeamDurationModel> Teams { get; set; }
    public virtual DateTime? DeleteTime { get; set; }
    public virtual DateTime CreateTime { get; set; }
    public virtual int CountPerPage { get; set; }
    public virtual String JobDescription { get; set; }

    public virtual long? JobDescriptionFromTemplateId { get; set; }
    public virtual Boolean EvalOnly { get; set; }


    public override string GetImageUrl() {
      return this.ImageUrl(true);
    }

    public override OriginType GetOrigin() {
      return OriginType.User;
    }

    public virtual OriginType GetOriginType() {
      return OriginType.User;
    }

    public virtual String GetSpecificNameForOrigin() {
      return this.GetName();
    }

    public virtual OrganizationModel.OrganizationSettings GetOrganizationSettings() {
      return Organization.NotNull(x => x.Settings) ?? new OrganizationModel.OrganizationSettings();
    }

    #region Helpers
    public virtual Dictionary<String, List<String>> Properties { get; set; }
    public virtual Boolean IsAttached() {
      return User != null;
    }
    public virtual List<UserOrganizationModel> AllSubordinates { get; set; }

    public virtual bool IsClient { get; set; }
    public virtual bool IsPlaceholder { get; set; }

    #endregion

    public UserOrganizationModel() : base() {
      CreateTime = DateTime.UtcNow;
      ManagedBy = new List<ManagerDuration>();
      ManagingUsers = new List<ManagerDuration>();
      Groups = new List<GroupModel>();
      ManagingGroups = new List<GroupModel>();
      AttachTime = DateTime.UtcNow;
      Properties = new Dictionary<string, List<String>>();
      Reviews = new List<ReviewModel>();
      //Positions = new List<PositionDurationModel>();
      Teams = new List<TeamDurationModel>();
      TempUser = null;
      IsClient = false;
      IsPlaceholder = false;
      Cache = new UserLookup();
      IsFreeUser = false;
    }


    public override string ToString() {
      return Organization.NotNull(x => x.Name) + " - " + this.GetNameAndTitle();
    }


    public virtual List<IOrigin> OwnsOrigins() {
      var owns = new List<IOrigin>();
      owns.AddRange(ManagingUsers.Cast<IOrigin>());
      owns.AddRange(ManagingGroups.Cast<IOrigin>());
      return owns;
    }

    public virtual List<IOrigin> OwnedByOrigins() {
      var ownedBy = new List<IOrigin>();
      ownedBy.AddRange(ManagedBy.Cast<IOrigin>());
      ownedBy.Add(Organization);
      return ownedBy;
    }

    public override string GetNameExtended() {
      return this.GetNameAndTitle();
    }
    public override string GetNameShort() {
      return this.GetFirstName();
    }
    public override string GetName(GivenNameFormat nameFormat = GivenNameFormat.FirstAndLast) {
      var user = this.NotNull(x => x.User);

      if (user != null) {
        return user.Name(nameFormat);
      }

      var tempUser = this.NotNull(x => x.TempUser);

      if (tempUser != null) {
        return tempUser.Name(nameFormat);
      }

      return this.Cache.NotNull(x => x.Name) ?? this.EmailAtOrganization;
    }
    public virtual string GetFirstName() {
      if (this.User != null && !String.IsNullOrWhiteSpace(this.User.FirstName)) {
        return this.User.FirstName.Trim();
      }

      if (TempUser != null && !String.IsNullOrWhiteSpace(this.TempUser.FirstName)) {
        return this.TempUser.FirstName.Trim();
      }

      return GetName();
    }
    public virtual string GetLastName() {
      if (this.User != null && !String.IsNullOrWhiteSpace(this.User.LastName)) {
        return this.User.LastName.Trim();
      }

      if (TempUser != null && !String.IsNullOrWhiteSpace(this.TempUser.LastName)) {
        return this.TempUser.LastName.Trim();
      }

      return GetName();
    }

    public virtual string GetTitles(int numToShow = int.MaxValue) {
      try {
        if (this.Cache == null || string.IsNullOrWhiteSpace(this.Cache.Positions)) {
          return "";
        }

        var positions = this.Cache.GetPositions(false).Distinct().ToArray();
        var count = positions.Count();

        String titles = null;
        var actualPositions = positions.ToList();
        /*if (callerUserId == Id) {
					actualPositions.Insert(0, "You");
				}*/

        titles = String.Join(", ", actualPositions.Take(numToShow));
        if (actualPositions.Count > numToShow) {
          titles += "+" + (actualPositions.Count - numToShow);
        }
        return titles;
      } catch (Exception e) {
        return "";
      }
    }

    public override string GetGroupType() {
      return DisplayNameStrings.user;
    }

    public virtual string GetUsername() {
      return User.NotNull(x => x.UserName) ?? TempUser.Email;
    }


    public virtual UserOrganizationModel UpdateCache(ISession s) {

      Cache = this.NotNull(x => x.Cache) ?? new UserLookup();


      var INCLUDE_UNNAMED_POSITIONS = true;
      var positions = AccountabilityAccessor.GetPositionsForUser_Unsafe(s, Id, INCLUDE_UNNAMED_POSITIONS).Select(x => x.Name).ToArray();


      if (Cache.Id > 0) {
        s.Evict(Cache);
        Cache = s.Get<UserLookup>(Cache.Id);
      }



      if (Cache.OrganizationId != Organization.Id) {
        Cache.OrganizationId = Organization.Id;
      }

      if (Cache._ImageUrlSuffix != this.ImageUrl(true, ImageSize._suffix)) {
        Cache._ImageUrlSuffix = this.ImageUrl(true, ImageSize._suffix);
      }

      if (Cache.AttachTime != AttachTime) {
        Cache.AttachTime = AttachTime;
      }

      if (Cache.CreateTime != CreateTime) {
        Cache.CreateTime = CreateTime;
      }

      if (Cache.DeleteTime != DeleteTime) {
        Cache.DeleteTime = DeleteTime;
      }

      if (Cache.IsRadialAdmin != this.IsRadialAdmin) {
        Cache.IsRadialAdmin = this.IsRadialAdmin;
      }

      if (Cache.Email != this.GetEmail()) {
        Cache.Email = this.GetEmail();
      }

      if (Cache.IsClient != this.IsClient) {
        Cache.IsClient = this.IsClient;
      }

      if (Cache.HasJoined != (User != null)) {
        Cache.HasJoined = User != null;
      }

      if (Cache.HasSentInvite != (!(TempUser != null && TempUser.LastSent == null))) {
        Cache.HasSentInvite = !(TempUser != null && TempUser.LastSent == null);
      }

      if (Cache.IsAdmin != this.ManagingOrganization) {
        Cache.IsAdmin = ManagingOrganization;
      }

      if (Cache.IsManager != this.IsManager(true)) {
        Cache.IsManager = this.IsManager(true);
      }

      if (Cache.EvalOnly != this.EvalOnly) {
        Cache.EvalOnly = this.EvalOnly;
      }

      if (Cache.LastSupportCodeReset != LastSupportCodeReset) {
        Cache.LastSupportCodeReset = LastSupportCodeReset;
      }

      UserOrganizationModel managerA = null;
      UserLookup cacheA = null;
      try {
        var managersQ = s.QueryOver<ManagerDuration>()
          .JoinAlias(x => x.Manager, () => managerA)
          .JoinAlias(x => managerA.Cache, () => cacheA)
          .Where(x => x.DeleteTime == null && x.SubordinateId == Id && managerA.DeleteTime == null)
          .Select(x => cacheA.Name).List<string>().Distinct().ToList();

        var managers = String.Join(", ", managersQ);
        if (Cache.Managers != managers) {
          Cache.Managers = managers;
        }
      } catch (Exception e) {
        log.Error(e);
      }



      //var positions = String.Join(", ", Positions.ToListAlive().Select(x => x.PositionName).Distinct());
      var positionsStr = UserLookup.CreatePositionsString(!INCLUDE_UNNAMED_POSITIONS, positions);


      if (Cache.Positions != positionsStr) {
        Cache.Positions = positionsStr;
      }

      if (Cache.IsImplementer != IsImplementer) {
        Cache.IsImplementer = IsImplementer;
      }


      try {
        OrganizationTeamModel teamA = null;
        var teams = s.QueryOver<TeamDurationModel>()
          .JoinAlias(x => x.Team, () => teamA)
          .Where(x => x.DeleteTime == null && x.UserId == Id).Select(x => teamA.Name).List<string>().ToList();

        var teamsStr = String.Join(", ", teams);
        if (Cache.Teams != teamsStr) {
          Cache.Teams = teamsStr;
        }
      } catch (Exception e) {
        log.Error(e);
      }
      if (Cache.Name != this.GetName()) {
        Cache.Name = this.GetName();
      }

      var measurable = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && x.AccountableUserId == Id).ToRowCountQuery().FutureValue<int>();

      var rock = s.QueryOver<RockModel>().Where(x => x.DeleteTime == null && x.ForUserId == Id).ToRowCountQuery().FutureValue<int>();

      var role = AccountabilityAccessor.Unsafe.CountRoles_Unsafe(s, Id);

      if (Cache.NumMeasurables != measurable.Value) {
        Cache.NumMeasurables = measurable.Value;
      }

      if (Cache.NumRoles != role) {
        Cache.NumRoles = role;
      }

      if (Cache.NumRocks != rock.Value) {
        Cache.NumRocks = rock.Value;
      }

      if (Cache.UserId != Id) {
        Cache.UserId = Id;
      }

      if (Cache.Id == 0) {
        s.Save(Cache);
      } else {
        s.Merge(Cache);
      }
      try {

      } catch (Exception e) {
        log.Error(new Exception("Could not update Session", e));
      }
      return this;

    }


    public virtual string ClientOrganizationName { get; set; }

    public virtual string UserModelId { get { return User.NotNull(x => x.Id); } set { } }

    public virtual long ModelId { get { return Id; } }
    public virtual string ModelType { get { return ForModel.GetModelType<UserOrganizationModel>(); } }

    public virtual bool IsClientSuccess() {
      if (this.IsRadialAdmin)
        return true;
      if (this.User!=null && this.User.IsRadialAdmin)
        return true;

      var email = (this.GetEmail()??"").ToLower().Trim();
      if (email.EndsWith("@bloomgrowth.com") || email.EndsWith("@mytractiontools.com") || email.EndsWith("@winterinternational.io"))
        return true;

      return false;
    }

    public class PrimaryWorkspaceModel {
      public DashboardType Type { get; set; }
      public long WorkspaceId { get; set; }
      public class Map : ComponentMap<PrimaryWorkspaceModel> {
        public Map() {
          this.Map(x => x.Type).CustomType<DashboardType>();
          this.Map(x => x.WorkspaceId);
        }
      }

      public bool IsPrimaryForUser(UserOrganizationModel forUser) {
        if (forUser == null || forUser.PrimaryWorkspace == null)
          return false;
        return WorkspaceId == forUser.PrimaryWorkspace.WorkspaceId && Type == forUser.PrimaryWorkspace.Type;
      }

      public bool IsGenerated() {
        return Type != DashboardType.Standard;
      }
    }

    public virtual PrimaryWorkspaceModel PrimaryWorkspace { get; set; }
    public virtual string _ConnectionId { get; set; }

    public virtual bool Is<T>() {
      return typeof(UserOrganizationModel).IsAssignableFrom(typeof(T));
    }
    public virtual string ToPrettyString() {
      return GetName();
    }

    public virtual DataContract GetUserDataContract() {
      return new DataContract(this);
    }

    [DataContract]
    public class DataContract {
      [DataMember]
      public virtual long Id { get; set; }
      [DataMember]
      public virtual String Name { get; set; }
      [DataMember]
      public virtual String Username { get; set; }

      public DataContract(UserOrganizationModel self) {
        Id = self.Id;
        Name = self.GetName();
        Username = self.GetUsername();
      }
    }

    public virtual int GetTimezoneOffset() {
      return _ClientOffset ?? GetOrganizationSettings().GetTimezoneOffset();
    }

    public virtual string GetConnectionId() {
      return _ConnectionId;
    }

    public virtual bool TestIsRadialAdmin() {
      return IsRadialAdmin || (User!=null && User.IsRadialAdmin) || (_PermissionsOverrides!=null && _PermissionsOverrides.Admin !=null && _PermissionsOverrides.Admin.IsRadialAdmin);
    }
  }

  public class UserOrganizationModelMap : SubclassMap<UserOrganizationModel> {
    public UserOrganizationModelMap() {

      Map(x => x.IsRadialAdmin);
      Map(x => x.IsImplementer);
      Map(x => x.CountPerPage).Default("10");
      Map(x => x.ManagingOrganization);
      Map(x => x.ManagerAtOrganization);
      Map(x => x.AttachTime);
      Map(x => x.CreateTime);
      Map(x => x.DetachTime);
      Map(x => x.DeleteTime);
      Map(x => x.EnableWhale);
      Map(x => x.EmailAtOrganization);
      Map(x => x.NumViewedNewFeatures);

      Map(x => x.IsClient);
      Map(x => x.IsPlaceholder);

      Map(x => x.UserModelId).Column("UserModel_id");
      Map(x => x.AgileUserId);
      Map(x => x.EvalOnly);

      Map(x => x.LastSupportCodeReset);
      Map(x => x.ClientOrganizationName);

      Map(x => x.JobDescription).Length(65000);
      Map(x => x.JobDescriptionFromTemplateId);
      Map(x => x.IsFreeUser);

      Component(x => x.PrimaryWorkspace).ColumnPrefix("PrimaryWorkspace_");

      References(x => x.TempUser).Not.LazyLoad().Cascade.All();
      References(x => x.Cache).LazyLoad().Cascade.All();

      HasMany(x => x.Reviews)
        .Cascade.SaveUpdate();

      /*HasMany(x => x.Positions)
				.KeyColumn("UserId")
				.Not.LazyLoad()
				.Cascade.SaveUpdate();*/

      References(x => x.User)
        .Fetch.Join()
        .LazyLoad()
        .Cascade.SaveUpdate();

      HasMany(x => x.ManagedBy)
        .LazyLoad()
        .KeyColumn("SubordinateId")
        .Cascade.SaveUpdate();

      HasMany(x => x.ManagingUsers)
        .LazyLoad()
        .KeyColumn("ManagerId")
        .Cascade.SaveUpdate();

      HasManyToMany(x => x.Groups)
        .LazyLoad()
        .Table("GroupMembers")
        .Inverse();
      HasManyToMany(x => x.ManagingGroups)
        .LazyLoad()
        .Table("GroupManagement")
        .Inverse();



    }
  }

}

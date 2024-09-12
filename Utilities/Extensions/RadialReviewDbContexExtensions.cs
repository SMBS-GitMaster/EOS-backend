namespace RadialReview.Core.Utilities.Extensions;

using System;
using System.Linq.Expressions;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using EF = RadialReview.DatabaseModel.Entities;

public static class RadialReviewDbContextExtensions
{
    public static readonly Expression<Func<EF.UserOrganizationModel, UserOrganizationModel>> ToLegacyUserOrganizationModel = x => 
        new UserOrganizationModel {
            Id = x.ResponsibilityGroupModelId,
            AgileUserId = x.AgileUserId,
            AttachTime = x.AttachTime ?? default(DateTime),
            //Cache = new RadialReview.Models.UserModels.UserLookup { Id = x.CacheId.Value },
            CreateTime = x.CreateTime ?? default(DateTime),
            CountPerPage = x.CountPerPage ?? default(int),
            ClientOrganizationName = x.ClientOrganizationName,
            DeleteTime = x.DeleteTime,
            DetachTime = x.DetachTime,
            EmailAtOrganization = x.EmailAtOrganization,
            EnableWhale = x.EnableWhale ?? default(bool),
            IsRadialAdmin = x.IsRadialAdmin ?? default(bool),
            IsImplementer = x.IsImplementer ?? default(bool),
            NumViewedNewFeatures = x.NumViewedNewFeatures ?? default(int),
            IsClient = x.IsClient ?? default(bool),
            IsPlaceholder = x.IsPlaceholder ?? default(bool),
            UserModelId = x.UserModelId,
            EvalOnly = x.EvalOnly ?? default(bool),
            LastSupportCodeReset = x.LastSupportCodeReset,
            JobDescription = x.JobDescription,
            JobDescriptionFromTemplateId = x.JobDescriptionFromTemplateId ?? default(long),
            IsFreeUser = x.IsFreeUser ?? default(bool),
            PrimaryWorkspace = new () {
                WorkspaceId = x.PrimaryWorkspaceWorkspaceId ?? default(long),
                Type = (RadialReview.Models.Enums.DashboardType) (x.PrimaryWorkspaceType ?? default(int)),
            },
            // NumRocks = x.NumRocks,
            // NumRoles = x.NumRoles, 
            // NumMeasurables = x.NumMeasurables,
            ManagingOrganization = x.ManagingOrganization ?? default(bool),
            ManagerAtOrganization = x.ManagerAtOrganization ?? default(bool),
            
        };

    public static UserOrganizationModel ToLegacyModel(this EF.UserOrganizationModel x) =>
        new UserOrganizationModel {
            Id = x.ResponsibilityGroupModelId,
            AgileUserId = x.AgileUserId,
            AttachTime = x.AttachTime.Value,
            // CacheId = x.Cache.Id,
            CreateTime = x.CreateTime.Value,
            CountPerPage = x.CountPerPage.Value,
            ClientOrganizationName = x.ClientOrganizationName,
            DeleteTime = x.DeleteTime,
            DetachTime = x.DetachTime,
            EmailAtOrganization = x.EmailAtOrganization,
            EnableWhale = x.EnableWhale.Value,
            IsRadialAdmin = x.IsRadialAdmin.Value,
            IsImplementer = x.IsImplementer.Value,
            NumViewedNewFeatures = x.NumViewedNewFeatures.Value,
            IsClient = x.IsClient.Value,
            IsPlaceholder = x.IsPlaceholder.Value,
            UserModelId = x.UserModelId,
            EvalOnly = x.EvalOnly.Value,
            LastSupportCodeReset = x.LastSupportCodeReset,
            JobDescription = x.JobDescription,
            JobDescriptionFromTemplateId = x.JobDescriptionFromTemplateId.Value,
            IsFreeUser = x.IsFreeUser.Value,
            PrimaryWorkspace = new () {
                WorkspaceId = x.PrimaryWorkspaceWorkspaceId.Value,
                Type = (RadialReview.Models.Enums.DashboardType) x.PrimaryWorkspaceType.Value,
            },
            // NumRocks = x.NumRocks,
            // NumRoles = x.NumRoles, 
            // NumMeasurables = x.NumMeasurables,
            ManagingOrganization = x.ManagingOrganization.Value,
            ManagerAtOrganization = x.ManagerAtOrganization.Value,
        };

    public static readonly Expression<Func<UserOrganizationModel, EF.UserOrganizationModel>> FromLegacyUserOrganizationModel = x =>
        new EF.UserOrganizationModel 
        {
            ResponsibilityGroupModelId = x.Id,
            AgileUserId = x.AgileUserId,
            AttachTime = x.AttachTime,
            CacheId = x.Cache.Id,
            CreateTime = x.CreateTime,
            CountPerPage = x.CountPerPage,
            ClientOrganizationName = x.ClientOrganizationName,
            DeleteTime = x.DeleteTime,
            DetachTime = x.DetachTime,
            EmailAtOrganization = x.EmailAtOrganization,
            EnableWhale = x.EnableWhale,
            IsRadialAdmin = x.IsRadialAdmin,
            IsImplementer = x.IsImplementer,
            NumViewedNewFeatures = x.NumViewedNewFeatures,
            IsClient = x.IsClient,
            IsPlaceholder = x.IsPlaceholder,
            UserModelId = x.UserModelId,
            EvalOnly = x.EvalOnly,
            LastSupportCodeReset = x.LastSupportCodeReset,
            JobDescription = x.JobDescription,
            JobDescriptionFromTemplateId = x.JobDescriptionFromTemplateId,
            IsFreeUser = x.IsFreeUser,
            PrimaryWorkspaceWorkspaceId = x.PrimaryWorkspace.WorkspaceId,
            PrimaryWorkspaceType = (int?) x.PrimaryWorkspace.Type,
            // NumRocks = x.NumRocks,
            // NumRoles = x.NumRoles, 
            // NumMeasurables = x.NumMeasurables,
            ManagingOrganization = x.ManagingOrganization,
            ManagerAtOrganization = x.ManagerAtOrganization,
        };

    public static EF.UserOrganizationModel FromLegacyModel(this UserOrganizationModel x) =>
        new EF.UserOrganizationModel {
            ResponsibilityGroupModelId = x.Id,
            AgileUserId = x.AgileUserId,
            AttachTime = x.AttachTime,
            CacheId = x.Cache.Id,
            CreateTime = x.CreateTime,
            CountPerPage = x.CountPerPage,
            ClientOrganizationName = x.ClientOrganizationName,
            DeleteTime = x.DeleteTime,
            DetachTime = x.DetachTime,
            EmailAtOrganization = x.EmailAtOrganization,
            EnableWhale = x.EnableWhale,
            IsRadialAdmin = x.IsRadialAdmin,
            IsImplementer = x.IsImplementer,
            NumViewedNewFeatures = x.NumViewedNewFeatures,
            IsClient = x.IsClient,
            IsPlaceholder = x.IsPlaceholder,
            UserModelId = x.UserModelId,
            EvalOnly = x.EvalOnly,
            LastSupportCodeReset = x.LastSupportCodeReset,
            JobDescription = x.JobDescription,
            JobDescriptionFromTemplateId = x.JobDescriptionFromTemplateId,
            IsFreeUser = x.IsFreeUser,
            PrimaryWorkspaceWorkspaceId = x.PrimaryWorkspace.WorkspaceId,
            PrimaryWorkspaceType = (int?) x.PrimaryWorkspace.Type,
            // NumRocks = x.NumRocks,
            // NumRoles = x.NumRoles, 
            // NumMeasurables = x.NumMeasurables,
            ManagingOrganization = x.ManagingOrganization,
            ManagerAtOrganization = x.ManagerAtOrganization,
        };

    public static readonly Expression<Func<EF.L10recurrenceAttendee, MeetingAttendeeQueryModel>> ToMeetingAttendeeQueryModel = x => 
        new MeetingAttendeeQueryModel
        {
            Id = x.UserId ?? default(long),
            MeetingId = x.L10recurrenceId ?? default(long),
            IsUsingV3 = x.IsUsingV3,
            HasSubmittedVotes = x.HasSubmittedVotes ?? default(bool),
            IsPresent = x.IsPresent ?? default(bool),
            // UserAvatarColor = x.User.UserAvatarColor,
            // DateCreated = x.CreateTime.Value,

        };
        
}
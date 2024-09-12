using Microsoft.Extensions.Logging;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.Core.GraphQL.Types;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.GraphQL.Models
{

  public class UserQueryModel : ILogProperties
  {

    #region Base Properties

    public long Id { get; set; }
    public long AttendeeId { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }

    #endregion

    #region Properties

    public string Avatar { get; set; }
    public string ProfilePictureUrl { get; set; }
    public string OrgAvatarPictureUrl { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }

    public int NumViewedNewFeatures { get; set; }

    public string SupportContactCode { get; set; }

    public gqlUserAvatarColor? UserAvatarColor { get; set; }
    public bool IsOrgAdmin { get; set; }

    public long CurrentOrgId { get; set; }

    public string CurrentOrgName { get; set; }

    public string CurrentOrgAvatar {  get; set; }

    public OrgSettingsModel OrgSettings { get; set; }

    public List<MeetingMetadataModel> CreateIssueMeetings { get; set; }

    public List<MeetingMetadataModel> CreateHeadlineMeetings { get; set; }


    public List<MeetingMetadataModel> CreateTodoMeetings { get; set; }

    public List<MeetingMetadataModel> EditTodoMeetings { get; set; }

    #endregion

    #region Public Methods

    public void Log(ITagCollector collector, string prefix)
    {
      collector.Add($"{prefix}.{nameof(this.Id)}", this.Id);
      collector.Add($"{prefix}.{nameof(this.Avatar)}", this.Avatar);
      collector.Add($"{prefix}.{nameof(this.ProfilePictureUrl)}", this.ProfilePictureUrl);
      collector.Add($"{prefix}.{nameof(this.OrgAvatarPictureUrl)}", this.OrgAvatarPictureUrl);
      collector.Add($"{prefix}.{nameof(this.FirstName)}", this.FirstName);
      collector.Add($"{prefix}.{nameof(this.LastName)}", this.LastName);
      collector.Add($"{prefix}.{nameof(this.FullName)}", this.FullName);
      collector.Add($"{prefix}.{nameof(this.Email)}", this.Email);
      collector.Add($"{prefix}.{nameof(this.NumViewedNewFeatures)}", this.NumViewedNewFeatures);
      collector.Add($"{prefix}.{nameof(this.SupportContactCode)}", this.SupportContactCode);
      collector.Add($"{prefix}.{nameof(this.UserAvatarColor)}", this.UserAvatarColor);
      collector.Add($"{prefix}.{nameof(this.IsOrgAdmin)}", this.IsOrgAdmin);
      collector.Add($"{prefix}.{nameof(this.CurrentOrgId)}", this.CurrentOrgId);
      collector.Add($"{prefix}.{nameof(this.CurrentOrgName)}", this.CurrentOrgName);
      collector.Add($"{prefix}.{nameof(this.CurrentOrgAvatar)}", this.CurrentOrgAvatar);

      this.CreateIssueMeetings.Log(collector, $"{prefix}.{nameof(this.CreateIssueMeetings)}");
      this.CreateHeadlineMeetings.Log(collector, $"{prefix}.{nameof(this.CreateHeadlineMeetings)}");
      this.CreateTodoMeetings.Log(collector, $"{prefix}.{nameof(this.CreateTodoMeetings)}");
      this.EditTodoMeetings.Log(collector, $"{prefix}.{nameof(this.EditTodoMeetings)}");
    }

    public static UserQueryModel FromQuery(string email, IQueryable<UserQueryModel> users)
    {
      UserQueryModel result = new UserQueryModel();

      var userQuery = users.Where(x => x.Email == email);
      if (userQuery.Count() > 0)
      {
        var user = userQuery.First();
        result.Id = user.Id;
        result.FirstName = user.FirstName;
        result.LastName = user.LastName;
        result.FullName = user.FirstName + " " + user.LastName;
        result.Email = user.Email;
        result.OrgSettings = user.OrgSettings;
        result.SupportContactCode = user.SupportContactCode;
      }

      return result;
    }

    public static UserQueryModel FromAttendee(L10Meeting.L10Meeting_Attendee attendee, long businessPlanId)
    {
      bool? enableCoreProcess = attendee.User?.Organization?.Settings?.EnableCoreProcess;

      return new UserQueryModel()
      {
        AttendeeId = attendee.Id,
        Id = attendee.User.Id,
        FirstName = attendee.User.GetFirstName(),
        LastName = attendee.User.GetLastName(),
        OrgSettings = new OrgSettingsModel
        {
          WeekStart = attendee.User?.Organization?.Settings?.WeekStart.ToString(),
          BusinessPlanId = businessPlanId,
          IsCoreProcessEnabled = enableCoreProcess.HasValue ? enableCoreProcess.Value : false,
        }
      };
    }

    #endregion

    #region Subscription Data

    public static class Collections
    {
      public enum Meeting3
      {
        Meetings
      }

      public enum Notification1
      {
        Notifications
      }

      public enum Workspace1
      {
        Workspaces
      }

      public enum MeetingListLookup
      {
        MeetingsListLookup
      }

      public enum MeetingPermissionLookup
      {
        MeetingPermissionLookups
      }
      public enum MetricFormulaLookup
      {
        MetricFormulaLookup
      }

      public enum UserGoal
      {
        Goal
      }

      public enum UserMetric
      {
        Metrics
      }

      public enum UserNodeTodos
      {
        Todos
      }
    }
    public static class Associations
    {
      public enum OrgSettings
      {
        OrgSettings
      }

      public enum Workspaces
      {
        Workspace2
      }

      public enum UserMetrics
      {
        Metrics
      }

      public enum UserGoals
      {
        Goals
      }
      public enum OrgChart
      {
        OrgChart
      }

    }

    #endregion

  }

}
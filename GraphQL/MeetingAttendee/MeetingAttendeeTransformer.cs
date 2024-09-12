using System;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.Utilities;
using RadialReview.Utilities.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RadialReview.Models.L10;
using RadialReview.Models.Rocks;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Angular.Users;
using FluentNHibernate.Conventions;
using L10PageType = RadialReview.Models.L10.L10Recurrence.L10PageType;
using RadialReview.Models.Application;
using RadialReview.Models.L10.VM;
using RadialReview.Core.Models.Scorecard;
using RadialReview.Core.GraphQL.Models.Mutations;
using Humanizer;
using RadialReview.Models.Enums;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Core.GraphQL.Models.Query;
using RadialReview.Utilities.DataTypes;
using RadialReview.Core.GraphQL.Enumerations;
using ModelIssue = RadialReview.Models.Issues.IssueModel;
using static RadialReview.GraphQL.Models.MeetingQueryModel.Collections;

namespace RadialReview.Core.Repositories
{
  public static class MeetingAttendeeTransformer
  {

    public static MeetingAttendeeQueryModel MeetingAttendeeFromUserOrgModel(this RadialReview.Models.UserOrganizationModel attendee, long recurrenceId)
    {
      var meetingAttendee = new MeetingAttendeeQueryModel(recurrenceId)
      {
        Id = attendee.Id,
        Avatar = UserTransformer.TransformUserAvatar(attendee.GetImageUrl()),
        FirstName = attendee.GetFirstName(),
        LastName = attendee.GetLastName(),
        FullName = attendee.GetName(),
        DateCreated = attendee.CreateTime.ToUnixTimeStamp(),
        User = UserTransformer.TransformUser(attendee),
        IsPresent = true,
      };
      meetingAttendee.UserAvatarColor = meetingAttendee.User.UserAvatarColor;
      return meetingAttendee;
    }

    public static MeetingAttendeeQueryModel MeetingAttendeeFromRecurrenceAttendee(this RadialReview.Models.L10.L10Recurrence.L10Recurrence_Attendee attendee, long recurrenceId)
    {
      var meetingAttendee = attendee.User.MeetingAttendeeFromUserOrgModel(recurrenceId);
      meetingAttendee.IsUsingV3 = attendee.IsUsingV3;
      meetingAttendee.HasSubmittedVotes = attendee.HasSubmittedVotes.HasValue ? attendee.HasSubmittedVotes.Value : false;
      meetingAttendee.IsPresent = attendee.IsPresent.HasValue ? attendee.IsPresent.Value : false;
      meetingAttendee.UserAvatarColor = meetingAttendee.User.UserAvatarColor;
      return meetingAttendee;
    }

    public static MeetingAttendeeQueryModel TransformAttendee(this L10Recurrence.L10Recurrence_Attendee attendee, bool isLeader, bool isPresent)
    {
      var user = attendee.User;
      var result = MeetingAttendeeTransformer.MeetingAttendeeFromUserOrgModel(user, attendee.L10Recurrence.Id);
      result.IsUsingV3 = attendee.IsUsingV3;
      result.HasSubmittedVotes = attendee.HasSubmittedVotes.HasValue ? attendee.HasSubmittedVotes.Value : false;
      result.IsPresent = attendee.IsPresent.HasValue ? attendee.IsPresent.Value : false;
      result.IsLeader = isLeader;
      result.UserAvatarColor = result.User.UserAvatarColor;
      return result;
    }

    public static L10Meeting.L10Meeting_Attendee TransformAttendeeEmail(this L10Recurrence.L10Recurrence_Attendee recurrenceAttendee, L10Meeting.L10Meeting_Attendee meetingAttendee)
    {
      if (meetingAttendee.User.User != null)
      {
        meetingAttendee.User.User.IsUsingV3 = recurrenceAttendee.IsUsingV3;
      }

      return meetingAttendee;
    }

    public static MeetingAttendeeQueryModelLookup TransformAttendeeLookup(this L10Recurrence.L10Recurrence_Attendee attendee)
    {
      var user = attendee.User;
      var meetingAttendee = new MeetingAttendeeQueryModelLookup(attendee.L10Recurrence.Id)
      {
        Id = user.Id,
        Avatar = UserTransformer.TransformUserAvatar(user.GetImageUrl()),
        FirstName = user.GetFirstName(),
        LastName = user.GetLastName(),
        FullName = user.GetName(),
        DateCreated = user.CreateTime.ToUnixTimeStamp(),
        User = UserTransformer.TransformUser(user),
      };
      meetingAttendee.UserAvatarColor = meetingAttendee.User.UserAvatarColor;
      return meetingAttendee;
    }

    public static MeetingAttendeeQueryModelLookup TransformTinyUserToMeetingAttendee(this TinyUser attendee)
    {
      var meetingAttendee = new MeetingAttendeeQueryModelLookup(attendee.ModelId)
      {
        Id = attendee.ModelId,
        Avatar = UserTransformer.TransformUserAvatar(attendee.GetImageUrl()),
        FirstName = attendee.FirstName,
        LastName = attendee.LastName,
        FullName = attendee.Name,
        UserAvatarColor = attendee.UserAvatarColor
      };

      return meetingAttendee;
    }
  }
}

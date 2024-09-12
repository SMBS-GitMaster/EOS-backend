using DocumentFormat.OpenXml.Bibliography;
using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Enumerations;
using RadialReview.GraphQL.Models;
using RadialReview.Models;
using RadialReview.Models.Angular.Users;
using System;
using System.Linq;

namespace RadialReview.Core.Repositories
{
  public static class UserTransformer
  {

    public static UserQueryModel TransformUser(this RadialReview.Models.UserOrganizationModel source)
    {
      var userHashCode = UserOrganizationExtensions.GeUserHashCode(source);
      gqlUserAvatarColor userAvatarColor = UserOrganizationExtensions.MapToUserAvatarColor(userHashCode);

      var model = new UserQueryModel
      {
        Avatar = TransformUserAvatar(source.GetImageUrl()),
        ProfilePictureUrl = TransformUserAvatar(source.ImageUrl(true, ImageSize._img)),
        Id = source.Id,//source.User != null ? source.User.Id : string.Empty,
        FirstName = source.GetFirstName(),
        LastName = source.GetLastName(),
        FullName = source.GetName(), // source.GetFirstName() + " " + source.GetLastName(),
        Email = source.GetEmail(), //source.EmailAtOrganization,
        DateCreated = source.CreateTime.ToUnixTimeStamp(),
        NumViewedNewFeatures = source.NumViewedNewFeatures,
        UserAvatarColor = userAvatarColor,
        IsOrgAdmin = source.IsManagingOrganization(),
        CurrentOrgId = source.Organization.Id,
        CurrentOrgName = source.Organization.Name,
        CurrentOrgAvatar = source.Organization.Settings.GetImageUrlV3(),
        OrgAvatarPictureUrl = source.Organization.Settings.GetImageUrlV3(ImageSize._img)
      };

      return model;
    }

    public static UserQueryModel TransformUser(long id, string imageUrl, string firstName, string lastName, string fullName, string email, DateTime createTime, bool? isUsingV3)
    {
      RadialReview.Models.UserModel userModel = new RadialReview.Models.UserModel
      {
        FirstName = firstName,
        LastName = lastName
      };
      var userHashCode = UserOrganizationExtensions.GetUserHashCode(userModel);
      gqlUserAvatarColor userAvatarColor = UserOrganizationExtensions.MapToUserAvatarColor(userHashCode);

      return new UserQueryModel
      {
        Avatar = TransformUserAvatar(imageUrl),
        ProfilePictureUrl = imageUrl != null ? imageUrl.Replace("/64/", "/img/") : null,
        Id = id,//source.User != null ? source.User.Id : string.Empty,
        FirstName = firstName,
        LastName = lastName,
        FullName = fullName, // source.GetFirstName() + " " + source.GetLastName(),
        Email = email, //source.EmailAtOrganization,
        DateCreated = createTime.ToUnixTimeStamp(),
        UserAvatarColor = userAvatarColor,
        //Timezone = timezoneOffset.ToString() //TODO should this be a string or an int? A tz name or a tz offset.
      };
    }

    public static UserQueryModel TransformUser(this AngularUser source)
    {
      return new UserQueryModel
      {
        Avatar = TransformUserAvatar(source.ImageUrl),
        ProfilePictureUrl = source.ImageUrl != null ? source.ImageUrl.Replace("/64/", "/img/") : null,
        Id = source.Id,//source.User != null ? source.User.Id : string.Empty,
        FirstName = source.Name.Split(" ").FirstOrDefault(),
        LastName = string.Join(" ", source.Name.Split(" ").Skip(1)),
        FullName = source.Name, // source.GetFirstName() + " " + source.GetLastName(),
        DateCreated = source.CreateTime.NotNull(x => x.ToUnixTimeStamp()) ?? 0,
        //Email = source., //source.EmailAtOrganization,
        //Timezone = source.GetTimezoneOffset().ToString() //TODO should this be a string or an int? A tz name or a tz offset.
      };
    }

    public static string TransformUserAvatar(string url)
    {
      // Requested by FE
      if (url == "/i/userplaceholder")
        return null;
      return url;
    }

    public static UserQueryModel AddUserAvatarInfo(this UserQueryModel user)
    {
      if (user != null)
      {
        var userModel = new UserOrganizationModel()
        {
          Id = user.Id,
          User = new UserModel()
          {
            FirstName = user.FirstName,
            LastName = user.LastName,
            ImageGuid = user.Avatar
          },
        };

        var userHashCode = UserOrganizationExtensions.GeUserHashCode(userModel);
        gqlUserAvatarColor userAvatarColor = UserOrganizationExtensions.MapToUserAvatarColor(userHashCode);


        user.Avatar = user.Avatar != null ? userModel.GetImageUrl() : null;
        user.UserAvatarColor = userAvatarColor;
      }

      return user;
    }

  }
}
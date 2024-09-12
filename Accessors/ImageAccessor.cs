using Imageflow.Fluent;
using Microsoft.AspNetCore.Http;
using RadialReview.Exceptions;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Core.Properties;
using RadialReview.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Utilities.Hooks;

namespace RadialReview.Accessors {
  public class ImageAccessor : BaseAccessor {
    public static String GetImagePath(UserModel caller, String imageId) {
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          if (imageId == null)
            return ConstantStrings.AmazonS3Location + ConstantStrings.ImagePlaceholder;
          try {
            return ConstantStrings.AmazonS3Location + s.Get<ImageModel>(Guid.Parse(imageId)).Url;
          } catch (Exception e) {
            return "";
          }
        }
      }
    }
    public static string HUGE_INSTRUCTIONS = "width=2048&height=2048&format=png&mode=max";

    public static string BIG_INSTRUCTIONS = "width=256&height=256&format=png&mode=max";
    public static string TINY_INSTRUCTIONS = "width=32&height=32&format=png&mode=max";
    public static string MED_INSTRUCTIONS = "width=64&height=64&format=png&mode=max";
    public static string LARGE_INSTRUCTIONS = "width=128&height=128&format=png&mode=max";

    public static async Task Upload(IBlobStorageProvider bsp, Stream stream, string path, string instructions) {
      using (var ms = new MemoryStream()) {
        stream.Seek(0, SeekOrigin.Begin);
        using (var i = new ImageJob()) {
          var r = await i.BuildCommandString(new StreamSource(stream, false), new StreamDestination(ms, false), instructions).Finish().InProcessAsync();

          ms.Seek(0, SeekOrigin.Begin);
          var repoSettings = Config.GetBucketSettings(BucketName.ImageRepoBucket);
          await bsp.UploadFile(repoSettings.BucketCredentials, path, ms, true);



        }
      }
    }

    public static bool RemoveImage(UserModel caller, string userId) {

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {

          if (caller.Id != userId)
            throw new PermissionsException("Cannot remove image");

          var user = s.Get<UserModel>(userId);
          user.ImageGuid = null;
          s.Save(user);
          tx.Commit();
          s.Flush();
          return true;
        }
      }
    }


    public static async Task<ImageModel> UploadProfileImageForUser(UserOrganizationModel caller, IBlobStorageProvider bsp, long forUserId, string filename, Stream inputStream, bool huge = false) {

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var perms = PermissionsUtility.Create(s, caller);
          perms.EditUserDetails(forUserId);
        }
      }

      var img = await RawUploadImage(caller.User, bsp, filename, inputStream, UploadType.ProfileImage, huge);

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var userOrg = s.Get<UserOrganizationModel>(forUserId);

          if (userOrg.TempUser != null) {
            userOrg.TempUser.ImageGuid = img.Id.ToString();
          } else {
            var user = userOrg.User;
            user.ImageGuid = img.Id.ToString();

            if (user.UserOrganization != null && user.UserOrganization.Any()) {
              foreach (var u in user.UserOrganization) {
                u.UpdateCache(s);
              }
            }

          }

          tx.Commit();
          s.Flush();

        }
      }
      return img;
    }

    private static async Task<ImageModel> RawUploadImage(UserModel uploader, IBlobStorageProvider bsp, string filename, Stream inputStream, UploadType type, bool huge = false) {
      var img = new ImageModel() {
        OriginalName = Path.GetFileName(filename),
        UploadedBy = uploader,
        UploadType = type
      };
      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          s.Save(img);
          tx.Commit();
          s.Flush();
        }
      }
      var guid = img.Id.ToString();
      var path = "img/" + guid + ".png";
      var pathTiny = "32/" + guid + ".png";
      var pathMed = "64/" + guid + ".png";
      var pathLarge = "128/" + guid + ".png";
      var pathHuge = "2048/" + guid + ".png";

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          var sBig = new MemoryStream();
          var sTiny = new MemoryStream();
          var sMed = new MemoryStream();
          var sLarge = new MemoryStream();
          var sHuge = new MemoryStream();
          inputStream.Seek(0, SeekOrigin.Begin);
          await inputStream.CopyToAsync(sBig);
          inputStream.Seek(0, SeekOrigin.Begin);
          await inputStream.CopyToAsync(sTiny);
          inputStream.Seek(0, SeekOrigin.Begin);
          await inputStream.CopyToAsync(sMed);
          inputStream.Seek(0, SeekOrigin.Begin);
          await inputStream.CopyToAsync(sLarge);
          inputStream.Seek(0, SeekOrigin.Begin);

          if (huge) {
            await inputStream.CopyToAsync(sHuge);
            inputStream.Seek(0, SeekOrigin.Begin);
          }
          await Upload(bsp, sBig, path, BIG_INSTRUCTIONS);
          await Upload(bsp, sTiny, pathTiny, TINY_INSTRUCTIONS);
          await Upload(bsp, sMed, pathMed, MED_INSTRUCTIONS);
          await Upload(bsp, sLarge, pathLarge, LARGE_INSTRUCTIONS);

          if (huge) {
            await Upload(bsp, sHuge, pathHuge, HUGE_INSTRUCTIONS);
          }

          img.Url = path;

          if (huge) {
            img.Url = pathHuge;
          }

          s.Update(img);
        }
      }
      return img;
    }

    public static async Task<String> UploadImage(UserModel user, UserOrganizationModel caller, IBlobStorageProvider bsp, string filename, Stream inputStream, UploadType type, bool huge = false) {
      var img = await RawUploadImage(user, bsp, filename, inputStream, type, huge);

      using (var s = HibernateSession.GetCurrentSession()) {
        using (var tx = s.BeginTransaction()) {
          switch (type) {
            case UploadType.ProfileImage: {
                user = s.Get<UserModel>(user.Id);
                var old = user.ImageGuid;
                user.ImageGuid = img.Id.ToString();
                s.Update(user);
                if (user.UserOrganization != null && user.UserOrganization.Any()) {
                  foreach (var u in user.UserOrganization) {
                    u.UpdateCache(s);
                  }
                }
              };
              break;
            case UploadType.AppImage:
              break;
            case UploadType.Logo:
              PermissionsUtility.Create(s, caller).ManagingOrganization(caller.Organization.Id);
              var org = s.Get<OrganizationModel>(caller.Organization.Id);
              org._Settings.ImageGuid = img.Id.ToString();
              s.Update(org);
              // trigger event UpdateOrganization
              var updates = new IOrganizationHookUpdates();
              var orgId = caller.Organization.Id;
              await HooksRegistry.Each<IOrganizationHook>((ses, x) => x.UpdateOrganization(ses, orgId, updates, caller));
              break;
            default:
              throw new PermissionsException();
          }
         

          tx.Commit();
          s.Flush();
        }
      }
      return ConstantStrings.AmazonS3Location + img.Url;
    }

    public static async Task<String> UploadImage(UserModel user, UserOrganizationModel caller, IBlobStorageProvider bsp, IFormFile file, UploadType uploadType) {
      var filename = file.FileName;
      var inputStream = file.OpenReadStream();
      return await UploadImage(user, caller, bsp, filename, inputStream, uploadType);
    }

  }
}

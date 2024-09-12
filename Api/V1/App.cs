using Microsoft.AspNetCore.Mvc;
using RadialReview.Accessors;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Api.V1 {
	[Route("api/v1")]
	[ApiExplorerSettings(IgnoreApi = true)]
	public class AppController : BaseApiController {

		private IBlobStorageProvider _blobStorage;
		public AppController(IBlobStorageProvider blobStorage, RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
			_blobStorage = blobStorage;

		}

		// GET: api/Scores/5
		[Route("app/roles")]
		[HttpGet]
		public List<AngularUserRole> Roles() {
			var res = GetUser().NotNull(x => x.User.UserOrganization.Select(y => new AngularUserRole(y.Id, x.GetName(), ""/*y.GetTitles()*/, y.Organization.GetName())).ToList());
			return res ?? new List<AngularUserRole>();
		}

		public class UploadImageResult {
			public string Name { get; set; }

			public string Url { get; set; }

			public bool Success { get; set; }
		}

		// GET: api/Scores/5
		[Route("app/uploadImage")]
		[HttpPost]
		public async Task<List<UploadImageResult>> Upload() {
			if (Request.Form.Files.Any()) {
				// Get the uploaded image from the Files collection
				var o = new List<UploadImageResult>();
				var files = Request.Form.Files;
				var ia = new ImageAccessor();
				foreach (var f in files) {
					bool success = true;
					string url = null;
					string filename = null;
					try {
						filename = f.FileName;
						url = await ImageAccessor.UploadImage(GetUser().User, null, _blobStorage, filename, f.OpenReadStream(), UploadType.AppImage, true);
					} catch (Exception) {
						success = false;
					}
					o.Add(new UploadImageResult() { Name = filename, Url = url, Success = success, });
				}

				return o;
			}

			throw new Exception("Image is not uploaded");
		}

		[Route("app/uploadProfilePicture")]
		[HttpPost]
		public async Task<UploadImageResult> UploadProfilePicture() {
			var userModel = GetUser().User;
			var file = Request.Form.Files.FirstOrDefault();
			if (file == null)
				throw new Exception("No file");
			//you can put your existing save code here
			if (file != null && file.Length > 0) {
				// extract only the filename
				var url = await ImageAccessor.UploadImage(userModel, null, _blobStorage, file.FileName, file.OpenReadStream(), UploadType.ProfileImage, true);
				return new UploadImageResult() { Name = file.FileName, Success = true, Url = url };
			}
			throw new Exception("Image is not uploaded");
		}

		//[Route("app/roles/{ROLE_ID}")]
		//[HttpPost]
		//public bool SetRole(long ROLE_ID) {
		//    UserOrganizationModel userOrg = null;
		//    try {
		//        userOrg = GetUser();
		//    } catch (Exception) {
		//    }
		//    new UserAccessor().ChangeRole(GetUserModel(), userOrg, ROLE_ID);
		//    GetUser(ROLE_ID);
		//    return true;
		//}
	}
}
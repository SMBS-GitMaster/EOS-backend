using Microsoft.AspNetCore.Http;
using RadialReview.Exceptions;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Downloads;
using RadialReview.Models.Enums;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public enum FileType {
		Invalid,
		CSV,
		Lines,
		XLS,
		XLSX,
	}
	public class UploadInfo {
		public UploadInfo(long uploadMetaDataId, long encryptedFileId, byte[] contents) {
			UploadMetaDataId = uploadMetaDataId;
			EncryptedFileId = encryptedFileId;
			Contents = contents;
		}

		public long UploadMetaDataId { get; set; }
		public long EncryptedFileId { get; set; }
		public byte[] Contents { get; set; }

		public List<List<string>> Csv { get; set; }
		public List<string> Lines { get; set; }
		public DiscreteDistribution<FileType> FileType { get; set; }

		public FileType GetLikelyFileType() {
			return FileType.ResolveOne();
		}
	}
	public class UploadAccessor {
		public static async Task<UploadInfo> UploadFile(UserOrganizationModel caller, IBlobStorageProvider bsp, UploadType type, IFormFile file, ForModel forModel = null) {
			return await UploadFile(caller, bsp, type, file.ContentType, file.FileName, file.OpenReadStream(), forModel);
		}

		public static async Task<UploadInfo> UploadFile(UserOrganizationModel caller, IBlobStorageProvider bsp, UploadType type, String contentType, String originalName, Stream stream, ForModel forModel = null) {
			UploadMetadataModel upload = null;
			long fileId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					stream.Seek(0, SeekOrigin.Begin);
					PermissionsUtility.Create(s, caller).CanUpload();
					upload = new UploadMetadataModel() {
						ForModel = forModel,
						MimeType = contentType,
						OrganizationId = caller.Organization.Id,
						OriginalName = originalName,
						CreatedBy = caller.Id,
						UploadType = type,
					};
					fileId = FileAccessor.SaveGeneratedFilePlaceholder_Unsafe(
						s, caller.Id, upload.Identifier, MimeTypeMap.GetExtension(contentType), "Upload:" + type,
						FileOrigin.Uploaded, FileOutputMethod.Save, forModel, null,
						TagModel.Create("upload", forModel), TagModel.Create(TagModel.Constants.UPLOADER + "-" + type), TagModel.Create(TagModel.Constants.UPLOADER)
					);


					upload.EncryptedFileId = fileId;

					s.Save(upload);

					tx.Commit();
					s.Flush();
				}
			}

			using (var ms = new MemoryStream()) {
				await stream.CopyToAsync(ms);
				ms.Position = 0;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						await FileAccessor.Save_Unsafe(s, bsp, fileId, ms, FileNotification.DoNotNotify());
						tx.Commit();
						s.Flush();
					}
				}
				ms.Position = 0;

				return new UploadInfo(upload.Id, fileId, ms.ToArray());
			}




		}


		private static Dictionary<string, string> Backup = new Dictionary<string, string>();

		public static async Task<UploadInfo> UploadAndParseExcel(UserOrganizationModel caller, IBlobStorageProvider bsp, UploadType type, IFormFile file, ForModel forModel) {
			if (file == null)
				throw new FileNotFoundException("File was not found.");
			if (file.Length == 0)
				throw new FileNotFoundException("File was empty.");

			using (var ms = new MemoryStream()) {
				await file.OpenReadStream().CopyToAsync(ms);
				return await UploadAccessor.UploadFile(caller, bsp, type, file, forModel);
			}

		}

		public static async Task<UploadInfo> UploadAndParse(UserOrganizationModel caller, IBlobStorageProvider bsp, UploadType type, IFormFile file, ForModel forModel) {
			if (file == null)
				throw new FileNotFoundException("File was not found.");
			if (file.Length == 0)
				throw new FileNotFoundException("File was empty.");

			using (var ms = new MemoryStream()) {
				await file.OpenReadStream().CopyToAsync(ms);

				if (file.ContentType.NotNull(x => x.ToLower().Contains("application/vnd.ms-excel")) && (file.FileName.NotNull(x => x.ToLower()) ?? "").EndsWith(".xls")) {
					throw new FileTypeException(FileType.XLS);
				}

				if (file.ContentType.NotNull(x => x.ToLower().Contains("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")) && (file.FileName.NotNull(x => x.ToLower()) ?? "").EndsWith(".xlsx")) {
					throw new FileTypeException(FileType.XLSX);
				}

				var o = await UploadAccessor.UploadFile(caller, bsp, type, file, forModel);

				await Parse(ms, o);
				if (file.ContentType.NotNull(x => x.ToLower().Contains("csv"))) {
					o.FileType.Add(FileType.CSV, 2);
				}
				if (file.FileName.NotNull(x => x.ToLower().Contains(".csv"))) {
					o.FileType.Add(FileType.CSV, 1);
				}
				if (file.FileName.NotNull(x => x.ToLower().Contains(".txt"))) {
					o.FileType.Add(FileType.Lines, 1);
				}
				return o;
			}

		}


		public static async Task<UploadInfo> DownloadAndParse(UserOrganizationModel caller, IBlobStorageProvider bsp, long uploadMetaId) {
			long fileId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var umd = s.Get<UploadMetadataModel>(uploadMetaId);
					perms.Self(umd.CreatedBy); //Permissions also checked by file accessor
					fileId = umd.EncryptedFileId.Value;
				}
			}

			var data = await FileAccessor.GetFileData(caller, bsp, fileId);
			var ui = new UploadInfo(uploadMetaId, fileId, data);
			await Parse(new MemoryStream(data), ui);
			return ui;
		}
		private static async Task Parse(MemoryStream ms, UploadInfo ui) {
			ms.Seek(0, SeekOrigin.Begin);
			var text = await ms.ReadToEndAsync();
			ui.Lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
			ui.Csv = CsvUtility.Load(text.ToStream());

			var dist = new DiscreteDistribution<FileType>(0, 2, true);

			foreach (var l in ui.Lines) {
				if (l.Split(',').Count() > 1) {
					dist.Add(FileType.CSV, 1);
				} else {
					dist.Add(FileType.Lines, 1);
				}
			}

			ui.FileType = dist;
		}

		public static async Task<string> UploadUndocumented(UserOrganizationModel caller, IBlobStorageProvider bsp, IFormFile file, string folder, BucketName bucket = BucketName.RadialBucket) {
			var stream = file.OpenReadStream();
			var contentType = file.ContentType;
			var guid = Guid.NewGuid().ToString();
			var fileName = file.FileName;
			var endingSplit = fileName.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			var ending = "";
			if (endingSplit.Length > 1) {
				ending = "." + endingSplit.Last();
			}

			var path = "";
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					stream.Seek(0, SeekOrigin.Begin);
					PermissionsUtility.Create(s, caller).CanUpload();
					path = Path.Combine(folder, guid + ending);

					var creds = Config.GetBucketSettings(bucket).BucketCredentials;

					return (await bsp.UploadFile(creds, path, stream, true)).ToString();



				}
			}
		}
	}
}

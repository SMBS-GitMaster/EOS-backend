using Amazon.S3;
using Hangfire;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Hangfire.Activator;
using RadialReview.Hubs;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Models;
using RadialReview.Models.Downloads;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Accessors {

	public class FileNotification {
		public bool Notify { get; set; }
		public string ConnectionId { get; set; }

		[Obsolete("do not use")]
		public FileNotification() {
		}

		private FileNotification(bool notify, string connectionId) {
			Notify = notify;
			ConnectionId = connectionId;
		}

		public static FileNotification DoNotNotify() {
			return new FileNotification(false, null);
		}

		public static FileNotification NotifyCaller() {
			return new FileNotification(true, null);
		}
		public static FileNotification NotifyCaller(string connectionId) {
			return new FileNotification(true, connectionId);
		}
		public static FileNotification NotifyCaller(UserOrganizationModel caller) {
			return new FileNotification(true, caller._ConnectionId);
		}
	}


	public class FileAccessor {


		#region Getters
		public class TinyFile {
			public long Id { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public long Size { get; set; }
			public DateTime CreateTime { get; set; }
		}
		public static List<TinyFile> GetVisibleFiles(UserOrganizationModel caller, long forUserId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(forUserId);
					var filePerms = perms.GetAllPermItemsForUser(PermItem.ResourceType.File, forUserId);

					var files = s.QueryOver<EncryptedFileModel>().Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(x => x.Id)
						.IsIn(filePerms.Select(x => x.ResId).ToArray()).List();

					return files.Select(x => new TinyFile() {
						Id = x.Id,
						CreateTime = x.CreateTime,
						Description = x.FileDescription,
						Name = x.FileName,
						Size = x.Size,
					}).ToList();
				}
			}
		}

		public static async Task<string> GetFileUrl(UserOrganizationModel caller, IBlobStorageProvider bsp, long fileId, bool inline) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanViewFile(fileId);
					var file = s.Get<EncryptedFileModel>(fileId);
					if (file.Generating) {
						throw new StillGeneratingException("File not generated yet.", TimeSpan.FromSeconds(5));
					}

					return await GeneratePreSignedURL_Unsafe(bsp, file.FilePath, file.FileName, file.FileType, inline);
				}
			}
		}

		public static async Task<byte[]> GetFileData(UserOrganizationModel caller, IBlobStorageProvider bsp, long fileId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.CanViewFile(fileId);
					var file = s.Get<EncryptedFileModel>(fileId);
					if (file.Generating) {
						throw new StillGeneratingException("File not generated yet.", TimeSpan.FromSeconds(5));
					}
					var bucket = Config.GetBucketSettings(BucketName.DocumentRepoBucket).BucketCredentials;
					var key = GenerateFileKey(file.FilePath);
					return await bsp.GetFileData(bucket, key);
				}
			}
		}


		public static async Task<Dictionary<long, string>> GetFileUrls(UserOrganizationModel caller, IBlobStorageProvider bsp, IEnumerable<long> fileIds, bool inline, CancellationToken? token) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					//All permissions
					foreach (var fid in fileIds) {
						perms.CanViewFile(fid);
						if (token != null) {
							token.Value.ThrowIfCancellationRequested();
						}
					}
					//All file links
					var models = s.QueryOver<EncryptedFileModel>().WhereRestrictionOn(x => x.Id).IsIn(fileIds.ToArray()).List().ToList();

					//Build ouptut
					var output = new Dictionary<long, string>();
					foreach (var m in models) {
						output[m.Id] = await GeneratePreSignedURL_Unsafe(bsp, m.FilePath, m.FileName, m.FileType, inline);
						if (token != null) {
							token.Value.ThrowIfCancellationRequested();
						}
					}
					return output;
				}
			}
		}
		#endregion

		#region Local only
		public class LocalFile {
			public string FileType { get; set; }
			public byte[] Bytes { get; set; }
			public string FileName { get; set; }
		}

		private static Dictionary<string, LocalFile> _LocalUploads = new Dictionary<string, LocalFile>();
		public static LocalFile GetLocalFile(string fileKey) {
			if (Config.UploadFiles()) {
				throw new Exception("Excepted local access (1)");
			}
			if (!Config.IsLocal()) {
				throw new Exception("Excepted local access (2)");
			}
			if (!_LocalUploads.ContainsKey(fileKey)) {
				throw new Exception("File not found. Local storage is ephemeral and deleted after server restarts");
			}
			return _LocalUploads[fileKey];

		}
		#endregion

		#region Save
		public static long SaveGeneratedFilePlaceholder_Unsafe(ISession s, long creatorId, string name, string type, string description, FileOrigin fileOrigin, FileOutputMethod outputMethod, IForModel forModel, PermTiny[] additionalPermissions, params TagModel[] tags) {
			return SaveFilePlaceholder_Unsafe(s, creatorId, name, type, description, fileOrigin, outputMethod, forModel, additionalPermissions, tags).Id;
		}

		public static async Task Save_Unsafe(ISession s, IBlobStorageProvider bp, long fileId, Stream stream, FileNotification notifyRecipient) {
			var file = s.Get<EncryptedFileModel>(fileId);
			var filePath = file.FilePath;

			if (file.Size != 0) {
				throw new Exception("File already exists");
			}
			if (string.IsNullOrWhiteSpace(filePath)) {
				throw new Exception("File path was empty");
			}
			if (filePath.Length < 10) {
				throw new Exception("File path was too short");
			}
			long size;
			using (var ms = new MemoryStream()) {
				if (stream.Position > 0)
					stream.Position = 0;
				await stream.CopyToAsync(ms);
				stream.Seek(0, SeekOrigin.Begin);
				ms.Seek(0, SeekOrigin.Begin);
				var fileKey = GenerateFileKey(filePath);

				if (Config.UploadFiles()) {
					var creds = Config.GetBucketSettings(BucketName.DocumentRepoBucket).BucketCredentials;
					await bp.UploadFile(creds, fileKey, ms, false);
					size = stream.Length;
				} else {
					_LocalUploads[fileKey] = new LocalFile() {
						FileType = file.FileType,
						Bytes = ms.ToArray(),
						FileName = file.FileName
					};
					size = ms.Length;
				}
			}
			await CompletedUpload_Unsafe(s, fileId, size, notifyRecipient, file.FileOutputMethod);
		}

		public static async Task<long> Save_Unsafe(IBlobStorageProvider bp, long creatorId, Stream file, string name, string type, string description, FileOrigin origin, FileOutputMethod outputMethod, IForModel forModel, FileNotification notify, PermTiny[] additionalPermissions, params TagModel[] tags) {
			EncryptedFileModel fileObj;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					fileObj = SaveFilePlaceholder_Unsafe(s, creatorId, name, type, description, origin, outputMethod, forModel, additionalPermissions, tags);
					tx.Commit();
					s.Flush();
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await Save_Unsafe(s, bp, fileObj.Id, file, notify);
					tx.Commit();
					s.Flush();
				}
			}
			return fileObj.Id;
		}

		#endregion

		#region Intermediate Steps
		public class FileCompleteModel {
			public FileCompleteModel(long id, string fileName, FileOutputMethod outputMethod) {
				Id = id;
				FileName = fileName;
				OutputMethod = "" + outputMethod;
			}
			public long Id { get; set; }
			public string FileName { get; set; }
			public string OutputMethod { get; set; }
			public string Error { get; set; }

		}
		private static async Task CompletedUpload_Unsafe(ISession s, long fileId, long size, FileNotification notify, FileOutputMethod outputMethod) {
			var f = s.Get<EncryptedFileModel>(fileId);
			f.Complete = true;
			f.Generating = false;
			f.Size = size;
			s.Update(f);

			notify = notify ?? FileNotification.DoNotNotify();
			if (notify.Notify) {
				try {

					await using (var rt = RealTimeUtility.Create()) {
						if (notify.ConnectionId == null) {
							rt.UpdateGroup(RealTimeHub.Keys.UserId(f.CreatorId)).Call("fileCompleted", new FileCompleteModel(f.Id, f.FileName, outputMethod));
						} else {
							rt.UpdateConnection(notify.ConnectionId).Call("fileCompleted", new FileCompleteModel(f.Id, f.FileName, outputMethod));
						}
					}
				} catch (Exception) {
				}
			}
		}

		[Queue(HangfireQueues.Immediate.TEMPORARY_FILES)]/*Queues must be lowecase alphanumeric. You must add queues to BackgroundJobServerOptions in Startup.auth.cs*/
		[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]

		public async static Task ClearTemporaryFiles(DateTime today, [ActivateParameter] IBlobStorageProvider bsp) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var files = s.QueryOver<EncryptedFileModel>().Where(x =>
							x.FileOrigin == FileOrigin.TemporaryAutoGenerated &&
							x.DeleteTime == null &&
							x.CreateTime < today - TimeSpan.FromDays(1)
						).List().ToList();

					foreach (var a in files) {
						await DeleteFile_Unsafe(bsp, a.FilePath);
						a.DeleteTime = DateTime.UtcNow;
					}

					tx.Commit();
					s.Flush();
				}
			}
		}


		private static async Task DeleteFile_Unsafe(IBlobStorageProvider bsp, string filePath) {
			try {
				if (string.IsNullOrWhiteSpace(filePath))
					return;

				if (Config.UploadFiles()) {
					var creds = Config.GetBucketSettings(BucketName.DocumentRepoBucket).BucketCredentials;
					await bsp.DeleteFile(creds, GenerateFileKey(filePath));
				}

			} catch (Exception e) {
				int a = 0;
			}
		}

		private static EncryptedFileModel SaveFilePlaceholder_Unsafe(ISession s, long creatorId, string name, string type, string description, FileOrigin fileOrigin, FileOutputMethod outputMethod, IForModel forModel, PermTiny[] additionalPermissions, params TagModel[] tags) {
			string filePath;
			var creator = s.Get<UserOrganizationModel>(creatorId);
			var fileObj = new EncryptedFileModel() {
				CreatorId = creatorId,
				EncryptionType = EncryptionType.None,
				FileDescription = description,
				FileName = name,
				FileType = type,
				FileOrigin = fileOrigin,
				FileOutputMethod = outputMethod,
				Generating = fileOrigin.IsGenerated(),
				OrgId = creator.Organization.Id,
				ParentModel = forModel?.ToImpl() ?? null,
			};
			fileObj.FilePath += type.NotNull(x => "." + x.Trim('.')) ?? "";
			filePath = fileObj.FilePath;
			s.Save(fileObj);

			if (tags != null) {
				foreach (var tag in tags) {
					s.Save(new EncryptedFileTagModel() {
						FileId = fileObj.Id,
						Tag = tag.Tag,
						ForModel = tag.ForModel,
						CreateTime = fileObj.CreateTime
					});
				}
			}


			//Permissions
			var additionalPermissionsList = (additionalPermissions ?? new PermTiny[0]).ToList();
			additionalPermissionsList.Add(PermTiny.Creator());
			PermissionsAccessor.InitializePermItems_Unsafe(s, creator, PermItem.ResourceType.File, fileObj.Id, additionalPermissionsList.ToArray());

			return fileObj;
		}
		#endregion

		#region Utilities
		private static string GenerateFileKey(string filePath) {
			var settings = Config.GetBucketSettings(BucketName.DocumentRepoBucket);
			return settings.Path + "/" + filePath;
		}



		private static async Task<string> GeneratePreSignedURL_Unsafe(IBlobStorageProvider bsp, string filePath, string filename, string type, bool inline) {
			if (!Config.UploadFiles()) {
				return "/Export/LocalFile?id=" + GenerateFileKey(filePath);
			}
			var settings = Config.GetBucketSettings(BucketName.DocumentRepoBucket);

			return await bsp.GeneratePreSignedUrl(
				settings.BucketCredentials,
				GenerateFileKey(filePath),
				new PreSignedUrlSettings() {
					Inline = inline,
					FileType = type,
					DisplayFileName = filename
				});


		}
		#endregion



	}
}

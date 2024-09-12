using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.BlobStorageProvider {



	public class S3BlobProvider : IBlobStorageProvider {
		private readonly ILogger _logger;
		private Dictionary<BucketCredentials, IAmazonS3> _s3Clients;

		public S3BlobProvider(ILogger<S3BlobProvider> logger) {
			_logger = logger;
			_s3Clients = new Dictionary<BucketCredentials, IAmazonS3>();
		}


		public async Task DeleteFile([NotNull] BucketCredentials bucketCredentials, [NotNull] string fileKey) {
			if (bucketCredentials == null)
				throw new ArgumentNullException(nameof(bucketCredentials));
			var _s3Client = GetS3Client(bucketCredentials);
			if (_s3Client == null)
				return;
			if (string.IsNullOrEmpty(fileKey))
				throw new ArgumentNullException(nameof(fileKey));

			var request = new DeleteObjectRequest {
				BucketName = bucketCredentials.BucketName,
				Key = fileKey
			};
			await _s3Client.DeleteObjectAsync(request);
		}

		public async Task<string> GeneratePreSignedUrl([NotNull] BucketCredentials bucketCredentials, [NotNull] string fileKey, [NotNull] PreSignedUrlSettings settings) {
			if (bucketCredentials == null)
				throw new ArgumentNullException(nameof(bucketCredentials));
			var _s3Client = GetS3Client(bucketCredentials);
			if (_s3Client == null)
				return null;

			if (string.IsNullOrEmpty(fileKey))
				throw new ArgumentNullException(nameof(fileKey));
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));


			var request1 = new GetPreSignedUrlRequest {
				BucketName = bucketCredentials.BucketName,
				Key = fileKey,
				Expires = DateTime.UtcNow.AddMinutes(5),
			};
			request1.ResponseHeaderOverrides.ContentDisposition = settings.GetContentDisposition();
			return _s3Client.GetPreSignedURL(request1);

		}

		public async Task<byte[]> GetFileData([NotNull] BucketCredentials bucketCredentials, [NotNull] string fileKey) {
			try {
				if (bucketCredentials == null)
					throw new ArgumentNullException(nameof(bucketCredentials));
				var _s3Client = GetS3Client(bucketCredentials);
				if (_s3Client == null)
					return null;
				if (string.IsNullOrEmpty(fileKey))
					throw new ArgumentNullException(nameof(fileKey));

				GetObjectRequest request = new GetObjectRequest {
					BucketName = bucketCredentials.BucketName,
					Key = fileKey
				};
				using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
				using (Stream responseStream = response.ResponseStream)
				using (var reader = new MemoryStream()) {
					await responseStream.CopyToAsync(reader);
					return reader.ToArray();
				}
			} catch {
				return null;
			}
		}

		public async Task<string> GetStringData([NotNull] BucketCredentials bucketCredentials, [NotNull] string fileKey) {
			try {
				if (bucketCredentials == null)
					throw new ArgumentNullException(nameof(bucketCredentials));
				var _s3Client = GetS3Client(bucketCredentials);
				if (_s3Client == null)
					return null;
				if (string.IsNullOrEmpty(fileKey))
					throw new ArgumentNullException(nameof(fileKey));

				GetObjectRequest request = new GetObjectRequest {
					BucketName = bucketCredentials.BucketName,
					Key = fileKey
				};
				using (GetObjectResponse response = await _s3Client.GetObjectAsync(request))
				using (Stream responseStream = response.ResponseStream)
				using (StreamReader reader = new StreamReader(responseStream)) {
					return reader.ReadToEnd();
				}
			} catch {
				return null;
			}
		}

		public async Task<string> StoreStringData([NotNull] BucketCredentials bucketCredentials, [NotNull] string fileKey, string data) {

			try {
				if (bucketCredentials == null)
					throw new ArgumentNullException(nameof(bucketCredentials));
				var _s3Client = GetS3Client(bucketCredentials);
				if (_s3Client == null)
					return null;
				if (string.IsNullOrEmpty(fileKey))
					throw new ArgumentNullException(nameof(fileKey));

				var request = new PutObjectRequest {
					BucketName = bucketCredentials.BucketName,
					ContentBody = data,
					Key = fileKey
				};
				await _s3Client.PutObjectAsync(request);
				return fileKey;
			} catch (Exception ex) {
				_logger.LogError(ex, $"BlobProvider.StoreStringData: Exception storing string {data}");
				throw;
			}
		}

		public async Task<Uri> UploadFile([NotNull] BucketCredentials bucketCredentials, [NotNull] string fileKey, Stream fileContent, bool makePublic) {
			if (bucketCredentials == null)
				throw new ArgumentNullException(nameof(bucketCredentials));
			var _s3Client = GetS3Client(bucketCredentials);
			if (_s3Client == null)
				return null;
			if (string.IsNullOrWhiteSpace(fileKey))
				throw new ArgumentNullException(nameof(fileKey));
			if (fileContent == null)
				throw new ArgumentNullException(nameof(fileContent));

			try {
				var fileTransferUtility = new TransferUtility(_s3Client);
				var fileTransferUtilityRequest = new TransferUtilityUploadRequest {
					BucketName = bucketCredentials.BucketName,
					InputStream = fileContent,
					StorageClass = S3StorageClass.Standard,
					Key = fileKey,
					CannedACL = makePublic ? S3CannedACL.PublicRead : S3CannedACL.Private,
				};

				//cache 7 days
				fileTransferUtilityRequest.Headers.CacheControl = "public, max-age=604800";

				await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
				var url = string.Format(bucketCredentials.PathTemplate, bucketCredentials.BucketName, fileKey);
				return new Uri(url);
			} catch (Exception ex) {
				_logger.LogError(ex, $"BlobProvider.UploadFile: Exception uploading file {fileKey}");
				throw;
			}
		}


		private IAmazonS3 GetS3Client([NotNull] BucketCredentials credentials) {
			if (credentials == null)
				throw new ArgumentNullException(nameof(credentials));

			if (!_s3Clients.ContainsKey(credentials)) {
				_s3Clients[credentials] = ConstructClient(credentials);
			}
			return _s3Clients[credentials];
		}

		private IAmazonS3 ConstructClient(BucketCredentials credentials) {

			var bucketRegion = credentials.BucketRegion;
			if (string.IsNullOrEmpty(bucketRegion)) {
				_logger.LogWarning("Bucket region is not configured.  S3 transport will not be executed");
				return null;
			}
			var region = RegionEndpoint.GetBySystemName(bucketRegion);

			var awsAccessKeyId = credentials.AccessKeyId;
			if (!string.IsNullOrEmpty(awsAccessKeyId)) {
				var awsSecretAccessKey = credentials.SecretAccessKey;
				_logger.LogWarning($"AWSBlobProvider employing access key id={awsAccessKeyId[0..7]}... secret={awsSecretAccessKey[0..4]}...");
				if (!string.IsNullOrEmpty(awsSecretAccessKey)) {
					var creds = new BasicAWSCredentials(awsAccessKeyId, awsSecretAccessKey);
					return new AmazonS3Client(creds, region);
				}
			}

			return new AmazonS3Client(region);
		}
	}
}

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.BlobStorageProvider {


	public class PreSignedUrlSettings {
		public bool Inline { get; set; }
		public string DisplayFileName { get; set; }
		public string FileType { get; set; }

		public string GetContentDisposition() {
			var attachementOrInline = Inline ? "inline" : "attachment";
			var guid = Guid.NewGuid().ToString();
			return attachementOrInline + "; filename=" + CleanFileName(AppendType(DisplayFileName ?? guid, FileType) ?? guid);
		}

		private static string AppendType(string path, string type) {
			if (path == null) { return null; }
			return path + type.NotNull(x => "." + x.Trim('.')) ?? "";
		}

		private static string CleanFileName(string fileName) {
			var invalidCharacters = Path.GetInvalidFileNameChars().ToList();
			invalidCharacters.Add(',');
			return invalidCharacters.Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
		}
	}


	public class BucketCredentials {
		public string BucketName { get; set; }
		public string AccessKeyId { get; set; }
		public string BucketRegion { get; set; }
		public string SecretAccessKey { get; set; }
		public string PathTemplate { get; set; }
		public BucketCredentials() {
			PathTemplate = "https://{0}.s3.amazonaws.com/{1}";
		}

		private object CreateTuple() {
			return Tuple.Create(BucketName, AccessKeyId, BucketRegion, SecretAccessKey, PathTemplate);
		}

		public override bool Equals(object obj) {
			if (obj is BucketCredentials bc)
				return bc.CreateTuple().Equals(CreateTuple());
			return false;
		}

		public override int GetHashCode() {
			return CreateTuple().GetHashCode();
		}

	}

	public interface IBlobStorageProvider {
		Task DeleteFile([NotNull] BucketCredentials credentials, [NotNull] string fileKey);
		Task<string> GetStringData([NotNull] BucketCredentials credentials, [NotNull] string fileKey);
		Task<string> StoreStringData([NotNull] BucketCredentials credentials, [NotNull] string fileKey, string data);
		Task<Uri> UploadFile([NotNull] BucketCredentials credentials, [NotNull] string fileKey, Stream fileContent, bool makePublic);
		Task<byte[]> GetFileData([NotNull] BucketCredentials credentials, [NotNull] string fileKey);
		Task<string> GeneratePreSignedUrl([NotNull] BucketCredentials credentials, [NotNull] string fileKey, [NotNull] PreSignedUrlSettings settings);
	}
}

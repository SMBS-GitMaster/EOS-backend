using System;

namespace RadialReview.Models.Documents {
	public class DocumentItemInfoVM {
		public string Name { get; set; }
		public string CreateTime { get; set; }
		public string LastModifiedTime { get; set; }
		public string CreatedBy { get; set; }
		public string LastModifiedBy { get; set; }
		public string Description { get; set; }
		public string Type { get; set; }
		public string FileType { get; set; }
		public string FileExtension { get; set; }
		public string FileTypeDetails { get; set; }
		public bool Generated { get; set; }
		public long Size { get; set; }

		public string GetFriendlySize() {
			if (Size <= 0)
				return "-";
			if (Size < 100000) {
				return String.Format("{0:0.#}KB", Size / 1000.0);
			}
			return String.Format("{0:0.#}MB", Size / 1000000.0);
		}

	}
}
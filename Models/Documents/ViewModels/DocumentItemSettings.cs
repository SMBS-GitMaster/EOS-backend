namespace RadialReview.Models.Documents {
	public class DocumentItemSettings {
		public long? ParentFolderId { get; set; }
		public string ParentFolderName { get; set; }
		public bool? CanDelete { get; set; }
		public bool? CanEdit { get; set; }
		public bool? CanAdmin { get; set; }
		public DocumentItemSettings() { }
		public DocumentItemSettings(DocumentsFolder parentFolder) {
			if (parentFolder != null) {
				ParentFolderId = parentFolder.Id;
				ParentFolderName = parentFolder.Name;
			}
		}
		public DocumentItemSettings(DocumentItemPathVM parentFolder) {
			if (parentFolder != null) {
				ParentFolderId = parentFolder.FolderId;
				ParentFolderName = parentFolder.Name;
			}
		}

	}
}
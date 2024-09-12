namespace RadialReview.Models.Documents {
	public class DocumentItemPathVM {
		public string Name { get; set; }
		public long FolderId { get; set; }
		public string Id { get; set; }
		public long? ParentFolderId { get; set; }
		public string Class { get; set; }
		public string Url { get; set; }
	}
}

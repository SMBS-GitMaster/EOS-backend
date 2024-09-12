using RadialReview.Models.Documents.Enums;
using System;

namespace RadialReview.Models.Documents {
	public class DocumentItemLinkSettings {
		public string Url { get; set; }
		public string Name { get; set; }
		public string IconHint { get; set; }
		public string Description { get; set; }
		public DateTime? CreateTime { get; set; }
		public string ImageUrl { get; set; }
		public DocumentItemWindowTarget? Target { get; set; }
		public bool CanDelete { get; set; }
		public bool Generated { get; set; }
		public bool ForceViewable { get; set; }
		public DocumentItemLinkSettings() {
			//Will want to remove if ever we have non-auto-generated links
			ForceViewable = true;
		}
	}
}
using RadialReview.Models.Documents;

namespace RadialReview.Models.Documents {
	public enum DocumentItemType {
		Invalid = 0,
		EncryptedFile = 1,
		DocumentFolder = 2,
		Link = 3,
		Process = 4,
		Whiteboard = 5,
		VTO = 6,
	}


}

namespace RadialReview {
	public static class DocumentItemTypeExtensions {

		public static string ToFriendlyName(this DocumentItemType type) {
			return GetFriendlyName(type);
		}

		public static string GetFriendlyName(this DocumentItemType type) {
			switch (type) {
				case DocumentItemType.Invalid:
					return "Invalid";
				case DocumentItemType.EncryptedFile:
					return "File";
				case DocumentItemType.DocumentFolder:
					return "Folder";
				case DocumentItemType.Link:
					return "Link";
				case DocumentItemType.Process:
					return "Process";
				case DocumentItemType.Whiteboard:
					return "Whiteboard";
				case DocumentItemType.VTO:
					return "VTO";
				default:
					return "" + type;
			}

		}

	}
}
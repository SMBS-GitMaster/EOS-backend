namespace RadialReview.Models.Documents {

	public class GS {
		private GS() { }

		public static GS Folder(string name, string iconHint, string tagHint) {
			return new GS() {
				Name = name,
				IconHint = iconHint,
				CanDelete = true,
				TagHint = tagHint
			};
		}

		public static GS Historical(string name, GeneratedFolderConst key) {
			return new GS() {
				Name = name,
				Class = key.Class,
				Interceptor = key.Interceptor,
				IconHint = "<span class='docs-icon docs-icon-historical'></span>",
				HueRotate = 140,
				CanDelete = false
			};
		}

		public static GS AutoPopFolder(string name, GeneratedFolderConst key, string iconHint, int? hueRotate, bool canDelete) {
			return new GS() {
				Name = name,
				Class = key.Class,
				Interceptor = key.Interceptor,
				IconHint = iconHint,
				HueRotate = hueRotate,
				CanDelete = canDelete,
			};
		}

		public static GS StaticFile(string name, string url, string interceptor, string iconHint, bool canDelete) {
			return new GS() {
				Name = name,
				Url = url,
				Interceptor = interceptor,
				HueRotate = 30,
				IconHint = iconHint,
				CanDelete = canDelete,
			};
		}

		public static GS Link(string name, string url, string iconHint, bool canDelete) {
			return new GS() {
				Name = name,
				Url = url,
				IconHint = iconHint,
				HueRotate = 30,
				CanDelete = canDelete,
			};
		}


		public bool IsFile { get; set; }
		public string Name { get; set; }
		public string Interceptor { get; set; }
		public string Command { get; set; }
		public string IconHint { get; set; }
		public string ImageUrl { get; set; }
		public int? HueRotate { get; set; }
		public string Url { get; set; }
		public string Class { get; set; }
		public bool CanDelete { get; set; }
		public string TagHint { get; set; }
	}
}

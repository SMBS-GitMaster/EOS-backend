using System;

namespace RadialReview.Models.Documents {
	public class GeneratedFolderConst {
		public GeneratedFolderConst() { }
		public GeneratedFolderConst(string interceptor, string @class) {
			if (interceptor == null)
				throw new ArgumentNullException(nameof(interceptor));
			if (@class == null)
				throw new ArgumentNullException(nameof(@class));

			Interceptor = interceptor;
			Class = @class;
		}

		public string Interceptor { get; set; }
		public string Class { get; set; }
		public bool InterceptorMatches(DocumentsFolder folder) {
			if (folder == null)
				return false;
			return folder.Interceptor == Interceptor;
		}

		public bool ClassMatches(DocumentsFolder folder) {
			if (folder == null)
				return false;
			return folder.Class == Class;
		}


		public bool InterceptorMatches(GS structure) {
			if (structure == null)
				return false;
			return structure.Interceptor == Interceptor;
		}
		public bool ClassMatches(GS structure) {
			if (structure == null)
				return false;
			return structure.Class == Class;
		}
	}
}
namespace RadialReview.Models {
  public static class ShareVtoPagesExtension {
		public static bool ViewVision(this ShareVtoPages self) {
			switch (self) {
				case ShareVtoPages.BothFFAndSTFNoIssues:
				case ShareVtoPages.BothFFAndSTF:
				case ShareVtoPages.FutureFocusOnly:
					return true;
				default:
					return false;
			}
		}
		public static bool ViewTraction(this ShareVtoPages self) {
			switch (self) {
				case ShareVtoPages.BothFFAndSTFNoIssues:
				case ShareVtoPages.BothFFAndSTF:
					return true;
				case ShareVtoPages.FutureFocusOnly:
					return false;
				default:
					return false;
			}
		}
		public static bool IncludeIssues(this ShareVtoPages self) {
			switch (self) {
				case ShareVtoPages.BothFFAndSTF:
					return true;
				case ShareVtoPages.FutureFocusOnly:
				case ShareVtoPages.BothFFAndSTFNoIssues:
					return false;
				default:
					return false;
			}
		}
	}
}

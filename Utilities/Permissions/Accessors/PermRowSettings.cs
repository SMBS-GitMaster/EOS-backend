namespace RadialReview.Utilities.Permissions.Accessors {
	public class PermRowSettings {
		public PermRowSettings() {
			ShowView = true;
			ShowEdit = true;
			ShowAdmin = true;
			ShowDelete = true;
			DisableView = false;
			DisableEdit = false;
			DisableAdmin = false;
			DisableDelete = false;
		}

		public bool ShowView { get; set; }
		public bool ShowEdit { get; set; }
		public bool ShowAdmin { get; set; }
		public bool ShowDelete { get; set; }

		public bool DisableView { get; set; }
		public bool DisableEdit { get; set; }
		public bool DisableAdmin { get; set; }
		public bool DisableDelete { get; set; }

		public PermRowSettings WithAllDisabled() {
			return DisableAll(this);
		}

		public static PermRowSettings ALL_ALLOWED() {
			return new PermRowSettings() {
				ShowView = true,
				ShowEdit = true,
				ShowAdmin = true,
				ShowDelete = true,
				DisableView = false,
				DisableEdit = false,
				DisableAdmin = false,
				DisableDelete = false,
			};
		}

		public static PermRowSettings DisableAll(PermRowSettings p) {
			return new PermRowSettings() {
				ShowView = p.ShowView,
				ShowEdit = p.ShowEdit,
				ShowAdmin = p.ShowAdmin,
				ShowDelete = p.ShowDelete,
				DisableView = true,
				DisableEdit = true,
				DisableAdmin = true,
				DisableDelete = true,
			};
		}

	}
}
namespace RadialReview.Models.Documents {
	public class DocumentItemMenuItemVM {
		public string Text { get; set; }
		public string Description { get; set; }
		public string Icon { get; set; }
		public bool Enabled { get; set; }
		public bool Separator { get; set; }
		public string OnClick { get; set; }

		private DocumentItemMenuItemVM() { }

		public DocumentItemMenuItemVM(string text, string onClick) {
			Text = text;
			OnClick = onClick;
			Enabled = true;
		}
		public DocumentItemMenuItemVM(string text, string icon, string onclick) : this(text, onclick) {
			Icon = icon;
		}
		public DocumentItemMenuItemVM(string text, string icon, string description, string onclick) : this(text, icon, onclick) {
			Description = description;
		}

		public static DocumentItemMenuItemVM CreateDisabled(string text, string icon = null) {
			var item = new DocumentItemMenuItemVM(text, icon, null, null) {
				Enabled = false,
			};
			return item;
		}
		public static DocumentItemMenuItemVM Create(string text, string onclick) {
			return Create(true, text, onclick);
		}

		public static DocumentItemMenuItemVM Create(bool enabled, string text, string onClick) {
			var item = new DocumentItemMenuItemVM(text, null, null, enabled ? onClick : null) {
				Enabled = enabled,
			};
			return item;
		}

		public static DocumentItemMenuItemVM Create(bool enabled, string text, string icon, string onClick) {
			var item = new DocumentItemMenuItemVM(text, icon, null, enabled ? onClick : null) {
				Enabled = enabled,
			};
			return item;
		}
		public static DocumentItemMenuItemVM Create(string text, string icon, string onClick) {
			return Create(true, text, icon, onClick);
		}

		public static DocumentItemMenuItemVM CreateSeparator() {
			return new DocumentItemMenuItemVM() {
				Separator = true
			};
		}
	}
}
using System;

namespace RadialReview.Models.Documents {
	public class TinyDocumentItem {
		private TinyDocumentItem() { }

		public long ItemId { get; set; }
		public DocumentItemType ItemType { get; set; }

		public TinyDocumentItem(long itemId, DocumentItemType itemType) {
			ItemId = itemId;
			ItemType = itemType;
		}
		public Tuple<long, DocumentItemType> ToTuple() {
			return Tuple.Create(ItemId, ItemType);
		}

		public override bool Equals(object obj) {
			if (obj is TinyDocumentItem) {
				return ((TinyDocumentItem)obj).ToTuple().Equals(ToTuple());
			}
			return false;
		}

		public override int GetHashCode() {
			return ToTuple().GetHashCode();
		}


		private bool _ForceViewable { get; set; }
		private bool _ForceEditable { get; set; }
		private bool _ForceAdminable { get; set; }
		public bool ForcePermitted(PermItem.AccessLevel accessLevel) {
			accessLevel.EnsureSingleAndValidAccessLevel();
			if (_ForceViewable && accessLevel == PermItem.AccessLevel.View)
				return true;
			if (_ForceEditable && accessLevel == PermItem.AccessLevel.Edit)
				return true;
			if (_ForceAdminable && accessLevel == PermItem.AccessLevel.Admin)
				return true;

			return false;
		}

		public TinyDocumentItem SetForceViewable(bool forceViewable) {
			_ForceViewable = forceViewable;
			return this;
		}
	}
}
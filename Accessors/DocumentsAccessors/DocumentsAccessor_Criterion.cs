using NHibernate.Criterion;
using RadialReview.Models.Documents;

namespace RadialReview.Accessors {
	public partial class DocumentsAccessor {
		public class Criterions {
			public static QueryOver<DocumentItemLocation> GetItemIdsInFolder_IncludingDeletedItems(long folderId, DocumentItemType itemType)
			{
				var allFileLocationsQ = QueryOver.Of<DocumentItemLocation>()
											.Where(x => x.DocumentFolderId == folderId && x.ItemType == itemType)
											.Select(Projections.Distinct(Projections.Property<DocumentItemLocation>(x => x.ItemId)));
				return allFileLocationsQ;
			}
			public static QueryOver<DocumentItemLocation> GetItemIdsInFolder(long folderId, DocumentItemType itemType) {
				var allFileLocationsQ = QueryOver.Of<DocumentItemLocation>()
											.Where(x => x.DocumentFolderId == folderId && x.DeleteTime == null && x.ItemType == itemType)
											.Select(Projections.Distinct(Projections.Property<DocumentItemLocation>(x => x.ItemId)));
				return allFileLocationsQ;
			}

			public static QueryOver<DocumentItemLocation> GetParentFolderId(long childFolderId, bool allowShortCuts) {
				var parentFolderIdsQ = QueryOver.Of<DocumentItemLocation>().Where(x => x.DeleteTime == null && x.ItemId == childFolderId && x.ItemType == DocumentItemType.DocumentFolder);
				if (!allowShortCuts) {
					parentFolderIdsQ = parentFolderIdsQ.Where(x => !x.IsShortcut);
				}
				return parentFolderIdsQ.Select(Projections.Distinct(Projections.Property<DocumentItemLocation>(x => x.DocumentFolderId)));
			}

		}
	}
}
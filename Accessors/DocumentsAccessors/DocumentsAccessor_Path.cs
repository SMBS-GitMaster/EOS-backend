using NHibernate;
using RadialReview.Models.Documents;
using RadialReview.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace RadialReview.Accessors {
	public partial class DocumentsAccessor {

		private static IEnumerable<DocumentsFolder> GetParentFolder_Future(ISession s, long folderId) {
			return s.QueryOver<DocumentsFolder>()
						.Where(x => x.DeleteTime == null)
						.WithSubquery.WhereProperty(x => x.Id)
						.In(Criterions.GetParentFolderId(folderId, false))
						.Take(1)
						.Future();
		}

		public static async Task<List<DocumentItemPathVM>> GetPath(ISession s, PermissionsUtility perms, long folderId, bool includeListing, string companyName) {
			long? foId = folderId;
			if (foId != null) {
				perms.ViewDocumentsFolder(foId.Value);
			}

			var output = new List<DocumentItemPathVM>();
			while (foId != null) {
				var o = GetFolderPathItem_Unsafe(s, foId.Value);
				output.Insert(0, o);
				foId = o.ParentFolderId;
			}
			//output.Insert(0,new FolderNameParentVM() {
			//	Name = "Main Folder",
			//	FolderId = null,
			//	ParentFolderId = null,
			//});
			if (includeListing) {
				var companyDocs = output.FirstOrDefault();
				if (companyDocs != null) {
					companyDocs.Name = companyName;
				}

				output.Insert(0, new DocumentItemPathVM() {
					Id = null,
					Name = "",
					Class = "docs-icon-listing",
					Url = "/documents/listing",
					FolderId = -1,
					ParentFolderId = null
				});
			}
			return output;
		}

	}
}
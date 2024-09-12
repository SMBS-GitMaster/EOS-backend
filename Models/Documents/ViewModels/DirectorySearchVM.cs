using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Documents {
	public class DirectoryVM {
		public string Id { get; set; }
		public string Name { get; set; }
		public bool CanEdit { get; set; }
		public bool CanAdmin { get; set; }
		public bool CanDelete { get; set; }
		public bool IsOrgFolder { get; set; }
		public List<DirectoryVM> Subdirectories { get; set; }
		public DirectoryVM(DocumentItemVM folder) {
			Id			= folder.Id;
			Name		= folder.Name;
			CanEdit		= folder.CanEdit;
			CanAdmin	= folder.CanAdmin;
			CanDelete	= folder.CanDelete ?? false;
		}

		public DirectoryVM(DocumentsFolderVM folder) : this(folder.Folder) {
			Subdirectories = folder.Contents.Where(x => x.Type == DocumentItemType.DocumentFolder).Select(x => new DirectoryVM(x)).ToList();			
		}

	}
}
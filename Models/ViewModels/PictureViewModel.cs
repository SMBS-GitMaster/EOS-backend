using RadialReview.Models.Askables;
using System.Collections.Generic;

namespace RadialReview.Models.ViewModels {
	public class PictureViewModel {
		public string Url { get; set; }
		public int? Hue { get; set; }
		public string Hex { get; set; }
		public string Initials { get; set; }
		public string Title { get; set; }
		public double? Size { get; set; }

		private List<string> Classes { get; set; }

		public void AddClass(string clss) {
			Classes.Add(clss);
		}

		public PictureViewModel() {
			Classes = new List<string>();
		}

		public string GetClasses() {
			return string.Join(" ", Classes);
		}
		public static PictureViewModel CreateFromInitials(string initials, string title) {
			return new PictureViewModel() {
				Initials = initials,
				Title = title
			};
		}

		public static PictureViewModel CreateFrom(ResponsibilityGroupModel rgm) {
			var output = new PictureViewModel();
			output.Hue = 0;
			if (rgm != null) {
				output.Title = rgm.GetName();
				output.Url = rgm.GetImageUrl();
				if (rgm is UserOrganizationModel) {
					var u = (UserOrganizationModel)rgm;
					output.Initials = u.GetInitials();
					output.Hue = u.GeUserHashCode();
				}
			}
			return output;
		}
	}
}
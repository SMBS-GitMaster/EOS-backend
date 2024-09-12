using System;
using System.Collections.Generic;
using System.Net;

namespace RadialReview.Utilities {
	public class ImageUtility {

		public static string GenerateImageHtml(string url, string name, string initials = null) {
			name = name ?? "";
			var hash = 0;
			if (name != "") {
				for (var i = 0; i < name.Length; i++) {
					var chr = name[i];
					hash = ((hash << 5) - hash) + chr;
					hash |= 0; // Convert to 32bit integer
				}
				hash = Math.Abs(hash) % 360;
			}
			string picture;
			if (url != "/i/userplaceholder" && url != null) {
				picture = "<span class='picture' style='background: url(" + url + ") no-repeat center center;'></span>";
			} else {
				if (name == "") {
					name = "n/a";
				}
				initials = GetInitials(name, initials).ToUpper();
				picture = "<span class='picture' style='background-color:hsla(" + hash + ", 36%, 49%, 1);color:hsla(" + hash + ", 36%, 93%, 1)'><span class='initials'>" + initials + "</span></span>";
			}

			return "<span class='profile-picture'>" +
					  "<span class='picture-container' title='" + WebUtility.HtmlEncode(name) + "'>" +
							picture +
					  "</span>" +
				   "</span>";

		}

		private static string GetInitials(string name, string initials) {
			if (name == null) {
				name = "";
			}
			if (initials == null) {
				var m = name.SplitAndTrim(' ');
				var arr = new List<string>();
				if (m.Length > 0)
					arr.Add(m[0]);
				if (m.Length > 1)
					arr.Add(m[1]);
				initials = string.Join(" ", arr);
			}
			return initials;
		}
	}
}

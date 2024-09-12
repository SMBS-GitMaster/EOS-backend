using RadialReview.Models.Enums;
using System;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace RadialReview.Controllers {
	public partial class BaseController : Controller {

		protected void ModifySettings(Action<SettingsViewModel> modify) {
			if (modify != null) {
				ViewBag.SettingsModifiers = ViewBag.SettingsModifiers ?? new List<Action<SettingsViewModel>>();
				((List<Action<SettingsViewModel>>)ViewBag.SettingsModifiers).Add(modify);
			}
		}

		protected void RemoveTitleBar(bool adjustPadding = true) {
			ModifySettings(x => {
				if (adjustPadding) {
					x.ui.outer_padding = "56px 20px 10px 20px !important";
				}
				x.ui.show_title_bar = false;
			});
			ViewBag.NoTitleBar = true;
		}

	}
}

using RadialReview.Models.Enums;
using System;
using Microsoft.AspNetCore.Mvc;


namespace RadialReview.Controllers {
	public partial class BaseController : Controller {
		protected void ShowAlert(string status, AlertType type = AlertType.Info, bool afterReload = false) {
			if (afterReload) {
				TempData["InfoAlert"] = status;
			} else {
				switch (type) {
					case AlertType.Info:
						ViewBag.InfoAlert = status;
						break;
					case AlertType.Error:
						ViewBag.Alert = status;
						break;
					case AlertType.Success:
						ViewBag.Success = status;
						break;
					default:
						throw new ArgumentOutOfRangeException("AlertType:" + type);
				}
			}
		}

	}
}

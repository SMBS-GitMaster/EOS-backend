using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Request {
  public class ViewSettingsUtility {

    public static void ModifySettings(ViewDataDictionary ViewDataDictionary, Action<SettingsViewModel> modify) {
      if (modify != null) {
        ViewDataDictionary["SettingsModifiers"] = ViewDataDictionary["SettingsModifiers"] ?? new List<Action<SettingsViewModel>>();
        ((List<Action<SettingsViewModel>>)ViewDataDictionary["SettingsModifiers"]).Add(modify);
      }
    }

    public static void RemoveTitleBar(ViewDataDictionary ViewDataDictionary, bool adjustPadding = true) {
      ModifySettings(ViewDataDictionary,
        x => {
        if (adjustPadding) {
            x.ui.hideV1SideNavAndTopNav = true;
        }
        x.ui.show_title_bar = false;
      });
      ViewDataDictionary["NoTitleBar"] = true;
    }
  }
}

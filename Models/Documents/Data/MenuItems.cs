using RadialReview.Utilities.FileTypes;
using System.Collections.Generic;

namespace RadialReview.Models.Documents.Interceptors.Data {
  public class MenuItems {

    public static DocumentItemMenuItemVM OpenInNewWindow(DocumentItemVM item) {
      return new DocumentItemMenuItemVM("Open in a new window", "docs-icon docs-icon-new-window", "window.open('" + item.Url + "','_blank')");
    }
    public static DocumentItemMenuItemVM Download(DocumentItemVM item) {
      var url = AppendQueryParam(item.Url, "inline", "false");
      return new DocumentItemMenuItemVM("Download", "docs-icon docs-icon-download", "window.open('" + url.ToString() + "','_blank')");
    }

    public static DocumentItemMenuItemVM Delete(DocumentItemVM item) {
      var canDelete = (item.CanDelete ?? true) && item.TinyItem.ItemId > 0 && item.TinyItem.ItemType != DocumentItemType.Invalid;
      return DocumentItemMenuItemVM.Create(canDelete, "Delete", "docs-icon docs-icon-delete", "Documents.delete(" + item.TinyItem.ItemId + ",'" + item.TinyItem.ItemType + "')");
    }
    public static DocumentItemMenuItemVM Info(DocumentItemVM item) {
      var hasInfo = item.TinyItem.ItemId > 0 && item.TinyItem.ItemType != DocumentItemType.Invalid;
      return DocumentItemMenuItemVM.Create(hasInfo, "Info", "docs-icon docs-icon-info", "Documents.info(" + item.TinyItem.ItemId + ",'" + item.TinyItem.ItemType + "')");
    }

    public static DocumentItemMenuItemVM Permissions(DocumentItemVM item) {
      var hasPermissions = item.TinyItem.ItemId > 0 && item.TinyItem.ItemType != DocumentItemType.Invalid;
      return DocumentItemMenuItemVM.Create(hasPermissions, "Permissions", "docs-icon docs-icon-permissions", "Documents.permissions(" + item.TinyItem.ItemId + ",'" + item.TinyItem.ItemType + "')");
    }
    public static DocumentItemMenuItemVM Rename(DocumentItemVM item) {
      var canRename = item.CanEdit && item.TinyItem.ItemId > 0 && item.TinyItem.ItemType != DocumentItemType.Invalid;
      return DocumentItemMenuItemVM.Create(canRename, "Rename", "docs-icon docs-icon-rename", "Documents.rename(" + item.TinyItem.ItemId + ",'" + item.TinyItem.ItemType + "')");
    }

    public static DocumentItemMenuItemVM EditCopyViaWhitboard(DocumentItemVM item, string imageUrl) {
      return DocumentItemMenuItemVM.Create("Edit a copy", "docs-icon docs-icon-rename", "Documents.editWhiteboardCopy('" + imageUrl + "')");
    }

    public static DocumentItemMenuItemVM Move(DocumentItemVM item) {
      var canMove = item.CanAdmin && item.TinyItem.ItemId > 0 && item.TinyItem.ItemType != DocumentItemType.Invalid;
      return DocumentItemMenuItemVM.Create(canMove, "Move", "docs-icon docs-icon-move", "Documents.move(" + item.TinyItem.ItemId + ",'" + item.TinyItem.ItemType + "')");
    }
    public static DocumentItemMenuItemVM Shortcut(DocumentItemVM item) {
      var canShortcut = item.TinyItem.ItemId > 0 && item.TinyItem.ItemType != DocumentItemType.Invalid;
      return DocumentItemMenuItemVM.Create(canShortcut, "Create shortcut", "docs-icon docs-icon-shortcut", "Documents.shortcut(" + item.TinyItem.ItemId + ",'" + item.TinyItem.ItemType + "')");
    }
    public static DocumentItemMenuItemVM CopyDocument(DocumentItemVM item) {
      var canShortcut = item.TinyItem.ItemId > 0 && item.TinyItem.ItemType != DocumentItemType.Invalid;
      return DocumentItemMenuItemVM.Create(canShortcut, "Create Editable Copy", "docs-icon docs-icon-shortcut", "Documents.clone(" + item.TinyItem.ItemId + ",'" + item.TinyItem.ItemType + "')");
    }


    public static DocumentItemMenuItemVM Separator() {
      return DocumentItemMenuItemVM.CreateSeparator();
    }

    public static IEnumerable<DocumentItemMenuItemVM> ConstructMenu(DocumentItemVM item) {
      if (item == null) {
        yield break;
      }

      if (item.Type == DocumentItemType.EncryptedFile && FileTypeExtensionUtility.GetFileTypeFromExtension(item.Extension).MediaType == FileTypeExtensionUtility.MediaType.Image) {
        yield return EditCopyViaWhitboard(item, "/Documents/inline/" + item.Id);
        yield return Separator();
      }


      yield return Info(item);
      yield return Rename(item);
      yield return Permissions(item);
      yield return Separator();

      if (item.TinyItem.ItemType == DocumentItemType.EncryptedFile) {
        yield return Download(item);
        yield return OpenInNewWindow(item);
        yield return Separator();
      }
      if (item.TinyItem.ItemType == DocumentItemType.Whiteboard) {
        yield return CopyDocument(item);
      }

      yield return Shortcut(item);
      yield return Move(item);
      yield return Delete(item);
    }

    private static string AppendQueryParam(string url, string name, string value) {
      if (url == null)
        return url;
      var toAppend = name + "=" + value;
      if (url.Contains("?")) {
        return url + "&" + toAppend;
      } else {
        return url + "?" + toAppend;
      }
    }
  }
}

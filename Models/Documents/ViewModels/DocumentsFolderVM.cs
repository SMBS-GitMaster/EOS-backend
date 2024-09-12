using RadialReview.Models.Documents.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Utilities.DataTypes;
using Microsoft.AspNetCore.Html;

namespace RadialReview.Models.Documents {

  public class DocumentsFolderVM {
    public DocumentItemVM Folder { get; set; }
    public List<DocumentItemVM> Contents { get; set; }
    public List<DocumentItemPathVM> Path { get; set; }
    public List<DocumentItemVM> Favorites { get; set; }
    public List<DocumentItemVM> Recent { get; set; }
    public List<DocumentHeadingGroup> HeadingGroups { get; set; }
    public bool ShowFavorites { get; set; }
    public bool ShowRecent { get; set; }
    public bool ShowHeadingGroups { get; set; }
    public bool EnableSidebar { get; set; }

    public DocumentsFolderDisplayType? DisplayType { get; set; }
    public DocumentsFolderOrderType? OrderType { get; set; }
    public bool OrderAscending { get; set; }
    public ITimeData TimeSettings { get; set; }
    public bool ContainsHiddenItems { get; internal set; }

    public void SetInstructionBar(string v) {
      if (v==null) {
        InstructionsHtml = null;
      } else {
        InstructionsHtml= new HtmlString(
$@"<div class='docs-section'>
  <div class='docs-section-instructions'>{v}</div>
</div>");
      }
    }

    public string MainSectionTitle { get; set; }
    public string ExecuteOnLoad { get; set; }
    public HtmlString InstructionsHtml { get; set; }

    public DocumentsFolderVM() {
      EnableSidebar = true;
    }

    public static DocumentsFolderVM GenerateListing(List<DocumentItemVM> contents, DateTime createTime) {
      return new DocumentsFolderVM() {
        Contents = contents,
        ShowFavorites = false,
        ShowRecent = false,
        EnableSidebar = false,
        Folder = DocumentItemVM.CreateLink(new DocumentItemLinkSettings() {
          CreateTime = createTime,
          Name = "Listing",
          Url = "/documents/listing",
          Generated = true,
          CanDelete = false,

        }, new DocumentItemSettings())
      };
    }

    public List<DocumentItemSortedGroupingVM> GetOrderedGrouping() {
      var now = DateTime.UtcNow;
      if (DisplayType == DocumentsFolderDisplayType.Grouped) {
        switch (OrderType) {
          case DocumentsFolderOrderType.Name:
            return CreateGroups(x => (x.Name??"").FirstOrDefault().ToString().ToUpper(), x => x.Name, OrderAscending, x => (x == "\0") ? "untitled" : ("" + x));
          case DocumentsFolderOrderType.Created:
            return CreateGroups(x => GetDateKey(x.CreateTime, now, TimeSettings), x => x.CreateTime, OrderAscending, x => x);
          case DocumentsFolderOrderType.Size:
            return CreateGroups(x => GetSizeKey(x.IsFolder() ? (OrderAscending ? -1 : long.MaxValue) : x.Size), x => x.Size, OrderAscending, x => x);
          case DocumentsFolderOrderType.LastModified:
            return CreateGroups(x => GetDateKey(x.LastModified, now, TimeSettings), x => x.LastModified, OrderAscending, x => x);
          default:
            break;
        }
      }
      return new List<DocumentItemSortedGroupingVM>() {
        new DocumentItemSortedGroupingVM(){
          GroupTitle =MainSectionTitle ?? "All Documents",
          Contents = GetOrderedContents(OrderType,OrderAscending)
        }
      };
    }

    private List<DocumentItemVM> GetOrderedContents(DocumentsFolderOrderType? type, bool ascending) {
      switch (type) {
        case DocumentsFolderOrderType.Name:
          if (ascending)
            return Contents.OrderBy(x => x.Name).ToList();
          else
            return Contents.OrderByDescending(x => x.Name).ToList();
        case DocumentsFolderOrderType.Created:
          if (ascending)
            return Contents.OrderBy(x => x.CreateTime).ToList();
          else
            return Contents.OrderByDescending(x => x.CreateTime).ToList();
        case DocumentsFolderOrderType.Size:
          if (ascending)
            return Contents.OrderBy(x => x.IsFolder() ? -1 : x.Size).ToList();
          else
            return Contents.OrderByDescending(x => x.IsFolder() ? long.MaxValue : x.Size).ToList();
        case DocumentsFolderOrderType.LastModified:
          if (ascending)
            return Contents.OrderBy(x => x.LastModified).ToList();
          else
            return Contents.OrderByDescending(x => x.LastModified).ToList();
        default:
          break;
      }
      return Contents.ToList();
    }

    private List<DocumentItemSortedGroupingVM> CreateGroups<GROUPBY, SORTBY>(Func<DocumentItemVM, GROUPBY> groupBy, Func<DocumentItemVM, SORTBY> sortBy, bool asc, Func<GROUPBY, string> keyConvert) {
      return DocumentItemSortedGroupingVM.CreateFromGroup(Contents.GroupBy(groupBy), sortBy, asc, keyConvert);
    }

    private class DateKey {
      public DateKey(string key, DateTime after) {
        Key = key;
        After = after;
      }
      public string Key { get; set; }
      public DateTime After { get; set; }
    }
    private class SizeKey {
      public SizeKey(string key, long bigger) {
        Key = key;
        Size = bigger;
      }
      public string Key { get; set; }
      public long Size { get; set; }
    }
    private static string GetDateKey(DateTime? itemTime, DateTime now, ITimeData timeSettings) {
      if (itemTime == null)
        return "Unknown";
      foreach (var group in GetDateGroups(now, timeSettings)) {
        if (itemTime.Value.IsAfter(group.After)) {
          return group.Key;
        }
      }
      return "Older";
    }
    private static string GetSizeKey(long size) {
      foreach (var group in GetSizeGroups()) {
        if (size < group.Size) {
          return group.Key;
        }
      }
      return "Gigantic";
    }

    private static long Kilobyte = 1000;
    private static long Megabyte = 1000000;
    private static IEnumerable<SizeKey> GetSizeGroups() {
      yield return new SizeKey("Unknown", 0);
      yield return new SizeKey("Tiny (Less than 100kb)", 100 * Kilobyte);
      yield return new SizeKey("Small (Less than 1mb)", 1 * Megabyte);
      yield return new SizeKey("Medium (Less than 10mb)", 10 * Megabyte);
      yield return new SizeKey("Large (Less than 100mb)", 100 * Megabyte);
      yield return new SizeKey("Gigantic", long.MaxValue);
    }

    private static IEnumerable<DateKey> GetDateGroups(DateTime asOfServerTime, ITimeData timeSettings) {
      var asOfLocalTime = timeSettings.ConvertFromServerTime(asOfServerTime);
      var startOfDay = timeSettings.ConvertToServerTime(asOfLocalTime.Date);
      yield return new DateKey("Today", startOfDay);
      yield return new DateKey("This week", startOfDay.AddDays(-7));
      yield return new DateKey("This month", startOfDay.AddMonths(-1));
      yield return new DateKey("Last 90 days", startOfDay.AddDays(-90));
      yield return new DateKey("Last 180 days", startOfDay.AddDays(-180));
      yield return new DateKey("Last year", startOfDay.AddYears(-1));
      yield return new DateKey("Older", new DateTime(1, 1, 1));
    }


  }
}
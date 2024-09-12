using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace RadialReview.Utilities.DataTypes {
  public class Csv {
    private List<CsvItem> Items { get; set; }
    private List<String> Rows { get; set; }
    private List<String> Columns { get; set; }

    private DefaultDictionary<string, int?> RowsPositions { get; set; }
    private DefaultDictionary<string, int?> ColumnPositions { get; set; }

    private int RowLength = 0;
    private int ColLength = 0;

    public string Title { get; set; }
    public Csv(string title = null) {
      Items = new List<CsvItem>();
      Rows = new List<String>();
      Columns = new List<String>();
      RowsPositions = new DefaultDictionary<string, int?>(x => null);
      ColumnPositions = new DefaultDictionary<string, int?>(x => null);
      Title = title;
    }

    public void Add(String row, String column, String value) {

      row = row ?? "null";
      column = column ?? "null";

      if (RowsPositions[row] == null) {
        Rows.Add(row);
        RowsPositions[row] = RowLength;
        RowLength += 1;
      }
      if (ColumnPositions[column] == null) {
        Columns.Add(column);
        ColumnPositions[column] = ColLength;
        ColLength += 1;
      }


      Items.Add(new CsvItem() { Row = row, Column = column, Value = value });
    }

    public void SetTitle(string title) {
      Title = title;
    }

    public byte[] ToBytes(bool showRowTitle = true) {
      return new UTF8Encoding().GetBytes(this.ToCsv(showRowTitle));
    }

    public List<string> GetColumn(string heading) {
      var cells = GetCells();
      var cid = ColumnPositions[heading].Value;

      var res = new List<string>();
      foreach (var row in cells.Skip(1)) {
        res.Add(row[cid]);
      }
      return res;
    }


    public HtmlString ToRawHtml(bool showRowTitle = true) {
      var sb = new StringBuilder();

      sb.Append("<table class=\"csv-table\" data-title=\""+HttpUtility.HtmlAttributeEncode(Title)+"\">");
      sb.Append("<thead><tr>");
      if (showRowTitle) {
        sb.Append("<th class=\"csv-table-title\">"+HttpUtility.HtmlEncode(Title)+"</th>");
      }
      foreach (var c in Columns.ToList()) {
        sb.Append("<th class=\"csv-column-title\" data-column=\""+HttpUtility.HtmlAttributeEncode(c)+"\">");
        sb.Append(HttpUtility.HtmlEncode(c));
        sb.Append("</th>");
      }
      sb.Append("</tr></thead>");
      sb.Append("<tbody>");
      var rows = Rows.ToList();
      var cols = Columns.ToList();
      var cells = GetCells();
      for (var rid = 0; rid<rows.Count; rid+=1) {
        sb.Append("<tr class=\"csv-row\" data-row-id=\""+rid+"\" data-row=\""+HttpUtility.HtmlAttributeEncode(rows[rid])+"\">");
        if (showRowTitle) {
          sb.Append("<td class=\"csv-row-title\">"+HttpUtility.HtmlEncode(rows[rid])+"</td>");
        }
        for (var cid = 0; cid<cols.Count; cid+=1) {
          sb.Append($@"<td class=""csv-cell"" data-row-id=""{rid}"" data-col-id=""{cid}""  data-row=""{HttpUtility.HtmlAttributeEncode(rows[rid])}""  data-col=""{HttpUtility.HtmlAttributeEncode(cols[cid])}""  data-cell=""{HttpUtility.HtmlAttributeEncode(cells[rid][cid])}"">{HttpUtility.HtmlEncode(cells[rid][cid])}</td>");
        }
        sb.Append("</tr>");
      }
      sb.Append("</tbody>");
      sb.Append("</table>");

      return new HtmlString(sb.ToString());
    }

    public List<string> GetColumnsCopy() {
      return Columns.Select(x => x).ToList();
    }
    public List<string> GetRowsCopy() {
      return Rows.Select(x => x).ToList();
    }

    public String ToCsv(bool showRowTitle = true) {
      var sb = new StringBuilder();
      var cols = Columns.ToList();
      var rows = Rows.ToList();

      string[][] items = GetCells();
      var cc = new List<String>();
      if (showRowTitle)
        cc.Add(CsvQuote(Title));

      cc.AddRange(cols.Select(CsvQuote));
      sb.AppendLine(String.Join(",", cc));

      for (var i = 0; i < rows.Count; i++) {
        var rr = new List<String>();
        if (showRowTitle)
          rr.Add(CsvQuote(rows[i]));

        rr.AddRange(items[i]);
        sb.AppendLine(String.Join(",", rr));
      }
      return sb.ToString();
    }

    private string[][] GetCells() {
      var rows = Rows.ToList();
      var items = new String[RowLength][];

      for (var i = 0; i < rows.Count; i++)
        items[i] = new String[ColLength];

      foreach (var item in Items) {
        var col = ColumnPositions[item.Column].Value;
        var row = RowsPositions[item.Row].Value;
        items[row][col] = CsvQuote(item.Value);
      }

      return items;
    }

    static public string CsvQuote(string cell) {
      if (cell == null) {
        return string.Empty;
      }

      if (cell.StartsWith("-"))
        cell = " " + cell;

      var containsQuote = false;
      var containsComma = false;
      var containsReturn = false;
      var len = cell.Length;
      for (var i = 0; i < len && (containsComma == false || containsQuote == false); i++) {
        var ch = cell[i];
        if (ch == '"') {
          containsQuote = true;
        } else if (ch == ',') {
          containsComma = true;
        } else if (ch == '\n') {
          containsReturn = true;
        }
      }

      var mustQuote = containsComma || containsQuote || containsReturn;

      if (containsQuote) {
        cell = cell.Replace("\"", "\"\"");
      }

      if (mustQuote) {
        return "\"" + cell + "\"";  // Quote the cell and replace embedded quotes with double-quote
      } else {
        return cell;
      }
    }

    public string Get(int i, int j) {
      var row = Rows[i];
      var col = Columns[j];

      return Items.FirstOrDefault(x => x.Column == col && x.Row == row).NotNull(x => x.Value);
    }
    public static Csv LoadString(string csv, bool hasHeader) {
      return LoadStream(csv.ToStream(), hasHeader);

    }

    public static Csv LoadStream(Stream ms, bool hasHeader) {
      var cells = CsvUtility.Load(ms, true);
      return LoadCells(cells, hasHeader);
    }



    public static Csv LoadCells(List<List<string>> cells, bool hasHeader) {
      var csv = new Csv();

      if (cells.Count==0) {
        return csv;
      }

      var headings = new List<string>();
      if (hasHeader && cells.Count>0) {
        foreach (var cell in cells[0]) {
          headings.Add(cell);
        }
      } else if (cells.Count>0) {
        headings= Enumerable.Range(0, cells[0].Count).Select((x, i) => "col-"+i).ToList();
      }

      var rid = 0;
      foreach (var row in cells) {
        var cid = 0;
        foreach (var col in row) {
          csv.Add("row-"+rid, headings[cid], col);
          cid+=1;
        }
        rid+=1;
      }

      return csv;

    }
  }

  public class CsvItem {
    public String Column { get; set; }
    public String Row { get; set; }
    public String Value { get; set; }
  }
}

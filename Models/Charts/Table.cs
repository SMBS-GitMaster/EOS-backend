using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models.Charts {
	public class TableData {
		public HashSet<String> Columns = new HashSet<string>();
		public HashSet<String> Rows = new HashSet<string>();

		public Dictionary<String, Dictionary<String, HtmlString>> Values = new Dictionary<string, Dictionary<string, HtmlString>>();



		public List<HtmlString> GetColumn(string column) {
			var o = new List<HtmlString>();
			foreach (var row in Values) {
				if (row.Value.ContainsKey(column))
					o.Add(row.Value[column]);
			}
			return o;
		}



		public void Set(String row, String column, HtmlString data) {
			row = row ?? "";
			column = column ?? "";

			if (!Values.ContainsKey(row))
				Values[row] = new Dictionary<string, HtmlString>();

			Values[row][column] = data;
			Columns.Add(column);
			Rows.Add(row);
		}

		public HtmlString[][] GetTable(IEnumerable<string> rows, IEnumerable<string> columns) {
			var rr = rows as IList<string> ?? rows.ToList();
			var cc = columns as IList<string> ?? columns.ToList();
			var output = new HtmlString[rr.Count][];

			for (var i = 0; i < rr.Count; i++) {
				output[i] = new HtmlString[cc.Count];
				for (var j = 0; j < cc.Count; j++) {
					var r = rr[i];
					var c = cc[j];
					output[i][j] = Get(r, c);
				}
			}
			return output;
		}

		public HtmlString Get(string row, string column) {
			if (Values.ContainsKey(row) && Values[row].ContainsKey(column)) {
				return Values[row][column];
			}
			return null;
		}
	}

	public class SpanCell {
		public string Class;
		public string Title;
		public HtmlString Contents;
		public Dictionary<string, string> Data;

		public HtmlString ToHtmlString() {
			var data = "";
			if (Data != null && Data.Any())
				data = string.Join(" ", Data.Select(x => "data-" + x.Key + "=\"" + x.Value + "\""));

			return new HtmlString("<span class='" + (Class ?? "") + "' " + data + " title='" + (Title ?? "").Replace("'", "&#39;").Replace("\"", "&quot;") + "'>" + (Contents.NotNull(x => x.Value) ?? "") + "</span>");
		}
	}

	public class Table {

		public List<string> Columns { get; set; }
		public List<string> Rows { get; set; }
		public TableData Data { get; set; }
		public string TableClass { get; set; }


		public Table(TableData data) : this(data.Rows, data.Columns, data) {
		}

		public Table(IEnumerable<string> rows, IEnumerable<string> columns, TableData data) {
			Rows = rows.ToList();
			Columns = columns.ToList();
			Data = data;
		}

		public static Table Create<T>(IEnumerable<T> data, Func<T, string> row, Func<T, string> column, Func<T, HtmlString> cellSelector, string tableClass = "") {
			var table = new TableData();
			foreach (var d in data) {
				var cell = cellSelector(d);
				if (cell == null)
					continue;
				table.Set(row(d), column(d), cell);
			}
			return new Table(table.Rows, table.Columns, table) { TableClass = tableClass };
		}
	}
}
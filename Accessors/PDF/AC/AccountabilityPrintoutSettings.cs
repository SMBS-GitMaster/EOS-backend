using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors.PDF {

	public class AccountabilityPrintoutSettings {
		public bool DrawPageNumbers { get; set; }
		public bool DebugMode { get; set; }
		public bool UsingFallback { get; set; }

		public AccountabilityPrintoutSettings() {
			Actions = new List<AccountabilityChartAction>();
			WaitForReady();
		}
		public List<AccountabilityChartAction> Actions { get; set; }

		public AccountabilityPrintoutSettings WaitForReady() {
			Actions.Add(new AccountabilityChartAction("console.log('Waiting for ready');", "body.RenderComplete"));
			return this;
		}

		public AccountabilityPrintoutSettings ShowUserNames(int count = 3) {
			Actions.Add(new AccountabilityChartAction("showUserNames(" + Math.Max(1, count) + ");", "body.RenderComplete"));
			return this;
		}

		public AccountabilityPrintoutSettings ExpandAll() {
			Actions.Add(new AccountabilityChartAction("expandAll();", "body.RenderComplete"));
			return this;
		}
		public AccountabilityPrintoutSettings Compactify(bool compact = true) {
			Actions.Add(new AccountabilityChartAction("compactify(" + (compact ? "true" : "false") + ");", "body.RenderComplete"));
			return this;
		}
		public AccountabilityPrintoutSettings IsolateNode(long nodeId) {
			Actions.Add(new AccountabilityChartAction("isolate(" + nodeId + ");", "body.RenderComplete"));
			return this;
		}
		public AccountabilityPrintoutSettings PreparePdfViewport() {
			Actions.Add(new AccountabilityChartAction("pdfViewport();", "body.PdfViewportReady"));
			return this;
		}
		public AccountabilityPrintoutSettings IsolateNodes(IEnumerable<long> nodeIds, int depth) {
			depth = Math.Max(depth, 0);
			if (nodeIds.Any()) {
				Actions.Add(new AccountabilityChartAction("isolate([" + string.Join(",", nodeIds) + "]," + depth + ");", "body.RenderComplete"));
			}
			return this;
		}

		public AccountabilityPrintoutSettings Levels(int levels) {
			Actions.Add(new AccountabilityChartAction("showLevels(" + levels + ");", "body.RenderComplete"));
			return this;
		}

		public AccountabilityPrintoutSettings UseFallback(bool useFallback) {
			UsingFallback = useFallback;
			return this;
		}

		public AccountabilityPrintoutSettings SetDrawPageNumbers(bool drawPageNumbers) {
			DrawPageNumbers = drawPageNumbers;
			return this;
		}


		public AccountabilityPrintoutSettings CollaseAll() {
			Actions.Add(new AccountabilityChartAction("collapseAll();", "body.RenderComplete"));
			return this;
		}

		public AccountabilityPrintoutSettings ExpandLevels(int i) {
			Actions.Add(new AccountabilityChartAction("setLevel(" + i + ");", "body.RenderComplete"));
			return this;
		}

		public AccountabilityPrintoutSettings Highlight(IEnumerable<long> highlight) {
			if (highlight.Any()) {
				Actions.Add(new AccountabilityChartAction("highlight([" + string.Join(",", highlight) + "]);", "body.RenderComplete"));
			}
			return this;
		}

		public AccountabilityPrintoutSettings ShowNodes(IEnumerable<long> nodeIds) {
			if (nodeIds.Any()) {
				Actions.Add(new AccountabilityChartAction("showNodes([" + string.Join(",", nodeIds) + "]);", "body.RenderComplete"));
			}
			return this;
		}
	}

	public class AccountabilityChartAction {

		public static int TIMEOUT = 60000;
		public AccountabilityChartAction(string executeScript, string waitForSelector = null) {
			ExecuteScript = executeScript;
			WaitForSelector = waitForSelector;
		}
		public string ExecuteScript { get; set; }
		public string WaitForSelector { get; set; }

		public async Task Execute(Page p) {
			await p.EvaluateExpressionAsync(ExecuteScript);
			if (!string.IsNullOrWhiteSpace(WaitForSelector)) {
				await p.WaitForSelectorAsync(WaitForSelector, new WaitForSelectorOptions() { Timeout = TIMEOUT });
			}
		}
	}
}

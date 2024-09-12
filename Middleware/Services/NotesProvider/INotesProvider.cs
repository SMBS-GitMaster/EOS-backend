using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Middleware.Services.NotesProvider {
	public interface INotesProvider {

		Task CreatePad(string padid, string text);
		Task<string> GetReadonlyUrl(string padid);
		Task<HtmlString> GetHtmlForPad(string padid);
		Task<string> GetTextForPad(string padid);

	}


	public static class INotesProviderExtensions {


		public static async Task<Dictionary<string, HtmlString>> GetHtmlForPads(this INotesProvider provider, IEnumerable<string> padIds) {
			var results = await Task.WhenAll(padIds.Distinct().Select(x => _GetHtml(provider, x)));
			return results.ToDictionary(x => x.Item1, x => x.Item2);
		}

		public static async Task<Dictionary<string, string>> GetTextForPads(this INotesProvider provider, IEnumerable<string> padIds) {
			var results = await Task.WhenAll(padIds.Distinct().Select(x => _GetText(provider, x)));
			return results.ToDictionary(x => x.Item1, x => x.Item2);
		}

		private static async Task<Tuple<string, HtmlString>> _GetHtml(INotesProvider provider, string padid) {
			if (padid != null) {
				var result = await provider.GetHtmlForPad(padid);
				return Tuple.Create(padid.ToString(), result);
			}
			return null;
		}
		private static async Task<Tuple<string, string>> _GetText(INotesProvider provider, string padid) {
			if (padid != null) {
				var result = await provider.GetTextForPad(padid);
				return Tuple.Create(padid.ToString(), result);
			}
			return null;
		}
	}
}

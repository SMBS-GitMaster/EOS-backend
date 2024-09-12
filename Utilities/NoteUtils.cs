using System;
using System.Net;
using Flurl;

namespace RadialReview.Utilities {
	public static class NoteUtils {
		public static Uri BuildURL(string padId, bool showControls, string callerName) {
			string enableControls = showControls.ToString().ToLower();
			string encodedCallerName = WebUtility.UrlEncode(callerName);

			Url flurl = Config.NotesUrl()
					 .AppendPathSegment($"p")
					 .AppendPathSegment($"{ padId }")
					 .SetQueryParams(new 
					 { 
						 showControls = enableControls, 
						 showChat = false, 
						 showLineNumbers = false, 
						 useMonospaceFont = false, 
						 userName = encodedCallerName
					 });

			return flurl.ToUri();
		}
	}
}

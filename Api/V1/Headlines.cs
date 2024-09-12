using RadialReview.Accessors;
using RadialReview.Models.Angular.Headlines;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Utilities;
using System.Net;
using RadialReview.Api.Common;
using System;
using Microsoft.AspNetCore.Http;

namespace RadialReview.Api.V1 {
	/*[TTActionWebApiFilter]*/
	[Route("api/v1")]
	public class HeadlinesController : BaseApiController {
		private INotesProvider _notesProvider;

		public HeadlinesController(INotesProvider notesProvider, RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
		{
			_notesProvider = notesProvider;
		}

		/// <summary>
		/// Get a URL with the Notes Pad for a specific headline
		/// </summary>
		/// <param name="HEADLINE_ID"></param>
		/// <param name="showControls"></param>
		/// <param name="readOnly"></param>
		[Route("headline/notes/{HEADLINE_ID:long}")]
		[HttpGet]
		[ProducesResponseType(typeof(NotesPadResponse), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetNotes(long HEADLINE_ID, bool showControls = true, bool readOnly = false)
		{
			var headline = HeadlineAccessor.GetHeadline(GetUser(), HEADLINE_ID);
			var padId = headline.HeadlinePadId;
			if (readOnly || !PermissionsAccessor.IsPermitted(GetUser(), x => x.EditHeadline(HEADLINE_ID)))
			{
				padId = await _notesProvider.GetReadonlyUrl(headline.HeadlinePadId);
			}


			Uri notesUrl = NoteUtils.BuildURL(padId: padId,
											  showControls: showControls,
											  callerName: GetUser().GetName());

			var response = new NotesPadResponse(URL: notesUrl);

			return Ok(response);
		}

		/// <summary>
		/// Get a specific headline
		/// </summary>
		/// <param name = "HEADLINE_ID">headline ID</param>
		/// <returns>The headline</returns>
		//[GET/POST/DELETE] /headline/{id}
		[Route("headline/{HEADLINE_ID:long}")]
		[HttpGet]
		public AngularHeadline GetHeadline(long HEADLINE_ID, bool Include_Origin = false) {
			var response = new AngularHeadline(HeadlineAccessor.GetHeadline(GetUser(), HEADLINE_ID));
			if (Include_Origin && response.OriginId != 0)
				response.Origin = L10Accessor.GetL10Recurrence(GetUser(), response.OriginId, LoadMeeting.False()).NotNull(x => x.Name);
			return response;
		}

		/// <summary>
		/// Update a Headline
		/// </summary>
		/// <param name = "HEADLINE_ID">Headline ID</param>
		/// <param name = "body">Updated title</param>
		[Route("headline/{HEADLINE_ID:long}")]
		[HttpPut]
		public async Task UpdateHeadlines(long HEADLINE_ID, [FromBody] TitleModel body) {
			await HeadlineAccessor.UpdateHeadline(GetUser(), HEADLINE_ID, body.title);
		}

		/// <summary>
		/// Delete a headline
		/// </summary>
		/// <param name = "HEADLINE_ID"></param>
		/// <returns></returns>
		[Route("headline/{HEADLINE_ID:long}")]
		[HttpDelete]
		public async Task RemoveHeadlines(long HEADLINE_ID) {
			var recurId = HeadlineAccessor.GetHeadline(GetUser(), HEADLINE_ID).RecurrenceId;
			await L10Accessor.Remove(_dbContext, GetUser(), new AngularHeadline() { Id = HEADLINE_ID }, recurId, null);
		}

		/// <summary>
		/// Get all headlines owned by a user.
		/// </summary>
		/// <param name = "USER_ID"></param>
		/// <returns>List of the user's headlines</returns>
		[Route("headline/users/{USER_ID:long}")]
		[HttpGet]
		public IEnumerable<AngularHeadline> GetUserHeadlines(long USER_ID, bool Include_Origin = false) {
			var response = HeadlineAccessor.GetHeadlinesForUser(GetUser(), USER_ID).Select(x => new AngularHeadline(x));
			if (Include_Origin) {
				response = response.ToList();
				foreach (var headline in response)
					if (headline.OriginId != 0)
						headline.Origin = L10Accessor.GetL10Recurrence(GetUser(), headline.OriginId, LoadMeeting.False()).NotNull(x => x.Name);
			}

			return response;
		}

		/// <summary>
		/// Get headlines you own
		/// </summary>
		/// <returns>List of the headlines own by you</returns>
		[Route("headline/users/mine")]
		[HttpGet]
		public IEnumerable<AngularHeadline> GetMineHeadlines(bool Include_Origin = false) {
			var response = HeadlineAccessor.GetHeadlinesForUser(GetUser(), GetUser().Id).Select(x => new AngularHeadline(x));
			if (Include_Origin) {
				response = response.ToList();
				foreach (var headline in response)
					if (headline.OriginId != 0)
						headline.Origin = L10Accessor.GetL10Recurrence(GetUser(), headline.OriginId, LoadMeeting.False()).NotNull(x => x.Name);
			}

			return response;
		}
	}
}
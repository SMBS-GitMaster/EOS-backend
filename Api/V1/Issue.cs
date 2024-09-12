using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using static RadialReview.Accessors.IssuesAccessor;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Utilities;
using System.Net;
using System;
using RadialReview.Api.Common;
using Microsoft.AspNetCore.Http;

namespace RadialReview.Api.V1 {
	/*[TTActionWebApiFilter]*/
	[Route("api/v1")]
	public class IssuesController : BaseApiController {

		private INotesProvider _notesProvider;

		public IssuesController(INotesProvider notesProvider, RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
		{
			_notesProvider = notesProvider;
		}

		#region Models
		public class CreateIssueModel {
			///<summary>
			///Weekly meeting ID
			///</summary>
			[Required]
			public long meetingId { get; set; }

			///<summary>
			///Title for the issue
			///</summary>
			[Required]
			public string title { get; set; }

			///<summary>
			///Owner's user ID (Default: you)
			///</summary>
			public long? ownerId { get; set; }

			///<summary>
			///Optional issue notes (Default: none)
			///</summary>
			public string notes { get; set; }
		}

		public class UpdateIssueModelCompletion {
			///<summary>
			///Set the issue completion
			///</summary>
			public bool? complete { get; set; }
		}

		public class UpdateIssueModel {
			///<summary>
			///Title for the issue
			///</summary>
			public string title { get; set; }

			///<summary>
			///Owner's user ID
			///</summary>
			public long? ownerId { get; set; }

			/// <summary>
			/// The compartment this issue belongs to. Short Term = Weekly Meeting, Long Term = V/TO
			/// </summary>
			public IssueCompartment? compartment { get; set; }
		}

		#endregion
		#region POST
		/// <summary>
		/// Create a new issue in for a Weekly Meeting
		/// </summary>
		/// <returns>The created issue</returns>
		// Put: api/Issue/mine
		[Route("issues/create")]
		[HttpPost]
		public async Task<AngularIssue> CreateIssue([FromBody] CreateIssueModel body) {
			bool createNotePad = true;
			body.ownerId = body.ownerId ?? GetUser().Id;
			//var issue = new IssueModel() { Message = body.title, Description = body.details };
			var creation = IssueCreation.CreateL10Issue(body.title, body.notes, body.ownerId, body.meetingId);
			var success = await IssuesAccessor.CreateIssue(GetUser(), creation, createNotePad); // body.meetingId, body.ownerId.Value, issue);
			return new AngularIssue(success.IssueRecurrenceModel);
		}

		/// <summary>
		/// Move issue to VTO
		/// </summary>
		/// <returns>HTTP response 200</returns>
		// Put: api/Issues/movetovto
		[Route("issues/{ISSUE_ID:long}/movetovto/")]
		[HttpPost]
		public async Task<IActionResult> MoveToVto(long ISSUE_ID) {
			await L10Accessor.MoveIssueToVto(GetUser(), ISSUE_ID, null);
			return Ok();
		}

		/// <summary>
		/// Move issue from VTO
		/// </summary>
		/// <returns>HTTP response 200</returns>
		// Put: api/Issues/movefromvto
		[Route("issues/{ISSUE_ID:long}/movefromvto/")]
		[HttpPost]
		public async Task<IActionResult> MoveFromVto(long ISSUE_ID) {
			var vtoItem = VtoAccessor.GetVTOIssueByIssueId(GetUser(), ISSUE_ID);
			await L10Accessor.MoveIssueFromVto(GetUser(), vtoItem.Id);
			return Ok();
		}

		/// <summary>
		/// Move issue to another meeting
		/// </summary>
		/// <returns>HTTP response 200</returns>
		// Put: api/Issues/complete
		[Route("issues/{ISSUE_ID:long}/movetomeeting/{MEETING_ID:long}")]
		[HttpPost]
		public async Task<IActionResult> MoveToMeeting(long ISSUE_ID, long MEETING_ID) {
			await IssuesAccessor.CopyIssue(GetUser(), ISSUE_ID, MEETING_ID);
			return Ok();
		}

		/// <summary>
		/// Mark issue as completed
		/// </summary>
		/// <returns>HTTP response 200</returns>
		// Put: api/Issues/complete
		[Route("issues/{ISSUE_ID:long}/complete/")]
		[HttpPost]
		public async Task<IActionResult> Complete(long ISSUE_ID, [FromBody] UpdateIssueModelCompletion body) {
			//await L10Accessor.CompleteIssue(GetUser(), ISSUE_ID);
			await IssuesAccessor.EditIssue(GetUser(), ISSUE_ID, complete: body.complete ?? true);
			return Ok();
		}

		#endregion
		#region PUT
		/// <summary>
		/// Update an issue
		/// </summary>
		/// <param name = "ISSUE_ID">Issue ID</param>
		/// <returns></returns>
		[Route("issues/{ISSUE_ID:long}")]
		[HttpPut]
		public async Task EditIssue(long ISSUE_ID, [FromBody] UpdateIssueModel body) {
			await IssuesAccessor.EditIssue(GetUser(), ISSUE_ID, message: body.title, owner: body.ownerId, compartment: body.compartment);
		}

		#endregion
		#region GET
		/// <summary>
		/// Get a specific issue
		/// </summary>
		/// <param name = "ISSUE_ID">Issue ID</param>
		/// <returns>The specified issue</returns>
		// GET: api/Issue/5
		[Route("issues/{ISSUE_ID:long}")]
		[HttpGet]
		public AngularIssue Get(long ISSUE_ID) {
			var model = IssuesAccessor.GetIssue_Recurrence(GetUser(), ISSUE_ID);
			var response = new AngularIssue(model);
			return response;
		}

		/// <summary>
		/// Get all issues you own.
		/// </summary>
		/// <returns>List of your issues</returns>
		[Route("issues/users/mine")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetMineIssues() {
			List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), GetUser().Id);
			return list.Select(x => new AngularIssue(x));
		}

		/// <summary>
		/// Get all issues owned by a user.
		/// </summary>
		/// <param name = "USER_ID"></param>
		/// <returns>List of the user's issues</returns>
		[Route("issues/users/{USER_ID:long}")]
		[HttpGet]
		public IEnumerable<AngularIssue> GetUserIssues(long USER_ID) {
			List<IssueModel.IssueModel_Recurrence> list = IssuesAccessor.GetVisibleIssuesForUser(GetUser(), USER_ID);
			return list.Select(x => new AngularIssue(x));
		}

		/// <summary>
		/// Get a URL with the Notes Pad for a specific issue
		/// </summary>
		/// <param name="ISSUE_ID"></param>
		/// <param name="showControls"></param>
		/// <param name="readOnly"></param>
		/// <returns></returns>
		[Route("issues/notes/{ISSUE_ID:long}")]
		[HttpGet]
		[ProducesResponseType(typeof(NotesPadResponse), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetNotes(long ISSUE_ID, bool showControls = true, bool readOnly = false)
		{
			var issue = IssuesAccessor.GetIssue(GetUser(), ISSUE_ID);
			var padId = issue.PadId;
			if (readOnly || !PermissionsAccessor.IsPermitted(GetUser(), x => x.EditIssue(ISSUE_ID)))
			{
				padId = await _notesProvider.GetReadonlyUrl(issue.PadId);
			}

			Uri notesUrl = NoteUtils.BuildURL(padId: padId,
											  showControls: showControls,
											  callerName: GetUser().GetName());

			var response = new NotesPadResponse(URL: notesUrl);

			return Ok(response);
		}
		#endregion
	}
}
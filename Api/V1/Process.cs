using System.Collections.Generic;
using System.Linq;
using RadialReview.Accessors;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using RadialReview.Models.Angular.Process;
using Microsoft.AspNetCore.Mvc;

namespace RadialReview.Api.V1 {
	//[TTActionWebApiFilter]
	[Route("api/v1")]
	public class ProcessController : BaseApiController {
    public ProcessController(RadialReview.DatabaseModel.DatabaseContexts.RadialReviewDbContext dbContext, RedLockNet.IDistributedLockFactory redLockFactory) : base(dbContext, redLockFactory)
    {
    }

		#region Models
		public class CompleteStepModel {
			/// <summary>
			/// Completion
			/// </summary>
			[Required]
			public bool completion { get; set; }
		}

		#endregion
		#region POST
		/// <summary>
		/// Start a process
		/// </summary>
		/// <returns></returns>
		[Route("process/{PROCESS_ID:long}/execute")]
		[HttpPost]
		public async Task<AngularProcessExecution> StartProcess(long PROCESS_ID) {
			var pvm = await ProcessAccessor.StartProcessExecution(GetUser(), PROCESS_ID);
			return new AngularProcessExecution(pvm);
		}

		#endregion
		#region PUT
		// PUT: api/Todo/5
		/// <summary>
		/// Update step completion
		/// </summary>
		/// <param name = "STEP_ID">Step ID</param>
		/// <param name = "body"></param>
		/// <returns></returns>
		[Route("process/steps/{STEP_ID:long}")]
		[HttpPost]
		public async Task CompleteStep(long STEP_ID, [FromBody] CompleteStepModel body) {
			//await L10Accessor.UpdateTodo(GetUser(), id, message, null, dueDate);
			await ProcessAccessor.MarkStepCompletion(GetUser(), STEP_ID, body.completion);
		}

		#endregion
		#region GET
		/// <summary>
		/// Get a list of processes
		/// </summary>
		/// <param name = "FOLDER_ID">Filter by folder id</param>
		/// <returns></returns>
		[Route("process/list")]
		[HttpGet]
		public async Task<IEnumerable<AngularProcess>> GetProcessList(long? FOLDER_ID = null) {
			var p = await ProcessAccessor.GetAllProcesses(GetUser(), GetUser().Organization.Id);
			return p.Select(x => new AngularProcess(x)).ToList();
		}

		/// <summary>
		/// Get a list of process folders
		/// </summary>
		/// <returns></returns>
		[Route("process/folder/list")]
		[HttpGet]
		public async Task<IEnumerable<AngularProcessFolder>> GetProcessFolderList() {
			var p = await ProcessAccessor.GetAllProcessFolders(GetUser(), GetUser().Organization.Id);
			return p.Select(x => new AngularProcessFolder(x)).ToList();
		}

		/// <summary>
		/// Get a process
		/// </summary>
		/// <returns></returns>
		// GET: api/Todo/mine
		[Route("process/{PROCESS_ID:long}")]
		[HttpGet]
		public async Task<AngularProcess> GetProcess(long PROCESS_ID) {
			var p = await ProcessAccessor.GetProcess(GetUser(), PROCESS_ID);
			return new AngularProcess(p);
		}

		/// <summary>
		/// Get steps for a process
		/// </summary>
		/// <returns></returns>
		[Route("process/{PROCESS_ID:long}/steps")]
		[HttpGet]
		public async Task<IEnumerable<AngularProcessStep>> GetProcessStepList(long PROCESS_ID) {
			var p = await ProcessAccessor.GetProcess(GetUser(), PROCESS_ID);
			return p.Substeps.Select(x => new AngularProcessStep(x)).ToList();
		}

		/// <summary>
		/// Get steps for an started process
		/// </summary>
		/// <returns></returns>
		[Route("process/execution/{STARTED_PROCESS_ID:long}/steps")]
		[HttpGet]
		public async Task<IEnumerable<AngularProcessStep>> GetProcessExecutionStepList(long STARTED_PROCESS_ID) {
			var p = await ProcessAccessor.GetProcessExecution(GetUser(), STARTED_PROCESS_ID);
			return p.Substeps.Select(x => new AngularProcessStep(x)).ToList();
		}
		#endregion
	}
}
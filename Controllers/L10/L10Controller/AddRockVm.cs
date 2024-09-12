using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using RadialReview.Models.Askables;

namespace RadialReview.Core.Controllers
{
  public partial class L10Controller
  {
    public class AddRockVm {
			public long RecurrenceId { get; set; }
			public List<SelectListItem> AvailableRocks { get; set; }
			public long SelectedRock { get; set; }
			public List<RockModel> Rocks { get; set; }
			public bool AllowCreateCompanyRock { get; set; }

			public static AddRockVm CreateRock(long recurrenceId, long ownerId, string message = null, bool allowBlankRock = false) {
				var model = new RockModel() { ForUserId = ownerId, Rock = message };
				if (model.ForUserId <= 0) {
					throw new ArgumentOutOfRangeException("You must specify an accountable user id");
				}
				if (recurrenceId <= 0) {
					throw new ArgumentOutOfRangeException("You must specify a recurrence id");
				}
				if (String.IsNullOrWhiteSpace(model.Rock) && !allowBlankRock) {
					throw new ArgumentOutOfRangeException("You must specify a title for the goal");
				}
				return new AddRockVm() { SelectedRock = -3, Rocks = model.AsList(), RecurrenceId = recurrenceId, };
			}

			[Obsolete("Remove", true)]
			public static AddRockVm CreateRock(long recurrenceId, RockModel model, bool allowBlankRock = false) {
				if (model == null) {
					throw new ArgumentNullException("model", "Goal was null");
				}
				if (model.ForUserId <= 0) {
					throw new ArgumentOutOfRangeException("You must specify an accountable user id");
				}
				if (model.OrganizationId <= 0) {
					throw new ArgumentOutOfRangeException("You must specify an organization id");
				}
				if (recurrenceId <= 0) {
					throw new ArgumentOutOfRangeException("You must specify a recurrence id");
				}
				if (String.IsNullOrWhiteSpace(model.Rock) && !allowBlankRock) {
					throw new ArgumentOutOfRangeException("You must specify a title for the goal");
				}
				return new AddRockVm() { SelectedRock = -3, Rocks = model.AsList(), RecurrenceId = recurrenceId, };
			}
		}
	}
}

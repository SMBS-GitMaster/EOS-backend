using FluentResults;
using System;

namespace RadialReview.Utilities.Extensions {
	public static class PermissionsUtilityExtensions {

		public static Result<PermissionsUtility> ViewL10RecurrenceWrapper(this PermissionsUtility permissions,long recurrenceId) 
		{
			try {
				return Result.Ok(permissions.ViewL10Recurrence(recurrenceId));
			} catch (Exception ex) {
				return Result.Fail(ex.Message);
			}
		}

	}
}

using RadialReview.Exceptions;
using RadialReview.Models.L10;
using System;

namespace RadialReview.Models.Todo {
	public class TodoModalVM {
		public long Id { get; set; }
		public string Message { get; set; }
		public DateTime CreateTime { get; set; }
		public DateTime DueDate { get; set; }
		public long AccountableUserId { get; set; }
		public TodoType TodoType { get; set; }
		public long? ForRecurrenceId { get; set; }
		public DateTime? CloseTime { get; set; }
		public bool Completed { get; set; }
		public bool Localize { get; set; }

		public TodoModalVM() { }
		public TodoModalVM(TodoModel todo, bool localize) {
			Id = todo.Id;
			Message = todo.Message;
			CreateTime = todo.CreateTime;
			DueDate = todo.DueDate;
			AccountableUserId = todo.AccountableUserId;
			TodoType = todo.TodoType;
			ForRecurrenceId = todo.ForRecurrenceId;
			Completed = todo.CompleteTime != null;
			Localize = localize;
		}
		public ForModel GetListSource() {
			if (this.ForRecurrenceId.HasValue && this.ForRecurrenceId > 0) {
				return RadialReview.ForModel.Create<L10Recurrence>(this.ForRecurrenceId.Value);
			} else if (this.ForRecurrenceId == null && this.TodoType == TodoType.Personal) {
				return RadialReview.ForModel.Create<UserOrganizationModel>(this.AccountableUserId);
			} else {
				throw new PermissionsException("Unhandled List Source.");
			}
		}
	}
}
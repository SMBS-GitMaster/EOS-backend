using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using System;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
  public class IMeetingRockHookUpdates
  {
    public DateTime? DeleteTime { get; set; } = null;
  }
  public interface IMeetingRockHook : IHook {
		Task AttachRock(ISession s,UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock);
		Task DetachRock(ISession s, RockModel rock,long recurrenceId, IMeetingRockHookUpdates updates);
		Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock);
	}

	public interface IMeetingMeasurableHook : IHook {
		Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable);
		Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId);
    Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId, long l10MeasurableId) => Task.FromResult(DetachMeasurable(s, caller , measurable , recurrenceId));

  }

    public interface IMeetingTodoHook : IHook {
        Task AttachTodo(ISession s, UserOrganizationModel caller, TodoModel todo, bool ignoreNotification = false);
        Task DetachTodo(ISession s, UserOrganizationModel caller, TodoModel todo);
    }

  public interface IMeetingRatingHook: IHook
  {
    Task ShowHiddeRating(UserOrganizationModel caller, L10Recurrence l10Recurrence, bool displayRating);
    Task FillUserRating(UserOrganizationModel caller, L10Recurrence l10Recurrence, long userId, HotChocolate.Optional<decimal?> rating);
  }
}

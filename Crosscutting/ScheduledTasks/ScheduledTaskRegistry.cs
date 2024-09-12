namespace RadialReview.Crosscutting.ScheduledTasks {
	public class ScheduledTaskRegistry {
		public static IScheduledTaskHandler[] Handlers = new IScheduledTaskHandler[]{
			new ChargeAccountHandler(),
			new WebReqeustHandler()
		};
	}
}

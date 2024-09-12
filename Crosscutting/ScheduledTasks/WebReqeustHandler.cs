using System.Net.Http;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.ScheduledTasks {
	public class WebReqeustHandler : IScheduledTaskHandler {
		public async Task Handle(ScheduledTaskHandlerData data) {
			using (var webClient = data.ClientFactory.CreateClient()) {
				await webClient.GetStringAsync(data.Url);
			}
		}

		public bool ShouldHandle(ScheduledTaskHandlerData data) {
			return !data.IsTest;
		}
	}
}

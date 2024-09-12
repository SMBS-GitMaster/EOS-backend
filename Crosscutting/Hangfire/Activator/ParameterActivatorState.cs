using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using RadialReview.Crosscutting.Hangfire.Jobs;
using System;

namespace RadialReview.Crosscutting.Hangfire.Activator {
	public class ParameterActivatorState : IDisposable {
		public ParameterActivatorState(PerformContext performContext, IServiceScope serviceScope, JobInfo jobInfo) {
			PerformContext = performContext;
			ServiceScope = serviceScope;
			JobInfo = jobInfo;
		}

		public PerformContext PerformContext { get; private set; }
		public IServiceScope ServiceScope { get; private set; }
		public JobInfo JobInfo { get; private set; }

		public void Dispose() {
			ServiceScope?.Dispose();
		}
	}
}

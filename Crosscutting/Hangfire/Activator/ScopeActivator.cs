using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using RadialReview.Crosscutting.Hangfire.Activator;
using RadialReview.Crosscutting.Hangfire.Jobs;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RadialReview.Hangfire.Activator {
	public class ServiceScopeActivator : JobActivator {

		private static List<IParameterActivator> _parameterActivators = ReflectionUtility.GetAllImplementationsOfInterface<IParameterActivator>().ToList();

		public class ServiceScopeJobActivatorScope : JobActivatorScope {
			private readonly ParameterActivatorState _state;
			private readonly List<IDisposable> _disposables = new List<IDisposable>();

			public ServiceScopeJobActivatorScope(ParameterActivatorState state) {
				//_activator = activator;
				_state = state;
				_disposables.Add(state);
			}


			public override object Resolve(Type type) {
				var ctors = type.GetConstructors();
				object obj;
				if (ctors.Length == 0) {
					obj = System.Activator.CreateInstance(type);
				} else if (ctors.Length == 1) {
					var defaultParameters = ctors[0].GetParameters().Select(p => ActivateParameter(_state, p)).ToArray();
					obj = System.Activator.CreateInstance(type, defaultParameters);
				} else {
					throw new Exception("Cannot activate instance of type " + type.Name + ". Hangfire activation requires either a single constructor or no constructors.");
				}
				IDisposable disposable = obj as IDisposable;
				if (disposable != null) {
					_disposables.Add(disposable);
				}

				return obj;
			}

			public override void DisposeScope() {

				foreach (IDisposable disposable in _disposables) {
					disposable.Dispose();
				}
			}
		}

		private readonly IServiceScopeFactory scopeFactory;

		public ServiceScopeActivator(IServiceScopeFactory scopeFactory) {
			this.scopeFactory = scopeFactory;
		}

		public override JobActivatorScope BeginScope(JobActivatorContext context) {
			throw new NotImplementedException();
		}

		public override JobActivatorScope BeginScope(PerformContext pc) {
			var context = new JobActivatorContext(pc.Connection, pc.BackgroundJob, pc.CancellationToken);

			var jobData = new JobInfo();
			try {
				jobData = HangfireJobUtility.GetJobData(pc, pc.BackgroundJob.Id);
			} catch (Exception e) {
			}


			var scope = scopeFactory.CreateScope();
			var args = context.BackgroundJob.Job.Args as object[];
			if (args == null)
				throw new NotImplementedException("Args is expected to an object[]. Service injector will not work.");

			var state = new ParameterActivatorState(pc, scope, jobData);

			foreach (var p in context.BackgroundJob.Job.Method.GetParameters()) {
				//Parameters must be decorated with [ActivateParameter]
				if (p.GetCustomAttributes<ActivateParameterAttribute>().Any()) {
					var parameterValue = context.GetJobParameter<object>(p.Name);
					if (parameterValue == null) {
						args[p.Position] = ActivateParameter(state, p);
					}
				}
			}
			return new ServiceScopeJobActivatorScope(/*this,*/ state);

		}

		private static object ActivateParameter(ParameterActivatorState state, ParameterInfo pi) {
			var activator = _parameterActivators.FirstOrDefault(pa => pa.ShouldApply(state, pi));
			if (activator == null)
				return null;
			return activator.Activate(state, pi);
		}

		//private static void ApplyAttribute(JobActivatorContext context, IServiceScope scope, object[] args, ParameterInfo p) {
		//	if (p.GetCustomAttributes<InjectService>().Any()) {
		//		if (found == null) {
		//			args[p.Position] = scope.ServiceProvider.GetService(p.ParameterType);
		//		}
		//	}
		//}
	}
}

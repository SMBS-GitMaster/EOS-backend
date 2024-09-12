using Microsoft.AspNetCore.SignalR;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using log4net;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility {
		public interface IBaseUpdater {
			void Call(string method, params object[] arguments);
			Task CallImmediately(string method, params object[] arguments);
		}

		public abstract class BaseUpdater<T> : IBaseUpdater where T : BaseUpdater<T> {
			protected RealTimeUtility backingRealTimeUtility;

			protected BaseUpdater(RealTimeUtility rt) {
				backingRealTimeUtility = rt;
			}
			protected IEnumerable<UpdaterSettings> GenerateUpdaterSettings() {
				return GenerateUpdaterSettingsImpl();
			}

			protected abstract IEnumerable<UpdaterSettings> GenerateUpdaterSettingsImpl();
			protected abstract IClientProxy ConstructProxy(IHubClients<IClientProxy> clients, UpdaterSettings settings, IReadOnlyList<string> excludedConnectionIds);

			public T Update(IAngularId item, Func<UpdaterSettings, UpdaterSettings> overridesSettings = null, int insert = int.MaxValue) {
				return UpdateHelper(_ => item, overridesSettings, insert);
			}

			public T Update(Func<IAngularId> item, Func<UpdaterSettings, UpdaterSettings> overridesSettings = null, int insert = int.MaxValue) {
				return UpdateHelper(_ => item(), overridesSettings, insert);
			}
			public T Update(Func<UpdaterSettings, IAngularId> item, Func<UpdaterSettings, UpdaterSettings> overridesSettings = null, int insert = int.MaxValue) {
				return UpdateHelper(item, overridesSettings, insert);
			}

			private T UpdateHelper(Func<UpdaterSettings, IAngularId> item, 
								   Func<UpdaterSettings, UpdaterSettings> overridesSettings = null, 
								   int insert = int.MaxValue) 
			{
				overridesSettings = overridesSettings ?? new Func<UpdaterSettings, UpdaterSettings>(x => x);
				backingRealTimeUtility.AddAction(() => {
					try {
						foreach (var settings in GenerateUpdaterSettings()) {
							var settingsOverride = overridesSettings(settings.Clone());
							var updater = GetUpdater<RealTimeHub>(settingsOverride);
							updater.Add(item(settings));
						}
					} catch (ArgumentException ae) {
						log.Error("There was an error calling the method UpdateHelper: ", ae);
					}
				}, insert);
				return (T)this;
			}

			public void Call(string method, params object[] arguments) {
				backingRealTimeUtility.AddAction(async () => {
					try {
						foreach (var settings in GenerateUpdaterSettings()) {
							await GetProxy<RealTimeHub>(settings).SendCoreAsync(method, arguments);
						}
					} catch (ArgumentException ae) {
						log.Error ( "There was an error calling the method " + method + ": ", ae );
					}
				});
			}
			public async Task CallImmediately(string method, params object[] arguments) {
				try {
					foreach (var settings in GenerateUpdaterSettings()) {
						await GetProxy<RealTimeHub>(settings).SendCoreAsync(method, arguments);
					}
				} catch (ArgumentException ae) {
					log.Error ( "There was an error calling the method " + method + ": ", ae );
				}
			}

			private IClientProxy GetProxy<HUB>(UpdaterSettings settings) where HUB : Hub {
				if (!backingRealTimeUtility._proxies.ContainsKey(settings)) {
					backingRealTimeUtility._proxies[settings] = ConstructProxy(
						GetHubContext<HUB>().Clients,
						settings,
						backingRealTimeUtility.GetExcludedUsers(settings)
					);
				}
				return backingRealTimeUtility._proxies[settings];
			}

			protected AngularUpdate GetUpdater<HUB>(UpdaterSettings key) where HUB : Hub {
				if (!backingRealTimeUtility._updaters.ContainsKey(key)) {
					GetProxy<HUB>(key); //Warm cache
					backingRealTimeUtility._updaters[key] = new AngularUpdate();
				}
				return backingRealTimeUtility._updaters[key];
			}

			protected Func<UpdaterSettings, UpdaterSettings> ForceNoSkip(bool forceNoSkip) {
				if (forceNoSkip == true) {
					return new Func<UpdaterSettings, UpdaterSettings>(x => {
						x.ApplySkip = false;
						return x;
					});
				}
				return null;
			}
		}
	}
}

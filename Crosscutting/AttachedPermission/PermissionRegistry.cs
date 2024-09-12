using NHibernate;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadialReview.Api;

namespace RadialReview.Crosscutting.AttachedPermission {
	public class PermissionRegistry {

		private static PermissionRegistry _Singleton { get; set; }
		private List<IAttachedPermissionHandler> _AttachedPermissionHandler { get; set; }
		private static object lck = new object();

		private PermissionRegistry() {
			lock (lck) {
				_AttachedPermissionHandler = new List<IAttachedPermissionHandler>();
			}
		}

		public static void RegisterPermission(IAttachedPermissionHandler permission) {
			var hooks = GetSingleton();
			lock (lck) {
				hooks._AttachedPermissionHandler.Add(permission);
			}
		}

		public static PermissionRegistry GetSingleton() {
			if (_Singleton == null)
				_Singleton = new PermissionRegistry();
			return _Singleton;
		}

		private static async Task<IAttachedPermissionHandler> GetHandler(ISession s, PermissionsUtility perm, Type type) {
			var list = GetSingleton()._AttachedPermissionHandler;
			foreach (var handler in list) {
				var handlerInterface = handler.GetType().GetInterfaces().SingleOrDefault(x => x.Name.StartsWith(nameof(IAttachedPermissionHandler)) && x.GenericTypeArguments.Length == 1);
				if (handlerInterface != null) {
					if (handlerInterface.GenericTypeArguments[0] == type)
						return handler;
				}
			}
			return null;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities
{
    public static class ReflectionUtility {

		public static IEnumerable<T> GetAllImplementationsOfInterface<T>() {
			var types = typeof(T).Assembly.GetTypes()
				.Where(x => !x.IsInterface && !x.IsAbstract)
				.Where(x => typeof(T).IsAssignableFrom(x));
			var objs = types.Select(x => (T)Activator.CreateInstance(x));
			return objs.ToList();
		}


		public static IEnumerable<T> GetAllImplementationsOfInterfaceConstructWithDefaultParameters<T>() {
			var types = typeof(T).Assembly.GetTypes()
				.Where(x => !x.IsInterface && !x.IsAbstract)
				.Where(x => typeof(T).IsAssignableFrom(x));


			var objs = types.Select(x => {
				var ctors = x.GetConstructors();
				if (ctors.Length == 0)
					return (T)Activator.CreateInstance(x);
				if (ctors.Length == 1) {
					var defaultParameters = ctors[0].GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
					return (T)Activator.CreateInstance(x, defaultParameters);
				}
				throw new Exception("Cannot activate instance of type " + typeof(T).Name + ". Activation requires either a single constructor or no constructors.");
			});
			return objs.ToList();
		}

	}
}

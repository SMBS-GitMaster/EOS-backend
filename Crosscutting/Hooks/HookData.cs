using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RadialReview.Crosscutting.Hooks {
	public class HookData {
		protected Dictionary<string, object> data { get; set; }


        public static void LoadFrom(ReadOnlyHookData a) {
			Clear();
			foreach (var d in a.ToDictionary())
                SetData(d.Key, d.Value);
        }

		protected HookData() {
			data = new Dictionary<string, object>();
		}

		protected void _SetData<T>(string key, T value) {
			data[key] = (T)value;
		}

		protected T _GetData<T>(string key, T deflt = default(T))  {
			if (data.ContainsKey(key))
				return (T)data[key];
			return deflt;
		}

		protected static void Clear() {
			Thread.SetData(Thread.GetNamedDataSlot("HookData"), null);
		}

		[Untested("Unit test me")]
		protected static HookData _GetThreadSingleton() {
			var found = ((HookData)Thread.GetData(Thread.GetNamedDataSlot("HookData")));
			if (found == null) {
				found = new HookData();
				Thread.SetData(Thread.GetNamedDataSlot("HookData"), found);
			}
			return found;
		}


        [Untested("Unit test me")]
		public static void SetData<T>(string key, T value)  {
			_GetThreadSingleton()._SetData(key, value);
		}


		
		public static ReadOnlyHookData ToReadOnly() {
			return ReadOnlyHookData.FromDictionary(_GetThreadSingleton().data);
		}
		
	}


	public class ReadOnlyHookData {

		protected Dictionary<string, object> data { get; set; }

		public static ReadOnlyHookData FromDictionary(Dictionary<string, object> dict) {
			return new ReadOnlyHookData() {
				data = dict.ToDictionary(x => x.Key, x => x.Value)
			};
		}

		public Dictionary<string, object> ToDictionary() {
			return data.ToDictionary(x => x.Key, x => x.Value);
		}


		public T GetData<T>(string key, T deflt = default(T))  {
			if (data.ContainsKey(key))
				return (T)data[key];
			return deflt;
		}
	}
}

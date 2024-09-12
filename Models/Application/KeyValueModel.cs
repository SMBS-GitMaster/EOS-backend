using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using RadialReview.Models.Interfaces;
using System;

namespace RadialReview {
	public class KeyValueModel : ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual String K { get; set; }
		public virtual String V { get; set; }
		public virtual DateTime Created { get; set; }

		public KeyValueModel() {
			Created = DateTime.UtcNow;
		}

		public KeyValueModel(KVFormat keyFormat, string value) : this() {
			K = keyFormat.Key;
			V = value;
		}

		public class KVFormat {
			public KVFormat(string key) {
				Key = key;
			}
			public string Key { get; private set; }
		}
	}

	public class KeyValueModelMap : ClassMap<KeyValueModel> {
		public KeyValueModelMap() {
			Id(x => x.Id);
			Map(x => x.K).Index("IDX_KEYVALUE_KEY");
			Map(x => x.V);
			Map(x => x.Created);
		}
	}


	public class KVFormats {
		public KeyValueModel.KVFormat ShowEosToolsForOrg(long orgId) {
			return new KeyValueModel.KVFormat("ShowEosToolsForOrg_" + orgId);
		}

	}
	public static class KeyValueExtensions {
		public class Switch { public bool On { get; set; } }

		public static bool GetSwitch(this ISession s, Func<KVFormats, KeyValueModel.KVFormat> format, bool deflt = false) {
			var found = s.GetKeyValue<Switch>(format);
			if (found == null)
				return deflt;
			return found.On;
		}
		public static void SetSwitch(this ISession s, Func<KVFormats, KeyValueModel.KVFormat> format, bool value) {
			s.SetKeyValue<Switch>(format, new Switch() { On = value });
		}


		public static T GetKeyValue<T>(this ISession s, Func<KVFormats, KeyValueModel.KVFormat> format) where T : class {
			if (format == null) {
				return null;
			}

			var f = format(new KVFormats());
			var found = s.QueryOver<KeyValueModel>().Where(x => x.K == f.Key).Take(1).SingleOrDefault();
			if (found == null || found.V == null)
				return null;
			try {
				return JsonConvert.DeserializeObject<T>(found.V);
			} catch (Exception e) {
				return null;
			}
		}
		public static void SetKeyValue<T>(this ISession s, Func<KVFormats, KeyValueModel.KVFormat> format, T value) where T : class {
			if (format == null)
				throw new NullReferenceException("format was null");
			var f = format(new KVFormats());
			var found = s.QueryOver<KeyValueModel>().Where(x => x.K == f.Key).Take(1).SingleOrDefault();
			string serialized = null;
			if (value != null)
				serialized = JsonConvert.SerializeObject(value, Formatting.Indented);
			if (found == null) {
				s.Save(new KeyValueModel(f, serialized));
			} else {
				found.V = serialized;
				s.Update(found);
			}
		}
	}
}

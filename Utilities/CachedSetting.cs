using NHibernate;
using System;

namespace RadialReview.Utilities {
	public class CachedSetting<T> {
		public CachedSetting(TimeSpan lifeTime, Func<T> generator) {
			Generator = generator;
			Lifetime = lifeTime;
			LastGenerated = DateTime.MinValue;
		}

		public DateTime LastGenerated { get; private set; }
		private Func<T> Generator { get; set; }
		private T LastValue { get; set; }
		private TimeSpan Lifetime { get; set; }

		public T Get() {
			if (DateTime.UtcNow - LastGenerated > Lifetime) {
				LastValue = Generator();
				LastGenerated = DateTime.UtcNow;
			}
			return LastValue;
		}

		public void ResetCache() {
			LastGenerated = DateTime.MinValue;
		}
	}


	public class CachedDatabaseSetting<T> {
		public CachedDatabaseSetting(TimeSpan lifeTime, Func<ISession, T> generator) {
			Generator = generator;
			Lifetime = lifeTime;
			LastGenerated = DateTime.MinValue;
		}

		public DateTime LastGenerated { get; private set; }
		private Func<ISession, T> Generator { get; set; }
		private T LastValue { get; set; }
		private TimeSpan Lifetime { get; set; }

		public T Get(ISession s) {
			if (DateTime.UtcNow - LastGenerated > Lifetime) {
				LastValue = Generator(s);
				LastGenerated = DateTime.UtcNow;
			}
			return LastValue;
		}
		public void ResetCache() {
			LastGenerated = DateTime.MinValue;
		}
	}

}
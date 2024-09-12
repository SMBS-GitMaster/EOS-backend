using Microsoft.Extensions.Primitives;
using System;

namespace RadialReview.Utilities.RealTime {
	public partial class RealTimeUtility {


		public struct UpdaterSettings {

			public const string DEFAULT_BATCH = "DEFAULT_BATCH";
			public const string DEFAULT_KIND = "DEFAULT_KIND";

			public long KeyId { get; private set; }
			public StringValues GroupKeys { get; private set; }
			public string Kind { get; private set; }

			public string BatchName { get; set; }
			public bool ApplySkip { get; set; }

			private object GetTuple() {
				return Tuple.Create(Kind, GroupKeys, BatchName, ApplySkip);
			}
			public override int GetHashCode() {
				return GetTuple().GetHashCode();
			}
			public override bool Equals(object obj) {
				return obj is UpdaterSettings other && (GetTuple().Equals(other.GetTuple()));
			}

			public UpdaterSettings Clone() {
				return this;
			}

			public UpdaterSettings(string kind, StringValues groupKeys, bool applySkip, string batchName, long? keyId) {
				if (groupKeys.Count == 0)
					throw new ArgumentOutOfRangeException(nameof(groupKeys));
				GroupKeys = groupKeys;
				Kind = kind ?? DEFAULT_KIND;
				ApplySkip = applySkip;
				BatchName = batchName;
				KeyId = keyId ?? long.MinValue;
			}

			public static UpdaterSettings Create(string kind, string groupKey) {
				if (groupKey == null)
					throw new ArgumentNullException(nameof(groupKey));
				return new UpdaterSettings(kind, groupKey, true, DEFAULT_BATCH, null);
			}
			public static UpdaterSettings Create(string kind, string[] groupKeys) {
				if (groupKeys == null)
					throw new ArgumentNullException(nameof(groupKeys));
				if (groupKeys.Length == 0)
					throw new ArgumentOutOfRangeException(nameof(groupKeys));
				return new UpdaterSettings(kind, groupKeys, true, DEFAULT_BATCH, null);
			}
			public static UpdaterSettings Create(string kind, string groupKey, long keyId) {
				if (groupKey == null)
					throw new ArgumentNullException(nameof(groupKey));
				return new UpdaterSettings(kind, groupKey, true, DEFAULT_BATCH, keyId);
			}


			public UpdaterSettings ForceNoSkip() {
				var c = Clone();
				c.ApplySkip = false;
				return c;
			}
		}



	}
}

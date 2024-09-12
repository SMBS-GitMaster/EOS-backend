using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Utilities.DataTypes {
	public class DefaultDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>, IDictionary<K, V> {
		public IEnumerable<K> Keys {
			get {
				var res = Backing.Keys.ToList();
				if (NullKeysValueSet) {
					res.Add(default(K));
				}
				return res;
			}
		}
		private bool NullKeysValueSet { get; set; }
		private V NullKeysValue { get; set; }
		public ConcurrentDictionary<K, V> Backing { get; private set; }

		//Key,Value,=DefaultValue
		public Func<K, V> DefaultFunction { get; private set; }
		/// <summary>
		/// Key,OldValue,AddedValue,=NewValue
		/// </summary>
		public Func<K, V, V, V> MergeFunction { get; private set; }


		/// <summary>
		/// Creates a default dictionary with mergeFunction as an overwrite
		/// </summary>
		/// <param name="defaultFunc"></param>
		public DefaultDictionary(Func<K, V> defaultFunc) : this(defaultFunc, (k, old, add) => add) {
		}

		public DefaultDictionary(Func<K, V> defaultFunc, Func<K, V, V, V> mergeFunc) {
			DefaultFunction = defaultFunc ?? new Func<K, V>(x => default(V));
			MergeFunction = mergeFunc;
			Backing = new ConcurrentDictionary<K, V>();
		}


		public V this[K key] {
			get {
				if (key == null) {
					if (NullKeysValueSet == false) {
						NullKeysValueSet = true;
						NullKeysValue = DefaultFunction(key);
					}
					return NullKeysValue;
				}


				if (Backing.ContainsKey(key)) {
					return Backing[key];
				} else {
					var defaultValue = DefaultFunction(key);
					Backing[key] = defaultValue;
					return defaultValue;
				}
			}
			set {
				if (key == null) {
					NullKeysValueSet = true;
					NullKeysValue = value;
					return;
				}

				Backing[key] = value;
			}
		}

		public V Merge(K key, V value) {
			var merged = MergeFunction(key, this[key], value);
			this[key] = merged;
			return merged;
		}


		public IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
			var res = Backing.ToList();
			if (NullKeysValueSet) {
				res.Add(new KeyValuePair<K, V>(default(K), NullKeysValue));
			}
			return res.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			var res = Backing.ToList();
			if (NullKeysValueSet) {
				res.Add(new KeyValuePair<K, V>(default(K), NullKeysValue));
			}
			return res.GetEnumerator();
		}


		ICollection<K> IDictionary<K, V>.Keys {
			get {
				var res = ((IDictionary<K, V>)Backing).Keys;
				if (NullKeysValueSet)
					res.Add(default(K));
				return res;
			}
		}
		public ICollection<V> Values {
			get {
				var res = ((IDictionary<K, V>)Backing).Values;
				if (NullKeysValueSet)
					res.Add(NullKeysValue);
				return res;
			}
		}
		public int Count {
			get {
				return ((IDictionary<K, V>)Backing).Count + (NullKeysValueSet ? 1 : 0);
			}
		}
		public bool IsReadOnly { get { return ((IDictionary<K, V>)Backing).IsReadOnly; } }
		public bool ContainsKey(K key) {
			if (key == null)
				return NullKeysValueSet;

			return ((IDictionary<K, V>)Backing).ContainsKey(key);
		}

		public void Add(K key, V value) {
			if (key == null) {
				NullKeysValueSet = true;
				NullKeysValue = value;
				return;
			}

			((IDictionary<K, V>)Backing).Add(key, value);
		}

		public bool Remove(K key) {
			if (key == null) {
				if (NullKeysValueSet == true) {
					NullKeysValueSet = false;
					NullKeysValue = default(V);
					return true;
				}
				return false;
			}
			return ((IDictionary<K, V>)Backing).Remove(key);
		}

		public bool TryGetValue(K key, out V value) {
			if (key == null) {
				if (NullKeysValueSet == true) {
					value = NullKeysValue;
					return true;
				}
				value = default(V);
				return false;
			}

			return ((IDictionary<K, V>)Backing).TryGetValue(key, out value);
		}

		public void Add(KeyValuePair<K, V> item) {
			if (item.Key == null) {
				NullKeysValueSet = true;
				NullKeysValue = item.Value;
				return;
			}
			((IDictionary<K, V>)Backing).Add(item);
		}

		public void Clear() {
			NullKeysValueSet = false;
			NullKeysValue = default(V);
			((IDictionary<K, V>)Backing).Clear();
		}

		public bool Contains(KeyValuePair<K, V> item) {
			if (item.Key == null) {
				return NullKeysValueSet && Object.Equals(item.Value, NullKeysValue);
			}
			return ((IDictionary<K, V>)Backing).Contains(item);
		}

		public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) {
			((IDictionary<K, V>)Backing).CopyTo(array, arrayIndex);
			if (NullKeysValueSet) {
				array[arrayIndex + Backing.Count] = new KeyValuePair<K, V>(default(K), NullKeysValue);
			}
		}

		public bool Remove(KeyValuePair<K, V> item) {
			if (item.Key == null) {
				if (NullKeysValueSet && object.Equals(item.Value, NullKeysValue)) {
					NullKeysValueSet = false;
					NullKeysValue = default(V);
					return true;
				}
				return false;
			}
			return ((IDictionary<K, V>)Backing).Remove(item);
		}
	}
}

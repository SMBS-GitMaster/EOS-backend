using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview {
	public enum SetStatus {

	}

	public class SetUtility {
		public class AddedRemoved<T> : IEnumerable<AddedRemoved<T>.SetItem> {


			public class SetItem {
				public SetItem(T item, bool added, bool removed, bool changed, bool inOldValues, bool inNewValues) {
					Item = item;
					Added = added;
					Removed = removed;
					Changed = changed;
					InOldValues = inOldValues;
					InNewValues = inNewValues;
				}

				public T Item { get; private set; }
				public bool Added { get; private set; }
				public bool Removed { get; private set; }
				public bool Changed { get; private set; }
				public bool InOldValues { get; private set; }
				public bool InNewValues { get; private set; }

			}

			public IEnumerable<T> OldValues { get; set; }
			public IEnumerable<T> NewValues { get; set; }
			public IEnumerable<T> AddedValues { get; set; }
			public IEnumerable<T> RemovedValues { get; set; }
			public IEnumerable<T> RemainingValues { get; set; }

			public bool AreSame() {
				return !AddedValues.Any() && !RemovedValues.Any();
			}
			public void PrintDifference(int spaces = 0) {
				Console.WriteLine(GetStringDifference(spaces));
			}

			public string GetStringDifference(int spaces = 0) {
				var space = new String(' ', spaces);
				var builder = "";
				if (AddedValues.Any()) {
					builder += (space + "Added:") + "\n";
					foreach (var a in AddedValues) {
						builder += (space + "  - " + a.ToString()) + "\n";
					}
				}
				if (RemovedValues.Any()) {
					builder += (space + "Removed:") + "\n";
					foreach (var a in RemovedValues) {
						builder += (space + "  - " + a.ToString()) + "\n";
					}
				}
				return builder;
			}

			public IEnumerator<SetItem> GetEnumerator() {
				foreach (var a in AddedValues) {
					yield return new SetItem(a, true, false, true, false, true);
				}
				foreach (var a in RemainingValues) {
					yield return new SetItem(a, false, false, false, true, true);
				}
				foreach (var a in RemovedValues) {
					yield return new SetItem(a, false, true, true, true, false);
				}
				yield break;
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return (IEnumerator)this.GetEnumerator();
			}
		}

		public class AddedRemoved<T, U> {

			public IEnumerable<U> AddedValues { get; set; }
			public IEnumerable<T> RemovedValues { get; set; }

			public bool AreSame() {
				return !AddedValues.Any() && !RemovedValues.Any();
			}
		}


		public static void AssertEqual<T>(IEnumerable<T> expected, IEnumerable<T> found, string additionaErrorInfo = null, bool inconclusive = false) {
			var res = AddRemove(found, expected);
			var finalErrors = new List<string>();
			if (res.AddedValues.Any()) {
				Console.WriteLine("Were Added:");
				foreach (var i in res.AddedValues) {
					Console.WriteLine("\t" + i.ToString());
				}
				finalErrors.Add("Found " + res.AddedValues.Count() + " additional items. " + additionaErrorInfo ?? "");

			}
			if (res.RemovedValues.Any()) {
				Console.WriteLine("Were Removed:");
				foreach (var i in res.RemovedValues) {
					Console.WriteLine("\t" + i.ToString());
				}
				finalErrors.Add("Expected " + res.RemovedValues.Count() + " additional items. " + additionaErrorInfo ?? "");
			}
			if (finalErrors.Any()) {
				var err = string.Join("\n", finalErrors);
				if (inconclusive)
					throw new Exception("INCONCLUSIVE:" + err);
				throw new Exception(err);
			}
		}




		public static AddedRemoved<T> AddRemove<T>(IEnumerable<T> oldValues, IEnumerable<T> newValues) {
			return AddRemove(oldValues, newValues, x => x);
		}

		public static AddedRemoved<object> AddRemoveBase(IEnumerable oldValues, IEnumerable newValues, Func<object, object> comparison) {
			var newEnum = newValues as object[] ?? newValues.Cast<object>().ToArray();
			var oldEnum = oldValues as object[] ?? oldValues.Cast<object>().ToArray();

			var removed = oldEnum.Where(o => !newEnum.Any(n => comparison(o).Equals(comparison(n)))).ToList();
			var added = newEnum.Where(n => !oldEnum.Any(o => comparison(o).Equals(comparison(n)))).ToList();


			return new AddedRemoved<object>() {
				AddedValues = added,
				RemovedValues = removed,
				OldValues = oldEnum,
				NewValues = newEnum
			};
		}


		public static AddedRemoved<T> AddRemove<T, E>(IEnumerable<T> oldValues, IEnumerable<T> newValues, Func<T, E> comparison) {
			var oldEnum = oldValues as IList<T> ?? oldValues.ToList();
			var newEnum = newValues as IList<T> ?? newValues.ToList();

			var removed = oldEnum.Where(o => !newEnum.Any(n => comparison(o).Equals(comparison(n)))).ToList();
			var added = newEnum.Where(n => !oldEnum.Any(o => comparison(o).Equals(comparison(n)))).ToList();
			var remain = oldEnum.Where(o => newEnum.Any(n => comparison(o).Equals(comparison(n)))).ToList();

			return new AddedRemoved<T>() {
				AddedValues = added,
				RemovedValues = removed,
				OldValues = oldEnum,
				NewValues = newEnum,
				RemainingValues = remain
			};
		}
		public static AddedRemoved<T, U> AddRemove<T, U, V>(IEnumerable<T> oldValues, Func<T, V> convertOld, IEnumerable<U> newValues, Func<U, V> convertNew) {

			var oldEnum = oldValues as IList<T> ?? oldValues.ToList();
			var newEnum = newValues as IList<U> ?? newValues.ToList();

			var removed = oldEnum.Where(o => !newEnum.Any(n => convertOld(o).Equals(convertNew(n)))).ToList();
			var added = newEnum.Where(n => !oldEnum.Any(o => convertOld(o).Equals(convertNew(n)))).ToList();



			return new AddedRemoved<T, U>() {
				AddedValues = added,
				RemovedValues = removed,
			};
		}

	}
}

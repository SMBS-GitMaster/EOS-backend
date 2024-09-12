using NHibernate;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Interfaces;
using RadialReview.Reflection;
using RadialReview.Utilities.RealTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace RadialReview.Accessors {
	public partial class BaseAccessor {
		protected static void ApplyInsert<T>(ISession s, IEnumerable<T> items, T insertItem, int insertAtIndex, Expression<Func<T, int>> orderVariable, Action<ReorderMeta<T>> onReordered) where T : ILongIdentifiable {
			Reordering.CreateInsert(items, insertItem, insertAtIndex, orderVariable)
						.ApplyInsert(s, onReordered);
		}

		protected class Reordering {
			private Reordering() { }
			public static ReorderingRecurrence<T> CreateRecurrence<T>(IEnumerable<T> items, long TId, long? recurrenceId, int oldOrder, int newOrder, Expression<Func<T, int>> orderVariable, Expression<Func<T, long>> idVariable) where T : ILongIdentifiable {
				var ordered = items.OrderBy(orderVariable.Compile()).ToList();
				return new ReorderingRecurrence<T> {
					Items = ordered,
					OldOrder = oldOrder,
					NewOrder = newOrder,
					SelectedId = TId,
					RecurrenceId = recurrenceId,
					OrderVariable = orderVariable,
					IdVariable = idVariable,
				};
			}
			public static Reordering<T> CreateReorder<T>(IEnumerable<T> items, long selectedId, int oldOrder, int newOrder, Expression<Func<T, int>> orderVariable) where T : ILongIdentifiable {
				var ordered = items.OrderBy(orderVariable.Compile()).ToList();
				return new Reordering<T> {
					Items = ordered,
					OldOrder = oldOrder,
					NewOrder = newOrder,
					SelectedId = selectedId,
					OrderVariable = orderVariable,
				};
			}
			public static Inserting<T> CreateInsert<T>(IEnumerable<T> items, T toInsert, int insertAtIndex, Expression<Func<T, int>> orderVariable) where T : ILongIdentifiable {
				var ordered = items.OrderBy(orderVariable.Compile()).ToList();
				return new Inserting<T> {
					Items = ordered,
					InsertAtIndex = insertAtIndex,
					ToInsert = toInsert,
					OrderVariable = orderVariable,
				};
			}
		}
		protected class Inserting<T> where T : ILongIdentifiable {
			public List<T> Items { get; set; }
			public Expression<Func<T, int>> OrderVariable { get; set; }
			public int InsertAtIndex { get; set; }
			public T ToInsert { get; set; }

			public bool ApplyInsert(ISession s, Action<ReorderMeta<T>> onReordered) {
				var allItems = Items.OrderBy(OrderVariable.Compile()).Where(x => x.Id != ToInsert.Id).ToList();
				Items.Insert(InsertAtIndex, ToInsert);
				var anyMoved = false;
				for (var i = 0; i < Items.Count; i++) {
					var rm = Items[i];
					var oldIndex = rm.Get(OrderVariable);
					if (oldIndex != i) {
						rm.Set(OrderVariable, i);
						anyMoved = true;
						s.Update(rm);
						onReordered?.Invoke(new ReorderMeta<T>(rm, oldIndex, i, true));
					}
				}

				//resend all indicies if nothing moved.
				if (!anyMoved && onReordered != null) {
					var index = 0;
					foreach (var rm in allItems) {
						onReordered(new ReorderMeta<T>(rm, index, index, false));
					}
					index++;
				}
				return true;
			}

		}

		protected class ReorderMeta<T> where T : ILongIdentifiable {
			public ReorderMeta(T obj, int oldOrder, int newOrder, bool anyMoved) {
				Object = obj;
				OldOrder = oldOrder;
				NewOrder = newOrder;
				AnyMoved = anyMoved;
			}
			public long Id { get { return Object.Id; } }
			public T Object { get; protected set; }
			public int OldOrder { get; protected set; }
			public int NewOrder { get; protected set; }
			public bool AnyMoved { get; protected set; }

		}

		protected class Reordering<T> where T : ILongIdentifiable {
			public List<T> Items { get; set; }
			public Expression<Func<T, int>> OrderVariable { get; set; }
			public int OldOrder { get; set; }
			public int NewOrder { get; set; }
			public long SelectedId { get; set; }



			public bool ApplyReorder(ISession s, Action<ReorderMeta<T>> onReordered) {
				var allItems = Items.OrderBy(OrderVariable.Compile()).ToList();
				var found = allItems.ElementAtOrDefault(OldOrder);
				bool doReorder = true;
				if (found != null && found.Id == SelectedId) {
					allItems.RemoveAt(OldOrder);
					allItems.Insert(Math.Min(allItems.Count, NewOrder), found);
				} else {
					var located = allItems.Select((x, i) => new { Item = x, Index = i })
						.FirstOrDefault(x => x.Item.Id == SelectedId);

					if (located != null) {
						allItems.RemoveAt(located.Index);
						allItems.Insert(Math.Min(allItems.Count, NewOrder), located.Item);
					} else {
						doReorder = false;
					}
				}

				if (doReorder) {
					var index = 0;
					var anyMoved = false;
					foreach (var rm in allItems) {
						var oldIndex = rm.Get(OrderVariable);
						if (oldIndex != index) {
							anyMoved = true;
							rm.Set(OrderVariable, index);
							s.Update(rm);
							onReordered?.Invoke(new ReorderMeta<T>(rm, oldIndex, index, true));
						}
						index++;
					}

					//resend all indicies if nothing moved.
					if (!anyMoved && onReordered != null) {
						index = 0;
						foreach (var rm in allItems) {
							onReordered(new ReorderMeta<T>(rm, index, index, false));
							index++;
						}
					}
				}
				return doReorder;
			}

		}

		protected class ReorderingRecurrence<T> {
			public List<T> Items { get; set; }
			public Expression<Func<T, long>> IdVariable { get; set; }
			public Expression<Func<T, int>> OrderVariable { get; set; }
			public int OldOrder { get; set; }
			public int NewOrder { get; set; }
			public long SelectedId { get; set; }
			public long? RecurrenceId { get; set; }

			public bool ApplyReorder(ISession s) {
				return ApplyReorder(null, s, null);
			}
			/// <summary>
			/// </summary>
			/// <param name="rt"></param>
			/// <param name="s"></param>
			/// <param name="ConstructAngularObject"> [Id,Order, new AngularItem(Id){Ordering=order}]</param>
			/// <returns></returns>
			public bool ApplyReorder(RealTimeUtility rt, ISession s, Func<object, int, T, IAngularId> constructAngularObject) {

				var allItems = Items.OrderBy(OrderVariable.Compile()).ToList();

				var found = allItems.ElementAtOrDefault(OldOrder);
				bool doReorder = true;
				if (found != null && found.Get(IdVariable) == SelectedId) {
					allItems.RemoveAt(OldOrder);
					allItems.Insert(Math.Min(allItems.Count, NewOrder), found);
				} else {
					var located = allItems.Select((x, i) => new { Item = x, Index = i })
						.FirstOrDefault(x => x.Item.Get(IdVariable) == SelectedId);

					if (located != null) {
						allItems.RemoveAt(located.Index);
						allItems.Insert(Math.Min(allItems.Count, NewOrder), located.Item);
					} else {
						doReorder = false;
					}
				}

				if (doReorder) {
					var updater = RecurrenceId == null || rt == null ? null : rt.UpdateRecurrences(RecurrenceId.Value);
					var index = 0;
					var anyMoved = false;
					foreach (var rm in allItems) {
						if (rm.Get(OrderVariable) != index) {
							anyMoved = true;
							rm.Set(OrderVariable, index);
							s.Update(rm);
							if (updater != null) {
								try {
									updater.Update(constructAngularObject(rm.Get(IdVariable), index, rm));
								} catch (Exception) {
								}
							}
						}
						index++;
					}

					if (!anyMoved && updater != null) {
						index = 0;
						//resend all indicies
						foreach (var rm in allItems) {
							try {
								updater.Update(constructAngularObject(rm.Get(IdVariable), index, rm));
							} catch (Exception) {
							}
						}
						index++;
					}
				}
				return doReorder;
			}
		}
	}
}

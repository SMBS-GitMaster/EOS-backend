using NHibernate;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview {
	public class MultiCriteria {
		public class MultiCriteriaExecuted {
			protected List<object> Results1 { get; set; }
			protected List<object> Results2 { get; set; }
			protected List<int> Ordered { get; set; }
			protected int Iterator { get; set; }
			protected int Iterator1 { get; set; }
			protected int Iterator2 { get; set; }

			public MultiCriteriaExecuted(List<object> results1, List<object> results2, List<int> ordered) {
				Results1 = results1;
				Results2 = results2;
				Ordered = ordered;
				Iterator = 0;
				Iterator1 = 0;
				Iterator2 = 0;
			}

			protected object Next() {
				object output = null;
				if (Ordered[Iterator] == 1) {
					output = Results1[Iterator1];
					Iterator1++;
				} else if (Ordered[Iterator] == 2) {
					output = Results2[Iterator2];
					Iterator2++;
				} else {
					throw new Exception("Unknown order");
				}
				Iterator++;
				return output;
			}

			public T Get<T>() {
				var output = ((IList)Next())[0];
				return (T)output;
			}

			public List<T> GetList<T>() {
				var output = ((IList)Next()).Cast<T>().ToList();
				return output;
			}


		}


		protected ISession Session { get; set; }
		protected IMultiCriteria UnderlyingCriteria { get; set; }
		protected IMultiQuery UnderlyingQuery { get; set; }


		protected bool Executed { get; set; }

		protected List<int> Ordered { get; set; }


		protected MultiCriteria(ISession session, IMultiCriteria underlyingCriteria, IMultiQuery underlyingQuery) {
			Session = session;
			UnderlyingCriteria = underlyingCriteria;
			UnderlyingQuery = underlyingQuery;
			Ordered = new List<int>();
			Executed = false;
		}

		public static MultiCriteria Create(ISession s) {
			return new MultiCriteria(s, s.CreateMultiCriteria(), s.CreateMultiQuery());
		}

		public MultiCriteria AddInt<T>(IQueryOver<T> query) {
			if (Executed)
				throw new Exception("Query has already been executed.");

			UnderlyingCriteria.Add<int>(query);
			Ordered.Add(1);

			return this;
		}
		public MultiCriteria Add(ICriteria query) {
			if (Executed)
				throw new Exception("Query has already been executed.");

			UnderlyingCriteria.Add(query);
			Ordered.Add(1);

			return this;
		}


		public MultiCriteria Add<T, R>(IQueryOver<T> query) {
			if (Executed)
				throw new Exception("Query has already been executed.");

			UnderlyingCriteria.Add<R>(query);
			Ordered.Add(1);

			return this;
		}
		public MultiCriteria Add(IQuery query) {
			if (Executed)
				throw new Exception("Query has already been executed.");

			UnderlyingQuery.Add(query);
			Ordered.Add(2);


			return this;
		}

		public MultiCriteria Add<T>(IQueryOver<T> query) {
			if (Executed)
				throw new Exception("Query has already been executed.");

			UnderlyingCriteria.Add<T>(query);
			Ordered.Add(1);


			return this;
		}

		public MultiCriteriaExecuted Execute() {
			Executed = true;
			var outputs1 = new List<object>();
			var outputs2 = new List<object>();
			if (Ordered.Any(x => x == 1)) {
				foreach (var o in UnderlyingCriteria.List()) {
					outputs1.Add(o);
				}
			}
			if (Ordered.Any(x => x == 2)) {
				foreach (var o in UnderlyingQuery.List()) {
					outputs2.Add(o);
				}
			}

			return new MultiCriteriaExecuted(outputs1, outputs2, Ordered);

		}
	}
}

﻿using System.Linq.Expressions;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using RadialReview.Reflection;
using NHibernate.Proxy;

namespace RadialReview {
	public static class _SessionExtensions {
		public static IList<TEntity> GetByMultipleIds<TEntity>(this ISession Session, IEnumerable<long> ids) {
			var result = Session
			  .CreateCriteria(typeof(TEntity))
			  .Add(Restrictions.In("Id", ids.ToArray()))
			  .List<TEntity>();
			return result;
		}

		public static AbstractQuery ToQueryProvider(this ISession session, bool onlyAlive) {
			return new SessionQuery(session, onlyAlive);
		}
		public static AbstractUpdate ToUpdateProvider(this ISession session) {
			return new SessionUpdate(session);
		}
		public static DataInteraction ToDataInteraction(this ISession session, bool onlyAlive) {
			return new DataInteraction(session.ToQueryProvider(onlyAlive), session.ToUpdateProvider());
		}


		public class BackRef<T> {
			public Expression<Func<T, object>> Reference;

			public Expression<Func<object, T>> BackReference;


			public static BackRef<T> From<TRef, TList>(Expression<Func<T, TList>> reference, Expression<Func<TRef, T>> backReference) where TList : IList<TRef> {
				return new BackRef<T>() {
					Reference = reference.AddBox(),
					BackReference = x => backReference.Compile()((TRef)x),
				};
			}
		}

		public static T GetFresh<T>(this ISession s, object id) {
			var proxy = s.Load<T>(id);
			s.Evict(proxy);
			return s.Get<T>(id);

		}
	}
}

namespace RadialReview.SessionExtension {
	public static class SessionExtension {
		public static T Deproxy<T>(this T model) {
			if (model is INHibernateProxy) {
				var lazyInitialiser = ((INHibernateProxy)model).HibernateLazyInitializer;
				model = (T)lazyInitialiser.GetImplementation();
			}
			return model;
		}
	}
}
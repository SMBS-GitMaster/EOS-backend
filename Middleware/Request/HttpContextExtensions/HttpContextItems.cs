using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RadialReview.Middleware.Request.HttpContextExtensions {
	public static partial class HttpContextItems {
		public struct HttpContextItemKey {
			public HttpContextItemKey(string name, params string[] options) {
				Name = name;
				Options = options;
			}
			public string Name { get; private set; }
			public StringValues Options { get; private set; }

			private object GetTuple() {
				return Tuple.Create(Name, Options);
			}

			public override bool Equals(object obj) {
				return obj is HttpContextItemKey other && ((HttpContextItemKey)obj).GetTuple().Equals(other.GetTuple());
			}
			public override int GetHashCode() {
				return GetTuple().GetHashCode();
			}
		}

		//See partial classes for methods
		public static void SetRequestItem<V>(this HttpContext ctx, HttpContextItemKey key, V value) {
			ctx.Items[key] = value;
		}

		public static V GetRequestItemOrDefault<V>(this HttpContext ctx, HttpContextItemKey key, V delft) {
			if (ctx.ContainsRequestItem(key))
				return ctx.GetRequestItem<V>(key);
			return delft;
		}

		public static V GetRequestItem<V>(this HttpContext ctx, HttpContextItemKey key) {
			if (ctx.Items.ContainsKey(key)) {
				if (ctx.Items[key] is V res) {
					return res;
				}
				throw new InvalidCastException("Expected:" + typeof(V).Name + " Found:" + ctx.Items[key].NotNull(x => x.GetType().Name) ?? "null");
			}
			throw new KeyNotFoundException("Key:" + key.Name);
		}

		public static bool ContainsRequestItem(this HttpContext ctx, HttpContextItemKey key) {
			return ctx.Items.ContainsKey(key);
		}

		public static V GetOrCreateRequestItem<V>(this HttpContext ctx, HttpContextItemKey key, [NotNull] Func<HttpContext, V> generator) {
			if (!ctx.Items.ContainsKey(key)) {
				SetRequestItem(ctx, key, generator(ctx));
			}
			return GetRequestItem<V>(ctx, key);
		}

		public static void ClearRequestItem(this HttpContext ctx, HttpContextItemKey key, bool matchExactOptions = true) {
			if (matchExactOptions) {
				ctx.Items.Remove(key);
			} else {
				//Remove all that have the same key.Name
				foreach (var match in ctx.Items.Where(x => x.Key is HttpContextItemKey k && k.Name == key.Name).Select(x => x.Key)) {
					ctx.Items.Remove(match);
				}
			}
		}

	}
}

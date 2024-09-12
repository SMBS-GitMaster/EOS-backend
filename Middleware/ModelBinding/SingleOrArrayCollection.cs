using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Middleware.ModelBinding.JsonConverters {
	//https://stackoverflow.com/questions/18994685/how-to-handle-both-a-single-item-and-an-array-for-the-same-property-using-json-n
	public class SingleOrArrayListConverter : JsonConverter {
		// Adapted from this answer https://stackoverflow.com/a/18997172
		// to https://stackoverflow.com/questions/18994685/how-to-handle-both-a-single-item-and-an-array-for-the-same-property-using-json-n
		// by Brian Rogers https://stackoverflow.com/users/10263/brian-rogers
		readonly bool canWrite;
		readonly IContractResolver resolver;

		public SingleOrArrayListConverter() : this(false) { }

		public SingleOrArrayListConverter(bool canWrite) : this(canWrite, null) { }

		public SingleOrArrayListConverter(bool canWrite, IContractResolver resolver) {
			this.canWrite = canWrite;
			// Use the global default resolver if none is passed in.
			this.resolver = resolver ?? new JsonSerializer().ContractResolver;
		}

		static bool CanConvert(Type objectType, IContractResolver resolver) {
			Type itemType;
			JsonArrayContract contract;
			return CanConvert(objectType, resolver, out itemType, out contract);
		}

		static bool CanConvert(Type objectType, IContractResolver resolver, out Type itemType, out JsonArrayContract contract) {
			if ((itemType = objectType.GetListItemType()) == null) {
				itemType = null;
				contract = null;
				return false;
			}
			// Ensure that [JsonObject] is not applied to the type.
			if ((contract = resolver.ResolveContract(objectType) as JsonArrayContract) == null)
				return false;
			var itemContract = resolver.ResolveContract(itemType);
			// Not implemented for jagged arrays.
			if (itemContract is JsonArrayContract)
				return false;
			return true;
		}

		public override bool CanConvert(Type objectType) {
			return CanConvert(objectType, resolver);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			Type itemType;
			JsonArrayContract contract;

			if (!CanConvert(objectType, serializer.ContractResolver, out itemType, out contract))
				throw new JsonSerializationException(string.Format("Invalid type for {0}: {1}", GetType(), objectType));
			if (reader.MoveToContent().TokenType == JsonToken.Null)
				return null;
			if (objectType.IsArray) {
				var list = (IList)(existingValue ?? new List<object>());
				return ProcessArray(reader, serializer, itemType, list);
			} else {
				var list = (IList)(existingValue ?? contract.DefaultCreator());
				return ProcessList(reader, serializer, itemType, list);
			}
		}

		private static object ProcessList(JsonReader reader, JsonSerializer serializer, Type itemType, IList list) {
			if (reader.TokenType == JsonToken.StartArray)
				serializer.Populate(reader, list);
			else
				// Here we take advantage of the fact that List<T> implements IList to avoid having to use reflection to call the generic Add<T> method.
				list.Add(serializer.Deserialize(reader, itemType));
			return list;
		}

		private static object ProcessArray(JsonReader reader, JsonSerializer serializer, Type itemType, IList list) {
			var helper = new List<object>();
			if (reader.TokenType == JsonToken.StartArray) {
				serializer.Populate(reader, helper);
				helper = helper.Select(x => Convert.ChangeType(x, itemType)).ToList();
			} else {
				// Here we take advantage of the fact that List<T> implements IList to avoid having to use reflection to call the generic Add<T> method.
				helper.Add(serializer.Deserialize(reader, itemType));
			}

			var arr= Array.CreateInstance(itemType, helper.Count);
			for (var i = 0; i < arr.Length; i++) {
				arr.SetValue(helper[i],i);
			}
			return arr;
		}

		public override bool CanWrite { get { return canWrite; } }

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			var list = value as ICollection;
			if (list == null)
				throw new JsonSerializationException(string.Format("Invalid type for {0}: {1}", GetType(), value.GetType()));
			// Here we take advantage of the fact that List<T> implements IList to avoid having to use reflection to call the generic Count method.
			if (list.Count == 1) {
				foreach (var item in list) {
					serializer.Serialize(writer, item);
					break;
				}
			} else {
				writer.WriteStartArray();
				foreach (var item in list)
					serializer.Serialize(writer, item);
				writer.WriteEndArray();
			}
		}
	}

	public static partial class JsonExtensions {
		public static JsonReader MoveToContent(this JsonReader reader) {
			while ((reader.TokenType == JsonToken.Comment || reader.TokenType == JsonToken.None) && reader.Read())
				;
			return reader;
		}

		internal static Type GetListItemType(this Type type) {
			// Quick reject for performance


			if (type.IsPrimitive || type == typeof(string))
				return null;
			if (type.IsArray)
				return type.GetElementType();
			while (type != null) {
				if (type.IsGenericType) {
					var genType = type.GetGenericTypeDefinition();
					if (genType == typeof(List<>))
						return type.GetGenericArguments()[0];
				}
				type = type.BaseType;
			}
			return null;
		}
	}
}

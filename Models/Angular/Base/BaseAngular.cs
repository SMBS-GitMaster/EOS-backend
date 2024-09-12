using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace RadialReview.Models.Angular.Base {
	[Serializable]
	public class BaseStringAngular : IAngularItemString {
		[JsonProperty(Order = -100)]
		public string Id { get; set; }
		[JsonProperty(Order = -100)]
		public string Type {
			get { return GetType().Name; }
		}
		[JsonProperty(Order = 100)]
		public string Key { get { return this.GetKey(); } }
		public BaseStringAngular() { }
		public BaseStringAngular(string id) {
			Id = id;
		}
		[IgnoreDataMember]
		public bool Hide { get; set; }
		[IgnoreDataMember]
		public Dictionary<string, object> _ExtraProperties { get; set; }

		public object GetAngularId() {
			return Id;
		}

		public string GetAngularType() {
			return Type;
		}
	}

	[Serializable]
	public class BaseAngular : IAngularItem {
		[JsonProperty(Order = -2)]
		public long Id { get; set; }
		[JsonProperty(Order = -2)]
		public string Type {
			get { return GetType().Name; }
		}
		[JsonProperty(Order = -2)]
		public string Key { get { return this.GetKey(); } }

		public BaseAngular() {}
		public BaseAngular(long id) {
			Id = id;			
		}

		[IgnoreDataMember]
		public bool Hide { get; set; }
		[IgnoreDataMember]
		public Dictionary<string, object> _ExtraProperties { get; set; }

		public object GetAngularId() {
			return Id;
		}

		public string GetAngularType() {
			return Type;
		}

		[IgnoreDataMember]
		//Absolute Update Time. Will not update if it is before last update
		public DateTime? UT { get; set; }

	}

	public class Removed {


		public static long Long() {
			return long.MaxValue - 1;
		}
		public static T From<T>() where T : IAngularItem, new() {
			var obj = (T)Activator.CreateInstance<T>();
			obj.Id = Long();
			return obj;
		}
		public static T FromAngularString<T>() where T : IAngularItemString, new() {
			var obj = (T)Activator.CreateInstance<T>();
			obj.Id = String();
			return obj;
		}
		public static DateTime Date() {
			return DateTime.MaxValue - TimeSpan.FromSeconds(1);
		}
		public static decimal Decimal() {
			return decimal.MaxValue + decimal.MinusOne;
		}
		public static string String() {
			return DELETED_KEY;
		}

		public const string DELETED_KEY = "`delete`";
	}











}

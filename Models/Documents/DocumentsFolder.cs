using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RadialReview.Models.Documents.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;

namespace RadialReview.Models.Documents {
	public class DocumentsFolder : ILongIdentifiable, IHistorical {

		public virtual long Id { get; set; }
		public virtual string LookupId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrgId { get; set; }
		public virtual String Name { get; set; }
		public virtual bool Root { get; set; }
		public virtual long? CreatorId { get; set; }
		public virtual string ImageUrl { get; set; }
		public virtual string IconHint { get; set; }
		public virtual string Description { get; set; }
		public virtual bool Generated { get; set; }
		public virtual string Class { get; set; }
		public virtual string Interceptor { get; set; }
		public virtual string InterceptorData { get; set; }
		public virtual int? HueRotate { get; set; }
		public virtual bool DisableDelete { get; set; }

		public virtual DocumentsFolderDisplayType? DisplayType { get; set; }
		public virtual DocumentsFolderOrderType? OrderType { get; set; }
		public virtual bool OrderAscending { get; set; }
		public virtual bool CanDelete { get { return !DisableDelete; } set { DisableDelete = !value; } }

		public virtual T GetInterceptorProperty<T>(string property, T deft = default(T)) {
			if (InterceptorData == null)
				return deft;

			try {
				return JObject.Parse(InterceptorData)[property].Value<T>();
			} catch (Exception) {
				return deft;
			}
		}
		public virtual bool SetInterceptorProperty<T>(string property, T value) {
			if (string.IsNullOrWhiteSpace(property)) {
				return false;
			}

			if (InterceptorData == null) {
				InterceptorData = "{}";
			}

			try {
				var obj = JObject.Parse(InterceptorData);
				obj[property] = JToken.FromObject(value);
				InterceptorData = obj.ToString(Formatting.None);
				return true;
			} catch (Exception) {
			}

			return false;
		}

		public virtual DocumentsFolder SetInterceptorData<T>(T data) {
			if (data != null) {
				InterceptorData = JsonConvert.SerializeObject(data);
			} else {
				InterceptorData = null;
			}
			return this;
		}
		public virtual T GetInterceptorData<T>() {
			if (InterceptorData == null)
				return default(T);
			try {
				return JsonConvert.DeserializeObject<T>(InterceptorData);
			} catch (Exception e) {
				return default(T);
			}
		}

		public DocumentsFolder() {
			CreateTime = DateTime.UtcNow;
			LookupId = RandomUtil.SecureRandomString();
		}

		public static DocumentsFolder CreateFrom<T>(string name, string iconHint, long orgId, long? creatorId, bool canDelete, GeneratedFolderConst folderConst, T data) {
			var f = new DocumentsFolder() {
				Name = name,
				CreateTime = DateTime.UtcNow,
				Generated = true,
				IconHint = iconHint,
				Class = folderConst.Class,
				Interceptor = folderConst.Interceptor,
				CanDelete = canDelete,
				OrgId = orgId,
				Root = false,
				CreatorId = creatorId,

			};
			f.SetInterceptorData(data);
			return f;
		}

		public static DocumentsFolder CreateFrom<T>(GS structure, long orgId, T data, params string[] stringFormatArgs) {
			//we'll never have more than 30 arguments right?? Cue your eye roll.
			var args = new string[30];
			for (var i = 0; i < args.Length; i++) {
				if (stringFormatArgs != null && i < stringFormatArgs.Length) {
					args[i] = stringFormatArgs[i];
				} else {
					args[i] = "?";
				}
			}

			var f = new DocumentsFolder() {
				LookupId = RandomUtil.SecureRandomString(),
				Name = string.Format(structure.Name, stringFormatArgs),
				OrgId = orgId,
				Interceptor = structure.Interceptor,
				Class = structure.Class,
				IconHint = structure.IconHint,
				HueRotate = structure.HueRotate,
				ImageUrl = structure.ImageUrl,
				CanDelete = structure.CanDelete,
				CreateTime = DateTime.UtcNow,
				Generated = true,
				Root = false,
				Description = null,
				CreatorId = null,
				DeleteTime = null,
			};
			f.SetInterceptorData(data);
			return f;
		}

		public class Map : ClassMap<DocumentsFolder> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.LookupId).Index("IDX_DOCUMENTSFOLDER_LOOKUPID");
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrgId);
				Map(x => x.Name);
				Map(x => x.Root);
				Map(x => x.ImageUrl);
				Map(x => x.CreatorId);
				Map(x => x.IconHint);
				Map(x => x.Description);
				Map(x => x.Generated).Column("IsGenerated");
				Map(x => x.Class);
				Map(x => x.HueRotate);
				Map(x => x.Interceptor);
				Map(x => x.InterceptorData);
				Map(x => x.OrderAscending);
				Map(x => x.DisableDelete);
				Map(x => x.DisplayType).CustomType<DocumentsFolderDisplayType>();
				Map(x => x.OrderType).CustomType<DocumentsFolderOrderType>();
			}
		}
	}
}
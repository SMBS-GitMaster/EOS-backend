using System;
using System.Linq;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Areas.People.Models.Survey;
using System.Diagnostics;
using RadialReview.SessionExtension;
using static RadialReview.Models.PermItem;
using RadialReview.Models.L10;
using RadialReview.Models.VTO;
using RadialReview.Models.Downloads;

namespace RadialReview {
	[DebuggerDisplay("ForModel: {FriendlyType()}[{ModelId}]")]
	public class ForModel : IForModel {
		public virtual long ModelId { get; set; }
		public virtual string ModelType { get; set; }
		public virtual string _PrettyString { get; set; }

		public virtual ForModel Clone() {
			return new ForModel {
				ModelId = ModelId,
				ModelType = ModelType,
				_PrettyString = _PrettyString
			};
		}

		public class ForModelMap : ComponentMap<ForModel> {
			public ForModelMap() {
				Map(x => x.ModelId);
				Map(x => x.ModelType);
			}
		}

		public static ForModel Create(ILongIdentifiable creator) {
			if (creator == null)
				return null;

			return new ForModel() {
				ModelId = creator.Id,
				ModelType = GetModelType(creator)
			};
		}

		public static ForModel Create<T>(long id) where T : ILongIdentifiable {
			return new ForModel() {
				ModelId = id,
				ModelType = GetModelType<T>()
			};
		}

		public virtual string FriendlyType() {
			if (ModelType == null)
				return null;
			return ModelType.Split('.').Last();
		}

		public static ForModel From(IForModel model) {
			return new ForModel() {
				ModelId = model.ModelId,
				ModelType = model.ModelType

			};
		}

		public static string GetModelType(ILongIdentifiable creator) {
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(creator.Deproxy().GetType()).EntityName;
		}

		public override bool Equals(object obj) {
			var found = (obj as IForModel);
			if (found == null)
				return false;
			return found.ModelId == ModelId && found.ModelType == ModelType;
		}

		public override int GetHashCode() {
			return HashUtil.Merge(ModelId.GetHashCode(), ModelType.GetHashCode());
		}

		public static string GetModelType<T>() where T : ILongIdentifiable {
			return GetModelType(typeof(T));
		}
		[Obsolete("Use other methods")]
		public static string GetModelType(Type t) {
			return HibernateSession.GetDatabaseSessionFactory().GetClassMetadata(t).EntityName;

		}

		public bool Is<T>() {
			var modelType = GetModelType(typeof(T));
			return ModelType == modelType || modelType.EndsWith("." + ModelType) || modelType.EndsWith("+" + ModelType);
		}

		public string ToPrettyString() {
			return _PrettyString;
		}

	}

	public static class ForModelExtensions {
		public static ForModel ToImpl(this IForModel obj) {
			return ForModel.From(obj);
		}

		public static string ToKey(this IForModel obj) {
			return obj.NotNull(x => x.ModelType + "_" + x.ModelId);
		}

		public static string ToKey(this IByAbout byAbout) {
			return byAbout.GetBy().ToKey() + "-" + byAbout.GetAbout().ToKey();
		}
		public static ByAbout ToImpl(this IByAbout obj) {
			return new ByAbout(obj.GetBy(), obj.GetAbout());
		}

		public static ResourceType GetResourceType(this IForModel obj) {
			if (obj.Is<L10Recurrence>()) {
				return ResourceType.L10Recurrence;
			} else if (obj.Is<VtoModel>()) {
				return ResourceType.VTO;
			} else if (obj.Is<EncryptedFileModel>()) {
				return ResourceType.File;
			} else if (obj.Is<L10Meeting>()) {
				return ResourceType.L10Meeting;
			} 
			throw new ArgumentOutOfRangeException(obj.ModelType);
		}

	}

}

namespace RadialReview.Extensions {

	public static class ForModelExtensions {
		public static IForModel ForModelFromKey(this string obj) {
			if (obj == null)
				throw new ArgumentNullException("obj");
			var split = obj.Split('_');
			if (split.Length < 2)
				throw new ArgumentOutOfRangeException("Requires a string in the format <ModelType>_<ModelId>");

			var mid = split.Last().ToLong();
			if (mid == 0)
				throw new Exception("Id should not be 0");

			return new ForModel() {
				ModelId = mid,
				ModelType = string.Join("_", split.Take(split.Length - 1))
			};
		}
	}
}

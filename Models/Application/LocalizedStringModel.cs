using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RadialReview.Models {
	public class LocalizedStringPairModel : ILongIdentifiable {
		public virtual long Id { get; protected set; }
		public virtual String Locale { get; set; }
		public virtual String Value { get; set; }

		public LocalizedStringPairModel() {

		}
		public LocalizedStringPairModel(String value, String locale)
			: this() {
			Locale = locale;
			Value = value;
		}
		public LocalizedStringPairModel(String value) : this() {
			Locale = Thread.CurrentThread.CurrentCulture.Name;
			Value = value;
		}

	}

	public class LocalizedStringModel : ILongIdentifiable {
		public virtual long Id { get; protected set; }
		public virtual String Standard { get; set; }
		public virtual String StandardLocale { get; set; }
		public virtual IList<LocalizedStringPairModel> Localizations { get; set; }


		public LocalizedStringModel(String defalt) : this() {
			this.UpdateDefault(defalt);
		}

		public LocalizedStringModel() {
			Localizations = new List<LocalizedStringPairModel>();
		}

		public override string ToString() {
			return this.Translate();
		}
	}

	public class LocalizedStringModelMap : ClassMap<LocalizedStringModel> {
		public LocalizedStringModelMap() {
			Id(x => x.Id);
			Map(x => x.Standard);
			Map(x => x.StandardLocale);
			HasMany(x => x.Localizations)
				.Fetch.Join()
				.Not.LazyLoad()
				.Cascade.SaveUpdate();
		}
	}

	public class LocalizedStringPairModelMap : ClassMap<LocalizedStringPairModel> {
		public LocalizedStringPairModelMap() {
			Id(x => x.Id);
			Map(x => x.Value);
			Map(x => x.Locale);
		}
	}
}

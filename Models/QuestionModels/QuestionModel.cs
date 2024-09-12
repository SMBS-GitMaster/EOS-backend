
using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;

namespace RadialReview.Models {
	public class QuestionModel : Askable, IDeletable {
		public virtual DateTime DateCreated { get; set; }
		public virtual LocalizedStringModel Question { get; set; }
		public virtual long CreatedById { get; set; }
		public virtual IList<LongModel> DisabledFor { get; set; }
		public virtual QuestionType QuestionType { get; set; }



		public virtual OriginType OriginType { get; set; }
		public virtual long OriginId { get; set; }

		public QuestionModel() : base() {
			DateCreated = DateTime.UtcNow;
			DisabledFor = new List<LongModel>();
			Question = new LocalizedStringModel();
		}


		public override QuestionType GetQuestionType() {
			return QuestionType;
		}

		public override string GetQuestion() {
			return Question.Translate();
		}
	}

	public class QuestionModelMap : SubclassMap<QuestionModel> {
		public QuestionModelMap() {
			Map(x => x.DateCreated);
			Map(x => x.QuestionType);
			Map(x => x.OriginId);
			Map(x => x.OriginType);



			References(x => x.Question)
				.Not.LazyLoad()
				.Cascade.SaveUpdate();


			Map(x => x.CreatedById);
			References(x => x.Category)
				.Not.LazyLoad()
				.Cascade.SaveUpdate();


			HasMany(x => x.DisabledFor)
				.Not.LazyLoad()
				.Cascade.SaveUpdate();

		}
	}
}

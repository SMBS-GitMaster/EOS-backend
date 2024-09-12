using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;

namespace RadialReview.Models.Askables {
	public class CompanyValueModel : Askable {

		public virtual string CompanyValueDetails { get; set; }
		public virtual string CompanyValue { get; set; }
		public virtual long OrganizationId { get; set; }

		public virtual int MinimumPercentage { get; set; }

		public CompanyValueModel() {
			//Minimum = PositiveNegativeNeutral.Neutral;
			MinimumPercentage = (3 * 100) / 5;
		}

		public override Enums.QuestionType GetQuestionType() {
			return QuestionType.CompanyValue;
		}

		public override string GetQuestion() {
			return CompanyValue;
		}
		public class CompanyValueModelMap : SubclassMap<CompanyValueModel> {
			public CompanyValueModelMap() {
				Map(x => x.CompanyValue).Length(512);
				Map(x => x.CompanyValueDetails).Length(14000);
				Map(x => x.OrganizationId);
				Map(x => x.MinimumPercentage);
			}
		}
	}
}

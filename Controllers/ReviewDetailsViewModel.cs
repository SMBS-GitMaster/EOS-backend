using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Periods;
using RadialReview.Models.Charts;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Enums;
using RadialReview.Utilities.DataTypes;
using RadialReview.Engines;

namespace RadialReview.Core.Controllers
{
  public partial class ReviewController
  {
    public class ReviewDetailsViewModel {
			public ReviewsModel ReviewContainer { get; set; }
			public ReviewModel Review { get; set; }
			public long xAxis { get; set; }
			public long yAxis { get; set; }
			public String JobDescription { get; set; }
			public List<SelectListItem> Axis { get; set; }
			public List<AnswerModel> AnswersAbout { get; set; }
			public Dictionary<long, String> Categories { get; set; }
			public List<String> Responsibilities { get; set; }
			public List<Askable> Questions { get; set; }
			public List<UserOrganizationModel> Supervisers { get; set; }
			public List<Askable> ActiveQuestions { get; set; }
			public List<ChartType> ChartTypes { get; set; }
			public PeriodModel Period { get; set; }
			public PeriodModel NextPeriod { get; set; }
			public List<RockModel> NextRocks { get; set; }
			public AngularScorecard Scorecard { get; set; }
			public DateTime CurrentTime { get; set; }
			public int NumberOfWeeks { get; set; }

			public static int NEUTRAL_CUTOFF = 2;
			public Table RockTable(long reviewId) {
				var rocks = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.Rock).Cast<RockAnswer>();
				var o = rocks.Select(x => Tuple.Create(x, 1)).Union(rocks.Select(x => Tuple.Create(x, 2)));
				return Table.Create(o, x => x.Item1.Askable.GetQuestion(), x => x.Item2 == 1 ? x.Item1.ReviewerUser.GetName() : "Override", xx => {
					var x = xx.Item1;
					if (xx.Item2 == 1) {
						return new SpanCell {
							Class = "fill rocks " + (String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason") +  " " + x.Completion,
							Title = x.Reason,
							Data = new Dictionary<string, string>()
						{{"reviewId", "" + reviewId}, {"rockId", "" + x.Askable.Id}, {"byuserid", "" + x.ReviewerUserId}},
						}.ToHtmlString();
					} else {
						return new SpanCell {
							Class = "fill rocks override " + (String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason") +  " " + x.Completion,
							Title = x.Reason,
							Data = new Dictionary<string, string>()
						{{"reviewId", "" + reviewId}, {"rockId", "" + x.Askable.Id}, {"byuserid", "" + x.ReviewerUserId}},
						}.ToHtmlString();
					}
				}, "goals");
			}

			private void AddCompanyValueRow(string row, TableData table, IEnumerable<IGrouping<long, CompanyValueAnswer>> values, Func<decimal, decimal, decimal, Microsoft.AspNetCore.Html.HtmlString> content, String clazz) {
				foreach (var x in values) {
					var pos = x.Count(y => y.Exhibits == PositiveNegativeNeutral.Positive);
					var neg = x.Count(y => y.Exhibits == PositiveNegativeNeutral.Negative);
					var neut = x.Count(y => y.Exhibits == PositiveNegativeNeutral.Neutral);
					var tot = x.Count(y => y.Exhibits != PositiveNegativeNeutral.Indeterminate);
					PositiveNegativeNeutral ex;
					if (neg > 0 || neut >= NEUTRAL_CUTOFF)
						ex = PositiveNegativeNeutral.Negative;
					else if (pos == tot && tot > 0)
						ex = PositiveNegativeNeutral.Positive;
					else if (tot > 0)
						ex = PositiveNegativeNeutral.Neutral;
					else
						ex = PositiveNegativeNeutral.Indeterminate;
					var html = new SpanCell() { Class = "fill companyValues " + clazz + " " + ex, Contents = content(pos, neg, neut) }.ToHtmlString();
					table.Set(row, x.First().Askable.GetQuestion(), html);
				}
			}

			public Table CompanyValuesScore {
				get {
					var values = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.CompanyValue).Cast<CompanyValueAnswer>().ToList();
					var data = new TableData();
					//Pull self
					var selfAns = values.Where(x => x.AboutType.HasFlag(AboutType.Self)).GroupBy(x => x.Askable.Id);
					AddCompanyValueRow(Review.ReviewerUser.GetName(), data, selfAns, (x, y, z) => new Microsoft.AspNetCore.Html.HtmlString(""), "");
					//Pull manager
					var managers = values.Where(x => x.AboutType.HasFlag(AboutType.Subordinate)).GroupBy(x => x.ReviewerUser.GetName());
					foreach (var m in managers) {
						var mAnswers = m.GroupBy(x => x.Askable.Id);
						AddCompanyValueRow(m.First().ReviewerUser.GetName(), data, mAnswers, (x, y, z) => new Microsoft.AspNetCore.Html.HtmlString(""), "");
					}

					//Pull peer answers
					var otherAns = values.Where(x => !x.AboutType.HasFlag(AboutType.Self) && !x.AboutType.HasFlag(AboutType.Subordinate)).GroupBy(x => x.Askable.Id);
					AddCompanyValueRow("Others", data, otherAns, (pos, neg, neut) => {
						var tot = pos + neg + neut;
						if (tot == 0)
							return new Microsoft.AspNetCore.Html.HtmlString("");
						return new Microsoft.AspNetCore.Html.HtmlString("" + Math.Round((pos + (neut / 2m)) / (tot) * 100m) + "<span class='percent'>%</span>");
					}, "companyValues companyValues-score");
					return new Table(data) { TableClass = "companyValues companyValues-client" };
				}
			}

			public Table CompanyValuesTable(long reviewId) {
				var dictionary = new DefaultDictionary<long, decimal[]>(x => new decimal[] { 0, 0, 0, 0, 0 });
				var values = AnswersAbout.Where(x => x.Askable.GetQuestionType() == QuestionType.CompanyValue).Cast<CompanyValueAnswer>().GroupBy(x => x.RevieweeUserId + "_" + x.ReviewerUserId + "_" + x.Askable.Id).Select(x => x.OrderByDescending(y => y.CompleteTime ?? DateTime.MinValue).First()).ToList();
				var dictionaryPerson = new DefaultDictionary<string, decimal>(x => 0);
				var dictionaryExhibits = new DefaultDictionary<long, List<PositiveNegativeNeutral>>(x => new List<PositiveNegativeNeutral>());
				var table = Table.Create(values, x => x.ReviewerUser.GetName(), x => x.Askable.GetQuestion(), x => {
					dictionaryPerson[x.ReviewerUser.GetName()] += x.Exhibits.Score2();
					return new SpanCell {
						Class = "fill companyValues " + (String.IsNullOrWhiteSpace(x.Reason) ? "" : "hasReason") + " " + x.Exhibits,
						Title = x.Reason,
						Data = new Dictionary<string, string>()
					{{"reviewId", "" + reviewId}, {"valueId", "" + x.Askable.Id}, {"byuserid", "" + x.ReviewerUserId}},
					}.ToHtmlString();
				}, "companyValues");
				foreach (var valueAnswers in values.GroupBy(x => x.Askable)) {
					var question = valueAnswers.Key.GetQuestion();
					var score = ChartsEngine.ScatterScorer.MergeValueScores(valueAnswers.ToList(), (CompanyValueModel)valueAnswers.Key);
					var clz = "";
					var ex = PositiveNegativeNeutral.Indeterminate;
					if (score.Above == true)
						ex = PositiveNegativeNeutral.Positive;
					if (score.Above == false)
						ex = PositiveNegativeNeutral.Negative;
					var reason = score.GetCompiledMessage();
					table.Data.Set("Score", question, new Microsoft.AspNetCore.Html.HtmlString("<span class='fill score " + clz + " " + ex + "' title='" + reason.Replace("'", "&#39;").Replace("\"", "&quot;") + "'></span>"));
				}

				table.Rows = table.Rows.OrderByDescending(x => dictionaryPerson[x]).ToList();
				table.Rows.Add("Score");

				return table;
			}

			public class ChartType {
				public String Title { get; set; }

				public String ImageUrl { get; set; }

				public bool Checked { get; set; }
			}

			public ReviewDetailsViewModel() {
				Axis = new List<SelectListItem>();
				AnswersAbout = new List<AnswerModel>();
				Categories = new Dictionary<long, string>();
				Responsibilities = new List<string>();
				Questions = new List<Askable>();
				Supervisers = new List<UserOrganizationModel>();
				ActiveQuestions = new List<Askable>();
				CurrentTime = DateTime.UtcNow;
			}
		}
    }
}

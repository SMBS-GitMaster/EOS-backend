using RadialReview.Models;
using System;

namespace RadialReview {
	public static class QuestionModelExtensions {

		public static long CategoryId(this QuestionModel self) {
			return self.Category.NotNull(x => x.Id);
		}

		public static String GetQuestion(this QuestionModel self) {
			return self.NotNull(x => x.Question.NotNull(y => y.Translate())) ?? "";
		}
	}
}

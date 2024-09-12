using RadialReview.Models.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace RadialReview.Models {
	public class MultibarCompletion : ICompletionModel {
		public List<CompletionModel> Completions { get; set; }

		public List<CompletionModel> GetCompletions() {
			return Completions;
		}

		public MultibarCompletion(IEnumerable<CompletionModel> completions) {
			Completions = completions.ToList();
		}

		public bool FullyComplete {
			get {
				var fullyComplete = true;
				foreach (var c in Completions) {
					fullyComplete = (c.FullyComplete && fullyComplete);
				}
				return fullyComplete;
			}
		}

		public bool Started {
			get {
				var started = false;
				foreach (var c in Completions) {
					started = (c.Started || started);
				}
				return started;
			}
		}


		public bool Illegal {
			get {
				var anyIllegal = false;
				foreach (var c in Completions) {
					anyIllegal = (c.Illegal || anyIllegal);
				}
				return anyIllegal;
			}
		}


		public decimal GetPercentage() {
			return Completions.Sum(x => x.RequiredCompleted) / (decimal)Completions.Sum(x => x.TotalRequired);
		}
	}
}

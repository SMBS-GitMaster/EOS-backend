using Pluralize.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Models.Terms {
  public class TermsPluralizer {

    private static IPluralize pluralizer = new Pluralizer();

    static TermsPluralizer() {
      pluralizer.AddIrregularRule("IDS", "IDS");
    }

    public static string Pluralize(string term) {
      return pluralizer.Pluralize(term);
    }
    public static string Singularize(string term) {
      return pluralizer.Singularize(term);
    }

  }
}

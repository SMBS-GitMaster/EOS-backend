using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Mutations {
  public class FeedbackSubmitModel {
    public long UserId { get; set; }
    [DefaultValue(null)]
    public double? Rating { get; set; }
  }

}

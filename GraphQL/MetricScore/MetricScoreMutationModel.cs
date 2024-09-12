using HotChocolate;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Mutations
{
  public class MetricScoreCreateModel
  {
    [Required]
    public string Value { get; set; }
    public double Timestamp { get; set; }
    public long MetricId { get; set; }
  }

  public class MetricScoreEditModel
  {
    public long Id { get; set; }
    [DefaultValue(null)] public Optional<string> Value { get; set; }
    [DefaultValue(null)] public Optional<string> NotesText { get; set; }
  }
}

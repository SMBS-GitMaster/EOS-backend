using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class SelectListQueryModel
  {
    public long Id { get; set; }
    public string Name { get; set; }
    public bool Disabled { get; set; }
    public string DisabledText { get; set; }
  }
}

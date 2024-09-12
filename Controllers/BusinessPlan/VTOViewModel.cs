using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Controllers.BusinessPlan {

  public class VTOViewModel {
    public long Id { get; set; }
    public bool IsPartial { get; set; }
    public bool OnlyCompanyWideRocks { get; set; }
    public long? VisionId { get; set; }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class OrgSettingsModel
  {

    #region Base Properties

    public long Id { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateCreated { get; set; }
    public double DateLastModified { get; set; }
    public string Type { get { return "orgSettings"; } }

    #endregion

    #region Properties

    public string WeekStart { get; set; }

    public long BusinessPlanId {get; set; }
    public long? V3BusinessPlanId { get; set; }

    public bool IsCoreProcessEnabled { get; set; }

    #endregion

  }
}

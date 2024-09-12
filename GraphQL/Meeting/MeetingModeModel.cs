using RadialReview.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class MeetingModeModel
  {

    #region Properties
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public bool Hidden { get; set; }
    public bool ImportLongTermIssues { get; set; }

    #endregion

    #region Public Methods

    public static MeetingModeModel ToMeetingModeModel(Mode mode)
    {
      return new MeetingModeModel()
      {
        Id = mode.Id,
        Name = mode.Name,
        Enabled = mode.Enabled,
        Hidden = mode.Hidden,
        ImportLongTermIssues = mode.ImportLongTermIssues,
      };
    }

    #endregion

  }
}
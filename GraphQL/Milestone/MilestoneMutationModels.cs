using HotChocolate.Types;
using RadialReview.Core.GraphQL.Models.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations
{

  public class MilestoneCreateModel
  {
    #region Properties

    public long RockId { get; set; }

    public string Title { get; set; }

    public double DueDate { get; set; }

    public bool Completed { get; set; }

    public string Status { get; set; }

    #endregion

  }


  public class MilestoneEditModel
  {

    #region Properties

    public long MilestoneId { get; set; }

    [DefaultValue(null)] public string Title { get; set; }

    public double? DueDate { get; set; }

    public bool? Completed { get; set; }

    [DefaultValue(null)] public string Status { get; set; }

    #endregion

  }
}

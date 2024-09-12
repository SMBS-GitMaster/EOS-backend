using HotChocolate;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations
{

  public class TodoCreateModel
  {
    #region Properties

    public string Title { get; set; }

    public double? DueDate { get; set; }

    public long AssigneeId { get; set; }

    public long? MeetingRecurrenceId { get; set; }

    public double? CompletedTimestamp { get; set; }

    [DefaultValue(null)] public string NotesId { get; set; }

    public ContextModel Context { get; set; }

    #endregion
  }


  public class TodoEditModel
  {
    #region Properties
    public long TodoId { get; set; }
    [DefaultValue(null)] public string Title { get; set; }
    public double? DueDate { get; set; }
    public long? AssigneeId { get; set; }
    public Optional<long?> MeetingRecurrenceId { get; set; }
    public Optional<double?> CompletedTimestamp { get; set; }
    public Optional<double?> ArchivedTimestamp { get; set; }
    [DefaultValue(null)] public string NotesId { get; set; }
    public ContextModel Context { get; set; }

    #endregion
  }
}

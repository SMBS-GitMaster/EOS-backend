using HotChocolate.Types;
using RadialReview.BusinessPlan.Models.Models;
using System.Collections.Generic;
using static RadialReview.Accessors.IssuesAccessor;

namespace RadialReview.GraphQL.Models.Mutations
{
  public class IssueCreateModel
  {

    #region Properties

    public string Title { get; set; }

    public long OwnerId { get; set; }

    public long RecurrenceId { get; set; }

    public bool? AddToDepartmentPlan { get; set; }

    [DefaultValue(null)] public string Notes { get; set; }

    [DefaultValue(null)] public string NotesId { get; set; }

    [DefaultValue(null)] public ContextModel Context { get; set; }
    [DefaultValue(null)] public List<long> FromMergedIds { get; set; }


    #endregion

  }

  public class IssueEditModel
  {
    #region Base Properties

    public long Id { get; set; }

    public long? AssigneeId { get; set; }

    public long? MeetingId { get; set; }

    [DefaultValue(null)] public string NotesId { get; set; }

    [DefaultValue(null)] public string Title { get; set; }

    public bool? AddToDepartmentPlan { get; set; }

    [DefaultValue(false)] public bool? Archived { get; set; }

    [DefaultValue(null)] public double? ArchivedTimestamp { get; set; }

    public bool? Completed { get; set; }

    public double? CompletedTimestamp { get; set; }

    [DefaultValue(null)] public int? NumStarVotes { get; set; }

    [DefaultValue(null)] public int? IssueNumber { get; set; }

    public int? PriorityVoteRank { get; set; }

    [DefaultValue(null)] public ContextModel Context { get; set; }

    [DefaultValue(null)] public IssueCompartment? IssueCompartment { get; set; }


    #endregion
  }

  public class IssueCreateSentToModel
  {
    #region Properties
    public long RecurrenceId { get; set; }
    public long IssueId { get; set; }
    #endregion
  }

  public class IssueSubmitStarVotesModel
  {
    public long RecurrenceId { get; set; }
    public List<IssueVotesModel> Votes { get; set; }
  }

  public class IssueVotesModel
  {
    public long IssueId { get; set; }
    public int NumberOfVotes { get; set; }
  }

  public class IssueSubmitPriorityVotesModel
  {

    public long MeetingId { get; set; }

    public long IssueId { get; set; }

    public long? VoteTimestamp { get; set; }

    //public long IssueId { get; set; }

    //public int Rank { get; set; }
  }

}
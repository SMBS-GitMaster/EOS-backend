using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.GraphQL.Models.Mutations;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Middleware.Services.NotesProvider;
using RadialReview.Models.L10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Repositories
{
  public partial interface IRadialReviewRepository
  {

    #region Queries

    IQueryable<HeadlineQueryModel> GetHeadlinesForUser(long userId, CancellationToken cancellationToken);

    HeadlineQueryModel GetHeadlineById(long id, CancellationToken cancellationToken);

    Task<IQueryable<HeadlineQueryModel>> GetHeadlinesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<IdModel> CreateHeadline(HeadlineCreateModel headlineCreateModel);

    Task<IdModel> EditHeadline(HeadlineEditModel headlineEditModel);
    Task<GraphQLResponse<bool>> CopyHeadlineToMeetings(CopyHeadlineToMeetingsModel copyHeadlineToMeetingsModel);

    #endregion

  }

  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public HeadlineQueryModel GetHeadlineById(long id, CancellationToken cancellationToken)
    {
      return RepositoryTransformers.HeadlineFromPeopleHeadline(HeadlineAccessor.GetHeadline(caller, id));
    }

    public async Task<IQueryable<HeadlineQueryModel>> GetHeadlinesForMeetings(IEnumerable<long> meetingIds, CancellationToken cancellationToken)
    {
      List<PeopleHeadline> headlines = new List<PeopleHeadline>();
      var results = meetingIds.SelectMany(recurrenceId => L10Accessor.GetHeadlinesForMeeting(caller, recurrenceId));
      var padIds = results.Select(x => x.HeadlinePadId).Where(x => x != null);
      var padTexts = await _notesProvider.GetHtmlForPads(padIds);

      return results.Select(x =>
      {
        string notesText = "";

        if (x.HeadlinePadId != null && padTexts.ContainsKey(x.HeadlinePadId))
        {
          notesText = padTexts[x.HeadlinePadId].ToString();
        }

        return x.HeadlineFromPeopleHeadline(notesText);
      }).AsQueryable();
    }

    public IQueryable<HeadlineQueryModel> GetHeadlinesForUser(long userId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        return HeadlineAccessor.GetHeadlinesForUser(caller, userId).Select(x => RepositoryTransformers.HeadlineFromPeopleHeadline(x));
      });
    }

    private IQueryable<HeadlineQueryModel> GetHeadlinesForMeetingRecurrance(long recurrenceId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        //throw new Exception("you have access to the caller, you dont need to rely on the requester (which introduces the possibilty of a permissions mistake");
        //throw new Exception("GetUserByEmail should never be used. you have x.Owner, you should be able to construct the UserModel from this object");
        //throw new Exception("UserModel.EmailAtOrganization is unreliable");
        //throw new Exception("UserModel.Id shouldn't be exposed");
        return L10Accessor.GetHeadlinesForMeeting(caller, recurrenceId).Select(x => RepositoryTransformers.TransformHeadline(x)).ToList();
      });
    }

    #endregion

    #region Mutations

    public async Task<IdModel> CreateHeadline(HeadlineCreateModel body)
    {
      if (body.Meetings == null || !body.Meetings.Any())
        throw new Exception("At least one Meeting Id is required");
      if (body.Meetings.Length != 1)
      {
        //NOTE: If you want this method to work with more than one meeting, find a way to duplicate notes across multiple pad ids
        throw new NotImplementedException("This method can only respond to one meeting right now.");
      }

      //foreach(var mid in body.Meetings){
      var headline = new PeopleHeadline()
      {
        Message = body.Title,
        OwnerId = body.Assignee,
        HeadlinePadId = body.NotesId,
        RecurrenceId = /* mid */ body.Meetings.Single(),
        OrganizationId = caller.Organization.Id,
      };
      await HeadlineAccessor.CreateHeadline(caller, headline);
      //}

      return new IdModel(headline.Id);

    }

    public async Task<IdModel> EditHeadline(HeadlineEditModel model)
    {
      await HeadlineAccessor.UpdateHeadline(caller, model);
      if (model.Meetings != null)
      {
        await HeadlineAccessor.CopyHeadline(caller, _notesProvider, model.HeadlineId, model.Meetings);
      }
      return new IdModel(model.HeadlineId);
    }

    public async Task<GraphQLResponse<bool>> CopyHeadlineToMeetings(CopyHeadlineToMeetingsModel copyHeadlineToMeetingsModel)
    {
      try
      {
        if (copyHeadlineToMeetingsModel.MeetingIds == null || !copyHeadlineToMeetingsModel.MeetingIds.Any())
          throw new Exception("At least one Meeting Id is required");

        List<NoteMeeting> meetingNotes = new List<NoteMeeting>();

        foreach (var meetingId in copyHeadlineToMeetingsModel.MeetingIds)
        {
          var newPadId = Guid.NewGuid().ToString();
          await _notesProvider.CreatePad(newPadId, copyHeadlineToMeetingsModel.NotesText);

          meetingNotes.Add(new NoteMeeting
          {
            MeetingId = meetingId,
            NotePadId = newPadId
          });
        }



        await HeadlineAccessor.CopyHeadlineToMeetings(caller, copyHeadlineToMeetingsModel.HeadlineToCopyId, meetingNotes);
        return GraphQLResponse<bool>.Successfully(true);

      }catch(Exception ex)
      {
        return GraphQLResponse<bool>.Error(ex);
      }
    }

    #endregion

  }
}

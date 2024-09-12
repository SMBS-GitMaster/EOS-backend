using RadialReview.Accessors;
using RadialReview.Core.GraphQL.Common.DTO;
using RadialReview.Core.Repositories;
using RadialReview.GraphQL.Models;
using RadialReview.GraphQL.Models.Mutations;
using RadialReview.Middleware.Services.NotesProvider;
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

    Task<IQueryable<TodoQueryModel>> GetTodosForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken, bool onlyActives);

    IQueryable<TodoQueryModel> GetTodosForUser(long userId, CancellationToken cancellationToken);

    TodoQueryModel GetTodoById(long id, CancellationToken cancellationToken);

    #endregion

    #region Mutations

    Task<TodoQueryModel> CreateTodo(TodoCreateModel todo);

    Task<GraphQLResponse<bool>> EditTodo(TodoEditModel todo);

    #endregion

  }


  public partial class RadialReviewRepository : IRadialReviewRepository
  {

    #region Queries

    public TodoQueryModel GetTodoById(long id, CancellationToken cancellationToken)
    {
      return RepositoryTransformers.TransformTodo(TodoAccessor.GetTodo(caller, _notesProvider, id));
    }

    public async Task<IQueryable<TodoQueryModel>> GetTodosForMeetings(IEnumerable<long> recurrenceIds, CancellationToken cancellationToken, bool onlyActives)
    {
      List<TodoQueryModel> todos = new List<TodoQueryModel>();

      foreach(long id in recurrenceIds)
      {
        var todosForRecurrence = await GetTodosForRecurrence(id, cancellationToken, onlyActives);
        todos.AddRange(todosForRecurrence);
      }

      return todos.AsQueryable();
    }

    public IQueryable<TodoQueryModel> GetTodosForUser(long userId, CancellationToken cancellationToken)
    {
      return DelayedQueryable.CreateFrom(cancellationToken, () =>
      {
        //throw new Exception("This method (correctly) does not use userId. I recommend removing it from the interface.");
        //throw new Exception("Never ever rely on user supplied userIds");
        return TodoAccessor.GetTodosForUser(caller, userId).Select(x => RepositoryTransformers.TransformTodo(x));
      });
    }

    private async Task<IQueryable<TodoQueryModel>> GetTodosForRecurrence(long recurrenceId, CancellationToken cancellationToken, bool onlyActives)
    {
      //throw new Exception("Shouldnt need to use a requester, use caller instead");
      //throw new Exception("fix issues from obsolete tag");
      var todos = L10Accessor.GetAllTodosForRecurrence(caller, recurrenceId, includeClosed: !onlyActives, range: null, includeDeleted: !onlyActives);
      var padIds = todos.Select(x => x.PadId).Where(x => x != null);
      var padTexts = await _notesProvider.GetHtmlForPads(padIds);

      return todos.Select(x =>
      {
        string notesText = "";

        if(x.PadId != null && padTexts.ContainsKey(x.PadId))
        {
          notesText = padTexts[x.PadId].ToString();
        }

        return x.TransformTodo(notesText);
      }).AsQueryable();
    }

    #endregion

    #region Mutations

    public async Task<TodoQueryModel> CreateTodo(TodoCreateModel todo)
    {
      TodoCreation creationModel;
      RadialReview.Models.Todo.TodoModel model;
      DateTime? dueDate = todo.DueDate.FromUnixTimeStamp();
      string contextTitle = todo.Context == null ? null : todo.Context.FromNodeTitle;
      string contextType = todo.Context == null ? null : todo.Context.FromNodeType;
      if (!todo.MeetingRecurrenceId.HasValue)
      {
        creationModel = TodoCreation.GeneratePersonalTodo(todo.Title, details: null, accountableUserId: caller.Id, dueDate: dueDate, padId: todo.NotesId, contextTitle: contextTitle, contextType: contextType);
        model = await TodoAccessor.CreateTodo(caller, creationModel, true, true);
      }
      else
      {
        creationModel = TodoCreation.GenerateL10Todo(todo.MeetingRecurrenceId.Value, todo.Title, null, todo.AssigneeId, dueDate, padId: todo.NotesId, contextTitle: contextTitle, contextType: contextType);
        model = await TodoAccessor.CreateTodo(caller, creationModel);
      }

      return RepositoryTransformers.TransformTodo(model);
    }

    public async Task<GraphQLResponse<bool>> EditTodo(TodoEditModel model)
    {
      bool duringMeeting = false;
      bool? completed = null;
      DateTime? closeTime = null;
      DateTime? dueDate = null;
      DateTime? completeDate = null;

      if (model.DueDate.HasValue)
        dueDate = model.DueDate.FromUnixTimeStamp();

      if (model.CompletedTimestamp.HasValue)
      {
        completeDate = model.CompletedTimestamp.Value.FromUnixTimeStamp() ?? null;
        completed = completeDate.HasValue;
        duringMeeting = completeDate.HasValue;
      }

      if (model.ArchivedTimestamp.HasValue)
        closeTime = model.ArchivedTimestamp.Value.FromUnixTimeStamp() ?? null;

      if (model.MeetingRecurrenceId.HasValue && model.MeetingRecurrenceId.Value == null)
      {
        // -1 is used in the UpdateTodo accessor to clear MeetingRecurrenceId
        model.MeetingRecurrenceId = -1;
      }

      try
      {
        if (model.MeetingRecurrenceId == 0) model.MeetingRecurrenceId = null;

        await TodoAccessor.UpdateTodo(caller, model.TodoId, completeTime: completeDate ?? DateTime.UtcNow, model.Title,
                                      dueDate, model.AssigneeId, closeTime: closeTime, completed,
                                      noteId: model.NotesId, recurrenceId: model.MeetingRecurrenceId, duringMeeting: duringMeeting);
        return GraphQLResponse<bool>.Successfully(true);
      }
      catch (Exception ex)
      {
        return GraphQLResponse<bool>.Error(ex);
      }
    }

    #endregion

  }
}
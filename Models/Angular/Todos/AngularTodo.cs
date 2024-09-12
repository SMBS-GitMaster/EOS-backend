using RadialReview.Models.Angular.Base;
using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Todo;
using Config = RadialReview.Utilities.Config;
using RadialReview.Models.Rocks;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RadialReview.Crosscutting.AttachedPermission;

namespace RadialReview.Models.Angular.Todos
{
  public class AngularTodo : BaseAngular, IAttachedPermission
  {
    public AngularTodo(TodoModel todo) : base(todo.Id)
    {
      Name = todo.Message;
      DetailsUrl = Config.BaseUrl(null, "/Todo/Pad/" + todo.Id);
      _PadId = todo.PadId;
      DeleteTime = todo.DeleteTime;
      CloseTime = todo.CloseTime;
      DueDate = todo.DueDate;
      Owner = AngularUser.CreateUser(todo.AccountableUser);
      CompleteTime = todo.CompleteTime;
      CreateTime = todo.CreateTime;
      Complete = todo.CompleteTime != null;
      TodoType = todo.TodoType;
      Ordering = todo.Ordering;
      Version = todo.Version;
      LastUpdatedBy = todo.LastUpdatedBy;
      DateLastModified = todo.DateLastModified;

      Origin = todo.ForRecurrence.NotNull(x => x.Name);

      if (Origin == null && todo.TodoType == Todo.TodoType.Personal)
      {
        Origin = "Personal To-Dos";

      }

      if (todo.ForRecurrenceId != null)
      {
        var id = todo.ForModelId == -1 ? todo.Id : todo.ForModelId;
        var mod = (todo.ForModel == "Transcript") ? "Transcript" : "TodoModel";
        Link = "/L10/Timeline/" + todo.ForRecurrenceId + "#" + mod + "-" + id;
        OriginId = todo.ForRecurrence.NotNull(x => x.Id);
        L10RecurrenceId = todo.ForRecurrence.NotNull(x => x.Id);
      }
      HasAudio = todo.HasAudio;
    }

    public AngularTodo(long Id) : base(Id)
    {
    }
    public AngularTodo(Milestone milestone, UserOrganizationModel owner, string origin = null) : base(-milestone.Id)
    {
      Name = milestone.Name;

      DueDate = milestone.DueDate;
      Owner = AngularUser.CreateUser(owner);
      CompleteTime = milestone.CompleteTime;
      CreateTime = milestone.CreateTime;
      Complete = milestone.Status == MilestoneStatus.Done;
      TodoType = Todo.TodoType.Milestone;
      Ordering = -10;
      Origin = origin ?? "Milestone";
      OriginId = milestone.RockId;
    }

    public AngularTodo()
    {
    }

    public string ContextTitle { get; set; }
    public string ContextType { get; set; }
    public string Name { get; set; }
    public string DetailsUrl { get; set; }
    public string Origin { get; set; }
    public long? OriginId { get; set; }
    public DateTime? DueDate { get; set; }
    public AngularUser Owner { get; set; }
    public DateTime? CloseTime { get; set; }
    public DateTime? CompleteTime { get; set; }
    [IgnoreDataMember]
    public DateTime? DeleteTime { get; set; }
    public DateTime? CreateTime { get; set; }
    public bool? Complete { get; set; }
    [IgnoreDataMember]
    public string Link { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    public TodoType? TodoType { get; set; }
    public long Ordering { get; set; }
    private string _PadId { get; set; }
    [IgnoreDataMember]
    public long? L10RecurrenceId { get; set; }
    public PermissionDto Permission { get; set; }
    public bool HasAudio { get; set; }
    public int Version { get; set; }
    public string LastUpdatedBy { get; set; }
    public double DateLastModified { get; set; }
    public string GetPadId()
    {
      return _PadId;
    }
  }
}
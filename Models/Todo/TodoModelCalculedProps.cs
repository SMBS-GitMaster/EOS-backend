using System;

namespace RadialReview.Models.Todo
{
    /*
     * Calculated properties, not mapped on db
     */
    public partial class TodoModel
    {
        public virtual bool IsNew
        {
            get
            {
                if (ForRecurrence?.L10MeetingInProgress?.StartTime is null)
                    return false;

                return CreateTime > ForRecurrence.L10MeetingInProgress.StartTime;
            }
        }
        public virtual bool IsLate => DueDate < DateTime.UtcNow;
        public virtual bool IsCompleted => CompleteTime != null;
        public virtual bool ShowLateTag => !IsCompleted && IsLate;
        public virtual bool ShowNewTag => !IsCompleted && !IsLate && IsNew;
    }
}
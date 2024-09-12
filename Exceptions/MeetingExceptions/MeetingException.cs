using System;
using RadialReview.Core.Properties;

namespace RadialReview.Exceptions.MeetingExceptions {
	public enum MeetingExceptionType {
		Invalid = 0,
		Unstarted = 1,
		TooMany = 2,
		AlreadyStarted = 3,

		Error = 100,
	}

	public class MeetingException : RedirectException {
		public MeetingExceptionType MeetingExceptionType { get; set; }
		public long RecurrenceId { get; set; }

		public MeetingException(long recurrenceId, String message, MeetingExceptionType exceptionType) : base(message) {
			MeetingExceptionType = exceptionType;
			RecurrenceId = recurrenceId;
		}

		public MeetingException(long recurrenceId, MeetingExceptionType exceptionType) : this(recurrenceId, ExceptionStrings.DefaultPermissionsException, exceptionType) {
		}
	}
}
using System;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.Askables;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using System.Runtime.Serialization;
using RadialReview.Crosscutting.AttachedPermission;
using System.Collections.Generic;
using RadialReview.Models.Application;

namespace RadialReview.Models.Angular.Rocks {
	public class AngularRock : BaseAngular, IAttachedPermission {
		public AngularRock() { }
		public AngularRock(long rockId) : base(rockId) { }

		public AngularRock(L10Meeting.L10Meeting_Rock meetingRock) : this(meetingRock.ForRock, meetingRock.VtoRock) {
		}

		public AngularRock(L10Recurrence.L10Recurrence_Rocks recurRock) : this(recurRock.ForRock, recurRock.VtoRock) {
			RecurrenceRockId = recurRock.Id;
			if (recurRock.DeleteTime != null)
				Archived = true;

		}

		public AngularRock(RockModel rock, bool? vtoRock) : base(rock.Id) {
			Name = rock.Rock;
			Owner = AngularUser.CreateUser(rock.AccountableUser);
			Complete = rock.CompleteTime != null;
			DueDate = rock.DueDate;
			Completion = rock.Completion;
			VtoRock = vtoRock;
			CreateTime = rock.CreateTime;
			Archived = rock.Archived;
			Origins = rock._Origins.NotNull(x => AngularList.Create(AngularListType.ReplaceAll, x));
			HasAudio = rock.HasAudio;
		}
		public string Name { get; set; }
		public AngularUser Owner { get; set; }
		public DateTime? DueDate { get; set; }
		public bool? Complete { get; set; }
		public RockState? Completion { get; set; }
		public DateTime? CreateTime { get; set; }
		public bool? Archived { get; set; }

		public long? RecurrenceRockId { get; set; }
		public bool? VtoRock { get; set; }
		[IgnoreDataMember]
		public long? ForceOrder { get; set; }
		public PermissionDto Permission { get; set; }
		public IEnumerable<NameId> Origins { get; set; }
		public bool HasAudio { get; set; }
	}
}

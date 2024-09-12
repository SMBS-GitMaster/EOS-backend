using NHibernate;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using System;
using System.Linq;
using System.Threading.Tasks;


namespace RadialReview.Accessors {
	public partial class L10Accessor {

		public class ShotClockVM {
			public bool valid { get; set; }
			public long meetingId { get; set; }
			public long modelId { get; set; }
			public string modelType { get; set; }
			public bool isRunning { get; set; }
			public DateTime lastStartTime { get; set; }
			public int accumulatedSeconds { get; set; }

			public ShotClockVM(ShotClock clock, long meetingId) {
				if (clock != null) {
					this.meetingId = clock.MeetingId;
					modelId = clock.ForModelId;
					modelType = clock.ForModelType;
					isRunning = clock.IsRunning;
					lastStartTime = clock.LastStartTime;
					accumulatedSeconds = clock.AccumulatedSeconds;
					valid = true;
				} else {
					this.meetingId = meetingId;
					valid = false;
				}
			}
		}

		public static ShotClockVM GetCurrentShotClock(UserOrganizationModel caller, long meetingId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					return GetCurrentShotClock(s, perms, meetingId);
				}
			}
		}

		private static ShotClockVM GetCurrentShotClock(ISession s, PermissionsUtility perms, long meetingId) {
			perms.ViewL10Meeting(meetingId);
			var clock = s.QueryOver<ShotClock>()
				.Where(x => x.Ended == false && x.MeetingId == meetingId)
				.Take(1).List().ToList()
				.FirstOrDefault();
			return new ShotClockVM(clock, meetingId);
		}

		public static async Task<ShotClock> ShotClock(UserOrganizationModel caller, long meetingId, ForModel forModel, bool? start) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);

						perms.ViewForModel(forModel);
						perms.ViewL10Meeting(meetingId);
						var recur = s.Get<L10Meeting>(meetingId);
						var orgId = recur.OrganizationId;
						var recurRt = rt.UpdateRecurrences(recur.Id);

						//SHOT CLOCKS ARE MEETING DEPENDENT...
						var clock = s.QueryOver<ShotClock>()
							.Where(x => x.ForModelId == forModel.ModelId && x.ForModelType == forModel.ModelType && x.MeetingId == meetingId)
							.List().ToList().FirstOrDefault();

						ShotClock output;

						if (start != null) {
							if (start == true) {
								//starting
								if (clock == null) {
									//clock doesnt exists. create.
									output = new ShotClock() {
										ForModelId = forModel.ModelId,
										ForModelType = forModel.ModelType,
										AccumulatedSeconds = 0,
										Ended = false,
										LastStartTime = DateTime.UtcNow,
										MeetingId = meetingId,
										OrgId = orgId,
										RecurrenceId = recur.Id,
										StartedBy = caller.Id,

									};
									s.Save(output);
								} else {
									//clock already exists
									if (clock.Ended) {
										//clock has ended. start again.
										output = clock;
										output.Ended = false;
										output.LastStartTime = DateTime.UtcNow;
										s.Update(output);
									} else {
										//clock already running.
										output = clock;
										if (DateTime.UtcNow - output.LastStartTime > TimeSpan.FromMinutes(300)) {
											//Too long. just add 15 minutes and call it good..
											output.LastStartTime = DateTime.UtcNow;
											output.AccumulatedSeconds += (int)TimeSpan.FromMinutes(15).TotalSeconds;
										}
									}
								}
							} else {
								//stopping
								if (clock == null) {
									//Clock doesnt exist? Create a finished one.
									output = new ShotClock() {
										ForModelId = forModel.ModelId,
										ForModelType = forModel.ModelType,
										AccumulatedSeconds = 0,
										Ended = true,
										LastStartTime = DateTime.MinValue,
										MeetingId = meetingId,
										OrgId = orgId,
										RecurrenceId = recur.Id,
										StartedBy = caller.Id,
									};
									s.Save(output);
								} else {
									//Clock exists.
									if (clock.Ended) {
										//clock has already ended?
										output = clock;
									} else {
										//clock running. stop it and calculate duration.
										output = clock;
										output.AccumulatedSeconds += (int)(DateTime.UtcNow - clock.LastStartTime).TotalSeconds;
										output.Ended = true;
										s.Update(output);
									}
								}
							}
							//Apply real time update
							recurRt.Call("receiveShotClock", new ShotClockVM(output, meetingId));
						} else {
							//start == null. Just getting the shotclock;
							if (clock == null) {
								//doesnt exist. create unstarted.
								output = new ShotClock() {
									ForModelId = forModel.ModelId,
									ForModelType = forModel.ModelType,
									AccumulatedSeconds = 0,
									Ended = true,
									LastStartTime = DateTime.MinValue,
									MeetingId = meetingId,
									OrgId = orgId,
									RecurrenceId = recur.Id,
									StartedBy = caller.Id,
								};
								s.Save(output);

							} else {
								output = clock;
							}
						}

						tx.Commit();
						s.Flush();

						return output;
					}
				}
			}
		}
	}
}
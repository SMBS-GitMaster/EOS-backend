using RadialReview.Models;
using RadialReview.Models.Downloads;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RadialReview.Hangfire;
using Hangfire;
using NHibernate;
using System.Text;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Models.Interfaces;
using RadialReview.Extensions;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Askables;
using RadialReview.Models.Todo;
using RadialReview.Models.Issues;
using static RadialReview.Accessors.FileAccessor;
using System.Threading;
using FluentNHibernate.Mapping;
using FFMpegCore.Pipes;
using NAudio.Wave;
using FFMpegCore;
using FFMpegCore.Arguments;
using RadialReview.Middleware.Services.BlobStorageProvider;
using RadialReview.Hangfire.Activator;
using FFMpegCore.Enums;

namespace RadialReview.Accessors {

	#region Models
	public enum AudioFormat {
		invalid,
		ogg,
		wav,
		webm
	}

	public class AudioDownloadModel {
		public virtual long Id { get; set; }
		public virtual long CallerId { get; set; }
		public virtual ForModel ForModel { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
		public AudioDownloadModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<AudioDownloadModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CallerId).Index("IDX_AudioDownloadModel_CallerId");
				Map(x => x.CreateTime);
				Map(x => x.CompleteTime);
				Component(x => x.ForModel).ColumnPrefix("ForModel_");
			}
		}
	}


	public class L10AudioModel {
		public virtual long Id { get; set; }
		public virtual long RecurrenceId { get; set; }
		public virtual long MeetingId { get; set; }
		public virtual ForModel ForModel { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual long FileId { get; set; }
		public virtual long CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual int DurationMs { get; set; }

		public virtual AudioFormat Format { get; set; }

		public L10AudioModel() {
		}
		public class Map : ClassMap<L10AudioModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.RecurrenceId);
				Map(x => x.MeetingId).Index("IDX_L10AudioModel_MeetingId");
				Component(x => x.ForModel).ColumnPrefix("ForModel_");
				Map(x => x.CreatedBy);
				Map(x => x.FileId);
				Map(x => x.DurationMs);
				Map(x => x.Format);
			}

		}
	}

	public class L10AudioAnchorModel {
		public virtual long Id { get; set; }
		public virtual long MeetingId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long CreatedBy { get; set; }
		public virtual long StartTime { get; set; }
		public virtual long? EndTime { get; set; }
		public virtual String Name { get; set; }
		public virtual ForModel ForModel { get; set; }

		public virtual DateTime CalcStart { get { return StartTime.ToDateTime(); } }
		public virtual DateTime CalcEnd { get { return EndTime.NotNull(x => x.Value.ToDateTime()); } }


		public L10AudioAnchorModel() {
			CreateTime = DateTime.UtcNow;
		}
		public class Map : ClassMap<L10AudioAnchorModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.MeetingId).Index("IDX_L10AudioAnchorModel_MeetingId");
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CreatedBy);
				Map(x => x.StartTime);
				Map(x => x.EndTime);
				Map(x => x.Name);
				Component(x => x.ForModel).ColumnPrefix("ForModel_");
			}

		}
	}


	public class AudioData {
		public long FileId { get; set; }
		public string Url { get; set; }
		public byte[] Bytes { get; set; }
		public AudioFormat FileFormat { get; set; }

		public string SampleBytes(int count = 30) {
			return Encoding.UTF8.GetString(Bytes.Take(count).ToArray());
		}

		public long StartTime { get; set; }
		public long EndTime { get; set; }
		public int TrimStart { get; set; }
		public int TrimEnd { get; set; }

		public DateTime CalcStart { get { return (StartTime + TrimStart).ToDateTime(); } }
		public DateTime CalcEnd { get { return (EndTime - TrimEnd).ToDateTime(); } }

		public long CreatorId { get; set; }
		public bool ContainsData() {
			return Bytes != null;
		}

		public AudioData() {
			TrimEnd = 0;
			TrimStart = 0;
		}

		public AudioData Clone() {
			return new AudioData() {
				FileId = FileId,
				Url = Url,
				FileFormat = FileFormat,
				Bytes = Bytes,
				StartTime = StartTime,
				CreatorId = CreatorId,
				EndTime = EndTime,
				TrimEnd = TrimEnd,
				TrimStart = TrimStart
			};
		}
	}

	#endregion

	public class AudioAccessor : BaseAccessor {

		#region Getters
		public async static Task<long> GetMergedAudioFilesForMeeting(UserOrganizationModel caller, long meetingId, bool notify) {
			long fileId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meetingId);
					fileId = FileAccessor.SaveGeneratedFilePlaceholder_Unsafe(s, caller.Id, "MergedAudio/" + Guid.NewGuid(), "ogg", "MID:" + meetingId, FileOrigin.TemporaryAutoGenerated, FileOutputMethod.Trigger, ForModel.Create<L10Meeting>(meetingId), new[] { PermTiny.Creator(true, false, false) });
					tx.Commit();
					s.Flush();
				}
			}
			Scheduler.Enqueue(() => GetMergedAudioFilesForMeeting_Hangfire(caller.Id, meetingId, fileId, notify, default(IBlobStorageProvider), CancellationToken.None));
			return fileId;
		}

		public async static Task<long> GetMergedAudioFilesForModel(UserOrganizationModel caller, long? meetingId, IForModel forModel,bool notify) {
			long fileId;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					if (meetingId != null) {
						perms.ViewL10Meeting(meetingId.Value);
					}
					perms.ViewForModel(forModel);

					fileId = FileAccessor.SaveGeneratedFilePlaceholder_Unsafe(s, caller.Id, "MergedAudio/" + Guid.NewGuid(), "ogg", "MID:" + meetingId + " FM:" + forModel.ToKey(), FileOrigin.TemporaryAutoGenerated, FileOutputMethod.Trigger, forModel, new[] { PermTiny.Creator(true, false, false) });
					tx.Commit();
					s.Flush();
				}
			}
			Scheduler.Enqueue(() => GetMergedAudioFilesForModel_Hangfire(caller.Id, meetingId, fileId, forModel.ToKey(), notify, default(IBlobStorageProvider), CancellationToken.None));
			return fileId;
		}



		#endregion
		#region Writers



		public async static Task<long> SaveL10Audio(UserOrganizationModel caller, IBlobStorageProvider storageProvider, long userId, long startTime, long endTime, AudioFormat fileFormat, Stream audio, long recurrenceId, long meetingId, ForModel forModel) {
			string recurrName;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					perms.ViewL10Recurrence(recurrenceId);
					perms.ViewL10Meeting(meetingId);
					recurrName = s.Get<L10Recurrence>(recurrenceId).Name;
				}
			}

			var permtypes = new[]{
				PermTiny.Creator(true,false,true),
				PermTiny.Members(true,false,true),
			};

			var tags = new List<TagModel>() {
				TagModel.Create("Meeting Recording"),
				TagModel.Create<L10Recurrence>(recurrenceId, recurrName),
			};

			if (forModel != null) {
				tags.Add(TagModel.Create(null, forModel));
			}

			var fileId = await FileAccessor.Save_Unsafe(storageProvider, userId, audio, "L10Audio/" + Guid.NewGuid(), "audio",
				string.Format("RID:{0} MID:{1} FMID:{2}", recurrenceId, meetingId, forModel.ToKey()),
				FileOrigin.Uploaded, FileOutputMethod.Save, ForModel.Create<L10Meeting>(meetingId),
				FileNotification.DoNotNotify(), permtypes, tags.ToArray()
			);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					s.Save(new L10AudioModel() {
						CreateTime = startTime,
						DurationMs = (int)(endTime - startTime),
						CreatedBy = userId,
						FileId = fileId,
						ForModel = forModel,
						MeetingId = meetingId,
						RecurrenceId = recurrenceId,
						Format = fileFormat,
					});
					tx.Commit();
					s.Flush();
				}
			}
			return fileId;
		}

		public async static Task<long> SetAnchor(UserOrganizationModel caller, long meetingId, string friendlyName, IForModel forModel, long startTime, long? endTime = null, string connectionId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meetingId);

					var recurrenceId = s.Get<L10Meeting>(meetingId).L10RecurrenceId;

					var clearOut = s.QueryOver<L10AudioAnchorModel>().Where(x => x.DeleteTime == null && x.MeetingId == meetingId && x.EndTime == null).List().ToList();
					foreach (var c in clearOut) {
						c.EndTime = startTime;
						s.Update(c);
					}

					var model = new L10AudioAnchorModel {
						Name = friendlyName,
						CreatedBy = caller.Id,
						StartTime = startTime,
						EndTime = endTime,
						ForModel = forModel.ToImpl(),
						MeetingId = meetingId,
					};

					s.Save(model);

					try {
						if (forModel.Is<RockModel>()) {
							perms.ViewRock(forModel.ModelId);
							var r = s.Get<RockModel>(forModel.ModelId);
							r.HasAudio = true;
							s.Update(r);
						}
						if (forModel.Is<TodoModel>()) {
							perms.ViewTodo(forModel.ModelId);
							var r = s.Get<TodoModel>(forModel.ModelId);
							r.HasAudio = true;
							s.Update(r);
						}
						if (forModel.Is<IssueModel.IssueModel_Recurrence>()) {
							perms.ViewIssueRecurrence(forModel.ModelId);
							var r = s.Get<IssueModel.IssueModel_Recurrence>(forModel.ModelId);
							r.HasAudio = true;
							s.Update(r);
						}
						if (forModel.Is<PeopleHeadline>()) {
							perms.ViewHeadline(forModel.ModelId);
							var r = s.Get<PeopleHeadline>(forModel.ModelId);
							r.HasAudio = true;
							s.Update(r);
						}
					} catch (Exception e) {
					}





					tx.Commit();
					s.Flush();


					await using (var rt = RealTimeUtility.Create(connectionId)) {
						rt.UpdateRecurrences(recurrenceId).Call("receiveAudioIndicator", forModel.ToKey());
					}


					return model.Id;
				}
			}
		}


		#endregion
		#region HangFire

		public static TimeSpan MAX_DURATION = TimeSpan.FromSeconds(360);

		public class Timeout {
			public DateTime StartTime { get; set; }
			public TimeSpan MaxDuration { get; set; }
			public CancellationToken Token { get { return OuterTokenSource.Token; } }
			private CancellationTokenSource OuterTokenSource;
			public Timeout(TimeSpan maxDuration, CancellationToken? requestToken) {
				requestToken = requestToken ?? CancellationToken.None;
				var timeoutTokenSource = new CancellationTokenSource(maxDuration);
				OuterTokenSource = CancellationTokenSource.CreateLinkedTokenSource(requestToken.Value, timeoutTokenSource.Token);
				StartTime = DateTime.UtcNow;
				MaxDuration = maxDuration;
			}

			public bool IsTimedOut() {
				return OuterTokenSource.IsCancellationRequested || DateTime.UtcNow - StartTime > MaxDuration;
			}

			public void ThrowIfTimeout() {
				if (IsTimedOut()) {
					throw new TimeoutException("Audio took too long...");
				}
			}

		}

		private static long LockProcessing(ISession s, long callerId, IForModel forModel) {
			var minStartTime = DateTime.UtcNow - MAX_DURATION;

			var active = s.QueryOver<AudioDownloadModel>().Where(x => (x.CompleteTime == null && x.CreateTime > minStartTime) && x.CallerId == callerId).List().ToList();

			if (active.Count > 3) {
				var minTime = active.Select(x => x.CreateTime).OrderBy(x => x).First();
				var remaining = (int)Math.Ceiling((MAX_DURATION - (DateTime.UtcNow - minTime)).TotalMinutes);
				var remainStr = remaining + " minutes";
				if (remaining == 1) {
					remainStr = "1 minute";
				}

				throw new IOException("Downloading too many concurrent files. Please wait " + remainStr + " and try again.");
			}
			var a = new AudioDownloadModel() {
				CallerId = callerId,
				ForModel = forModel.ToImpl(),
			};
			s.Save(a);
			return a.Id;
		}

		private static bool UnlockProcessing(ISession s, long lockId) {
			if (lockId > 0) {
				var a = s.Get<AudioDownloadModel>(lockId);
				a.CompleteTime = DateTime.UtcNow;
				s.Update(a);
				return true;
			}
			return false;
		}

		[Queue(HangfireQueues.Immediate.AUDIO)]
		[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
		public static async Task<long> GetMergedAudioFilesForMeeting_Hangfire(long callerId, long meetingId, long fileId, bool notify, [ActivateParameter] IBlobStorageProvider bsp, CancellationToken token) {
			long lockId = 0;
			try {
				var timeout = new Timeout(MAX_DURATION, token);

				bool removeEmptySpace = true;
				UserOrganizationModel caller;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						caller = s.Get<UserOrganizationModel>(callerId);
						lockId = LockProcessing(s, callerId, ForModel.Create<L10Meeting>(meetingId));
						tx.Commit();
						s.Flush();
					}
				}
				var audioData = await GetAudioFilesForMeeting(caller, bsp, meetingId, true, timeout);
				var notification = notify ? FileNotification.NotifyCaller() : FileNotification.DoNotNotify();
				return await MergeAudioAndSaveFile(bsp, fileId, removeEmptySpace, audioData, timeout, notification);
			} catch (Exception e) {
				await ProcessException(fileId, e);
				throw;
			} finally {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						UnlockProcessing(s, lockId);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}

		[Queue(HangfireQueues.Immediate.AUDIO)]
		[AutomaticRetry(Attempts = 0, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
		public static async Task<long> GetMergedAudioFilesForModel_Hangfire(long callerId, long? meetingId, long fileId, string forModelKey, bool notify, [ActivateParameter] IBlobStorageProvider bsp, CancellationToken token) {
			long lockId = 0;

			try {
				var timeout = new Timeout(MAX_DURATION, token);


				var forModel = forModelKey.ForModelFromKey();
				bool removeEmptySpace = true;
				UserOrganizationModel caller;
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						caller = s.Get<UserOrganizationModel>(callerId);
						lockId = LockProcessing(s, callerId, forModel);
						tx.Commit();
						s.Flush();
					}
				}
				var audioData = await GetAudioFilesForModel(caller, bsp, meetingId, forModel, true, timeout);
				var notification = notify ? FileNotification.NotifyCaller() : FileNotification.DoNotNotify();
				return await MergeAudioAndSaveFile(bsp, fileId, removeEmptySpace, audioData, timeout, notification);
			} catch (Exception e) {
				await ProcessException(fileId, e);
				throw;
			} finally {
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						UnlockProcessing(s, lockId);
						tx.Commit();
						s.Flush();
					}
				}
			}
		}
		#endregion
		#region Helpers

		private static async Task ProcessException(long fileId, Exception e) {
			try {
				var message = "Error downloading audio.";
				if (e is TimeoutException || e is IOException) {
					message = e.Message;
				}
				if (e is OperationCanceledException) {
					message = "Request canceled. Audio download took too long.";
				}
				using (var s = HibernateSession.GetCurrentSession()) {
					using (var tx = s.BeginTransaction()) {
						var f = s.Get<EncryptedFileModel>(fileId);
						await using (var rt = RealTimeUtility.Create()) {
							rt.UpdateUsers(f.CreatorId).Call("fileCompleted", new FileCompleteModel(fileId, f.FileName, FileOutputMethod.Error) {
								Error = e.Message
							});
						}
					}
				}
			} catch (Exception) {
			}
		}

		private static async Task<long> MergeAudioAndSaveFile(IBlobStorageProvider storageProvider, long fileId, bool removeEmptySpace, List<AudioData> audioData, Timeout timeout, FileNotification notify) {
			var toDispose = new List<IDisposable>();
			try {
				using (var mixer = new WaveMixerStream32 { AutoStop = true, }) {
					await _MergeAudio(removeEmptySpace, audioData, timeout, toDispose, mixer);
					return await _SaveAudio(storageProvider, fileId, removeEmptySpace, timeout, mixer, notify);
				}
			} finally {
				foreach (var d in toDispose) {
					try {
						d?.Dispose();
					} catch (Exception e) {
					}
				}
			}
		}

		private static async Task _MergeAudio(bool removeEmptySpace, List<AudioData> audioData, Timeout timeout, List<IDisposable> toDispose, WaveMixerStream32 mixer) {
			ReformulateStartTimes(audioData, removeEmptySpace);
			foreach (var file in audioData) {
				timeout.ThrowIfTimeout();
				try {
					var offset = file.StartTime + file.TrimStart;
					if (file.Bytes.Any()) {
						using (var input = new MemoryStream(file.Bytes)) {
							var output = new MemoryStream();
							toDispose.Add(output);
							await ConvertFFMpeg(input, output, file.FileFormat, AudioFormat.wav, false, timeout.Token);

							var reader = new WaveFileReader(output);
							toDispose.Add(reader);
							var offsetStream = new WaveOffsetStream(reader);
							toDispose.Add(offsetStream);
							offsetStream.StartTime = TimeSpan.FromMilliseconds(offset);
							offsetStream.SourceOffset = TimeSpan.FromMilliseconds(file.TrimStart);
							offsetStream.SourceLength = Math2.Max(TimeSpan.Zero, TimeSpan.FromMilliseconds(file.EndTime - file.StartTime) - TimeSpan.FromMilliseconds(file.TrimStart + file.TrimEnd));
							var waveStream = WaveFormatConversionStream.CreatePcmStream(offsetStream);
							toDispose.Add(waveStream);
							var channel = new WaveChannel32(waveStream) { };
							toDispose.Add(channel);
							mixer.AddInputStream(channel);

						}
					}
				} catch (TimeoutException) {
					throw;
				} catch (Exception e) {
					int a = 0;
				}
			}
		}

		private static async Task<long> _SaveAudio(IBlobStorageProvider storageProvider, long fileId, bool removeEmptySpace, Timeout timeout, WaveMixerStream32 mixer, FileNotification notify) {
			try {
				if (mixer.Length != 0) {

					using (var s = HibernateSession.GetCurrentSession()) {
						using (var tx = s.BeginTransaction()) {
							using (var source = new MemoryStream()) {
								using (var destination = new MemoryStream()) {
									var provider = new Wave32To16Stream(mixer);
									WaveFileWriter.WriteWavFileToStream(source, provider);
									source.Position = 0;
									await ConvertFFMpeg(source, destination, AudioFormat.wav, AudioFormat.webm, removeEmptySpace, timeout.Token);
									await FileAccessor.Save_Unsafe(s, storageProvider, fileId, destination, notify);
									tx.Commit();
									s.Flush();
									return fileId;
								}
							}
						}
					}
				} else {
					throw new IOException("Audio file empty");
				}
			} catch (Exception e) {
				var errorCode = -4;
				if (e is TimeoutException) {
					errorCode = -2;
				} else if (e is IOException) {
					errorCode = -3;
				}
				await ProcessException(fileId, e);
				return errorCode;

			}
		}


		private static async Task<Tuple<string, byte[]>> GetAudio(string url, CancellationToken cancel) {
			if (url.StartsWith("/")) {
				throw new Exception("Cannot download audio from local file. Please turn on remote storage in the web.config");
			}

			try {
				if (string.IsNullOrWhiteSpace(url)) {
					return Tuple.Create(url, new byte[0]);
				}

				var client = new HttpClient();
				HttpResponseMessage response = await client.GetAsync(url, cancel);
				cancel.ThrowIfCancellationRequested();
				HttpContent responseContent = response.Content;
				using (var memoryStream = new MemoryStream()) {
					(await responseContent.ReadAsStreamAsync()).CopyTo(memoryStream);
					return Tuple.Create(url, memoryStream.ToArray());

				}
			} catch (Exception e) {
				log.Error("Error GetAudio", e);
				return Tuple.Create(url, new byte[0]);
			}
		}

		public class RemoveSilenceArgument : IArgument {
			public RemoveSilenceArgument(int stopDuration = 1, int stopThresholdDb = -40, int stopPeriods = -1) {
				StopThresholdDb = stopThresholdDb;
				StopDuration = stopDuration;
				StopPeriods = stopPeriods;
			}
			public int StopThresholdDb { get; set; }
			public int StopDuration { get; set; }
			public int StopPeriods { get; set; }
			public string Text => $@"-af silenceremove=stop_periods={StopPeriods}:stop_duration={StopDuration}:stop_threshold={StopThresholdDb}dB";
		}

		public class HideBannerArgument : IArgument {
			public string Text => "-hide_banner";
		}


		private static async Task ConvertFFMpeg(MemoryStream source, MemoryStream destination, AudioFormat? fromType, AudioFormat? toType, bool removeEmptySpace, CancellationToken cancellationToken) {
			if (source.Length > 3E9)
				throw new IOException("Audio is too large: " + (int)(source.Length / 1e+6) + "mb");

			var outputFile = "/tmp/g" + Guid.NewGuid().ToString().Replace("-", "") + toType.NotNull(x => "." + x);
			try {

				cancellationToken.ThrowIfCancellationRequested();
				var options = FFMpegArguments
					.FromPipeInput(new StreamPipeSource(source), x => {
						if (fromType != null)
							x.ForceFormat("" + fromType.Value);
						x.OverwriteExisting();
						x.WithArgument(new HideBannerArgument());
					}).OutputToFile(outputFile, true, x => {
						if (toType != null)
							x.ForceFormat("" + toType);
						x.WithSpeedPreset(Speed.UltraFast);
						if (removeEmptySpace) {
							x.WithArgument(new RemoveSilenceArgument(1, -40, -1));
						}
					});
				await options.CancellableThrough(cancellationToken).ProcessAsynchronously();
				using (FileStream file = new FileStream(outputFile, FileMode.Open, FileAccess.Read)) {
					await file.CopyToAsync(destination);
				}
				destination.Position = 0;
				return;
			} catch (OperationCanceledException e) {
				throw new TimeoutException("Audio conversion took too long");
			} catch (Exception e) {
				if (Config.IsLocal()) {
					throw;
				} else {
					throw new Exception("Error converting audio");
				}
			} finally {
				if (File.Exists(outputFile)) {
					File.Delete(outputFile);
				}
			}





		}


		private async static Task<List<AudioData>> GetAudioFilesForModel(UserOrganizationModel caller, IBlobStorageProvider bsp, long? meetingId, IForModel forModel, bool download, Timeout timeout) {
			List<AudioData> audioData;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					if (meetingId != null) {
						perms.ViewL10Meeting(meetingId.Value);
					}
					perms.ViewForModel(forModel);


					var anchorQ = s.QueryOver<L10AudioAnchorModel>().Where(x => x.DeleteTime == null && x.ForModel.ModelId == forModel.ModelId && x.ForModel.ModelType == forModel.ModelType);

					if (meetingId != null) {
						anchorQ = anchorQ.Where(x => x.MeetingId == meetingId);
					}

					var ranges = anchorQ.List().ToList();


					long[] meetingIds;

					if (meetingId != null) {
						meetingIds = new[] { meetingId.Value };
					} else {
						meetingIds = ranges.Select(x => x.MeetingId).Distinct().Where(x => {
							try {
								perms.ViewL10Meeting(x);
								return true;
							} catch (Exception e) {
								return false;
							}
						}).ToArray();
					}

					var allAudios = s.QueryOver<L10AudioModel>()
								.Where(x => x.DeleteTime == null)
								.WhereRestrictionOn(x => x.MeetingId).IsIn(meetingIds)
								.Select(x => x.FileId, x => x.CreateTime, x => x.CreatedBy, x => x.DurationMs, x => x.Format)
								.List<object[]>()
								.Select(x => new AudioData {
									FileId = (long)x[0],
									StartTime = (long)x[1],
									EndTime = ((long)x[1] + (int)x[3]),
									CreatorId = (long)x[2],
									FileFormat = (AudioFormat)x[4]
								}).ToList();

					audioData = new List<AudioData>();
					foreach (var r in ranges) {
						foreach (var a in allAudios) {
							if (r.StartTime <= a.EndTime && (r.EndTime ?? long.MaxValue) >= a.StartTime) {
								var clone = a.Clone();
								if (a.StartTime < r.StartTime) {
									clone.TrimStart = (int)(r.StartTime - a.StartTime);
								}

								if (r.EndTime != null && r.EndTime < a.EndTime) {
									clone.TrimEnd = (int)(a.EndTime - r.EndTime.Value);
								}
								audioData.Add(clone);
							}
						}
					}
				}
			}
			return await PopulateAudioData(audioData, caller, bsp, download, timeout);
		}


		private async static Task<List<AudioData>> GetAudioFilesForMeeting(UserOrganizationModel caller, IBlobStorageProvider bsp, long meetingId, bool download, Timeout timeout) {
			List<AudioData> audioData;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meetingId);


					audioData = s.QueryOver<L10AudioModel>()
									.Where(x => x.DeleteTime == null && x.MeetingId == meetingId)
									.Select(x => x.FileId, x => x.CreateTime, x => x.CreatedBy, x => x.DurationMs, x => x.Format)
									.List<object[]>()
									.Select(x => new AudioData {
										FileId = (long)x[0],
										StartTime = (long)x[1],
										EndTime = ((long)x[1] + (int)x[3]),
										CreatorId = (long)x[2],
										FileFormat = (AudioFormat)x[4]
									}).ToList();
				}
			}

			return await PopulateAudioData(audioData, caller, bsp, download, timeout);
		}

		private static async Task<List<AudioData>> PopulateAudioData(List<AudioData> audioData, UserOrganizationModel caller, IBlobStorageProvider bsp, bool download, Timeout timeout) {
			var urlLookup = await FileAccessor.GetFileUrls(caller, bsp, audioData.Select(x => x.FileId).ToList(), false, timeout.Token);
			foreach (var a in audioData) {
				a.Url = urlLookup[a.FileId];
			}

			DefaultDictionary<string, byte[]> fileData = null;
			if (download) {

				var tasks = audioData.Distinct(x => x.FileId).Select(x => GetAudio(x.Url, timeout.Token));
				var jobs = (await Task.WhenAll(tasks));
				timeout.ThrowIfTimeout();
				fileData = jobs.ToDefaultDictionary(x => x.Item1, x => x.Item2, x => new byte[0]);
				foreach (var a in audioData) {
					a.Bytes = fileData[a.Url];
					if (a.SampleBytes(120).StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Error><Code>NoSuchKey</Code><Message>The specified key does not exist.</Message>")) {
						a.Bytes = new byte[0];
					}
					timeout.ThrowIfTimeout();
				}
			}
			return audioData;
		}

		private static Task<Tuple<string, byte[]>> GetAudio(string url, object token) {
			throw new NotImplementedException();
		}

		private static void ReformulateStartTimes(List<AudioData> audioData, bool removeEmptySpace) {

			long min = long.MaxValue;
			foreach (var f in audioData) {
				min = Math.Min(f.StartTime, Math.Min(f.EndTime, min));
			}

			foreach (var a in audioData) {
				a.StartTime -= min;
				a.EndTime -= min;
			}


			if (removeEmptySpace) {
				var minPadding = TimeSpan.FromSeconds(1);
				var dateRegions = new List<Tuple<long, bool, AudioData>>();
				foreach (var a in audioData.OrderBy(x => x.StartTime)) {
					dateRegions.Add(Tuple.Create(a.StartTime + a.TrimStart, true, a));
					dateRegions.Add(Tuple.Create(a.EndTime - a.TrimEnd, false, a));
				}

				dateRegions = dateRegions.OrderBy(x => x.Item1).ToList();
				var count = 0;
				int sub = 0;
				for (var i = 0; i < dateRegions.Count; i++) {
					var a = dateRegions[i];
					if (a.Item2 == true) {
						count += 1;
						a.Item3.StartTime -= sub;
						a.Item3.EndTime -= sub;
					} else {
						count -= 1;
					}

					if (count == 0 && i != dateRegions.Count - 1) {
						var diff = (int)(dateRegions[i + 1].Item1 - a.Item1) ;
						if (diff > 0) {
							sub += diff;
						}
					}
				}
			}
		}

		public static async Task SetRecordMeeting(UserOrganizationModel caller, long meetingId, bool status) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ViewL10Meeting(meetingId);

					var meeting = s.Get<L10Meeting>(meetingId);
					var recurrenceId = meeting.L10RecurrenceId;
					meeting.IsRecording = status;
					if (status) {
						meeting.HasRecording = true;
					}

					tx.Commit();
					s.Flush();
					await using (var rt = RealTimeUtility.Create()) {
						rt.UpdateRecurrences(recurrenceId).Call("setGlobalRecording", status);
					}
				}
			}
		}


		#endregion
	}
}

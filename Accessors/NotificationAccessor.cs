using Hangfire;
using NHibernate;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Crosscutting.Hooks.Interfaces;
using RadialReview.Crosscutting.Schedulers;
using RadialReview.Exceptions;
using RadialReview.Hangfire;
using RadialReview.Models;
using RadialReview.Models.Notifications;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RadialReview.Accessors {
	public class NotificationAccessor : BaseAccessor {


		public static async Task<UserDevice> TryRegisterPhone(string userName, string deviceId, string deviceType, string deviceVersion) {
			if (userName == null || deviceId == null || deviceType == null || deviceVersion == null) {
				return null;
			}

			userName = userName.ToLower();

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var found = s.QueryOver<UserDevice>()
						.Where(x => x.DeleteTime == null && x.DeviceId == deviceId && x.UserName == userName)
						.SingleOrDefault();

					if (found == null) {
						found = new UserDevice {
							DeviceId = deviceId,
							UserName = userName,
							DeviceType = deviceType,
							DeviceVersion = deviceVersion,
						};
						s.Save(found);
					} else {
						found.LastUsed = DateTime.UtcNow;
						found.DeviceVersion = deviceVersion;
						s.Update(found);
					}

					tx.Commit();
					s.Flush();

					return found;
				}
			}
		}

		public static async Task MarkAllSeen(UserOrganizationModel caller, long userId, NotificationDevices devices) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);
					var found = NotificationQuery_unsafe(s, userId, null, devices).List().ToList();
					var now = DateTime.UtcNow;
					foreach (var f in found) {
						if (f.Seen == null && f.CanBeMarkedSeen) {
							f.Seen = now;
							s.Update(f);
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			if (devices.HasFlag(NotificationDevices.Computer)) {
				await using (var rt = RealTimeUtility.Create()) {
					rt.UpdateUsers(userId).Call("clearNotifications");
				}
			}
		}

		public static async Task MarkSeen(UserOrganizationModel caller, long notificationId, bool force = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var notification = s.Get<NotificationModel>(notificationId);
					perms.Self(notification.UserId);
					if (notification.Seen == null) {
						if (force || notification.CanBeMarkedSeen) {
							notification.Seen = DateTime.UtcNow;
						}
					}
					tx.Commit();
					s.Flush();

					if (notification.OnComputer) {
						await using (var rt = RealTimeUtility.Create()) {
							rt.UpdateUsers(notification.UserId).Call("clearNotification", notificationId);
						}
					}
				}
			}
		}

		public static async Task MarkUnseen(UserOrganizationModel caller, long notificationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var notification = s.Get<NotificationModel>(notificationId);
					perms.Self(notification.UserId);
					var added = false;
					if (notification.Seen != null) {
						added = true;
					}

					notification.Seen = null;
					tx.Commit();
					s.Flush();

					if (notification.OnComputer) {
						await using (var rt = RealTimeUtility.Create()) {
							rt.UpdateUsers(notification.UserId).Call("unclearNotification", notificationId, added);
						}
					}
				}
			}
		}

		public static async Task MarkGroupSeenUnsafe(NotificationGroupKey groupKey, bool force) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await MarkGroupSeenUnsafe(s, groupKey, force);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task MarkGroupSeenUnsafe(ISession s, NotificationGroupKey groupKey, bool force) {

			if (groupKey == null || string.IsNullOrWhiteSpace(groupKey.ToString())) {
				throw new ArgumentOutOfRangeException(nameof(groupKey));
			}

			var q = s.QueryOver<NotificationModel>().Where(x => x.GroupKey == groupKey.ToString() && (x.DeleteTime == null || DateTime.UtcNow < x.DeleteTime) && x.Seen == null);
			if (!force) {
				q = q.Where(x => x.CanBeMarkedSeen == true);
			}
			var notifications = q.List().ToList();
			var now = DateTime.UtcNow;
			foreach (var n in notifications) {
				n.Seen = now;
				s.Update(n);
			}
			foreach (var n in notifications) {
				if (n.OnComputer) {
					await using (var rt = RealTimeUtility.Create()) {
						rt.UpdateUsers(n.UserId).Call("clearNotification", n.Id);
					}
				}
			}
		}

		public static async Task SetNotificationStatus(UserOrganizationModel caller, long notificationId, NotificationStatus status) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var n = s.Get<NotificationModel>(notificationId);
					perms.Self(n.UserId);
					var oldStatus = n.GetStatus();
					var updates = new INotificationHookUpdates();

					if (oldStatus != status) {
						updates.StatusChanged = true;
						switch (status) {
							case NotificationStatus.Unread:
								n.DeleteTime = null;
								n.Seen = null;
								break;
							case NotificationStatus.Read:
								n.Seen = DateTime.UtcNow;
								break;
							case NotificationStatus.Delete:
								n.DeleteTime = DateTime.UtcNow;
								n.Seen = n.Seen ?? n.DeleteTime;
								break;
							default:
								break;
						}
						s.Update(n);
					}

					await HooksRegistry.Each<INotificationHook>((ses, x) => x.UpdateNotification(ses, n, updates));

					tx.Commit();
					s.Flush();
				}
			}
		}

		public static NotificationModel GetNotification(UserOrganizationModel caller, long notificationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var notification = s.Get<NotificationModel>(notificationId);
					perms.Self(notification.UserId);
					return notification;
				}
			}
		}

		public static async Task<List<NotificationModel>> GetNotificationsForUser(UserOrganizationModel caller, long userId, DateTime? includeSeenAfter = null, NotificationDevices devices = NotificationDevices.All, bool markSeen = false) {

			List<NotificationModel> results;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					results = GetNotificationsForUser(s, perms, userId, includeSeenAfter, devices);
				}
			}
			if (markSeen) {
				await MarkAllSeen(caller, userId, devices);
			}
			return results;
		}

		public static List<NotificationModel> GetNotificationsForUser(ISession s, PermissionsUtility perms, long userId, DateTime? includeSeenAfter, NotificationDevices devices) {
			perms.Self(userId);// must be self.. 
			return GetNotificationsForUser_Unsafe(s, userId, includeSeenAfter, devices);
		}

		public static List<NotificationModel> GetNotificationsForUser_Unsafe(ISession s, long userId, DateTime? includeSeenAfter, NotificationDevices devices) {
			return NotificationQuery_unsafe(s, userId, includeSeenAfter, devices)
				.List().OrderByDescending(x => x.Seen == null)
				.ThenByDescending(x => x.CreateTime)
				.ToList();
		}

		public class TinyNotificationPlaceholder {
			public long Id { get; set; }
			public bool CanMarkSeen { get; set; }
		}

		private static IQueryOver<NotificationModel, NotificationModel> NotificationQuery_unsafe(ISession s, long userId, DateTime? includeSeenAfter, NotificationDevices devices) {
			var uids = s.Get<UserOrganizationModel>(userId).User.UserOrganizationIds;
			var q = s.QueryOver<NotificationModel>()
				.Where(x => x.DeleteTime == null || DateTime.UtcNow < x.DeleteTime)
				.WhereRestrictionOn(x => x.UserId).IsIn(uids);

			if (includeSeenAfter != null) {
				q = q.Where(x => includeSeenAfter < x.Seen || x.Seen == null);
			} else {
				q = q.Where(x => x.Seen == null);
			}
			if (!devices.HasFlag(NotificationDevices.Computer)) {
				q = q.Where(x => x.OnComputer == false);
			}
			if (!devices.HasFlag(NotificationDevices.Phone)) {
				q = q.Where(x => x.OnPhone == false);
			}
			return q;
		}
		public static async Task FireNotification_Unsafe(NotificationGroupKey groupKey, long userId, NotificationDevices devices, string message, string details = null, DateTime? expires = null, string actionUrl = null, bool canMarkSeen = true) {
			if (groupKey == null || string.IsNullOrWhiteSpace(groupKey.ToString())) {
				throw new PermissionsException("A groupKey is required");
			}
			if (userId <= 0) {
				throw new PermissionsException("A userId is required");
			}
			if (devices == NotificationDevices.None) {
				throw new PermissionsException("A device is required");
			}
			if (string.IsNullOrWhiteSpace(message)) {
				throw new PermissionsException("A message is required");
			}

			Scheduler.Enqueue(() => _FireNotification_Hangfire(groupKey.ToString(), userId, devices, message, details, expires, actionUrl, canMarkSeen));
		}


		[Queue(HangfireQueues.Immediate.FIRE_NOTIFICATION)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task<NotificationModel> _FireNotification_Hangfire(string groupKey, long userId, NotificationDevices devices, string message, string details, DateTime? expires, string actionUrl, bool canMarkSeen) {
			return await CreateNotification_Unsafe(NotifcationCreation.Build(groupKey, userId, message, devices, details: details, expires: expires, actionUrl: actionUrl, canBeMarkedSeen: canMarkSeen), true);
		}

		[Obsolete("Use FireNotification instead")]
		private static async Task<NotificationModel> CreateNotification_Unsafe(NotifcationCreation notification, bool send) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (notification.Devices == NotificationDevices.None) {
						throw new Exception("Notification not saving. Sent to no devices");
					}

					var n = await notification.Save(s);
					if (send) {
						await notification.Send(s);
					}
					tx.Commit();
					s.Flush();

					return n;
				}
			}
		}

		public static async Task DeleteNotification_Unsafe(long notificationId, DateTime? deleteTime = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var n = s.Get<NotificationModel>(notificationId);
					n.DeleteTime = deleteTime ?? DateTime.UtcNow;
					s.Update(n);
					tx.Commit();
					s.Flush();
				}
			}
		}


		[Queue(HangfireQueues.Immediate.AUTOGENERATE_NOTIFICATIONS)]
		[AutomaticRetry(Attempts = 0)]
		public static async Task AutoGenerateNotificationsHangfire(long userId) {

			var shouldRun = false;
			UserOrganizationModel caller;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					caller = s.Get<UserOrganizationModel>(userId);
					shouldRun = s.GetSettingOrDefault(Variable.Names.SHOULD_AUTOGENERATE_NOTIFICATION, true);
				}
			}

			if (shouldRun) {
				if (caller != null && !caller.IsRadialAdmin && (caller.User == null || !caller.User.IsRadialAdmin)) {
					//Confirm the quarter has been generated..
					await QuarterlyAccessor.GetQuarterOrGenerate(caller, caller.Organization.Id);
				}
			}

		}

		public static async Task<int> DeleteGroupKey_Unsafe(string groupKey) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var count = await DeleteGroupKey_Unsafe(s, groupKey);

					tx.Commit();
					s.Flush();
					return count;
				}
			}
		}
		public static async Task<int> DeleteGroupKey_Unsafe(ISession s, NotificationGroupKey groupKey) {
			return await DeleteGroupKey_Unsafe(s, groupKey.ToString());
		}

		public static async Task<int> DeleteGroupKey_Unsafe(ISession s, string groupKey) {
			if (string.IsNullOrWhiteSpace(groupKey)) {
				return 0;
			}
			var ns = s.QueryOver<NotificationModel>()
									.Where(x => (x.DeleteTime == null || DateTime.UtcNow < x.DeleteTime))
									.Where(x => x.GroupKey == groupKey)
									.List().ToList();

			var now = DateTime.UtcNow.AddMinutes(-10);

			await using (var rt = RealTimeUtility.Create()) {
				foreach (var n in ns) {
					n.DeleteTime = now;
					s.Update(n);

					if (n.OnComputer) {
						rt.UpdateUsers(n.UserId).Call("clearNotification", n.Id);
					}
				}
			}
			return ns.Count;
		}
	}

	public class NotificationGroupKey {
		private NotificationGroupKey(string groupKey) {
			GroupKey = groupKey;
		}

		private string GroupKey { get; set; }

		public static NotificationGroupKey UpdateQuarterly(long quarterModelId) {
			return new NotificationGroupKey("UpdateQuarterly_" + quarterModelId);
		}

		public override String ToString() {
			return GroupKey;
		}

		public static NotificationGroupKey FailedInvite(long userId) {
			return new NotificationGroupKey("FailedInvite_" + userId);
		}
		public static NotificationGroupKey VerifyEmail(string userId) {
			return new NotificationGroupKey("VerifyEmail_" + userId);
		}

		public static NotificationGroupKey FromString(string groupKey) {
			if (string.IsNullOrWhiteSpace(groupKey)) {
				throw new PermissionsException("GroupKey is required");
			}

			return new NotificationGroupKey(groupKey);
		}
	}

	public class NotifcationCreation {

		public long NotificationId { get; private set; }
		public string Message { get; private set; }
		public string Details { get; private set; }
		public string ImageUrl { get; private set; }
		public long UserId { get; private set; }
		public NotificationType Type { get; private set; }
		public NotificationPriority Priority { get; private set; }
		public NotificationDevices Devices { get; private set; }
		public bool Sensitive { get; private set; }
		public DateTime? Expires { get; private set; }
		public string ActionUrl { get; private set; }
		public string GroupKey { get; private set; }
		public bool CanBeMarkedSeen { get; private set; }

		public static NotifcationCreation Build(string groupKey, long userId, string message, NotificationDevices devices, string details = null, bool sensitive = true, string imageUrl = null, DateTime? expires = null, string actionUrl = null, bool canBeMarkedSeen = true) {
			if (groupKey == null) {
				throw new ArgumentNullException(nameof(groupKey));
			}

			return new NotifcationCreation {
				GroupKey = groupKey,
				Message = message,
				Details = details,
				ImageUrl = imageUrl,
				UserId = userId,
				Sensitive = sensitive,
				Devices = devices,
				Expires = expires,
				ActionUrl = actionUrl,
				CanBeMarkedSeen = canBeMarkedSeen,

			};
		}

		public async Task<NotificationModel> Save(ISession s) {
			var nm = GenerateModel();
			s.Save(nm);
			NotificationId = nm.Id;
			return nm;
		}

		protected NotificationModel GenerateModel() {
			return new NotificationModel {
				GroupKey = GroupKey,
				Details = Details,
				ImageUrl = ImageUrl,
				Name = Message,
				Priority = Priority,
				Type = Type,
				Sent = null,
				UserId = UserId,
				DeleteTime = Expires,
				OnPhone = Devices.HasFlag(NotificationDevices.Phone),
				OnComputer = Devices.HasFlag(NotificationDevices.Computer),
				ActionUrl = ActionUrl,
				CanBeMarkedSeen = CanBeMarkedSeen
			};
		}

		public async Task Send(ISession s) {
			var user = s.Get<UserOrganizationModel>(UserId);
			if (user.DeleteTime != null || user.Organization.DeleteTime != null) {
				//User or Organization was deleted
				return;
			}

			var userName = user.NotNull(x => x.GetUsername().ToLower());

			if (userName != null) {
				if (Devices.HasFlag(NotificationDevices.Phone)) {
					var devices = s.QueryOver<UserDevice>()
						.Where(x => x.DeleteTime == null && x.IgnoreDevice == false && x.UserName == userName)
						.List().Distinct(x => x.DeviceId).ToList();

					foreach (var d in devices) {
						await NotifcationCreation.SendToDevice(d, this);
					}
				}
			}

			if (Devices.HasFlag(NotificationDevices.Computer)) {
				await NotifcationCreation.SendToComputer(this);
			}
		}
		public static async Task SendToComputer(NotifcationCreation c) {
			try {
				await using (var rt = RealTimeUtility.Create()) {
					rt.UpdateUsers(c.UserId).Call("notification", new {
						Id = c.NotificationId,
						c.Message,
						c.Details,
						c.ImageUrl,
						c.Type,
						c.Priority,
						c.Sensitive,
						CanMarkSeen = c.CanBeMarkedSeen
					});
				}
			} catch (Exception) {
			}
		}

		public static async Task SendToDevice(UserDevice d, NotifcationCreation c) {
			await c.SendToDevice(d);
		}

		protected async Task SendToDevice(UserDevice d) {
			await SendFCM(d);
		}

		protected async Task SendFCM(UserDevice d) {
		}
	}
}

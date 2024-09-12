using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Models.VideoConference;
using RadialReview.Utilities;
using RadialReview.Utilities.RealTime;
using System;
using System.Threading.Tasks;

namespace RadialReview.Accessors.VideoConferenceProviders {
	public class VideoProviderAccessor {

		public static async Task<ZoomUserLink> GenerateLink(UserOrganizationModel caller, long userId, string zoomMeetingId, long? recurId = null, string name = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller);
						L10Recurrence recur = null;
						var user = s.Get<UserOrganizationModel>(userId);
						if (recurId != null) {
							perms.EditL10Recurrence(recurId.Value);
							recur = s.Get<L10Recurrence>(recurId);
						}


						if (name == null) {
							name = user.GetName() + " Zoom";
						}


						var zul = new ZoomUserLink() {
							FriendlyName = name,
							OwnerId = userId,
							ZoomMeetingId = zoomMeetingId,
						};

						s.Save(zul);

						if (recurId != null) {
							AttachVideoProviderToMeeting(s, perms, rt, zul, recurId.Value);

						}

						tx.Commit();
						s.Flush();

						return zul;
					}
				}
			}
		}


		public static async Task RemoveZoomMeeting(UserOrganizationModel caller, long vcProviderId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await using (var rt = RealTimeUtility.Create()) {
						var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(caller.Id, false);

						var videoProvider = s.QueryOver<L10Recurrence.L10Recurrence_VideoConferenceProvider>().Where(t => t.Provider.Id == vcProviderId).SingleOrDefault();
						if (videoProvider == null)
							throw new PermissionsException("Link not found..");


						if (videoProvider.L10Recurrence.Id != null) {
							perms.EditL10Recurrence(videoProvider.L10Recurrence.Id);
						}

						//update selected meeting
						if (videoProvider.L10Recurrence.SelectedVideoProviderId == vcProviderId) {
							videoProvider.L10Recurrence.SelectedVideoProviderId = null;
						}

						videoProvider.DeleteTime = DateTime.UtcNow;
						s.Update(videoProvider);

						if (videoProvider.L10Recurrence.Id != null) {
							RemoveVideoProviderToMeeting(s, perms, rt, vcProviderId, videoProvider.L10Recurrence.Id);

						}

						tx.Commit();
						s.Flush();

					}
				}
			}
		}


		public static async Task StartMeeting(UserOrganizationModel caller, long vcProviderId, string connectionId) {
			AbstractVCProvider vcp = null;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					vcp = s.Get<AbstractVCProvider>(vcProviderId);
					var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(vcp.OwnerId, false);
					vcp = (AbstractVCProvider)s.GetSessionImplementation().PersistenceContext.Unproxy(vcp);
					if (vcp is ZoomUserLink) {
						await using (var rt = RealTimeUtility.Create()) {
							rt.UpdateConnection(connectionId).Call("joinVideoConference", (ZoomUserLink)vcp);
						}
					} else {
						throw new PermissionsException("Unhandled video type");
					}
				}
			}


		}

		public static void AttachVideoProviderToMeeting(ISession s, PermissionsUtility perms, RealTimeUtility rt, AbstractVCProvider vcProvider, long recurId) {
			perms.EditL10Recurrence(recurId);
			if (vcProvider.Id <= 0)
				throw new PermissionsException("Link ID less than zero");

			var link = new L10Recurrence.L10Recurrence_VideoConferenceProvider {
				L10Recurrence = s.Load<L10Recurrence>(recurId),
				Provider = vcProvider,
			};

			rt.UpdateRecurrences(recurId).Call("addVideoProvider", vcProvider);

			s.Save(link);
		}


		public static void RemoveVideoProviderToMeeting(ISession s, PermissionsUtility perms, RealTimeUtility rt, long vcProviderId, long recurId) {

			perms.EditL10Recurrence(recurId);
			if (vcProviderId <= 0)
				throw new PermissionsException("Link ID less than zero");

			var vcProvider = s.Get<AbstractVCProvider>(vcProviderId);
			var resolved = (AbstractVCProvider)s.GetSessionImplementation().PersistenceContext.Unproxy(vcProvider);
			rt.UpdateRecurrences(recurId).Call("removeVideoProvider", resolved);
		}
	}
}

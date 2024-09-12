using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;

namespace RadialReview.Models.VideoConference {
	public class ZoomUserLink : AbstractVCProvider {

		public virtual string ZoomMeetingId { get; set; }

		public override VideoConferenceType GetVideoConferenceType() {
			return VideoConferenceType.Zoom;
		}

		public override string GetUrl() {
			if (!string.IsNullOrWhiteSpace(ZoomMeetingId) && ZoomMeetingId.StartsWith("http"))
				return ZoomMeetingId;

			return "https://www.zoom.us/j/" + ZoomMeetingId;
		}

		public ZoomUserLink() {
		}

		public class Map : SubclassMap<ZoomUserLink> {
			public Map() {
				Map(x => x.ZoomMeetingId);
			}
		}
	}
}
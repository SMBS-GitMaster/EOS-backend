using RadialReview.Models.Enums;

namespace RadialReview.Models {
	public class TinyRGM {
		public TinyRGM(long rgmId, long forUserId, OriginType type) {
			RgmId = rgmId;
			ForUserId = forUserId;
			Type = type;
		}

		public long RgmId { get; set; }
		public long ForUserId { get; set; }
		public OriginType Type { get; set; }

	}
}
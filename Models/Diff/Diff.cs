namespace RadialReview.Models {
	public class Diff {

		public Diff() { }

		public Diff(WhiteboardDiff x) {
			Id = x.WhiteboardId;
			Type = "wb";
			ElementId = x.ElementId;
			Version = x.Version;
			Delta = x.Delta;
			O = x.Id;
		}

		public string Id { get; set; }
		public string Type { get; set; }
		public string ElementId { get; set; }
		public int Version { get; set; }
		public string ConnectionId { get; set; }
		public string Delta { get; set; }
		public long O { get; set; }

		public string GetModelId() {
			return Id;
		}

		public Diff Clone() {
			return new Diff() {
				Id = this.Id,
				Type = this.Type,
				Version = this.Version,
				ElementId = this.ElementId,
				ConnectionId = this.ConnectionId,
				Delta = this.Delta,
				O = this.O
			};
		}
	}
}
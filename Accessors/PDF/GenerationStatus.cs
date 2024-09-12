namespace RadialReview.Accessors.PDF {

	public enum GenerationStatusStep {
		PageStarted = 0,
		InitialRenderComplete = 1,
		ScriptsExecuted = 2,
		FinalRenderComplete = 3,
		BeginningExport = 4,
		ExportComplete = 5,
		PageCompleted = 6,
		MAX = 6
	}
	public class GenerationStatus {


		public GenerationStatus(GenerationStatusStep status, int currentPageNumber, int totalPages) {
			Status = status;
			CurrentPageNumber = currentPageNumber;
			TotalPages = totalPages;
			CurrentStep = (currentPageNumber - 1) * (int)GenerationStatusStep.MAX + (int)status;
			TotalSteps = totalPages * (int)GenerationStatusStep.MAX;
		}

		public GenerationStatusStep Status { get; set; }
		public int CurrentPageNumber { get; set; }
		public int TotalPages { get; set; }
		public int CurrentStep { get; set; }
		public int TotalSteps { get; set; }
	}
}

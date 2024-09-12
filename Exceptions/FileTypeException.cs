using RadialReview.Accessors;
using System;

namespace RadialReview.Exceptions {
	public class FileTypeException : Exception, ISafeExceptionMessage {
		public FileTypeException(FileType fileType) : base("Invalid File Type") {
			FileType = fileType;
		}

		public FileType FileType { get; set; }
	}
}
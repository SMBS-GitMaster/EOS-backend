using System.Linq;

namespace RadialReview.Utilities.FileTypes {
	public class FileTypeExtensionUtility {

		public class FileType {
			public FileType(string extension, string kind, string fullName, MediaType mediaType, bool known = true) {
				Extension = extension.Trim().ToLower();
				Kind = (kind ?? "").Trim();
				FullName = (fullName ?? "").Trim();
				Known = known;
				MediaType = mediaType;
			}

			public string Extension { get; set; }
			public string Kind { get; set; }
			public string FullName { get; set; }
			public bool Known { get; set; }
			public MediaType MediaType { get; set; }

		}

		public enum MediaType {
			Unknown,
			RichText,
			Text,
			PDF,
			Spreadsheet,
			Slideshow,
			Image,
			Audio,
			Video,
			CompressedArchive,
			Executable,
			Code

		}


		public static FileType[] KnownExtensions = new FileType[]{
			new FileType("pdf","Adobe PDF","Portable Document Format",MediaType.PDF),

			new FileType("doc","Microsoft Word","Microsoft Word 97–2003 Document",MediaType.RichText),
			new FileType("docx","Microsoft Word"," Word document",MediaType.RichText),
			new FileType("docm","Microsoft Word","Word macro-enabled document",MediaType.RichText),
			new FileType("dotx","Microsoft Word","Word template",MediaType.RichText),
			new FileType("dotm","Microsoft Word","Word macro-enabled template",MediaType.RichText),

			new FileType("xls","Microsoft Excel","Microsoft Excel 97-2003 Worksheet",MediaType.Spreadsheet),
			new FileType("xlt","Microsoft Excel","Microsoft Excel 97-2003 Template",MediaType.Spreadsheet),
			new FileType("xlm","Microsoft Excel","Microsoft Excel 97-2003 Macro",MediaType.Spreadsheet),
			new FileType("xlsx","Microsoft Excel","Excel workbook",MediaType.Spreadsheet),
			new FileType("xlsm","Microsoft Excel","Excel macro-enabled workbook",MediaType.Spreadsheet),
			new FileType("xltx","Microsoft Excel","Excel template",MediaType.Spreadsheet),
			new FileType("xltm","Microsoft Excel","Excel macro-enabled template",MediaType.Spreadsheet),

			new FileType("ppt","Microsoft Powerpoint","Legacy PowerPoint presentation",MediaType.Slideshow),
			new FileType("pot","Microsoft Powerpoint","Legacy PowerPoint template",MediaType.Slideshow),
			new FileType("pps","Microsoft Powerpoint","Legacy PowerPoint slideshow",MediaType.Slideshow),
			new FileType("pptx","Microsoft Powerpoint","PowerPoint presentation",MediaType.Slideshow),
			new FileType("pptm","Microsoft Powerpoint","PowerPoint macro-enabled presentation",MediaType.Slideshow),
			new FileType("potx","Microsoft Powerpoint","PowerPoint template",MediaType.Slideshow),
			new FileType("potm","Microsoft Powerpoint","PowerPoint macro-enabled template",MediaType.Slideshow),

			new FileType("txt","Text","Plain Text",MediaType.Text),

			new FileType("wav","Audio","WAV Audio",MediaType.Audio),
			new FileType("mp1","Audio","MP1 Audio",MediaType.Audio),
			new FileType("mp2","Audio","MP2 Audio",MediaType.Audio),
			new FileType("mp3","Audio","MP3 Audio",MediaType.Audio),
			new FileType("mpg","Audio","MPG Audio",MediaType.Audio),
			new FileType("mpeg","Audio","MPEG Audio",MediaType.Audio),
			new FileType("wma","Audio","WMA Audio",MediaType.Audio),
			new FileType("midi","Audio","MIDI Audio",MediaType.Audio),
			new FileType("mid","Audio","MID Audio",MediaType.Audio),

			new FileType("mpeg","Video","MPEG Video",MediaType.Video),
			new FileType("avi","Video","AVI Video",MediaType.Video),
			new FileType("mov","Video","MOV Video",MediaType.Video),
			new FileType("qt","Video","QT Video",MediaType.Video),
			new FileType("ram","Video","RAM Video",MediaType.Video),


			new FileType("png","Image","PNG Image",MediaType.Image),
			new FileType("gif","Image","GIF Image",MediaType.Image),
			new FileType("bmp","Image","BMP Image",MediaType.Image),
			new FileType("jpg","Image","JPG Image",MediaType.Image),
			new FileType("jpeg","Image","JPEG Image",MediaType.Image),
			new FileType("tiff","Image","TIFF Image",MediaType.Image),
			new FileType("svg","Image","SVG Image",MediaType.Image),

			new FileType("zip","Compression","ZIP Compressed Files",MediaType.CompressedArchive),
			new FileType("7z","Compression","7Z Compressed Files",MediaType.CompressedArchive),
			new FileType("rar","Compression","RAR Compressed Files",MediaType.CompressedArchive),

			new FileType("html","Hypertext","HTML Hypertext",MediaType.Code),
			new FileType("htm","Hypertext","HTM Hypertext",MediaType.Code),
			new FileType("css","Stylesheet","CSS Stylesheet",MediaType.Code),
			new FileType("js","Javascript","Javascript",MediaType.Code),

			new FileType("exe","Executable","EXE Executable",MediaType.Executable),
			new FileType("dmg","Executable","DMG Executable",MediaType.Executable),
			new FileType("app","Executable","APP Executable",MediaType.Executable),


		};

		public static FileType GetFileTypeFromExtension(string extension) {
			if (string.IsNullOrWhiteSpace(extension))
				return new FileType("", "Unknown", "Unknown", MediaType.Unknown, false);

			extension = extension.ToLower().Trim().TrimStart('.');
			if (string.IsNullOrWhiteSpace(extension))
				return new FileType("", "Unknown", "Unknown", MediaType.Unknown, false);

			var found = KnownExtensions.FirstOrDefault(x => x.Extension == extension);
			if (found != null)
				return found;
			return new FileType(extension.ToUpper(), extension.ToUpper(), extension.ToUpper() + " Format", MediaType.Unknown, false);
		}
	}
}
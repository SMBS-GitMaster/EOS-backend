using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace RadialReview.Accessors.PDF {

	public class StreamAndMetaCollection : IDisposable, IEnumerable<StreamAndMeta> {
		private List<StreamAndMeta> Streams = new List<StreamAndMeta>();


		public void Insert(int index, [NotNull] StreamAndMeta streamAndMeta) {
			if (streamAndMeta == null)
				throw new ArgumentNullException(nameof(streamAndMeta));
			Streams.Insert(index, streamAndMeta);
		}

		public void Add([NotNull] StreamAndMeta streamAndMeta) {
			if (streamAndMeta == null)
				throw new ArgumentNullException(nameof(streamAndMeta));
			Streams.Add(streamAndMeta);
		}

		public void Add(string name, Stream content, int pageCount, bool drawPageNumber = true) {
			Add(new StreamAndMeta() {
				Content = content,
				DrawPageNumber = drawPageNumber,
				Name = name,
				Pages = pageCount
			});
		}

		public void Dispose() {
			if (Streams != null) {
				foreach (var s in Streams) {
					s?.Dispose();
				}
			}
		}

		public IEnumerator<StreamAndMeta> GetEnumerator() {
			return Streams.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return Streams.GetEnumerator();
		}
	}

	public class StreamAndMeta : IDisposable {
		public Stream Content { get; set; }
		public string Name { get; set; }
		public int Pages { get; set; }
		public bool DrawPageNumber { get; set; } = true;

		public void Dispose() {
			Content?.Dispose();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using NHibernate.Envers;
using NHibernate.Envers.Exceptions;
using log4net;
using NHibernate;
using NHibernate.Envers.Query;

namespace RadialReview.Utilities.Extensions {

	public static class AuditExtensions {
		public static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public class Revision<T> {
			public long RevisionId { get; set; }
			public DateTime Date { get; set; }
			public T Object { get; set; }
		}

		public class RevisionDiff<T> {
			public Revision<T> Before { get; set; }
			public Revision<T> After { get; set; }
		}

		public static IEnumerable<Revision<T>> GetRevisionsBetween<T>(this IAuditReader self, ISession session, object id, DateTime start, DateTime end) where T : class {
			if (start > end)
				throw new ArgumentOutOfRangeException("start", "Start must come before end.");

			start = start.AddSeconds(-1);
			end = end.AddSeconds(1);

			var revisionModels = self.CreateQuery()
				.ForHistoryOf<T, DefaultRevisionEntity>(true)
				.Add(AuditEntity.Id().Eq(id))
				.Results();


			var revisions = revisionModels.Select(x => x.RevisionEntity).ToList();
			var revisionIds = revisions.Where(x => start <= x.RevisionDate && x.RevisionDate <= end).OrderBy(x => x.RevisionDate).ToList();

			//     ----|--> ------> --->|
			//----x----|---x-------x----|---x------

			//Still need to add the one before the start.
			var startId = start;
			if (revisionIds.Any())
				startId = revisionIds.First().RevisionDate;
			var additional = revisions.Where(x => x.RevisionDate < startId).ToList();
			if (additional.Any()) {
				revisionIds.Add(additional.ArgMax(x => x.RevisionDate));
			}
			if (!revisionIds.Any())
				return new List<Revision<T>>();
			var low = revisionIds.Min(x => x.RevisionDate);
			var high = revisionIds.Max(x => x.RevisionDate);

			revisionModels = revisionModels.Where(x => low <= x.RevisionEntity.RevisionDate && x.RevisionEntity.RevisionDate <= high).ToList();

			return revisionModels.Select(x => new Revision<T>() {
				Date = x.RevisionEntity.RevisionDate,
				RevisionId = x.RevisionEntity.Id,
				Object = x.Entity
			});
		}
	}
}

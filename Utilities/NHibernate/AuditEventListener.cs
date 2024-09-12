using System;
using System.Threading;
using System.Threading.Tasks;
using NHibernate.Event;
using NHibernate.Persister.Entity;

namespace RadialReview.Utilities.NHibernate {
	public class AuditEventListener : IPreUpdateEventListener, IPreInsertEventListener
	{
		public bool OnPreUpdate(PreUpdateEvent @event)
		{

			var a = @event.OldState;
			var b = @event.State;

			return false;
		}

		public bool OnPreInsert(PreInsertEvent @event)
		{

			return false;
		}

		public async Task<bool> OnPreUpdateAsync(PreUpdateEvent @event, CancellationToken cancellationToken) {

			var a = @event.OldState;
			var b = @event.State;

			return false;
		}

		public async Task<bool> OnPreInsertAsync(PreInsertEvent @event, CancellationToken cancellationToken) {

			return false;
		}
	}
}

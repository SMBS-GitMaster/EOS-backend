using NHibernate;
using NHibernate.Transaction;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Query {
	public class IEnumerableTransaction : ITransaction {
		public void Begin(IsolationLevel isolationLevel) {
		}

		public void Begin() {
		}

		public void Commit() {
		}

		public void Enlist(IDbCommand command) {
		}

		public bool IsActive {
			get { return true; }
		}

		public void RegisterSynchronization(ISynchronization synchronization) {
		}

		public void Rollback() {

		}

		public bool WasCommitted {
			get { return true; }
		}

		public bool WasRolledBack {
			get { return false; }
		}

		public void Dispose() {

		}

		public async Task CommitAsync(CancellationToken cancellationToken = default) {
		}

		public async Task RollbackAsync(CancellationToken cancellationToken = default) {
		}

		public void Enlist(DbCommand command) {
		}
	}
}
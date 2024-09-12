using System.Data;
using NHibernate;
using NHibernate.Transaction;
using System.Threading.Tasks;
using System.Threading;
using System.Data.Common;
using System;

namespace RadialReview.Utilities.NHibernate {



	public class SingleRequestTransaction : ITransaction {

		private ITransaction _backingTransaction;
		public SingleRequestSession _request { get; set; }

		public int CommitCount { get; private set; }

		public bool IsActive { get { return _backingTransaction.IsActive; } }

		public bool WasRolledBack { get { return _backingTransaction.WasRolledBack; } }

		public bool WasCommitted { get { return _backingTransaction.WasCommitted; } }

		public void Begin() {
			_backingTransaction.Begin();
		}

		public void Begin(IsolationLevel isolationLevel) {
			_backingTransaction.Begin(isolationLevel);
		}

		private bool _IsCommitted { get; set; }
		public void Commit() {
			if (_request.TransactionDepth == 1) {
				try {
					_backingTransaction.Commit();
				} catch (Exception e) {
					throw;
				}
			}
			CommitCount += 1;
			_request.GetCurrentContext().TransactionCommitted = true;
		}
		public Task CommitAsync(CancellationToken cancellationToken = default) {
			return _backingTransaction.CommitAsync(cancellationToken);
		}

		public void Rollback() {
			_request.GetCurrentContext().TransactionRolledBack = true;
			_backingTransaction.Rollback();
		}
		public Task RollbackAsync(CancellationToken cancellationToken = default) {
			return _backingTransaction.RollbackAsync(cancellationToken);
		}

		public void RegisterSynchronization(ISynchronization synchronization) {
			_backingTransaction.RegisterSynchronization(synchronization);
		}

		public void Dispose() {
			_request.GetCurrentContext().TransactionDisposed = true;
			_request.TransactionDepth -= 1;
			if (_request.TransactionDepth == 0) {
				_backingTransaction.Dispose();
			}
		}

		public void Enlist(DbCommand command) {
			_backingTransaction.Enlist(command);
		}

		public SingleRequestTransaction(ITransaction toWrap, SingleRequestSession request) {
			_backingTransaction = toWrap;
			_request = request;
		}


	}
}

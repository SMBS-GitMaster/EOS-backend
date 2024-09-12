using System.Data;
using System.Linq.Expressions;
using NHibernate;
using System;
using System.Linq;
using NHibernate.Engine;
using NHibernate.Stat;
using NHibernate.Type;
using System.Threading.Tasks;
using System.Threading;
using System.Data.Common;
using RadialReview.Models.Synchronize;

namespace RadialReview.Utilities.NHibernate {

	public interface IOrderedSession : ISession {

	}

	public class OrderedSession : IOrderedSession {
		private ISession _backingSession;
		public bool FromSyncLock { get; set; }

		[Obsolete("only to be used in SyncUtil. Did you forget to wrap your request in an SyncUtil.EnsureStrictlyAfter")]
		private OrderedSession(ISession session, SyncLock _) {
			_backingSession = session;
			FromSyncLock = _ != null;
		}

		public static IOrderedSession Indifferent(ISession s) {
			return new OrderedSession(s, null);
		}
		public static IOrderedSession From(ISession s, SyncLock lck) {
			return new OrderedSession(s, lck);
		}


		#region wrapped

		public FlushMode FlushMode { get => _backingSession.FlushMode; set => _backingSession.FlushMode = value; }
		public CacheMode CacheMode { get => _backingSession.CacheMode; set => _backingSession.CacheMode = value; }
		public ISessionFactory SessionFactory => _backingSession.SessionFactory;
		public DbConnection Connection => _backingSession.Connection;
		public bool IsOpen => _backingSession.IsOpen;
		public bool IsConnected => _backingSession.IsConnected;
		public bool DefaultReadOnly { get => _backingSession.DefaultReadOnly; set => _backingSession.DefaultReadOnly = value; }
		public ITransaction Transaction => _backingSession.Transaction;
		public ISessionStatistics Statistics => _backingSession.Statistics;

		public ITransaction BeginTransaction() {
			return _backingSession.BeginTransaction();
		}

		public void Dispose() {
			_backingSession.Dispose();
		}
		public Task FlushAsync(CancellationToken cancellationToken = default) {
			return _backingSession.FlushAsync(cancellationToken);
		}

		public Task<bool> IsDirtyAsync(CancellationToken cancellationToken = default) {
			return _backingSession.IsDirtyAsync(cancellationToken);
		}

		public Task EvictAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.EvictAsync(obj, cancellationToken);
		}

		public Task<object> LoadAsync(Type theType, object id, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.LoadAsync(theType, id, lockMode, cancellationToken);
		}

		public Task<object> LoadAsync(string entityName, object id, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.LoadAsync(entityName, id, lockMode, cancellationToken);
		}

		public Task<object> LoadAsync(Type theType, object id, CancellationToken cancellationToken = default) {
			return _backingSession.LoadAsync(theType, id, cancellationToken);
		}

		public Task<T> LoadAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.LoadAsync<T>(id, lockMode, cancellationToken);
		}

		public Task<T> LoadAsync<T>(object id, CancellationToken cancellationToken = default) {
			return _backingSession.LoadAsync<T>(id, cancellationToken);
		}

		public Task<object> LoadAsync(string entityName, object id, CancellationToken cancellationToken = default) {
			return _backingSession.LoadAsync(entityName, id, cancellationToken);
		}

		public Task LoadAsync(object obj, object id, CancellationToken cancellationToken = default) {
			return _backingSession.LoadAsync(obj, id, cancellationToken);
		}

		public Task ReplicateAsync(object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = default) {
			return _backingSession.ReplicateAsync(obj, replicationMode, cancellationToken);
		}

		public Task ReplicateAsync(string entityName, object obj, ReplicationMode replicationMode, CancellationToken cancellationToken = default) {
			return _backingSession.ReplicateAsync(entityName, obj, replicationMode, cancellationToken);
		}

		public Task<object> SaveAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.SaveAsync(obj, cancellationToken);
		}

		public Task SaveAsync(object obj, object id, CancellationToken cancellationToken = default) {
			return _backingSession.SaveAsync(obj, id, cancellationToken);
		}

		public Task<object> SaveAsync(string entityName, object obj, CancellationToken cancellationToken = default) {
			return _backingSession.SaveAsync(entityName, obj, cancellationToken);
		}

		public Task SaveAsync(string entityName, object obj, object id, CancellationToken cancellationToken = default) {
			return _backingSession.SaveAsync(entityName, obj, id, cancellationToken);
		}

		public Task SaveOrUpdateAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.SaveOrUpdateAsync(obj, cancellationToken);
		}

		public Task SaveOrUpdateAsync(string entityName, object obj, CancellationToken cancellationToken = default) {
			return _backingSession.SaveOrUpdateAsync(entityName, obj, cancellationToken);
		}

		public Task SaveOrUpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = default) {
			return _backingSession.SaveOrUpdateAsync(entityName, obj, id, cancellationToken);
		}

		public Task UpdateAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.UpdateAsync(obj, cancellationToken);
		}

		public Task UpdateAsync(object obj, object id, CancellationToken cancellationToken = default) {
			return _backingSession.UpdateAsync(obj, id, cancellationToken);
		}

		public Task UpdateAsync(string entityName, object obj, CancellationToken cancellationToken = default) {
			return _backingSession.UpdateAsync(entityName, obj, cancellationToken);
		}

		public Task UpdateAsync(string entityName, object obj, object id, CancellationToken cancellationToken = default) {
			return _backingSession.UpdateAsync(entityName, obj, id, cancellationToken);
		}

		public Task<object> MergeAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.MergeAsync(obj, cancellationToken);
		}

		public Task<object> MergeAsync(string entityName, object obj, CancellationToken cancellationToken = default) {
			return _backingSession.MergeAsync(entityName, obj, cancellationToken);
		}

		public Task<T> MergeAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class {
			return _backingSession.MergeAsync(entity, cancellationToken);
		}

		public Task<T> MergeAsync<T>(string entityName, T entity, CancellationToken cancellationToken = default) where T : class {
			return _backingSession.MergeAsync(entityName, entity, cancellationToken);
		}

		public Task PersistAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.PersistAsync(obj, cancellationToken);
		}

		public Task PersistAsync(string entityName, object obj, CancellationToken cancellationToken = default) {
			return _backingSession.PersistAsync(entityName, obj, cancellationToken);
		}

		public Task DeleteAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.DeleteAsync(obj, cancellationToken);
		}

		public Task DeleteAsync(string entityName, object obj, CancellationToken cancellationToken = default) {
			return _backingSession.DeleteAsync(entityName, obj, cancellationToken);
		}

		public Task<int> DeleteAsync(string query, CancellationToken cancellationToken = default) {
			return _backingSession.DeleteAsync(query, cancellationToken);
		}

		public Task<int> DeleteAsync(string query, object value, IType type, CancellationToken cancellationToken = default) {
			return _backingSession.DeleteAsync(query, value, type, cancellationToken);
		}

		public Task<int> DeleteAsync(string query, object[] values, IType[] types, CancellationToken cancellationToken = default) {
			return _backingSession.DeleteAsync(query, values, types, cancellationToken);
		}

		public Task LockAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.LockAsync(obj, lockMode, cancellationToken);
		}

		public Task LockAsync(string entityName, object obj, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.LockAsync(entityName, obj, lockMode, cancellationToken);
		}

		public Task RefreshAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.RefreshAsync(obj, cancellationToken);
		}

		public Task RefreshAsync(object obj, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.RefreshAsync(obj, lockMode, cancellationToken);
		}

		public Task<IQuery> CreateFilterAsync(object collection, string queryString, CancellationToken cancellationToken = default) {
			return _backingSession.CreateFilterAsync(collection, queryString, cancellationToken);
		}

		public Task<object> GetAsync(Type clazz, object id, CancellationToken cancellationToken = default) {
			return _backingSession.GetAsync(clazz, id, cancellationToken);
		}

		public Task<object> GetAsync(Type clazz, object id, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.GetAsync(clazz, id, lockMode, cancellationToken);
		}

		public Task<object> GetAsync(string entityName, object id, CancellationToken cancellationToken = default) {
			return _backingSession.GetAsync(entityName, id, cancellationToken);
		}

		public Task<T> GetAsync<T>(object id, CancellationToken cancellationToken = default) {
			return _backingSession.GetAsync<T>(id, cancellationToken);
		}

		public Task<T> GetAsync<T>(object id, LockMode lockMode, CancellationToken cancellationToken = default) {
			return _backingSession.GetAsync<T>(id, lockMode, cancellationToken);
		}

		public Task<string> GetEntityNameAsync(object obj, CancellationToken cancellationToken = default) {
			return _backingSession.GetEntityNameAsync(obj, cancellationToken);
		}

		public ISharedSessionBuilder SessionWithOptions() {
			return _backingSession.SessionWithOptions();
		}

		public void Flush() {
			_backingSession.Flush();
		}

		public DbConnection Disconnect() {
			return _backingSession.Disconnect();
		}

		public void Reconnect() {
			_backingSession.Reconnect();
		}

		public void Reconnect(DbConnection connection) {
			_backingSession.Reconnect(connection);
		}

		public DbConnection Close() {
			return _backingSession.Close();
		}

		public void CancelQuery() {
			_backingSession.CancelQuery();
		}

		public bool IsDirty() {
			return _backingSession.IsDirty();
		}

		public bool IsReadOnly(object entityOrProxy) {
			return _backingSession.IsReadOnly(entityOrProxy);
		}

		public void SetReadOnly(object entityOrProxy, bool readOnly) {
			_backingSession.SetReadOnly(entityOrProxy, readOnly);
		}

		public object GetIdentifier(object obj) {
			return _backingSession.GetIdentifier(obj);
		}

		public bool Contains(object obj) {
			return _backingSession.Contains(obj);
		}

		public void Evict(object obj) {
			_backingSession.Evict(obj);
		}

		public object Load(Type theType, object id, LockMode lockMode) {
			return _backingSession.Load(theType, id, lockMode);
		}

		public object Load(string entityName, object id, LockMode lockMode) {
			return _backingSession.Load(entityName, id, lockMode);
		}

		public object Load(Type theType, object id) {
			return _backingSession.Load(theType, id);
		}

		public T Load<T>(object id, LockMode lockMode) {
			return _backingSession.Load<T>(id, lockMode);
		}

		public T Load<T>(object id) {
			return _backingSession.Load<T>(id);
		}

		public object Load(string entityName, object id) {
			return _backingSession.Load(entityName, id);
		}

		public void Load(object obj, object id) {
			_backingSession.Load(obj, id);
		}

		public void Replicate(object obj, ReplicationMode replicationMode) {
			_backingSession.Replicate(obj, replicationMode);
		}

		public void Replicate(string entityName, object obj, ReplicationMode replicationMode) {
			_backingSession.Replicate(entityName, obj, replicationMode);
		}

		public object Save(object obj) {
			return _backingSession.Save(obj);
		}

		public void Save(object obj, object id) {
			_backingSession.Save(obj, id);
		}

		public object Save(string entityName, object obj) {
			return _backingSession.Save(entityName, obj);
		}

		public void Save(string entityName, object obj, object id) {
			_backingSession.Save(entityName, obj, id);
		}

		public void SaveOrUpdate(object obj) {
			_backingSession.SaveOrUpdate(obj);
		}

		public void SaveOrUpdate(string entityName, object obj) {
			_backingSession.SaveOrUpdate(entityName, obj);
		}

		public void SaveOrUpdate(string entityName, object obj, object id) {
			_backingSession.SaveOrUpdate(entityName, obj, id);
		}

		public void Update(object obj) {
			_backingSession.Update(obj);
		}

		public void Update(object obj, object id) {
			_backingSession.Update(obj, id);
		}

		public void Update(string entityName, object obj) {
			_backingSession.Update(entityName, obj);
		}

		public void Update(string entityName, object obj, object id) {
			_backingSession.Update(entityName, obj, id);
		}

		public object Merge(object obj) {
			return _backingSession.Merge(obj);
		}

		public object Merge(string entityName, object obj) {
			return _backingSession.Merge(entityName, obj);
		}

		public T Merge<T>(T entity) where T : class {
			return _backingSession.Merge(entity);
		}

		public T Merge<T>(string entityName, T entity) where T : class {
			return _backingSession.Merge(entityName, entity);
		}

		public void Persist(object obj) {
			_backingSession.Persist(obj);
		}

		public void Persist(string entityName, object obj) {
			_backingSession.Persist(entityName, obj);
		}

		public void Delete(object obj) {
			_backingSession.Delete(obj);
		}

		public void Delete(string entityName, object obj) {
			_backingSession.Delete(entityName, obj);
		}

		public int Delete(string query) {
			return _backingSession.Delete(query);
		}

		public int Delete(string query, object value, IType type) {
			return _backingSession.Delete(query, value, type);
		}

		public int Delete(string query, object[] values, IType[] types) {
			return _backingSession.Delete(query, values, types);
		}

		public void Lock(object obj, LockMode lockMode) {
			_backingSession.Lock(obj, lockMode);
		}

		public void Lock(string entityName, object obj, LockMode lockMode) {
			_backingSession.Lock(entityName, obj, lockMode);
		}

		public void Refresh(object obj) {
			_backingSession.Refresh(obj);
		}

		public void Refresh(object obj, LockMode lockMode) {
			_backingSession.Refresh(obj, lockMode);
		}

		public LockMode GetCurrentLockMode(object obj) {
			return _backingSession.GetCurrentLockMode(obj);
		}

		public ITransaction BeginTransaction(IsolationLevel isolationLevel) {
			return _backingSession.BeginTransaction(isolationLevel);
		}

		public void JoinTransaction() {
			_backingSession.JoinTransaction();
		}

		public ICriteria CreateCriteria<T>() where T : class {
			return _backingSession.CreateCriteria<T>();
		}

		public ICriteria CreateCriteria<T>(string alias) where T : class {
			return _backingSession.CreateCriteria<T>(alias);
		}

		public ICriteria CreateCriteria(Type persistentClass) {
			return _backingSession.CreateCriteria(persistentClass);
		}

		public ICriteria CreateCriteria(Type persistentClass, string alias) {
			return _backingSession.CreateCriteria(persistentClass, alias);
		}

		public ICriteria CreateCriteria(string entityName) {
			return _backingSession.CreateCriteria(entityName);
		}

		public ICriteria CreateCriteria(string entityName, string alias) {
			return _backingSession.CreateCriteria(entityName, alias);
		}

		public IQueryOver<T, T> QueryOver<T>() where T : class {
			return _backingSession.QueryOver<T>();
		}

		public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class {
			return _backingSession.QueryOver(alias);
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class {
			return _backingSession.QueryOver<T>(entityName);
		}

		public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class {
			return _backingSession.QueryOver(entityName, alias);
		}

		public IQuery CreateQuery(string queryString) {
			return _backingSession.CreateQuery(queryString);
		}

		public IQuery CreateFilter(object collection, string queryString) {
			return _backingSession.CreateFilter(collection, queryString);
		}

		public IQuery GetNamedQuery(string queryName) {
			return _backingSession.GetNamedQuery(queryName);
		}

		public ISQLQuery CreateSQLQuery(string queryString) {
			return _backingSession.CreateSQLQuery(queryString);
		}

		public void Clear() {
			_backingSession.Clear();
		}

		public object Get(Type clazz, object id) {
			return _backingSession.Get(clazz, id);
		}

		public object Get(Type clazz, object id, LockMode lockMode) {
			return _backingSession.Get(clazz, id, lockMode);
		}

		public object Get(string entityName, object id) {
			return _backingSession.Get(entityName, id);
		}

		public T Get<T>(object id) {
			return _backingSession.Get<T>(id);
		}

		public T Get<T>(object id, LockMode lockMode) {
			return _backingSession.Get<T>(id, lockMode);
		}

		public string GetEntityName(object obj) {
			return _backingSession.GetEntityName(obj);
		}

		public IFilter EnableFilter(string filterName) {
			return _backingSession.EnableFilter(filterName);
		}

		public IFilter GetEnabledFilter(string filterName) {
			return _backingSession.GetEnabledFilter(filterName);
		}

		public void DisableFilter(string filterName) {
			_backingSession.DisableFilter(filterName);
		}

		public IMultiQuery CreateMultiQuery() {
			return _backingSession.CreateMultiQuery();
		}

		public ISession SetBatchSize(int batchSize) {
			return _backingSession.SetBatchSize(batchSize);
		}

		public ISessionImplementor GetSessionImplementation() {
			return _backingSession.GetSessionImplementation();
		}

		public IMultiCriteria CreateMultiCriteria() {
			return _backingSession.CreateMultiCriteria();
		}

		public ISession GetSession(EntityMode entityMode) {
			return _backingSession.GetSession(entityMode);
		}

		public IQueryable<T> Query<T>() {
			return _backingSession.Query<T>();
		}

		public IQueryable<T> Query<T>(string entityName) {
			return _backingSession.Query<T>(entityName);
		}

		#endregion


	}
}

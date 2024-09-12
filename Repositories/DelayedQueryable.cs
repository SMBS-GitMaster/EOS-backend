using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Core.Repositories {

  public class DelayedQueryable {


    public static IQueryable<T> CreateFrom<T>(CancellationToken? cancellationToken, Func<IEnumerable<T>> func) {
      return new DelayedQueryable<T>(cancellationToken, func);
    }
  }


  public class DelayedQueryable<T> : IQueryable<T> {
    private Func<IEnumerable<T>> Func { get; }
    private CancellationToken? CancellationToken { get; set; }

    public DelayedQueryable(CancellationToken? cancellationToken,Func<IEnumerable<T>> func) {
      Func=func;
      CancellationToken = cancellationToken;
    }

    private IQueryable<T> Backing { get; set; }
    private bool QueryableCreated { get; set; }

    private IQueryable<T> GetBackingQueryable() {
      if (!QueryableCreated) {
        if (CancellationToken==null || CancellationToken.Value.IsCancellationRequested) {
          Backing = Enumerable.Empty<T>().AsQueryable();
        } else {
          var res = Func();
          if (res == null)
            res = Enumerable.Empty<T>();
          Backing = res.AsQueryable();
        }
        QueryableCreated=true;
      }
      return Backing;
    }



    public Type ElementType { get { return GetBackingQueryable().ElementType; } }
    public Expression Expression { get { return GetBackingQueryable().Expression; } }
    public IQueryProvider Provider { get { return GetBackingQueryable().Provider; } }

    public IEnumerator<T> GetEnumerator() {
      return GetBackingQueryable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetBackingQueryable().GetEnumerator();
    }
  }
  //  public Type ElementType { get; private set; }
  //  public Expression Expression { get; private set; }
  //  public IQueryProvider Provider { get; private set; }

  //  private Func<IEnumerable<T>> Func;
  //  private CancellationToken? CancellationToken;


  //  public IQueryable<T> Queryable { get; set; }
  //  public bool Converted { get; set; }

  //  public void EnsureQueryableExists() {
  //    if (!Converted) {
  //      Queryable = Func().AsQueryable<T>();
  //      Converted=true;
  //    }
  //  }


  //  private bool WasExecuted;
  //  private IEnumerable<T> Results;

  //  private DelayedQueryable(CancellationToken? cancellationToken, Func<IEnumerable<T>> func) {
  //    Func = func;
  //    CancellationToken = cancellationToken;
  //    Provider = new DelayedQueryProvider<T>(this, func);
  //    ElementType = typeof(T);
  //    Expression =  Expression.Constant(Queryable, typeof(IQueryable<T>));
  //  }

  //  public static DelayedQueryable<T> CreateFrom(CancellationToken? cancellationToken, Func<IEnumerable<T>> func) {
  //    return new DelayedQueryable<T>(cancellationToken, func);
  //  }

  //  public IEnumerator<T> GetEnumerator() {
  //    if (!WasExecuted) {
  //      if (CancellationToken == null || !CancellationToken.Value.IsCancellationRequested) {
  //        //only execute if not cancelled... null handled below.
  //        Results = Func().NotNull(x => x.ToList());
  //      }
  //      WasExecuted = true;
  //    }

  //    if (Results == null) {
  //      return Enumerable.Empty<T>().GetEnumerator();
  //    }

  //    return Results.GetEnumerator();
  //  }

  //  IEnumerator IEnumerable.GetEnumerator() {
  //    throw new NotImplementedException();
  //  }
  //}

  //public class DelayedQueryProvider<T> : IQueryProvider {
  //  public DelayedQueryProvider(DelayedQueryable<T> ts, Func<IEnumerable<T>> func) {
  //    Parent=ts;
  //    Func=func;
  //  }

  //  public Func<IEnumerable<T>> Func { get; }
  //  public DelayedQueryable<T> Parent { get; }

  //  public IQueryable CreateQuery(Expression expression) {
  //    throw new NotImplementedException();
  //  }

  //  public IQueryable<TElement> CreateQuery<TElement>(Expression expression) {
  //    throw new NotImplementedException();
  //  }

  //  public object Execute(Expression expression) {
  //    throw new NotImplementedException();
  //  }

  //  public TResult Execute<TResult>(Expression expression) {
  //    Parent.EnsureQueryableExists();
  //    throw new NotImplementedException();
  //  }
  //}

}

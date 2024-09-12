using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RadialReview.Core.GraphQL.Types
{
  public enum ChangeAction
  {
    Created,
    Updated,
    Deleted,

    UpdatedAssociation,

    Inserted,
    Removed,
    Emptied
  }

  public static class Change
  {
    public static Target<TProperty, TKey> Target<TProperty, TKey>(TKey id, TProperty property) => new Target<TProperty, TKey>() { Id = id, Property = property };
  }

  public abstract class Change<TBearer> : IChange<TBearer>
  {
    public abstract void Log(ILogger<IChange<TBearer>> logger);

    #region Convenience methods to aid C# type inference (that is, reduce the need for explicit types at point of use)

    public static Created<TBearer, T, long> Created<T>(long id, T value) where T : class => new Created<TBearer, T, long>() { Id = id, Value = value };
    public static Updated<TBearer, T, long> Updated<T>(long id, T value, ContainerTarget[]? targets) where T : class => new Updated<TBearer, T, long>(id, value, targets);
    public static Deleted<TBearer, T, long> Deleted<T>(long id) where T : class => new Deleted<TBearer, T, long>() { Id = id };

    public static Created<TBearer, T, Guid> Created<T>(Guid id, T value) where T : class => new Created<TBearer, T, Guid>() { Id = id, Value = value };
    public static Updated<TBearer, T, Guid> Updated<T>(Guid id, T value, ContainerTarget[]? targets) where T : class => new Updated<TBearer, T, Guid>(id, value, targets);
    public static Deleted<TBearer, T, Guid> Deleted<T>(Guid id) where T : class => new Deleted<TBearer, T, Guid>() { Id = id };

    public static UpdatedAssociation<TBearer, TProperty, T, TKey> UpdatedAssociation<TProperty, T, TKey>(Target<TProperty, TKey> target, TKey id, T value) where T : class => new UpdatedAssociation<TBearer, TProperty, T, TKey>() { Target = target, Id = id, Value = value };

    public static Inserted<TBearer, TProperty, T, TKey> Inserted<TProperty, T, TKey>(Target<TProperty, TKey> target, TKey id, T value) where T : class => new Inserted<TBearer, TProperty, T, TKey>() { Target = target, Id = id, Value = value };

    public static Removed<TBearer, TProperty, T, TKey> Removed<TProperty, T, TKey>(Target<TProperty, TKey> target, TKey id, T value) where T : class => new Removed<TBearer, TProperty, T, TKey>() { Target = target, Id = id };
    public static Emptied<TBearer, TProperty, T, TKey> Emptied<TProperty, T, TKey>(Target<TProperty, long> target, TKey id, T value) where T : class => new Emptied<TBearer, TProperty, T, TKey>() { Target = target };

    #endregion

    public abstract ChangeAction Action { get; }
  }
}

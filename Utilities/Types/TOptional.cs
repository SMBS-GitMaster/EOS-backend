using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.Utilities.Types
{
  /// <summary>
  /// A custom implementation of an optional value type similar to the Optional type provided by the Hot Chocolate library.
  /// This class has been created to ensure that the Accessor classes remains independent of the Optional type from the Hot Chocolate package.
  /// By using TOptional instead of Optional, the Accessor class can maintain its functionality without tightly coupling to the Hot Chocolate library.
  /// </summary>
  public readonly struct TOptional<T>
  {
    public T? Value { get; }
    public bool HasValue { get; }

    public TOptional(T? value)
    {
      Value = value;
      HasValue = true;
    }

    public TOptional(T? value, bool hasValue)
    {
      Value = value;
      HasValue = hasValue;
    }

    public override string ToString()
    {
      return HasValue ? Value?.ToString() ?? "null" : "unspecified";
    }

    public static implicit operator TOptional<T>(T value)
    {
      return new TOptional<T>(value);
    }

    public static implicit operator T(TOptional<T> optional)
      => optional.Value;

    public static bool operator ==(TOptional<T> left, TOptional<T> right)
    {
      return left.Equals(right);
    }

    public static bool operator !=(TOptional<T> left, TOptional<T> right)
    {
      return !left.Equals(right);
    }

    public static TOptional<T> Empty(T? defaultValue = default)
    => new(defaultValue, false);
  }
}

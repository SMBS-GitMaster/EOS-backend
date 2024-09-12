using System;
using System.ComponentModel;

namespace RadialReview.Core.Utilities.Types
{
  public class TypeParser
  {

    /// <summary>
    /// Converts the input string to a valid nullable value of the specified type.
    /// If the input is null, returns null. If the input cannot be parsed to the specified
    /// type, throws a NotSupportedException.
    /// </summary>
    /// <typeparam name="T">The target value type.</typeparam>
    /// <param name="input">The input string to be converted.</param>
    /// <returns>A nullable value of the specified type if conversion is successful; otherwise, null.</returns>
    /// <exception cref="NotSupportedException">Thrown when the input cannot be parsed to the specified type.</exception>

    public static T? ConvertToValidNullable<T>(string input) where T : struct
    {
      if (input is null) return null;

      TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
      return (T)converter.ConvertFromString(input);
    }
  }
}

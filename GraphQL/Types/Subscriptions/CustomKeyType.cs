using HotChocolate.Language;
using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Types.Subscriptions
{
  public class CustomKeyType : ScalarType
  {
    public CustomKeyType() : base("CustomKeyType")
    {
    }

    public override Type RuntimeType => typeof(object);

    public override bool IsInstanceOfType(IValueNode valueSyntax)
    {
      if (valueSyntax is IntValueNode || valueSyntax is StringValueNode)
      {
        return true;
      }

      return false;
    }

    public override object ParseLiteral(IValueNode valueSyntax)
    {
      if (valueSyntax is IntValueNode intValueNode)
      {
        return intValueNode.ToInt64();
      }

      if (valueSyntax is StringValueNode stringValueNode)
      {
        if (Guid.TryParse(stringValueNode.Value, out var guid))
        {
          return guid;
        }
      }

      throw new ArgumentException("The value is not a supported type for this custom type.", nameof(valueSyntax));
    }

    public override IValueNode ParseResult(object resultValue)
    {
      throw new NotImplementedException();
    }

    public override IValueNode ParseValue(object runtimeValue)
    {
      switch (runtimeValue)
      {
        case long longValue:
          return new IntValueNode(longValue);
        case Guid guidValue:
          return new StringValueNode(guidValue.ToString());
        default:
          throw new ArgumentException("The runtime value is not a supported type for this custom type.", nameof(runtimeValue));
      }
    }

    public override object Serialize(object runtimeValue)
    {
      return runtimeValue switch
      {
        long longValue => longValue,
        Guid guidValue => guidValue.ToString(),
        _ => throw new ArgumentException("The runtime value is nota supported type for this custom type.", nameof(runtimeValue))
      };
    }

    public override bool TryDeserialize(object resultValue, out object runtimeValue)
    {
      switch (resultValue)
      {
        case long longValue:
          runtimeValue = longValue;
          return true;
        case string stringValue when Guid.TryParse(stringValue, out var guid):
          runtimeValue = guid;
          return true;
        default:
          runtimeValue = null;
          return false;
      }
    }

    public override bool TrySerialize(object runtimeValue, out object resultValue)
    {
      throw new NotImplementedException();
    }
  }
}

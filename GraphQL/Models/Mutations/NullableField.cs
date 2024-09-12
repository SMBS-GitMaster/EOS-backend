using Amazon.EC2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Models.Mutations
{
  public class NullableField<T>
    //where T : class
  {
    public T Value { get; init; }

    public NullableField()
    {
    }

    public NullableField(T value)
    {
      Value = value;   
    }
  }
}

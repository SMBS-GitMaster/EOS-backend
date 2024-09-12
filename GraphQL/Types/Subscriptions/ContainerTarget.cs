using System;

namespace RadialReview.Core.GraphQL.Types
{
  public class ContainerTarget
  {
    public string Type { get; set; }
    private object _id;
    public string Property { get; set; }

    public ContainerTarget() { }

    public ContainerTarget(long id)
    {
      Id = id;
    }

    public ContainerTarget(Guid id)
    {
      Id = id;
    }

    public object Id
    {
      get => _id;
      set
      {
        if (value is long || value is Guid)
        {
          _id = value;
        }
        else if(value is string strValue)
        {
          if(Guid.TryParse(strValue, out Guid guidValue)){
            _id = guidValue;
          }else
          {
            throw new ArgumentException("String value is not a valid GUID.");
          }
        }
        else
        {
          throw new ArgumentException("Id must be of type long or Guid.");
        }
      }
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Core.GraphQL.Common.DTO
{
  public class UIDModel
  {
    public Guid? Id { get; set; } 

    public UIDModel(Guid? Id = null)
    {
      this.Id = Id;
    }
  }

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models
{
  public class OrganizationQueryModel
  {
    public long Id { get; set; }
    public long UserId { get; set; }
    public string OrgName { get; set; }
    public string OrgImage { get; set; }
  }
}

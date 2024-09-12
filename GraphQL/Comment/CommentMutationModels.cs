using HotChocolate.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.GraphQL.Models.Mutations {

  public class CommentCreateModel {

    public string CommentParentType { get; set; }
    public long ParentId { get; set; }
    public string Body { get; set; }
    public double PostedTimestamp { get; set; }
    public long Author { get; set; }
  }

  public class CommentEditModel {

    public long CommentId { get; set; }
    [DefaultValue(null)] public string CommentParentType { get; set; }
    [DefaultValue(null)] public long? ParentId { get; set; }
    [DefaultValue(null)] public string Body { get; set; }
    [DefaultValue(null)] public long? Author { get; set; }
  }

  public class CommentDeleteModel {

    public long CommentId { get; set; }
    public string ParentType { get; set; }
  }

}

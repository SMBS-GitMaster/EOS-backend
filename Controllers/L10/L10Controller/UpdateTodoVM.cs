using System.Collections.Generic;

namespace RadialReview.Core.Controllers
{
  public partial class L10Controller
  {
    public class UpdateTodoVM {
			public List<long> todos { get; set; }

			public string connectionId { get; set; }
		}
	}
}

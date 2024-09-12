using log4net;
using System.Collections.Generic;

namespace RadialReview.Accessors {
	public partial class BaseAccessor {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		protected static Dictionary<string, object> CacheLookup = new Dictionary<string, object>();

		//SEE PARTIAL CLASSES FOR METHODS.

	}
}

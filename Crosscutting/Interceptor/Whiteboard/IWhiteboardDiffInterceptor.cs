using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Interceptor.Whiteboard {
	public interface IWhiteboardDiffInterceptor {
		bool ShouldApplyInterceptor(ISession s, PermissionsUtility perms,long callerId, long orgId, Diff diff);

		Task ApplyBefore(ISession s, PermissionsUtility perms, long callerId, long orgId, Diff diff);
		Task ApplyAfter(ISession s, PermissionsUtility perms, long callerId, long orgId, Diff diff);
	}
}
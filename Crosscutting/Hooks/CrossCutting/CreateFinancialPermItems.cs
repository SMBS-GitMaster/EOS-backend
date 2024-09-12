using RadialReview.Utilities.Hooks;
using RadialReview.Models;
using System.Threading.Tasks;
using NHibernate;
using RadialReview.Accessors;

namespace RadialReview.Crosscutting.Hooks.CrossCutting {
	public class CreateFinancialPermItems : IOrganizationHook {
		public bool CanRunRemotely() {
			return false;
		}
		public bool AbsorbErrors() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
		}

		public async Task CreateOrganization(ISession s, UserOrganizationModel creator, OrganizationModel organization, OrgCreationData data, IOrganizationHookCreate meta) {
			PermissionsAccessor.InitializePermItems_Unsafe(s, creator, PermItem.ResourceType.UpdatePaymentForOrganization, organization.Id, PermTiny.Admins(true, true, true) );
		}

		public async Task UpdateOrganization(ISession s, long organizationId, IOrganizationHookUpdates updates, UserOrganizationModel user) {
			//noop
		}
	}
}

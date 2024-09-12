using NHibernate;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Notifications;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;

namespace RadialReview.Crosscutting.Hooks.UserRegistration {
	public class SendVerification : ICreateUserOrganizationHook {
		public bool AbsorbErrors() {
			return true;
		}

		public bool CanRunRemotely() {
			return false;
		}

		public async Task CreateUserOrganization(ISession s, UserOrganizationModel user, CreateUserOrganizationData data) {
			//noop
		}


		public HookPriority GetHookPriority() {
			return HookPriority.Highest;
		}

		public async Task OnUserOrganizationAttach(ISession s, UserOrganizationModel user, OnUserOrganizationAttachData data) {
			//noop
			if (user.User.EmailNotVerified) {
				await NotificationAccessor.FireNotification_Unsafe(
					NotificationGroupKey.VerifyEmail(user.User.Id),
					user.Id,
					NotificationDevices.Computer,
					"Please verify your email address.",
					"<a href='javascript: void(0)' onclick='VerifyEmail()'>Resend Verification Link</a>",
					canMarkSeen: false
				);
			}
		}

		public async Task OnUserRegister(ISession s, UserModel user, OnUserRegisterData data) {
			await UserAccessor.SendVerificationEmail_Unsafe(s, user.Email, user._TimezoneOffset ?? 0);
		}

	}
}

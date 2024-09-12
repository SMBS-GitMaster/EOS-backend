using NHibernate;
using RadialReview.Models;
using System;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {

	public class OnUserOrganizationAttachData {
		public bool DuringSelfOnboarding { get; set; }		
	}
	public class OnUserRegisterData {
		public bool DuringSelfOnboarding { get; set; }
	}
	public class CreateUserOrganizationData {
		public bool DuringSelfOnboarding { get; set; }
	}

	public interface ICreateUserOrganizationHook : IHook {
		[Obsolete("User might not be attached yet")]
		Task CreateUserOrganization(ISession s, UserOrganizationModel user, CreateUserOrganizationData data);
		Task OnUserOrganizationAttach(ISession s, UserOrganizationModel user, OnUserOrganizationAttachData data);
		Task OnUserRegister(ISession s, UserModel user, OnUserRegisterData data);
	}

	public interface IUpdateUserModelHook : IHook {
		Task UpdateUserModel(ISession s, UserModel user);
	}

	interface IDeleteUserOrganizationHook : IHook {
		Task DeleteUser(ISession s, UserOrganizationModel user, DateTime deleteTime);
		Task UndeleteUser(ISession s, UserOrganizationModel user, DateTime deleteTime);
	}
}



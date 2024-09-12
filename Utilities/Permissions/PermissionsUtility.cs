using log4net;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Areas.People.Accessors;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.EventAnalyzers.Models;
using RadialReview.Crosscutting.Zapier;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Admin;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Documents;
using RadialReview.Models.Enums;
using RadialReview.Models.Integrations;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Prereview;
using RadialReview.Models.Process;
using RadialReview.Models.Rocks;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Survey;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Models.UserTemplate;
using RadialReview.Models.VTO;
using RadialReview.Reflection;
using RadialReview.Utilities.Permissions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace RadialReview.Utilities {
	public partial class PermissionsUtility : IPermissionsUtility {
		protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		protected ISession session;
		protected UserOrganizationModel caller;


    public UserOrganizationModel GetCaller() {
      return caller;
    }

    protected PermissionsUtility(ISession session, UserOrganizationModel caller) {
			this.session = session;
			this.caller = caller;

		}

		public static PermissionsUtility Create(ISession session, UserOrganizationModel caller) {
			var attached = caller;
			if (!session.Contains(caller) && caller.Id != UserOrganizationModel.ADMIN_ID) {
				attached = session.Load<UserOrganizationModel>(caller.Id);
				attached._ClientTimestamp = caller._ClientTimestamp;
				attached._PermissionsOverrides = caller._PermissionsOverrides;
				attached._IsTestAdmin = caller._IsTestAdmin;
			}
			if (caller.DeleteTime != null && caller.DeleteTime < DateTime.UtcNow) {
				throw new PermissionsException("User has been deleted") {
					NoErrorReport = true
				};
			}
			if (caller.Organization != null && caller.Organization.DeleteTime != null && caller.Organization.DeleteTime < DateTime.UtcNow && caller.Organization.DeleteTime != new DateTime(1, 1, 1)) {
				LockoutUtility.ProcessLockout(caller);
			}

			return new PermissionsUtility(session, attached);
		}

  }
}

using NHibernate;
using RadialReview.Areas.People.Models.Survey;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System;

namespace RadialReview.Accessors {
	public class ForModelAccessor {




		[Todo]
		public static TinyUser GetTinyUser_Unsafe(ISession s, IForModel forModel) {
			if (forModel.Is<UserOrganizationModel>()) {
				return s.Get<UserOrganizationModel>(forModel.ModelId).NotNull(x => TinyUser.FromUserOrganization(x));
			} else if (forModel.Is<AccountabilityNode>()) {
				throw new NotImplementedException();
			} else if (forModel.Is<SurveyUserNode>()) {
				return s.Get<SurveyUserNode>(forModel.ModelId).NotNull(x => TinyUser.FromUserOrganization(s.Get<UserOrganizationModel>(x.UserOrganizationId)));
			} else {
				Console.WriteLine("Unhandled type:" + forModel.ModelType);
			}
			return null;
		}

	}
}

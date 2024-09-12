using System;
using NHibernate;
using System.Linq;
using RadialReview.Controllers;
using RadialReview.Exceptions;
using RadialReview.Crosscutting.Hooks;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Onboard;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ISession = NHibernate.ISession;

namespace RadialReview.Accessors
{
	public partial class OnboardingAccessor : BaseAccessor
	{

		static readonly string _cookieName = "Onboarding";

		public static OnboardingUser GetOrCreate(ISession s, Controller ctrl, string cookieOverride, string page = null, bool overrideDisable = false)
		{
			var request = ctrl.Request;
			var response = ctrl.Response;

			var cookie = cookieOverride ?? request.Cookies[_cookieName];
			if (cookie != null && !string.IsNullOrWhiteSpace(cookie))
			{
				var found = s.QueryOver<OnboardingUser>().Where(x => x.Guid == cookie && x.DeleteTime == null).SingleOrDefault();
				if (found != null && found.DeleteTime == null)
				{
					if (found.DisableEdit && !overrideDisable)
						throw new PermissionsException("Organization already exists. Please login and try again.");
					if (found.UserId != null)
					{
						found._UserOrg = s.Get<UserOrganizationModel>(found.UserId);
						found._User = found._UserOrg.User;
					}

					if (page != null)
					{
						found.CurrentPage = page;
						s.Update(found);
					}
					return found;
				}
			}
			var f = Create(s, ctrl);

			return f;
		}
		public static OnboardingUser GetOrCreate(BaseController ctrl, string cookieOverride, string page = null)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var f = GetOrCreate(s, ctrl, cookieOverride, page: page);
					tx.Commit();
					s.Flush();
					return f;
				}
			}
		}

		public static async Task<OnboardingUser> Update(BaseController ctrl, string cookieOverride, Action<OnboardingUser> update, bool overrideDisable = false)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var found = GetOrCreate(s, ctrl, cookieOverride, overrideDisable: overrideDisable);
					update(found);
					s.Update(found);

					await EventUtil.Trigger(x => x.Create(s, EventType.SignupStep, null, found, found.CurrentPage));

					tx.Commit();
					s.Flush();
					return found;
				}
			}
		}

		public static OnboardingUser Create(ISession s, Controller ctrl)
		{
			var request = ctrl.Request;
			var response = ctrl.Response;
			var u = new OnboardingUser()
			{
				Guid = Guid.NewGuid().ToString(),
				StartTime = DateTime.UtcNow,
				CurrentPage = "TheBasics",
				UserAgent = request.Headers[HeaderNames.UserAgent],
				Languages = request.Headers[HeaderNames.AcceptLanguage]
			};

			s.Save(u);
			CreateOnBoardingCookie(response.Cookies, u.Guid);

			return u;
		}


		public static UserModel TryActivateOrganization(IResponseCookies cookies, OnboardingUser o)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					if (o.OrganizationId == null)
						throw new PermissionsException("Could not activate organization. Organization id does not exist.");
					var found = s.GetFresh<OrganizationModel>(o.OrganizationId);
					if (found == null)
						throw new PermissionsException("Could not activate organization. Organization does not exist.");

					if (found.DeleteTime == null)
					{
						return null; // already activated
					}
					else if (found.DeleteTime == new DateTime(1, 1, 1))
					{
						found.DeleteTime = null;
						o.DisableEdit = true;
						o.DeleteTime = DateTime.UtcNow;

						s.Update(o);
						s.Update(found);

						var user = s.Get<UserOrganizationModel>(o.UserId).User;

						tx.Commit();
						s.Flush();

						DeleteOnBoardingCookie(cookies);

						return user;
					}
					else
					{
						throw new PermissionsException("Could not activate organization. Organization was deleted.");
					}
				}
			}

		}

		public static void CreateOnBoardingCookie(IResponseCookies cookies, string userId)
		{
			cookies.Append(_cookieName, userId, new CookieOptions()
			{
				Expires = DateTimeOffset.UtcNow.AddDays(100)
			});
		}

		public static void DeleteOnBoardingCookie(IResponseCookies cookies)
		{
			if (cookies is null)
			{
				throw new ArgumentNullException(nameof(cookies));
			}

			cookies.Delete(_cookieName);
		}

		public static void TryUpdateOrganization(OnboardingUser o)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					try
					{
						if (o.OrganizationId == null)
							return;
						if (o.DisableEdit)
							throw new PermissionsException("Organization already exists. Please login and try again.");

						var org = s.Get<OrganizationModel>(o.OrganizationId);
						if (org == null)
							return;

						var organizationName = o.CompanyName;
						if (!String.IsNullOrWhiteSpace(organizationName) && org.Name != organizationName)
						{
							org.Name = organizationName;
							var managers = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.Managers).List().FirstOrDefault();
							if (managers != null)
							{
								managers.Name = Config.ManagerName() + "s at " + organizationName;
								s.Update(managers);
							}
							var allTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == org.Id && x.Type == TeamType.AllMembers).List().FirstOrDefault();
							if (allTeam != null)
							{
								allTeam.Name = organizationName;
								s.Update(allTeam);
							}
						}

						tx.Commit();
						s.Flush();
					}
					catch (Exception e)
					{
						log.Error("Error updating organization in get stated.", e);
					}
				}
			}
		}


	}
}
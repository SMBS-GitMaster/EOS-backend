using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.Threading;

namespace RadialReview.NHibernate {




	public class NHibernateUserStore : IUserLoginStore<UserModel>, IUserClaimStore<UserModel>,
		IUserRoleStore<UserModel>, IUserPasswordStore<UserModel>, IUserSecurityStampStore<UserModel>,
		IUserEmailStore<UserModel>, IUserStore<UserModel>, IDisposable {


		#region Implemented

		public async Task<IdentityResult> CreateAsync(UserModel user, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					db.Save(user);
					tx.Commit();
					db.Flush();
					return IdentityResult.Success;
				}
			}
		}
		public async Task<IdentityResult> DeleteAsync(UserModel user, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					user = db.Get<UserModel>(user);
					user.DeleteTime = DateTime.UtcNow;
					db.SaveOrUpdate(user);
					tx.Commit();
					db.Flush();
					return IdentityResult.Success;
				}
			}
		}
		public async Task<UserModel> FindByIdAsync(string userId, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					return db.Get<UserModel>(userId);
				}
			}
		}
		public async Task<UserModel> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					return db.QueryOver<UserModel>().Where(x => x.UserName == normalizedUserName).SingleOrDefault();
				}
			}
		}
		public async Task<IdentityResult> UpdateAsync(UserModel user, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					db.SaveOrUpdate(user);
					return IdentityResult.Success;
				}
			}
		}
		public async Task AddToRoleAsync(UserModel user, string roleName, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					user = db.Get<UserModel>(user.Id);
					user.Roles.Add(new UserRoleModel() { Role = roleName });
					db.SaveOrUpdate(user);
					tx.Commit();
					db.Flush();
				}
			}
		}
		public async Task<IList<string>> GetRolesAsync(UserModel user, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					user = db.Get<UserModel>(user.Id);
					return user.Roles.NotNull(y => y.Where(x => !x.Deleted).Select(x => x.Role).ToList());
				}
			}
		}
		public async Task<bool> IsInRoleAsync(UserModel user, string roleName, CancellationToken cancellationToken) {
			return user.Roles.NotNull(y => y.Any(x => x.Role == roleName && x.Deleted == false));
		}
		public async Task RemoveFromRoleAsync(UserModel user, string roleName, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					user = db.Get<UserModel>(user.Id);
					var found = user.Roles.NotNull(y => y.ToList().FirstOrDefault(x => x.Role == roleName));
					if (found != null) {
						found.Deleted = true;
						db.Delete(found);
						user.Roles.Remove(found);
					} else {
						throw new PermissionsException("Role could not be removed because it doesn't exist.");
					}

					tx.Commit();
					db.Flush();
				}
			}
		}
		public async Task<string> GetSecurityStampAsync(UserModel user, CancellationToken cancellationToken) {
			return user.SecurityStamp;
		}
		public async Task SetSecurityStampAsync(UserModel user, string stamp, CancellationToken cancellationToken) {
			SetAndMaybeSave(user, x => x.SecurityStamp = stamp);			
		}
		public async Task<bool> HasPasswordAsync(UserModel user, CancellationToken cancellationToken) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return s.Get<UserModel>(user.Id).PasswordHash != null;
				}
			}
		}
		public async Task<string> GetPasswordHashAsync(UserModel user, CancellationToken cancellationToken) {
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					return db.Get<UserModel>(user.Id).PasswordHash;
				}
			}
		}
		public async Task SetPasswordHashAsync(UserModel user, string passwordHash, CancellationToken cancellationToken) {
			SetAndMaybeSave(user, x=>x.PasswordHash = passwordHash);
		}

	

		public void Dispose() {
		}
		public async Task<IList<Claim>> GetClaimsAsync(UserModel user, CancellationToken cancellationToken) {
			return new List<Claim>() {
				new Claim(ClaimTypes.NameIdentifier,user.Id),
				new Claim(ClaimTypes.Name,user.Name()),
				new Claim(ClaimTypes.Email,user.UserName),
			};
		}

		public async Task<string> GetUserIdAsync(UserModel user, CancellationToken cancellationToken) {
			return user.Id;
		}

		public async Task<string> GetUserNameAsync(UserModel user, CancellationToken cancellationToken) {
			return user.UserName; 
		}

		public async Task<UserModel> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					return s.QueryOver<UserModel>().Where(x => x.UserName == normalizedEmail.ToLower()).Take(1).SingleOrDefault();
				}
			}
		}

		public async Task<string> GetNormalizedEmailAsync(UserModel user, CancellationToken cancellationToken) {
			return user.NormalizedEmail;
		}

		public async Task SetNormalizedEmailAsync(UserModel user, string normalizedEmail, CancellationToken cancellationToken) {
			SetAndMaybeSave(user, x => x.NormalizedEmail = normalizedEmail);
		}

		public async Task<string> GetNormalizedUserNameAsync(UserModel user, CancellationToken cancellationToken) {
			return user.NormalizedUserName;
		}
		public async Task SetNormalizedUserNameAsync(UserModel user, string normalizedName, CancellationToken cancellationToken) {
			SetAndMaybeSave(user, x => x.NormalizedUserName = normalizedName);
		}
		#endregion

		#region helpers
		private static void SetAndMaybeSave(UserModel user, Action<UserModel> setField) {
			setField(user);
			using (var db = HibernateSession.GetCurrentSession()) {
				using (var tx = db.BeginTransaction()) {
					var foundUser = db.Get<UserModel>(user.Id);
					if (foundUser != null) {
						setField(foundUser);
						db.Update(foundUser);
						tx.Commit();
						db.Flush();
					}
				}
			}
		}

	public async Task<IList<UserLoginInfo>> GetLoginsAsync(UserModel user, CancellationToken cancellationToken = default(CancellationToken)) {
      using (var db = HibernateSession.GetCurrentSession())
      {
        using (var tx = db.BeginTransaction())
        {
          return (IList<UserLoginInfo>)db.QueryOver<UserLogin>().Where(x => x.UserId == user.Id && user.DeleteTime == null).Select(x => new UserLoginInfo(x.LoginProvider, x.ProviderKey, x.ProviderDisplayName));
        }
      }
    }

	public async Task RemoveLoginAsync(UserModel user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken)) {
      cancellationToken.ThrowIfCancellationRequested();

      if(user == null)
        throw new ArgumentNullException(nameof(user));


      var entry = await FindUserLoginAsync(user.Id, loginProvider, providerKey, cancellationToken);
      if (entry != null)
      {
        using (var db = HibernateSession.GetCurrentSession())
        {
          using (var tx = db.BeginTransaction())
          {
            entry.DeleteTime = DateTime.Now;
            db.SaveOrUpdate(entry);
            tx.Commit();
            db.Flush();
          }
        }
      }
    }
	public async Task AddLoginAsync(UserModel user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken)) {
      using (var db = HibernateSession.GetCurrentSession())
      {
        cancellationToken.ThrowIfCancellationRequested();

        if(user == null)
          throw new ArgumentNullException(nameof(user));

        if (login == null)
          throw new ArgumentNullException(nameof(login));

        using (var tx = db.BeginTransaction())
        {
          var dbuser = db.Get<UserModel>(user.Id);
          dbuser.Logins.Add(new UserLogin { LoginProvider = login.LoginProvider, ProviderKey = login.ProviderKey, ProviderDisplayName = login.ProviderDisplayName });
          db.SaveOrUpdate(user);
          tx.Commit();
          db.Flush();
          return;
        }
      }
    }


	public async Task<UserModel> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken)) {

        cancellationToken.ThrowIfCancellationRequested();

        var userLogin = await FindUserLoginAsync(loginProvider, providerKey, cancellationToken);
        if (userLogin != null)
        {
          return await FindByIdAsync(userLogin.UserId, cancellationToken);
        }
        return null;

    }

    protected async Task<UserLogin> FindUserLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
    {
      using (var db = HibernateSession.GetCurrentSession())
      {
        using (var tx = db.BeginTransaction())
        {
          return db.QueryOver<UserLogin>().Where(x => x.LoginProvider == loginProvider && x.ProviderKey == providerKey && x.DeleteTime == null).SingleOrDefault();
        }
      }
    }

    protected async Task<UserLogin> FindUserLoginAsync(string userId, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
    {
      using (var db = HibernateSession.GetCurrentSession())
      {
        using (var tx = db.BeginTransaction())
        {
          return db.QueryOver<UserLogin>().Where(x => x.UserId == userId && x.LoginProvider == loginProvider && x.ProviderKey == providerKey && x.DeleteTime == null).SingleOrDefault();
        }
      }
    }

    #endregion

    #region not implemented

    public async Task AddClaimsAsync(UserModel user, IEnumerable<Claim> claims, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public async Task<IList<UserModel>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public async Task<IList<UserModel>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public async Task RemoveClaimsAsync(UserModel user, IEnumerable<Claim> claims, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public async Task ReplaceClaimAsync(UserModel user, Claim claim, Claim newClaim, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public async Task SetUserNameAsync(UserModel user, string userName, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		Task IUserEmailStore<UserModel>.SetEmailAsync(UserModel user, string email, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		public async Task<string> GetEmailAsync(UserModel user, CancellationToken cancellationToken) {
			return user.UserName;
		}

		Task<bool> IUserEmailStore<UserModel>.GetEmailConfirmedAsync(UserModel user, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		Task IUserEmailStore<UserModel>.SetEmailConfirmedAsync(UserModel user, bool confirmed, CancellationToken cancellationToken) {
			throw new NotImplementedException();
		}

		#endregion
	}
}
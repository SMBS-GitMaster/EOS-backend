using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace RadialReview.Api.Authentication
{
  public class TokenManager
  {
		private static string AUTHENTICATION_FAILED_MESSAGE = "Authentication failed";

		private UserManager<UserModel> _userManager;

		public TokenManager(UserManager<UserModel> userManager) {
			_userManager = userManager;
		}

    public async Task<object> GetToken(string username, string password)
    {
      if (string.IsNullOrWhiteSpace(username))
        throw new ApiException("Invalid request. The username was missing.");
      if (string.IsNullOrWhiteSpace(password))
        throw new ApiException("Invalid request. The password was missing.");

      username = username.ToLower().Trim();

      var unverifiedUser = await _userManager.FindByNameAsync(username);
      if (unverifiedUser == null)
        throw new ApiException(AUTHENTICATION_FAILED_MESSAGE);
      if (!await _userManager.CheckPasswordAsync(unverifiedUser, password))
        throw new ApiException(AUTHENTICATION_FAILED_MESSAGE);
      if (unverifiedUser.DeleteTime != null)
        throw new ApiException("User no longer exists");

      var verifedUser = unverifiedUser;
      var claims = (await _userManager.GetClaimsAsync(verifedUser)).ToList();
      claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
      claims.Add(new Claim("jwt", "true"));

      try
      {
        var jwt = Config.GetJwtSecretSettings();
        var credentials = jwt.GetSigningCredentials();
        var issued = DateTime.UtcNow;
        var token = new JwtSecurityToken(jwt.Issuer, jwt.Audience, claims, expires: issued.AddMinutes(jwt.TimeoutMinutes), signingCredentials: credentials);

        var output = new Dictionary<string, object>(){
            { "access_token", new JwtSecurityTokenHandler().WriteToken(token) },
            { "token_type","bearer" },
            { "expires_in", (int)(token.ValidTo - DateTime.UtcNow).TotalSeconds },
            { "userName", username.ToLower() },
            { ".issued:", FormatDate(issued) },
            { ".expires",FormatDate(token.ValidTo)},
          };
        AppendVerifiedInfo(verifedUser, output);
        return output;

      }
      catch (Exception e)
      {
        throw new ApiException(AUTHENTICATION_FAILED_MESSAGE, e);
      }
    }

    public async Task<TokenResult> GetToken(string username, string password, CancellationToken cancellationToken)
    {
      var obj  = await GetToken(username, password);
      var dict = (Dictionary<string, object>) obj;
      var result =
          new TokenResult(){
            Id = Guid.NewGuid().ToString(),
            Token = (string) dict["access_token"],
            ValidTo = DateTime.Parse((string) dict[".expires"])
          };

      return result;
    }

		private static void AppendVerifiedInfo(UserModel verifedUserModel, Dictionary<string, object> output) {
			try {
				if (verifedUserModel.CurrentRole > 0) {
					var unsafeUser = UserAccessor.Unsafe.GetUserOrganizationById(verifedUserModel.CurrentRole);
					if (unsafeUser.DeleteTime == null && unsafeUser.Organization.DeleteTime == null) {
						output["user_id"] = verifedUserModel.CurrentRole;
						output["organization_id"] = unsafeUser.Organization.Id;
					}
				}
			} catch (Exception) {
			}
		}
    private string FormatDate(DateTime date) {
      //"Mon, 28 Dec 2015 07:05:37 GMT"
      //"Mon, 11 Jan 2016 07:05:37 GMT"
      return date.ToString("ddd, dd MMM yyyy HH:mm:ss") + " GMT";
    }
  }
}
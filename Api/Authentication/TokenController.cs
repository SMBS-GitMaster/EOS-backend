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
using System.Threading.Tasks;

namespace RadialReview.Api.Authentication {

	[ApiController]
	public class TokenController : ControllerBase, IApiController {
		private static string TOKEN_INVALID = "Token invalid";

		private UserManager<UserModel> _userManager;
		private TokenManager _tokenManager;

		public TokenController(UserManager<UserModel> userManager, TokenManager tokenManager) {
			_userManager = userManager;
			_tokenManager = tokenManager;
		}

		[Route("/token")]
		[HttpGet]
		public object Get() {
			throw new ApiException("You must use a POST request to generate a token.");
		}

		[Route("/token/invalid")]
		[HttpGet]
		[HttpPost]
		public async Task Invalid() {
			throw new ApiException(TOKEN_INVALID);
		}

		public class TokenData {
			public string grant_type { get; set; }
			public string userName { get; set; }
			public string password { get; set; }
		}

		[Route("/token")]
		[HttpPost]
		public async Task<object> Post() {
			//try both Json and Forms
			var data = JsonConvert.DeserializeObject<TokenData>(await Request.Body.ReadToEndAsync()) ?? new TokenData();

			if (Request.HasFormContentType) {
				data.grant_type = data.grant_type ?? Request.Form["grant_type"];
				data.userName = data.userName ?? Request.Form["userName"];
				data.password = data.password ?? Request.Form["password"];
			}

			if (data == null)
				throw new ApiException("Invalid request. Request was empty.");
			if (data.grant_type != "password")
				throw new ApiException("Invalid request. The grant_type must be 'password'.");
			if (string.IsNullOrWhiteSpace(data.userName))
				throw new ApiException("Invalid request. The userName is required.");
			if (string.IsNullOrWhiteSpace(data.password))
				throw new ApiException("Invalid request. The password is required.");

			return await _tokenManager.GetToken(data.userName, data.password);
		}
	}
}

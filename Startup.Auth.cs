using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RadialReview.Exceptions;
using RadialReview.Middleware.Utilities;
using RadialReview.Models;
using RadialReview.NHibernate;
using RadialReview.Utilities;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace RadialReview {
	public static class StartupAuth {

		public static readonly TimeSpan COOKIE_DURATION = TimeSpan.FromDays(8.75);

    public static void ConfigureAuth(this IServiceCollection services)
    {
      var identityAuth = services.AddAuthentication(options =>
      {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
      })

      .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
      {
        options.ClientId = Config.GetAppSetting("GoogleClientID");
        options.ClientSecret = Config.GetAppSetting("GoogleClientSecret");

      });
      identityAuth = identityAuth.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, options =>
      {
        options.ClientId = Config.GetAppSetting("MicrosoftClientID");
        options.ClientSecret = Config.GetAppSetting("MicrosoftClientSecret");

      });
      identityAuth = identityAuth.AddCookie(IdentityConstants.ExternalScheme, IdentityConstants.ExternalScheme, o =>
       {
         o.Cookie.Name = IdentityConstants.ExternalScheme;
         o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
       });

      var cookieDomains = Config.GetCookieDomains();
      if (Config.GetCookieDomains().Any())
      {
        foreach (var c in cookieDomains)
        {
          identityAuth = identityAuth.AddCookie(IdentityConstants.ApplicationScheme, x =>
          {
            x.LoginPath = new PathString("/Account/Login");
            x.Cookie.Name = "AspSub" + c;
            x.Cookie.Domain = c;
            x.ExpireTimeSpan = COOKIE_DURATION;
            x.SlidingExpiration = true;
            x.Events = new CookieAuthenticationEvents
            {
              OnValidatePrincipal = async context =>
              {
                var currentUtc = DateTimeOffset.UtcNow;
                var issuedUtc = context.Properties.IssuedUtc;

                if (issuedUtc != null)
                {
                  var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                  var timeRemaining = context.Properties.ExpiresUtc.Value.Subtract(currentUtc);

                  if (timeRemaining < TimeSpan.FromDays(5))
                  {
                    context.ShouldRenew = true;
                  }
                }
                await Task.CompletedTask;
              }
            };
          });
        }
      }
      else
      {
        identityAuth = identityAuth.AddCookie(IdentityConstants.ApplicationScheme, x =>
        {
          x.LoginPath = new PathString("/Account/Login");
          x.ExpireTimeSpan = COOKIE_DURATION;
          x.SlidingExpiration = true;
          x.Events = new CookieAuthenticationEvents
          {
            OnValidatePrincipal = async context =>
            {
                var currentUtc = DateTimeOffset.UtcNow;
                var issuedUtc = context.Properties.IssuedUtc;

                if (issuedUtc != null)
                {
                  var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                  var timeRemaining = context.Properties.ExpiresUtc.Value.Subtract(currentUtc);

                  if (timeRemaining < TimeSpan.FromDays(5))
                  {
                    context.ShouldRenew = true; 
                  }
                }
              await Task.CompletedTask;
            }
          };
        });
      }

      identityAuth = identityAuth.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, x =>
      {
        var jwt = Config.GetJwtSecretSettings();
        //Allow http only on local.
        x.RequireHttpsMetadata = !Config.IsLocal();
        x.SaveToken = true;
      
        x.Events = new JwtBearerEvents()
        {
          OnAuthenticationFailed = ProcessFail,
          OnMessageReceived = context =>
          {
            string authorization = context.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Authorization];
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
              context.Token = authorization.Substring("Bearer ".Length).Trim();

            var jwtHandler = new JwtSecurityTokenHandler();
            if (jwtHandler.CanReadToken(context.Token))
            {
              var issuer = jwtHandler.ReadJwtToken(context.Token).Issuer;
              if (issuer != jwt.Issuer)
                context.NoResult();
            }

            return Task.CompletedTask;

          }
        };

        x.TokenValidationParameters = new TokenValidationParameters
        {
          IssuerSigningKey = jwt.GetSecurityKey(),
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidIssuer = jwt.Issuer,
          ValidAudience = jwt.Audience,

        };
      });
      identityAuth = identityAuth.AddCookie(IdentityConstants.TwoFactorRememberMeScheme, o =>
       {
         o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
         o.Events = new CookieAuthenticationEvents
         {
           OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>
         };
       })
        .AddCookie(IdentityConstants.TwoFactorUserIdScheme, o =>
        {
          o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
          o.Events = new CookieAuthenticationEvents
          {
            OnRedirectToReturnUrl = _ => Task.CompletedTask
          };
          o.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
        {
          o.Cookie.Name = CookieAuthenticationDefaults.AuthenticationScheme;
        });

      services.AddHttpContextAccessor();
      //Normalize email and names by converting to lowercase and trimming.
      services.AddScoped<ILookupNormalizer, LowercaseInvariantLookupNormalizer>();
      services.AddScoped<IPasswordHasher<UserModel>, PasswordHasher<UserModel>>();
      services.Configure<PasswordHasherOptions>(x => x.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);
      services.AddScoped<IUserValidator<UserModel>, UserValidator<UserModel>>();
      services.AddScoped<IPasswordValidator<UserModel>, PasswordValidator<UserModel>>();
      services.AddScoped<IUserConfirmation<UserModel>, DefaultUserConfirmation<UserModel>>();
      services.AddScoped<IdentityErrorDescriber>();
      services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<UserModel>>();
      services.AddScoped<IUserClaimsPrincipalFactory<UserModel>, UserClaimsPrincipalFactory<UserModel>>();
      services.AddScoped<UserManager<UserModel>>();
      services.AddScoped<SignInManager<UserModel>>();
      services.AddTransient<IUserStore<UserModel>, NHibernateUserStore>();

      services.AddAuthorization(options =>
      {
      var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(
          GoogleDefaults.AuthenticationScheme,
          MicrosoftAccountDefaults.AuthenticationScheme,
          IdentityConstants.ApplicationScheme,
          IdentityConstants.ExternalScheme,
          JwtBearerDefaults.AuthenticationScheme);

        defaultAuthorizationPolicyBuilder =
            defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
        options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
      });
    }

		public static async Task ProcessFail(AuthenticationFailedContext x) {
			if (x.Request.Path.StartsWithSegments("/api")) {
				await ApiException.WriteJsonErrorToResponse(new ApiException("Token invalid"), x.HttpContext.Response);
			}
		}


    public static void ConfigureAuth(this IApplicationBuilder app) {

			app.UseAuthentication();
			app.UseAuthorization();
		}
	}
}

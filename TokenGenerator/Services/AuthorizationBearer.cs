using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    public class AuthorizationBearer : IAuthorizationBearer
    {
        private readonly Settings settings;
        private readonly object cmLockMaskinporten = new object();
        private ConfigurationManager<OpenIdConnectConfiguration> configurationManager;

        public ConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager
        {
            get
            {
                if (configurationManager != null) return configurationManager;
                lock (cmLockMaskinporten)
                {
                    configurationManager ??= new ConfigurationManager<OpenIdConnectConfiguration>(
                        settings.TokenAuthorizationWellKnownEndpoint,
                        new OpenIdConnectConfigurationRetriever(),
                        new HttpClient {Timeout = TimeSpan.FromMilliseconds(10000)});
                }

                return configurationManager;
            }

            set
            {
                lock (cmLockMaskinporten)
                {
                    configurationManager = value;
                }
            }
        }

        public AuthorizationBearer(IOptions<Settings> settings)
        {
            this.settings = settings.Value;
        }

        public async Task<ActionResult> IsAuthorized(string authorizationString)
        {
            try
            {
                IdentityModelEventSource.ShowPII = true;
                JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
                JwtSecurityToken jwtToken = (JwtSecurityToken)tokenHandler.ReadToken(authorizationString);

                if (jwtToken.SignatureAlgorithm != SecurityAlgorithms.RsaSha256)
                {
                    return new BadRequestObjectResult("Expected RsaSha256 signature");
                }

                OpenIdConnectConfiguration configuration = await ConfigurationManager.GetConfigurationAsync();

                TokenValidationParameters parameters = new TokenValidationParameters()
                {
                    RequireExpirationTime = true,
                    ValidIssuer = configuration.Issuer,
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    IssuerSigningKeys = configuration.SigningKeys,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(1)
                };

                ClaimsPrincipal principal = tokenHandler.ValidateToken(authorizationString, parameters, out SecurityToken _);

                Claim claim = principal.Claims.FirstOrDefault(x => x.Type == "scope");
                if (claim == null || claim.Value.Split(' ').FirstOrDefault(x => x.Equals(settings.AuthorizedScope)) == null) 
                {
                    return new UnauthorizedObjectResult("Missing required scope: " + settings.AuthorizedScope) { StatusCode = 403 };
                }

                return null;
            }
            catch (Exception e)
            {
                return new UnauthorizedObjectResult(e.Message) { StatusCode = 403 };
            }
        }
    }
}

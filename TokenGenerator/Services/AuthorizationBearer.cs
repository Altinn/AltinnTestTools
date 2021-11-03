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
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using TokenGenerator.Services.Interfaces;

namespace TokenGenerator.Services
{
    public class AuthorizationBearer : IAuthorizationBearer
    {
        private readonly Settings settings;
        private readonly object cmLockMaskinporten = new object();
        private ConfigurationManager<OpenIdConnectConfiguration> configurationManager;
        private readonly HttpContext httpContext;

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

        public AuthorizationBearer(IOptions<Settings> settings, IHttpContextAccessor contextAccessor)
        {
            this.settings = settings.Value;
            this.httpContext = contextAccessor.HttpContext;
        }

        public async Task<ActionResult> IsAuthorized(string authorizationString, string requiredScope)
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

                Claim scopeClaim = principal.Claims.FirstOrDefault(x => x.Type == "scope");
                if (scopeClaim == null) 
                {
                    return new UnauthorizedObjectResult("Missing required scope: " + requiredScope) { StatusCode = 403 };
                }

                string[] scopes = scopeClaim.Value.Split(' ');
                
                // Do a substring match, eg. having "altinn:testtools/tokengenerator" should satisfy a requirement for "altinn:testtools/tokengenerator/personal"
                if (scopes.Any(requiredScope.Contains))
                {
                    Claim consumerClaim = principal.Claims.FirstOrDefault(x => x.Type == "consumer");

                    httpContext.Items["AuthenticatedParty"] = consumerClaim == null ? "unknown" : GetOrganizationNumberFromClaimValue(consumerClaim.Value);
                    return null;
                }
                    
                return new UnauthorizedObjectResult("Missing required scope: " + requiredScope) { StatusCode = 403 };

            }
            catch (Exception e)
            {
                return new UnauthorizedObjectResult(e.Message) { StatusCode = 403 };
            }
        }

        private static string GetOrganizationNumberFromClaimValue(string rawClaimValue)
        {
            ConsumerClaim consumerClaim;
            try
            {
                consumerClaim = JsonConvert.DeserializeObject<ConsumerClaim>(rawClaimValue);
            }
            catch (JsonReaderException)
            {
                throw new ArgumentException("Invalid consumer claim: invalid JSON");
            }

            if (consumerClaim.Authority != "iso6523-actorid-upis")
            {
                throw new ArgumentException("Invalid consumer claim: unexpected authority");
            }

            string[] identityParts = consumerClaim.Id.Split(':');
            if (identityParts[0] != "0192")
            {
                throw new ArgumentException("Invalid consumer claim: unexpected ISO6523 identifier");
            }

            return identityParts[1];
        }
    }

    internal sealed class ConsumerClaim
    {
        /// <summary>
        /// Gets or sets the format of the identifier. Must always be "iso6523-actorid-upis"
        /// </summary>
        [DataMember(Name = "authority")]
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the consumer. Must have ISO6523 prefix, which should be "0192:" for norwegian organization numbers
        /// </summary>
        [DataMember(Name = "ID")]
        public string Id { get; set; }
    }
}
